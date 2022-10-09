namespace WebApi.Services.HostedServices.models
{
    public class FolderInUseEvent
    {
        public FolderInUseEvent(FileInfo path)
        {
            File = path;
            Folder = path.Directory;
            TimeOfEvent = DateTime.UtcNow;
        }

        public FileInfo File { get; set; }
        public DirectoryInfo Folder { get; set; }
        public DateTime TimeOfEvent { get; set; }

        public int FolderId { get; set; } = -1;
    }
}
