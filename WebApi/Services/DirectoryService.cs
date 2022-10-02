﻿using AutoMapper;
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

        public void ScanForChanges(DirectoryInfo fo)
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

            ScanForChanges(folder);
        }

        public void ScanForChanges(FFolder fo)
        {
            Console.WriteLine($"Scanning folder: {fo.Name} for changes");

            var lastKnownFiles = _context.FFiles.Where(x => x.FFolder.Id == fo.Id).ToArray();

            var currentFiles = fo.GetFiles().ToArray();

            var differences = currentFiles.Except(lastKnownFiles, _fileInfoEqualityComparer).ToArray();

            Console.WriteLine($"{differences.Length} changes found in folder {fo.Path}");

            foreach (var f in differences)
            {
                _fileService.Create(new Models.FFiles.CreateRequest(f.FullPath));
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

    }
}