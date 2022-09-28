namespace WebApi.Entities
{
    public class CopyJob
    {
        public int Id { get; set; }
        public int IdToUpdate { get; set; }
        public string PathToFile { get; set; }
        public string ArchivePath { get; set; }
        public int Retries { get; set; }
        public bool processed { get; set; }

    }
}
