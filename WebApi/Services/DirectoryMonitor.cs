using Newtonsoft.Json;
using System.IO;
using System.Text;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models.FFiles;
using WebApi.Services;
using WebApi.Services.Interface;

namespace FileOnLib
{
    public class DirectoryMonitor : IHostedService
    {
        IConfiguration _configuration;
        IServiceScopeFactory _scopeFactory;

        public DirectoryMonitor(IConfiguration configuration, IServiceScopeFactory scope)
        {
            Console.WriteLine($"Starting directory monitor service");
            _configuration = configuration;
            _scopeFactory = scope;

            Directories = new List<DirectoryInfo>();

            var folders = _configuration.GetSection("DirectoryMonitor:DirectoriesToMonitor").Get<List<string>>();

            foreach (var folder in folders)
            {
                var folderInfo = new DirectoryInfo(folder);

                if (folderInfo.Exists)
                    Directories.Add(folderInfo);
                else
                    Console.WriteLine($"Warning skipping non-existent folder {folderInfo.FullName}");
            }
        }

        public List<DirectoryInfo> Directories { get; set; }
        List<FileSystemWatcher> fileSystemWatchers = new List<FileSystemWatcher>();

        async Task StartMonitor()
        {

            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<DataContext>();

                foreach (var folder in Directories)
                {
                    var ffolder = new FFolder(folder);



                    _context.FFolders.Add(ffolder);

                    ConfigureFileWatcher(folder);
                }

                await _context.SaveChangesAsync();
            }

        }

        void StopMonitor()
        {
            foreach (var watcher in fileSystemWatchers)
            {
                watcher.EnableRaisingEvents = false;
            }
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(StartMonitor);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Calling stop async on directory monitor");
            return Task.Run(StopMonitor);
        }
        
        // Define the event handlers.  
        public async void OnChanged(object source, FileSystemEventArgs e)
        {
            var fileInfo = new FileInfo(e.FullPath);

            if (!fileInfo.Exists)
                return;

            using (var scope = _scopeFactory.CreateScope())
            {
                var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                Console.WriteLine("Detected type: {0} in file: {1}, with path: {2}", e.ChangeType, e.Name, e.FullPath);

                var model = new CreateRequest(fileInfo.FullName);

                var folder = await context.FFolders.FindAsync(1);

                await Task.Run(() => { fileService.Create(model); });
            }

        }

        public void OnRenamed(object source, RenamedEventArgs e)
        {
            Console.WriteLine(" {0} renamed to {1}", e.OldFullPath, e.FullPath);

            // TODO logic to streamline interactions when renaming folders etc
        }

        void ConfigureFileWatcher(DirectoryInfo folder)
        {
            if (!folder.Exists)
            {
                Console.WriteLine($"Folder {folder.FullName} doesnt exist, skipping");
                return;
            }

            Console.WriteLine($"Starting to watch folder: {folder.FullName}");
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = folder.FullName;
            watcher.IncludeSubdirectories = true;
            // Watch for all changes specified in the NotifyFilters  
            //enumeration.  
            watcher.NotifyFilter = NotifyFilters.Attributes |
            NotifyFilters.CreationTime |
            NotifyFilters.DirectoryName |
            NotifyFilters.FileName |
            NotifyFilters.LastAccess |
            NotifyFilters.LastWrite;

            // Watch all files. Handle filtering in onchange event as filter doesnt support more than one extension.
            watcher.Filter = "*";

            // Add event handlers.  
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            //Start monitoring.  
            watcher.EnableRaisingEvents = true;
        }


    }
}