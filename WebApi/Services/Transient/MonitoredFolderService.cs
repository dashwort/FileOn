using Microsoft.EntityFrameworkCore;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Services.HostedServices;
using WebApi.Services.Interface;

namespace WebApi.Services.Transient
{
    public class MonitoredFolderService : IMonitoredFolderService
    {
        IDirectoryService _directoryService;
        DataContext _context;
        public MonitoredFolderService(IDirectoryService directoryService, DataContext context)
        {
            _directoryService = directoryService;
            _context = context;
        }

        #region Api_methods

        // TODO implement Api Methods

        #endregion

        public FolderToMonitor CreateFolderToMonitor(DirectoryInfo folder)
        {
            var existingFolder = _context.FoldersToMonitor
                .Where(x => x.FullPath == folder.FullName.ToLower())
                .FirstOrDefault();

            if (existingFolder == null)
            {
                var folderToMonitor = new FolderToMonitor(folder);

                // Adds top level folder, so the files aren't missed
                folderToMonitor.FFolders.Add(new FFolder(folder));

                _context.FoldersToMonitor.Add(folderToMonitor);
                _context.SaveChanges(true);

                var copyJobs = new List<CopyJob>();

                foreach (var ffolder in folderToMonitor.FFolders)
                {
                    foreach (var ffile in ffolder.FFiles)
                    {
                        var job = FileTransferService.CreateCopyJob(ffile);
                        copyJobs.Add(job);
                    }
                }

                _context.CopyJobs.AddRange(copyJobs);
                _context.SaveChanges(true);

                return folderToMonitor;
            }

            return existingFolder;
        }

        public async Task ScanMonitoredFolder(DirectoryInfo folder)
        {
            Console.WriteLine($"Scanning monitored folder {folder.FullName}");

            var dir = new DirectoryInfo(folder.FullName);

            var scannedFolders = dir.GetDirectories("*", SearchOption.AllDirectories).ToList();

            // add monitored folder
            scannedFolders.Add(dir);

            var scannedFolderObjects = new List<FFolder>();

            foreach (var f in scannedFolders)
                scannedFolderObjects.Add(new FFolder(f));

            foreach (var f in scannedFolderObjects)
            {
                await _directoryService.ScanForFFolderChanges(f);
            }
        }
    }
}
