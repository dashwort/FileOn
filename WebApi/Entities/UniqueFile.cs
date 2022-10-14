namespace WebApi.Entities
{
    public class UniqueFile
    {
        public UniqueFile()
        {

        }

        public UniqueFile(FFile f)
        {
            FullPath = f.FullPath;
            ArchivedFilePath = f.ArchivePath;
            Name = f.Name;
            Versions = 1;
            FFile = f;
        }
        public int Id { get; set; }
        public string FullPath { get; set; }
        public string ArchivedFilePath { get; set; }
        public string Name { get; set; }
        public int Versions { get; set; }
        public FFile FFile { get; set; }
    }
}
