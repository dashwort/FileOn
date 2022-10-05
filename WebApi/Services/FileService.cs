using AutoMapper;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models.FFiles;
using WebApi.Services.Interface;

namespace WebApi.Services
{
    public class FileService : IFileService
    {
        DataContext _context;
        readonly IMapper _mapper;
        IConfiguration _configuration;
        static IConfiguration _staticConfiguration;   

        public static DirectoryInfo FileArchiveLocation { get; set; }

        static ConcurrentDictionary<string, bool> WorkItems = new ConcurrentDictionary<string, bool>();

        public FileService(DataContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _staticConfiguration = _configuration;

            var folder = _configuration.GetSection("FileService:FileArchive").Value;

            FileArchiveLocation = new DirectoryInfo(folder);


            if (!FileArchiveLocation.Exists)
                FileArchiveLocation.Create();
        }

        public IEnumerable<FFile> GetAll()
        {
            return _context.FFiles;
        }

        public FFile GetById(int id)
        {
            return getFFile(id);
        }

        public void Create(CreateRequest model)
        {
            try
            {
                var modelFile = new FileInfo(model.FullPath);

                // we carry out two checks as these events can file at any moment and the file monitor can trigger multiple events in quick succession. 
                // Dictionary in memory is used as a rapid WIP cache to prevent duplication
                // The search against the copy jobs is to ensure that we're not duplicating efforts 
                bool insertSuccess = WorkItems.TryAdd(modelFile.FullName, false);

                if (!insertSuccess)
                {
                    //Console.WriteLine($"skipping {model.Name} item already present in dictionary lines 59, create model");
                    return;
                }


                if (!modelFile.Exists)
                {
                    //Console.WriteLine("File doesnt exist or is not a valid file path e.g. folder");
                    return;
                }


                model.Hash = CalculateMD5(model.FullPath);
                model.ParentFolder = modelFile.DirectoryName;
                model.CreationTime = modelFile.CreationTimeUtc;
                model.LastModified = modelFile.LastWriteTimeUtc;
                model.Size = modelFile.Length;
                model.Extension = modelFile.Extension;
                model.Iszip = false;
                model.ArchivePath = FileService.GetCopyPath(model.FullPath);


                var parentFolder = _context.FFolders.
                    Where(x => x.Path.ToLower() == model.ParentFolder.ToLower())
                    .FirstOrDefault();

                model.FFolder = parentFolder;

                if (CheckIfDuplicate(model))
                    return;

                // map model to new ffile object
                var file = _mapper.Map<FFile>(model);

                // save FFile
                _context.FFiles.Add(file);
                _context.SaveChanges();
                WorkItems[modelFile.FullName] = true;

                CreateCopyJob(file);

                // TODO add check to clean up dictionary if required
                WorkItems.TryRemove(model.FullPath, out bool success);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during copy process {ex.Message}");
                WorkItems.TryRemove(model.FullPath, out bool success);
            }

        }

        public void Delete(int id)
        {
            var ffile = getFFile(id);
            _context.FFiles.Remove(ffile);
            _context.SaveChanges();
        }

        public void CreateCopyJob(FFile file)
        {
            var job = new CopyJob();

            job.PathToFile = file.FullPath;
            job.ArchivePath = file.ArchivePath;
            job.IdToUpdate = file.Id;
            job.Retries = 0;

            _context.CopyJobs.Update(job);
            _context.SaveChanges(true);
        }

        bool CheckIfDuplicate(CreateRequest model)
        {
            if (_context.FFiles.Any(x => x.Hash == model.Hash) &&
                   _context.FFiles.Any(x => x.FullPath == model.FullPath))
            {

                Console.WriteLine("File already exists, skipping that mofo");
                return true;
            }

            if (_context.CopyJobs.Any(x => x.PathToFile == model.FullPath))
            {
                Console.WriteLine($"Duplicate copy job detected");
                return true;
            }

            return false;
        }

        // helper methods

        private FFile getFFile(int id)
        {
            var file = _context.FFiles.Find(id);
            if (file == null) throw new KeyNotFoundException("ffile not found");
            return file;
        }

        public static DirectoryInfo GetArchivePath()
        {
            string base64Guid = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            base64Guid = new string((from c in base64Guid
                                     where char.IsWhiteSpace(c)
                                     || char.IsLetterOrDigit(c)
                                     select c).ToArray());

            var path = Path.Combine(FileArchiveLocation.FullName, base64Guid);

            return new DirectoryInfo(path);
        }

        public static string GetCopyPath(string file)
        {
            var fileObj = new FileInfo(file);

            if (!fileObj.Exists)
                throw new FileNotFoundException("File to backup, cannot be found");

            var outputFolder = GetArchivePath();

            return Path.Combine(outputFolder.FullName, fileObj.Name);
        }

        public static string CalculateMD5(string filename)
        {
            WaitWhileInUse(filename);

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        static void WaitWhileInUse(string filename)
        {
            int retries = 0;

            if (!FileService.IsFileLocked(filename))
                return;

            while (retries < 10)
            {
                FileService.IsFileLocked(filename);
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
    }
}
