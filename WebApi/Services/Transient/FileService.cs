using AutoMapper;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models.FFiles;
using WebApi.Services.HostedServices;
using WebApi.Services.Interface;

namespace WebApi.Services.Transient
{
    public class FileService : IFileService
    {
        #region properties_constructor
        DataContext _context;
        readonly IMapper _mapper;
        IConfiguration _configuration;
        static IConfiguration _staticConfiguration;

        public static DirectoryInfo FileArchiveLocation { get; set; }

        static ConcurrentDictionary<string, bool> WorkItems = new ConcurrentDictionary<string, bool>();

        public FileService(DataContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _staticConfiguration = _configuration;

            var folder = _configuration.GetSection("FileService:FileArchive").Value;

            FileArchiveLocation = new DirectoryInfo(folder);


            if (!FileArchiveLocation.Exists)
                FileArchiveLocation.Create();
        }
        #endregion

        #region api_methods
        public void Create(FFileCreateRequest model)
        {
            try
            {
                var modelFile = new FileInfo(model.FullPath);

                // we carry out two checks as these events can file at any moment and the file monitor can trigger multiple events in quick succession. 
                // Dictionary in memory is used as a rapid WIP cache to prevent duplication
                // The search against the copy jobs is to ensure that we're not duplicating efforts 
                bool insertSuccess = WorkItems.TryAdd(modelFile.FullName, false);

                if (!insertSuccess)
                    return;

                if (!modelFile.Exists)
                    return;


                model.Hash = FileTransferService.CalculateMD5(model.FullPath);
                model.ParentFolder = modelFile.DirectoryName;
                model.CreationTime = modelFile.CreationTimeUtc;
                model.LastModified = modelFile.LastWriteTimeUtc;
                model.Size = modelFile.Length;
                model.Extension = modelFile.Extension;
                model.Iszip = false;
                model.ArchivePath = GetCopyPath(model.FullPath);


                var parentFolder = _context.FFolders.
                    Where(x => x.Path.ToLower() == model.ParentFolder.ToLower())
                    .FirstOrDefault();

                model.FFolder = parentFolder;

                if (CheckIfDuplicate(model))
                    return;

                // map model to new ffile object
                var file = _mapper.Map<FFile>(model);

                // save FFile
                _context.FFiles.Add(file);
                _context.SaveChanges();
                WorkItems[modelFile.FullName] = true;

                var job = FileTransferService.CreateCopyJob(file);
                _context.CopyJobs.Add(job);
                _context.SaveChanges(true);

                // TODO add check to clean up dictionary if required
                WorkItems.TryRemove(model.FullPath, out bool success);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during copy process {ex.Message}");
                WorkItems.TryRemove(model.FullPath, out bool success);
            }

        }

        public IEnumerable<FFile> GetAll()
        {
            return _context.FFiles;
        }

        public FFile GetById(int id)
        {
            return getFFile(id);
        }

        private FFile getFFile(int id)
        {
            var file = _context.FFiles.Find(id);
            if (file == null) throw new KeyNotFoundException("ffile not found");
            return file;
        }

        public void Delete(int id)
        {
            var ffile = getFFile(id);
            _context.FFiles.Remove(ffile);
            _context.SaveChanges();
        }
        #endregion

        #region Helper_Methods
        bool CheckIfDuplicate(FFile fi)
        {
            if (_context.FFiles.Any(x => x.Hash == fi.Hash) &&
                   _context.FFiles.Any(x => x.FullPath == fi.FullPath))
            {

                Console.WriteLine($"File: {fi.Name} already exists, skipping that mofo");
                return true;
            }

            if (_context.CopyJobs.Any(x => x.PathToFile == fi.FullPath))
            {
                Console.WriteLine($"Duplicate copy job detected");
                return true;
            }

            return false;
        }

        bool CheckIfDuplicate(FFileCreateRequest model)
        {

            var hashMatch = _context.FFiles.Any(x => x.Hash == model.Hash);
            var pathMatch = _context.FFiles.Any(x => x.FullPath == model.FullPath);

            if (hashMatch && pathMatch)
            {
                Console.WriteLine($"File: {model.FullPath} already exists, skipping that mofo");
                return true;
            }

            if (_context.CopyJobs.Any(x => x.PathToFile == model.FullPath))
            {
                Console.WriteLine($"Duplicate copy job detected");
                return true;
            }

            return false;
        }


        #endregion

        public void CreateFFile(FileInfo f)
        {
            try
            {
                // we carry out two checks as these events can file at any moment and the file monitor can trigger multiple events in quick succession. 
                // Dictionary in memory is used as a rapid WIP cache to prevent duplication
                // The search against the copy jobs is to ensure that we're not duplicating efforts 
                bool insertSuccess = WorkItems.TryAdd(f.FullName, false);

                if (!insertSuccess)
                    return;

                if (!f.Exists)
                    return;


                FFile ffile = new FFile(f);

                if (string.IsNullOrEmpty(ffile.Hash))
                    return;

                var parentFolder = _context.FFolders.
                    Where(x => x.Path.ToLower() == ffile.ParentFolder.ToLower())
                    .FirstOrDefault();

                ffile.FFolder = parentFolder;

                if (CheckIfDuplicate(ffile))
                    return;

                // save FFile
                _context.FFiles.Add(ffile);
                _context.SaveChanges();
                WorkItems[ffile.FullPath] = true;

                var job = FileTransferService.CreateCopyJob(ffile);
                _context.CopyJobs.Add(job);
                _context.SaveChanges(true);

                // TODO add check to clean up dictionary if required
                WorkItems.TryRemove(ffile.FullPath, out bool success);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during copy process {ex.Message}");
                WorkItems.TryRemove(f.FullName, out bool success);
            }

        }

        public void ScanForChanges(FileInfo f)
        {
            // TODO reimplement
            var ffile = new FFile(f);


            if (CheckIfDuplicate(ffile))
            {
                Console.WriteLine($"No changes detected in FFile {ffile.FullPath}");
            }
            else
            {
                Console.WriteLine($"FFile has changed: {ffile.FullPath}");
                var job = FileTransferService.CreateCopyJob(ffile);

                _context.CopyJobs.Add(job);
                _context.SaveChanges();
            }
        }

        // helper methods

        #region static_methods
        public static DirectoryInfo GetArchivePath()
        {
            string base64Guid = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            base64Guid = new string((from c in base64Guid
                                     where char.IsWhiteSpace(c)
                                     || char.IsLetterOrDigit(c)
                                     select c).ToArray());

            var path = Path.Combine(FileArchiveLocation.FullName, base64Guid);

            return new DirectoryInfo(path);
        }

        public static string GetCopyPath(string file)
        {
            var fileObj = new FileInfo(file);

            if (!fileObj.Exists)
                throw new FileNotFoundException("File to backup, cannot be found");

            var outputFolder = GetArchivePath();

            return Path.Combine(outputFolder.FullName, fileObj.Name);
        }

        #endregion
    }
}