using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Timers;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models.FFiles;
using WebApi.Services.HostedServices.models;
using WebApi.Services.Interface;
using WebApi.Services.Transient;

namespace WebApi.Services.HostedServices
{
    public class DirectoryMonitor : IHostedService
    {
        IConfiguration _configuration;
        IServiceScopeFactory _scopeFactory;

        System.Timers.Timer _scanEventTimer;

        public static EventHandler DirectoryActivity; 
        public DirectoryMonitor(IConfiguration configuration, IServiceScopeFactory scope)
        {
            Console.WriteLine($"Starting directory monitor service");
            _configuration = configuration;
            _scopeFactory = scope;

            ReadConfiguration();

            if (ScanInterval != 0)
            {
                _scanEventTimer = new System.Timers.Timer(1000 * ScanInterval);
                _scanEventTimer.Elapsed += TriggerAutomaticScan;
            }
        }

        public List<DirectoryInfo> Directories { get; set; }
        public bool AutoScan { get; set; }
        public int ScanInterval { get; set; }
        public bool AutoScanIsRunning { get; set; }

        List<FileSystemWatcher> fileSystemWatchers = new List<FileSystemWatcher>();

        void ReadConfiguration()
        {
            try
            {
                Directories = new List<DirectoryInfo>();

                var folders = _configuration.GetSection("DirectoryMonitor:DirectoriesToMonitor").Get<List<string>>();
                var autoScanstring = _configuration.GetSection("DirectoryMonitor:AutoDetectChanges").Get<string>();
                var scanIntervalstring = _configuration.GetSection("DirectoryMonitor:ScanInterval").Get<string>();

                bool autoScan = false;
                bool parseAutoScan = bool.TryParse(autoScanstring, out autoScan);

                if (parseAutoScan)
                    this.AutoScan = autoScan;

                int scanInterval = 0;
                bool parsescanInterval = int.TryParse(scanIntervalstring, out scanInterval);

                if (parsescanInterval)
                    this.ScanInterval = scanInterval;

                foreach (var folder in folders)
                {
                    var folderInfo = new DirectoryInfo(folder);

                    if (!folderInfo.Exists)
                    {
                        Console.WriteLine($"Warning: folder to monitor doesnt exist. Path: {folderInfo.FullName}");
                        continue;
                    }

                    Directories.Add(folderInfo);
                }

                Console.WriteLine($"Automatic scan interval: {ScanInterval}");
                Console.WriteLine($"Enable auto scan = {AutoScan}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading Directory monitor info from appsettings.json. " +
                    $"Error: {ex.Message}");
            }           
        }

        async Task StartMonitor()
        {
            await ScanMonitoredFolders();

            if (!AutoScan)
                return;
                
            foreach (var folder in Directories)
                ConfigureFileWatcher(folder);

            _scanEventTimer.Start();
        }

        async Task ScanMonitoredFolders()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var monitoredFolderService = scope.ServiceProvider.GetRequiredService<IMonitoredFolderService>();

                foreach (var folder in Directories)
                    await monitoredFolderService.ScanMonitoredFolder(folder);
            }
        }

        void StopMonitor()
        {
            foreach (var watcher in fileSystemWatchers)
            {
                watcher.EnableRaisingEvents = false;
            }

            _scanEventTimer.Stop();
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

        public async void OnChanged(object source, FileSystemEventArgs e)
        {
            var fileInfo = new FileInfo(e.FullPath);

            if (!fileInfo.Exists)
                return;

            using (var scope = _scopeFactory.CreateScope())
            {
                var directoryService = scope.ServiceProvider.GetRequiredService<IDirectoryService>();

                await directoryService.RaiseFolderEvent(fileInfo);

                var folder = await directoryService.FindFFolder(fileInfo.Directory);

                await directoryService.ScanForFFolderChanges(folder);
            }

        }

        public async void TriggerAutomaticScan(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (AutoScanIsRunning)
                    return;

                AutoScanIsRunning = true;
                Console.WriteLine($"Running automatic scan");

                await ScanMonitoredFolders();

                AutoScanIsRunning = false;
            }
            catch (Exception ex)
            {
                AutoScanIsRunning = false;
                Console.WriteLine($"Error during automatic scan: {ex.Message}");
            }
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
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite;

            // Watch all files. Handle filtering in onchange event as filter doesnt support more than one extension.
            watcher.Filter = "*";

            // Add event handlers.  
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);

            //Start monitoring.  
            watcher.EnableRaisingEvents = true;
        }
    }
}