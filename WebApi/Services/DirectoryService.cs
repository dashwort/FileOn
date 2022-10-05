using AutoMapper;
using System.Security.Cryptography.X509Certificates;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models.FFolder;
using WebApi.Services.Interface;

namespace WebApi.Services
{
    public class DirectoryService : IDirectoryService
    {

        DataContext _context;
        readonly IMapper _mapper;
        IConfiguration _configuration;
        IFileService _fileService;

        FFolderInfoEqualityComparer _folderInfoEqualityComparer;
        FFileInfoEqualityComparer _fileInfoEqualityComparer;

        public DirectoryService(DataContext context, IMapper mapper, IConfiguration configuration, IFileService fileService)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _fileService = fileService;

            _folderInfoEqualityComparer = new FFolderInfoEqualityComparer();
            _fileInfoEqualityComparer = new FFileInfoEqualityComparer();
        }

        #region FFolders
        public IEnumerable<FFolder> GetAll()
        {
            return _context.FFolders;
        }

        public FFolder GetById(int id)
        {
            return getFFolder(id);
        }

        public void Create(CreateRequest model)
        {
            //
        }

        public void Create(string folderPath)
        {
            Create(new DirectoryInfo(folderPath));
        }

        public void Create(DirectoryInfo folder)
        {
            var ffolder = new FFolder(folder);

            var existingFolder = _context.FFolders.
                Where(x => x.Path.ToLower() == folder.FullName.ToLower())
                .FirstOrDefault();

            if (existingFolder == null)
            {
                _context.FFolders.Add(ffolder);
                _context.SaveChanges(true);

                var copyJobs = new List<CopyJob>();

                foreach (var f in ffolder.FFiles)
                {
                    var job = CreateCopyJob(f);
                    copyJobs.Add(job);
                }

                _context.CopyJobs.AddRange(copyJobs);
                _context.SaveChanges(true);
            }

        }

        public CopyJob CreateCopyJob(FFile file)
        {
            var job = new CopyJob();

            job.PathToFile = file.FullPath;
            job.ArchivePath = file.ArchivePath;
            job.IdToUpdate = file.Id;
            job.Retries = 0;

            return job;
        }

        public void Delete(int id)
        {
            var ffile = getFFolder(id);
            _context.FFolders.Remove(ffile);
            _context.SaveChanges();
        }

        // helper methods

        private FFolder getFFolder(int id)
        {
            var file = _context.FFolders.Find(id);
            if (file == null) throw new KeyNotFoundException("ffile not found");
            return file;
        }
        #endregion

        #region FolderToMonitor

        //public void Create(CreateRequest model)
        //{
        //    //
        //}

        //public void Create(string folderPath)
        //{
        //    Create(new DirectoryInfo(folderPath));
        //}

        public void CreateFolderToMonitor(string f)
        {
            var folderToMonitor = new FolderToMonitor(f);

            // Adds top level folder, so the files aren't missed
            folderToMonitor.FFolders.Add(new FFolder(f));

            var existingFolder = _context.FoldersToMonitor.
                Where(x => x.FullPath.ToLower() == folderToMonitor.FullPath.ToLower())
                .FirstOrDefault();

            if (existingFolder == null)
            {
                _context.FoldersToMonitor.Add(folderToMonitor);
                _context.SaveChanges(true);

                var copyJobs = new List<CopyJob>();

                foreach (var ffolder in folderToMonitor.FFolders)
                {
                    foreach (var ffile in ffolder.FFiles)
                    {
                        var job = CreateCopyJob(ffile);
                        copyJobs.Add(job);
                    }
                }

                _context.CopyJobs.AddRange(copyJobs);
                _context.SaveChanges(true);
            }
        }

        public void ScanForFFolderChanges(DirectoryInfo fo)
        {
            var folder = _context.FFolders
                .Where(x => x.Path.ToLower() == fo.FullName.ToLower())
                .FirstOrDefault();

            if (folder == null)
            {
                Console.WriteLine($"Folder: {fo.FullName} doesnt exist in database, passing to create folder method.");
                Create(fo);
                return;
            }

            ScanForFFolderChanges(folder);
        }

        public void ScanForFFolderChanges(FFolder fo)
        {
            var folder = _context.FFolders.Where(x => x.Path == fo.Path).FirstOrDefault();

            if (folder is null)
                Create(fo.Path);

            var lastKnownFiles = _context.FFiles.Where(x => x.FFolder.Id == folder.Id).ToArray();

            var lastKnownFilesDictionary = new Dictionary<string, FFile>(); 

            foreach (var file in lastKnownFiles)
                lastKnownFilesDictionary.Add(file.FullPath, file);

            if (lastKnownFiles.Length != lastKnownFilesDictionary.Count)
                Console.WriteLine($"Warning! line 203 is DirectoryService is not equal");

            var currentFiles = fo.FFiles.ToArray();

            var listOfDifferences = new List<FFile>();

            foreach (var file in currentFiles)
            {
                var lkFile = lastKnownFilesDictionary.GetValueOrDefault(file.FullPath);

                if (lkFile != null)
                {
                    bool hashMatch = file.Hash == lkFile.Hash;
                    bool pathMatch = file.FullPath == lkFile.FullPath;

                    if (hashMatch && pathMatch)
                        continue;

                    listOfDifferences.Add(file);
                } else
                {
                    Console.WriteLine($"file doesnt exist: {file.FullPath}");
                    listOfDifferences.Add(file);
                }
            }

            if (listOfDifferences.Count > 0)
            {
                Console.WriteLine($"{listOfDifferences.Count} changes found in folder {fo.Path}");
                foreach (var f in listOfDifferences)
                {
                    _fileService.Create(new Models.FFiles.CreateRequest(f.FullPath));
                }
            }
        }

        public void ScanMonitoredFolder(string folder)
        {
            var f = _context.FoldersToMonitor
                .Where(x => x.FullPath.ToLower() == folder.ToLower())
                .FirstOrDefault();

            if (f == null)
            {
                Console.WriteLine($"FolderToMonitor: {folder} doesnt exist in database, passing to create folder method.");
                CreateFolderToMonitor(folder);
                return;
            }

            ScanMonitoredFolder(f);
        }

        public void ScanMonitoredFolder(FolderToMonitor folder)
        {
            Console.WriteLine($"Scanning monitored folder {folder.FullPath}");

            var dir = new DirectoryInfo(folder.FullPath);

            var scannedFolders = dir.GetDirectories("*", SearchOption.AllDirectories);

            var scannedFolderObjects = new List<FFolder>();

            foreach (var f in scannedFolders)
                scannedFolderObjects.Add(new FFolder(f));

            //// TODO implement an option to delete empty folders
            //var ffoldersInDB = _context.FFolders.Where(x => x.FolderToMonitor.Id == folder.Id).ToArray();

            ////// compare folders in DB with folders to DirectoryToMonitor and return the differences
            //var differences = scannedFolderObjects.Except(ffoldersInDB, _folderInfoEqualityComparer).ToArray();

            //if (ffoldersInDB.Length > 0)
            //    Console.WriteLine($"{differences.Length} differences detected in monitored folder: {folder.FullPath}");

            foreach (var f in scannedFolderObjects)
            {
                ScanForFFolderChanges(f);
            }
        }

        #endregion
    }
}
