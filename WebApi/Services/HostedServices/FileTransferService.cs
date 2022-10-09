using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Services.HostedServices.models;
using WebApi.Services.Transient;

namespace WebApi.Services.HostedServices
{
    public class FileTransferService : IHostedService
    {
        IServiceScopeFactory _scopeFactory;
        IConfiguration _configuration;

        // timers 
        System.Timers.Timer ProcessCopyJobs;
        System.Timers.Timer FolderActivity;

        ConcurrentDictionary<string, FolderInUseEvent> _foldersInUse;

        public bool IsRunning { get; set; }
        public bool IsFolderActivityRunning { get; set; }

        public FileTransferService(IServiceScopeFactory scope, IConfiguration config)
        {
            _scopeFactory = scope;
            _configuration = config;
            _foldersInUse = new ConcurrentDictionary<string, FolderInUseEvent>();

            ProcessCopyJobs = new System.Timers.Timer(10000);
            ProcessCopyJobs.Elapsed += HandleCopyJobs;

            FolderActivity = new System.Timers.Timer(1000);
            FolderActivity.Elapsed += HandleFolderActivityTimer;
            DirectoryMonitor.DirectoryActivity += HandleFolderActivity;
        }

        void HandleFolderActivityTimer(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (IsFolderActivityRunning)
                    return;
                else
                    IsFolderActivityRunning = true;

                foreach (var folderEvent in _foldersInUse)
                {
                    var now = DateTime.UtcNow;

                    var delta = now - folderEvent.Value.TimeOfEvent;

                    if (delta.Minutes > 1)
                    {
                        _foldersInUse.TryRemove(folderEvent);
                        Console.WriteLine($"folder expired: {folderEvent.Value.Folder.FullName}");
                    }

                }

                IsFolderActivityRunning = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in handle folder activity timer: {ex.Message} ");
            }
        }

        void HandleFolderActivity(object sender, EventArgs e)
        {
            try
            {
                var folderEvent = sender as FolderInUseEvent;

                bool success = _foldersInUse.TryAdd(folderEvent.Folder.FullName, folderEvent);

                if (success)
                    Console.WriteLine($"Added {folderEvent.Folder.FullName} to folder in use dictionary");

                if (!success)
                {
                    var removeSuccess = _foldersInUse.TryRemove(
                        folderEvent.Folder.FullName, out FolderInUseEvent value);

                    success = _foldersInUse.TryAdd(folderEvent.Folder.FullName, folderEvent);

                    Console.WriteLine($"Warning key was already present, deleted and added {folderEvent.Folder.FullName} to folder in use dictionary");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error during handlefolder activity: {ex.Message}");
            }
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
            Console.WriteLine($"Calling stop file transfer service");
            ProcessCopyJobs?.Stop();
            FolderActivity?.Stop();
        }

        void Start()
        {
            Console.WriteLine($"Starting file transfer service");
            ProcessCopyJobs?.Start();
            FolderActivity?.Start();
        }

        async void HandleCopyJobs(object sender, ElapsedEventArgs e)
        {
            if (IsRunning)
                return;
            else
                IsRunning = true;

            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<DataContext>();

                var jobs = _context.CopyJobs.Where(x => x.processed == false).ToList();

                if (jobs.Any())
                    Console.WriteLine($"Calling handle copy jobs, number of jobs to process: {jobs.Count}");

                foreach (var job in jobs)
                {
                    try
                    {
                        var copyPath = new FileInfo(job.PathToFile);

                        if (FolderIsInUse(copyPath.Directory, _context))
                            continue;

                        if (copyPath.Exists && !IsFileLocked(copyPath))
                        {
                            var parent = Path.GetDirectoryName(job.ArchivePath);

                            if (!Directory.Exists(parent))
                                Directory.CreateDirectory(parent);

                            await CopyFileAsync(job.PathToFile, job.ArchivePath);

                            var ffile = _context.FFiles.Find(job.IdToUpdate);

                            var checksumOfCopiedFile = CalculateMD5(job.ArchivePath);

                            if (checksumOfCopiedFile == ffile.Hash)
                            {
                                job.processed = true;
                                Console.WriteLine($"Verified copy job with ID: {job.Id}, path {job.PathToFile}");

                                _context.Remove(job);
                                _context.SaveChanges();
                                continue;
                            }
                            else
                            {
                                // TODO handle this edge case
                                //File.Delete(job.PathToFile);
                                job.Retries++;

                                Console.WriteLine($"Warning line 177 in FileTransferservices");
                            }

                        }
                        else
                        {
                            job.Retries++;
                        }

                        _context.Update(job);
                        _context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during copy job {ex.Message}");
                    }
                }
            }

            IsRunning = false;
        }

        bool FolderIsInUse(DirectoryInfo directory, DataContext context)
        {
            bool InUse = false;

            try
            {
                InUse = _foldersInUse.TryGetValue(directory.FullName, out FolderInUseEvent folderEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during checking folder is in use: {ex.Message}");
                InUse = true;
            }

            return InUse;
        }

        void HandleOnStart(object sender, EventArgs e)
        {
            Console.WriteLine("Starting process file copy job");
            ProcessCopyJobs.Start();
            FolderActivity.Start();
        }

        #region Static_Methods
        public static CopyJob CreateCopyJob(FFile file)
        {
            var job = new CopyJob();

            job.PathToFile = file.FullPath;
            job.ArchivePath = file.ArchivePath;
            job.IdToUpdate = file.Id;
            job.Retries = 0;

            return job;
        }

        public static string CalculateMD5(string filename)
        {
            try
            {
                var fileInfo = new FileInfo(filename);

                var sizeInMb = fileInfo.Length / (1024 * 1024);

                WaitWhileInUse(fileInfo.FullName);

                if (IsFileLocked(fileInfo.FullName))
                    return String.Empty;

                using (var md5 = SHA512.Create())
                {
                    using (var stream = File.OpenRead(fileInfo.FullName))
                    {
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during MD5 Calc: {ex.Message}");
                return String.Empty;
            }
        }

        static void WaitWhileInUse(string filename)
        {
            int retries = 0;

            if (!IsFileLocked(filename))
                return;

            while (retries < 25)
            {
                IsFileLocked(filename);
                retries++;
                Thread.Sleep(250);
            }
        }

        public static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            if (File.Exists(destinationFile))
                return;

            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var destinationStream = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                await sourceStream.CopyToAsync(destinationStream);
        }

        public static bool IsFileLocked(FileInfo file)
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

        public static bool IsFileLocked(string file)
        {
            return IsFileLocked(new FileInfo(file));
        }

        #endregion

    }
}
