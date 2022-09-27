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

            Directories.Add(new DirectoryInfo(@"C:\temp\monitor1"));
            Directories.Add(new DirectoryInfo(@"C:\temp\monitor2"));
            Directories.Add(new DirectoryInfo(@"C:\temp\monitor3"));
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
            Console.WriteLine("{0}, with path {1} has been {2}", e.Name, e.FullPath, e.ChangeType);

            var createRequest = new CreateRequest(e.FullPath);

            var requestDict = new Dictionary<string, string>();
            requestDict.Add("fullPath", e.FullPath);
            var myJson = JsonConvert.SerializeObject(requestDict);

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(
                    "http://localhost:4000/Files",
                     new StringContent(myJson, Encoding.UTF8, "application/json"));

                Console.WriteLine(response.ToString());
            }
        }

        public void OnRenamed(object source, RenamedEventArgs e)
        {
            Console.WriteLine(" {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }

        void ConfigureFileWatcher(DirectoryInfo folder)
        {
            if (!folder.Exists)
            {
                Console.WriteLine($"Folder {folder.FullName} doesnt exist, skipping");
                return;
            }

            // Create a new FileSystemWatcher and set its properties.  
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = folder.FullName;

            // Watch both files and subdirectories.  
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