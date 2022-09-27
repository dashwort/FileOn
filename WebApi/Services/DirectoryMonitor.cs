using Newtonsoft.Json;
using System.Text;
using WebApi.Models.FFiles;
using WebApi.Services.Interface;

namespace FileOnLib
{
    public class DirectoryMonitor : IHostedService
    {
        IConfiguration _configuration;
        public DirectoryMonitor(IConfiguration configuration)
        {
            Console.WriteLine($"Starting directory monitor service");
            _configuration = configuration;

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
            Console.WriteLine("Detected {0} in file {1}, with path {2}", e.ChangeType, e.Name, e.FullPath);

            var fileInfo = new FileInfo(e.FullPath);
            var url = _configuration.GetSection("GlobalAppSettings:Url").Value + "/Files";

            // these events are also raised for directory changes, so by wrapping in fileinfo we can check the type safely.
            if (!fileInfo.Exists)
                return;

            // put filepath into usable format for web request
            var requestDict = new Dictionary<string, string>();
            requestDict.Add("fullPath", fileInfo.FullName);
            var filePath = JsonConvert.SerializeObject(requestDict);

            var response = await client.PostAsync(url, new StringContent(filePath, Encoding.UTF8, "application/json"));
            Console.WriteLine($"Archived file: {fileInfo.Name} with status {response.StatusCode}");
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