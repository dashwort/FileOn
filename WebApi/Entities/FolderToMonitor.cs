namespace WebApi.Entities
{
    public class FolderToMonitor
    {
        public FolderToMonitor()
        {

        }

        public FolderToMonitor(string directory)
        {
            var folder = new DirectoryInfo(directory);

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

        private ICollection<FFolder> GetFFolders()
        {
            var ffiles = new List<FFolder>();

            var folderToMonitor = new DirectoryInfo(FullPath);

            var subFolders = folderToMonitor.GetDirectories("*", SearchOption.AllDirectories);

            foreach (var f in subFolders)
            {
                ffiles.Add(new FFolder(f));
            }

            return ffiles;
        }

        private long GetMaxSize()
        {
            // TODO implement an option to pass down maxsize to ffolders
            return 0;
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
