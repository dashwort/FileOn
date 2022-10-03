namespace WebApi.Entities
{
    public class FolderToMonitor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string[] Extensions { get; set; }
        public string FullPath { get; set; }
        public long MaxSize { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime CreationTime { get; set; }
        public ICollection<FFolder> FFolders { get; set; }
        public bool Enabled { get; set; }
    }
}
