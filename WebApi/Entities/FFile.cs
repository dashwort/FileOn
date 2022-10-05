using System.ComponentModel.DataAnnotations;
using WebApi.Models.FFiles;
using WebApi.Services;

namespace WebApi.Entities
{
    public class FFile
    {
        IConfiguration _configuration;

        public FFile(IConfiguration config)
        {
            _configuration = config;
        }
        public FFile()
        {

        }

        public FFile(FileInfo fi)
        {
            this.Name = fi.Name.ToLower();
            this.FullPath = fi.FullName.ToLower();
            this.ParentFolder = fi.Directory.FullName.ToLower();
            this.Hash = FileService.CalculateMD5(fi.FullName).ToLower();
            this.CreationTime = fi.CreationTimeUtc;
            this.LastModified = fi.LastWriteTimeUtc;
            this.Size = fi.Length;
            this.Extension = fi.Extension.ToLower();
            this.ArchivePath = FileService.GetCopyPath(fi.FullName).ToLower();

            if (this.Extension.ToLower() == ".zip")
                this.Iszip = true;
        }

        public FFile(CreateRequest request)
        {
            throw new NotImplementedException();
        }

        public int Id { get; set; }

        [Required]
        public FFolder FFolder { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string ParentFolder { get; set; }
        public string Hash { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
        public string Extension { get; set; }
        public string ArchivePath { get; set; }
        public bool Iszip { get; set; }
    }

    public class FFileInfoEqualityComparer : IEqualityComparer<FFile>
    {
        // Interface for comparing two FileInfoLists
        public FFileInfoEqualityComparer()
        {
            //ctor for equity comparer
        }

        public bool Equals(FFile x, FFile y)
        {
            bool hash = x.Hash == y.Hash;
            return x.Name.Equals(y.Name) && hash;
        }


        /// <summary>
        /// compare hashes of each file based on file size and name
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(FFile fi)
        {
            string s = $"{fi.FullPath}{fi.Size}{fi.LastModified}";
            return s.GetHashCode();
        }
    }
}
