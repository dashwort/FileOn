using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models.FFolder;
using WebApi.Services.HostedServices;
using WebApi.Services.HostedServices.models;
using WebApi.Services.Interface;

namespace WebApi.Services.Transient
{
    public class DirectoryService : IDirectoryService
    {

        #region props_and_ctor
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
        #endregion

        #region CRUD_Methods_For_API
        public IEnumerable<FFolder> GetAll()
        {
            return _context.FFolders;
        }

        public FFolder GetById(int id)
        {
            return getFFolder(id);
        }

        public FFolder Create(DirectoryInfo folder)
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
                    var job = FileTransferService.CreateCopyJob(f);
                    copyJobs.Add(job);
                }

                _context.CopyJobs.AddRange(copyJobs);
                _context.SaveChanges(true);
            }

            return ffolder;
        }

        public void Delete(int id)
        {
            var ffile = getFFolder(id);
            _context.FFolders.Remove(ffile);
            _context.SaveChanges();
        }

        private FFolder getFFolder(int id)
        {
            var file = _context.FFolders.Find(id);
            if (file == null) throw new KeyNotFoundException("ffile not found");
            return file;
        }
        #endregion

        #region FolderToMonitor
        public void CreateFolder(DirectoryInfo folder)
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
                    var job = FileTransferService.CreateCopyJob(f);
                    copyJobs.Add(job);
                }

                _context.CopyJobs.AddRange(copyJobs);
                _context.SaveChanges(true);
            }

        }

        public async Task<FolderInUseEvent> RaiseFolderEvent(FileInfo file)
        {
            var ffolder = await _context.FFolders
                .Where(x => x.Path == file.Directory.FullName.ToLower())
                .FirstOrDefaultAsync();

            var folderEvent = new FolderInUseEvent(file);

            if (ffolder != null)
                folderEvent.FolderId = ffolder.Id;

            return folderEvent;
        }

        public async Task ScanForFFolderChanges(FFolder folder)
        {
            var fo = await _context.FFolders
                .Include(x => x.FFiles)
                .Where(x => x.Path == folder.Path).FirstOrDefaultAsync();

            if (fo is null)
                fo = Create(new DirectoryInfo(folder.Path));

           var hashes = fo.FFiles.Select(x => x.Hash).ToList();

            var currentFiles = folder.FFiles.ToArray();

            var listOfDifferences = new List<FFile>();

            foreach (var file in currentFiles)
            {
                if (hashes.Contains(file.Hash))
                    continue;

                Console.WriteLine($"file with hash: {file.Hash} doesnt exist: {file.FullPath}");
                listOfDifferences.Add(file);
            }

            if (listOfDifferences.Count > 0)
            {
                Console.WriteLine($"{listOfDifferences.Count} changes found in folder {fo.Path}");
                foreach (var f in listOfDifferences)
                {
                    _fileService.Create(new Models.FFiles.FFileCreateRequest(f.FullPath));
                }
            }
        }
        #endregion
    }
}
