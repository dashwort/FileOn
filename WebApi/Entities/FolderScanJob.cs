namespace WebApi.Entities
{
    public class FolderScanJob
    {
        public FolderScanJob(int id)
        {
            FFolderId = id;
            TimeAdded = DateTime.UtcNow;
        }

        public int Id { get; set; }
        public int FFolderId { get; set; }
        public DateTime TimeAdded  { get; set; }
    }
}
