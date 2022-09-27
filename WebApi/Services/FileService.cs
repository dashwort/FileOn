using AutoMapper;
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

        public DirectoryInfo FileArchiveLocation { get; set; }

        public event EventHandler FileChanged;

        public FileService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;

            FileArchiveLocation = new DirectoryInfo(@"C:\temp\FileOn\FileArchive");

            if (!FileArchiveLocation.Exists)
                FileArchiveLocation.Create();

            FileChanged += HandleFileChange;
        }

        void HandleFileChange(object sender, EventArgs e)
        {
            Console.WriteLine($"Called handle file change");

            var createRequest = sender as CreateRequest;

            if (createRequest != null)
            {
                Console.WriteLine("Raising createRequest to FileService");

                this.Create(createRequest);
            }
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
            var modelFile = new FileInfo(model.FullPath);

            if (!modelFile.Exists)
                throw new ApplicationException("File doesnt exist or is not a valid file path e.g. folder");

            model.Hash = CalculateMD5(model.FullPath);
            model.ParentFolder = modelFile.DirectoryName;
            model.CreationTime = modelFile.CreationTimeUtc.ToShortDateString();
            model.Extension = modelFile.Extension;
            model.Iszip = false;


            if (_context.FFiles.Any(x => x.Hash == model.Hash))
            {
                Console.WriteLine("File already exists, skipping that mofo");
                return;
            }


            bool success = CreateCopy(model.FullPath, out string outputPath);

            if (success)
            {
                model.ArchivePath = outputPath;

                // map model to new ffile object
                var file = _mapper.Map<FFile>(model);

                // save FFile
                _context.FFiles.Add(file);
                _context.SaveChanges();
            }

        }

        public void Delete(int id)
        {
            var ffile = getFFile(id);
            _context.FFiles.Remove(ffile);
            _context.SaveChanges();
        }

        // helper methods

        private FFile getFFile(int id)
        {
            var file = _context.FFiles.Find(id);
            if (file == null) throw new KeyNotFoundException("ffile not found");
            return file;
        }

        DirectoryInfo GetArchivePath()
        {
            string base64Guid = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

            var path = Path.Combine(FileArchiveLocation.FullName, base64Guid);

            return new DirectoryInfo(path);
        }

        bool CreateCopy(string file, out string outputPath)
        {
            bool success = false;
            outputPath = string.Empty;

            try
            {
                var fileObj = new FileInfo(file);

                if (!fileObj.Exists)
                    throw new FileNotFoundException("File to backup, cannot be found");

                var outputFolder = GetArchivePath();
                outputPath = Path.Combine(outputFolder.FullName, fileObj.Name);

                outputFolder.Create();
                File.Copy(fileObj.FullName, outputPath);

                fileObj.Refresh();
                success = fileObj.Exists;
            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }

        string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }


    }
}
