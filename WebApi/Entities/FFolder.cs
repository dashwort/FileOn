﻿using WebApi.Services;

namespace WebApi.Entities
{
    public class FFolder
    {
        public FFolder()
        {

        }

        public FFolder(DirectoryInfo fo)
        {
            this.Name = fo.Name;
            this.Path = fo.FullName;
            this.CreatedDate = fo.CreationTimeUtc;
            this.LastModified = fo.LastWriteTimeUtc;
            this.FFiles = GetFiles(fo);

            Console.WriteLine($"Folder created for {fo.FullName}");
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }

        public ICollection<FFile> FFiles { get; set; }

        string[] GetExtensions()
        {
            // TODO implement better system thats configurable
            return new string[] { ".txt", ".pdf", ".zip", ".cs", ".ps1", ".db", ".md", ".vs" };
        }

        public ICollection<FFile> GetFiles()
        {
            return GetFiles(new DirectoryInfo(this.Path));
        }

        public ICollection<FFile> GetFiles(DirectoryInfo fo)
        {
            var ffilesToReturn = new List<FFile>();

            var extensions = GetExtensions();

            var files = fo.GetFiles("*", SearchOption.AllDirectories).
                Where(x => extensions.Contains(x.Extension)); 

            foreach (var f in files)
                ffilesToReturn.Add(new FFile(f));

            return ffilesToReturn;
        }

    }
        public class FFolderInfoEqualityComparer : IEqualityComparer<FFolder>
        {
            // Interface for comparing two FileInfoLists
            public FFolderInfoEqualityComparer()
            {
                //ctor for equity comparer
            }

            public bool Equals(FFolder x, FFolder y)
            {
                return x.Path == y.Path;
            }


            /// <summary>
            /// compare hashes of each file based on file size and name
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public int GetHashCode(FFolder fi)
            {
                string s = $"{fi.Path}";
                return s.GetHashCode();
            }
        }
    }

  
