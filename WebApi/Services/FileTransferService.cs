using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Timers;
using WebApi.Helpers;

namespace WebApi.Services
{
    public class FileTransferService : IHostedService
    {
        IServiceScopeFactory _scopeFactory;
        IConfiguration _configuration;

        // timers 
        System.Timers.Timer ProcessCopyJobs;

        public FileTransferService(IServiceScopeFactory scope, IConfiguration config)
        {
            _scopeFactory = scope;
            _configuration = config;


            ProcessCopyJobs = new System.Timers.Timer(10000);
            ProcessCopyJobs.Elapsed += HandleCopyJobs;
        }

        

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(Start);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.Run(Stop);
        }

        void Stop()
        {
            Console.WriteLine($"Calling stop");
        }

        void Start()
        {
            ProcessCopyJobs?.Start();
        }

        async void HandleCopyJobs(object sender, ElapsedEventArgs e)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<DataContext>();

                var jobs = _context.CopyJobs.Where(x => x.processed == false).ToList();

                Console.WriteLine($"Calling handle copy jobs, number of jobs to process: {jobs.Count}");

                foreach (var job in jobs)
                {
                    Console.WriteLine($"Processing job with ID: {job.Id}, archive path: {job.ArchivePath}");

                    var copyPath = new FileInfo(job.PathToFile);

                    if (copyPath.Exists && !IsFileLocked(copyPath))
                    {
                        await FileService.CopyFileAsync(job.PathToFile, job.ArchivePath);

                        var ffile = _context.FFiles.Find(job.IdToUpdate);

                        var checksumOfCopiedFile = FileService.CalculateMD5(job.ArchivePath);

                        if (checksumOfCopiedFile == ffile.Hash)
                        {
                            job.processed = true;
                            Console.WriteLine($"Verified job with ID: {job.Id}");
                        }
                        else
                        {
                            File.Delete(job.PathToFile);
                            job.Retries++;
                        }

                    }
                    else
                    {
                        job.Retries++;
                    }

                    _context.Update(job);
                    _context.SaveChanges();
                }
            }
        }

        void HandleOnStart(object sender, EventArgs e)
        {
            Console.WriteLine("Starting process file copy job");
            ProcessCopyJobs?.Start();
        }

        bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

            //file is not locked
            return false;
        }
    }
}
