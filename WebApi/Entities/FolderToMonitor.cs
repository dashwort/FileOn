using System.IO;

namespace WebApi.Entities
{
    public class FolderToMonitor
    {
        public FolderToMonitor()
        {

        }

        public FolderToMonitor(DirectoryInfo folder)
        {
            Name = folder.Name;
            FullPath = folder.FullName;
            Exists = folder.Exists;
            MaxSize = GetMaxSize();
            Extensions = GetExtensions();
            LastModified = folder.LastAccessTimeUtc;
            CreationTime = folder.CreationTimeUtc;
            FFolders = GetFFolders();
            Enabled = true;
        }

        List<string> _excludedDirectories = new List<string>() { ".git", ".vs" };

        private ICollection<FFolder> GetFFolders()
        {
            var ffolders = new List<FFolder>();

            var folderToMonitor = new DirectoryInfo(FullPath);

            var subFolders = folderToMonitor.GetDirectories("*", SearchOption.AllDirectories)
                .Where(d => !isExcluded(_excludedDirectories, d)).ToArray();

            foreach (var f in subFolders)
            {
                ffolders.Add(new FFolder(f));
            }

            ffolders.Add(new FFolder(FullPath));

            return ffolders;
        }

        private long GetMaxSize()
        {
            // TODO implement an option to pass down maxsize to ffolders
            return 0;
        }

        static bool isExcluded(List<string> exludedDirList, DirectoryInfo target)
        {
            return exludedDirList.Any(d => target.FullName.Contains(d));
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public bool Exists { get; set; }
        public string Extensions { get; set; }
        public string FullPath { get; set; }
        public long MaxSize { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime CreationTime { get; set; }
        public ICollection<FFolder> FFolders { get; set; }
        public bool Enabled { get; set; }
        

        string GetExtensions()
        {
            // TODO implement better system thats configurable
            return string.Empty;
        }
    }
}
