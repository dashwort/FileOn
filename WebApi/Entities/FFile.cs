namespace WebApi.Entities
{
    public class FFile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string ParentFolder { get; set; }
        public string Hash { get; set; }
        public string CreationTime { get; set; }
        public string Extension { get; set; }
        public string ArchivePath { get; set; }
        public bool Iszip { get; set; }
    }
}
