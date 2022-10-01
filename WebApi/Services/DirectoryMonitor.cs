using Newtonsoft.Json;
using System.Text;
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
        private static readonly HttpClient client = new HttpClient();

        void StartMonitor()
        {
            foreach (var folder in Directories)
            {
                ConfigureFileWatcher(folder);
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
            Console.WriteLine("Start Async called on Directory monitor");
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

            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<IFileService>();

                Console.WriteLine("Detected type: {0} in file: {1}, with path: {2}", e.ChangeType, e.Name, e.FullPath);

                var fileInfo = new FileInfo(e.FullPath);

                if (!fileInfo.Exists)
                    return;

                var model = new CreateRequest(fileInfo.FullName);

                await Task.Run(() => { _context.Create(model); });
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

            // Watch all files.  
            watcher.Filter = "*.*";

            // Add event handlers.  
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            //Start monitoring.  
            watcher.EnableRaisingEvents = true;
        }


    }
}