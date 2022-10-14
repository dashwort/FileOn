using Microsoft.EntityFrameworkCore;
using System.Timers;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Services.Interface;
using WebApi.Services.Transient;

namespace WebApi.Services.HostedServices
{
    public class UniqueFileService : IHostedService
    {
        System.Timers.Timer _scanEventTimer;
        IServiceScopeFactory _scopeFactory;
        IConfiguration _configuration;
        public UniqueFileService(IConfiguration configuration, IServiceScopeFactory scope)
        {
            _scopeFactory = scope;
            _configuration = configuration;

            _scanEventTimer = new System.Timers.Timer(1000 * 60);
            _scanEventTimer.Elapsed += TriggerAutomaticScan;
        }

        public int ScanInterval { get; set; }
        public bool AutoScanIsRunning { get; set; }

        void StartMonitor()
        {
            _scanEventTimer.Start();
        }

        void StopMonitor()
        {
            _scanEventTimer.Stop();
        }

        async void TriggerAutomaticScan(object sender, ElapsedEventArgs e)
        {
            await CalculateUniqueFiles();
        }

        public async Task CalculateUniqueFiles()
        {
            Console.WriteLine($"Calculating unique files for DB table");

            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<DataContext>();

                var allFFiles = _context.FFiles;

                var uniqueFiles = new Dictionary<string, UniqueFile>();

                foreach (var file in allFFiles)
                {
                    if (!uniqueFiles.ContainsKey(file.FullPath))
                    {
                        uniqueFiles.Add(file.FullPath, new UniqueFile(file));
                        continue;
                    }

                    var uniqueFile = uniqueFiles[file.FullPath];

                    if (file.LastModified > uniqueFile.FFile.LastModified)
                    {
                        var updatedUniqueFile = new UniqueFile(file);
                        updatedUniqueFile.Versions = uniqueFile.Versions;
                        updatedUniqueFile.Versions++;
                        continue;
                    }

                    uniqueFile.Versions++; 
                }

                await AddUniqueFilesToDb(uniqueFiles, _context);
            }
        }

        async Task AddUniqueFilesToDb(Dictionary<string, UniqueFile> uniqueFiles, DataContext context)
        {
            Console.WriteLine($"Number of unique files in DB: {uniqueFiles.Count}");

            foreach (var entry in uniqueFiles)
            {
                var uniqueFile = context.UniqueFiles.Where(x => x.FullPath == entry.Value.FullPath)
                    .FirstOrDefault();

                if (uniqueFile != null)
                {
                    uniqueFile.FullPath = entry.Value.FullPath;
                    uniqueFile.ArchivedFilePath = entry.Value.ArchivedFilePath;
                    uniqueFile.Name = entry.Value.Name;
                    uniqueFile.Versions = entry.Value.Versions;
                    uniqueFile.FFile = entry.Value.FFile;
                    continue;
                }

                await context.UniqueFiles.AddAsync(entry.Value);
                    
            }

            await context.SaveChangesAsync();
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
    }
}
