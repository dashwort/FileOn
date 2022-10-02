using WebApi.Services;

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
            this.FFiles = GetFFiles(fo);
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
        public List<FFile> FFiles { get; set; }

        string[] GetExtensions()
        {
            // TODO implement better system thats configurable
            return new string[] { ".txt", ".pdf", ".zip", ".cs", ".ps1", ".db", ".md", ".vs" };
        }

        List<FFile> GetFFiles(DirectoryInfo fo)
        {
            var ffiles = new List<FFile>();

            var extensions = GetExtensions();

            if (extensions.Length == 0)
                extensions = new string[] { "*" };

            var fs = GetFileList("*", fo.FullName).Where(x => extensions.Contains(x.Extension));

            foreach (var f in fs)
                ffiles.Add(new FFile(f));

            return ffiles;
        }

        IEnumerable<FileInfo> GetFileList(string searchPattern, string rootFolderPath)
        {
            var rootDir = new DirectoryInfo(rootFolderPath);
            var dirList = rootDir.GetDirectories("*", SearchOption.AllDirectories);

            return from directoriesWithFiles in ReturnFiles(dirList, searchPattern).SelectMany(files => files)
                   select directoriesWithFiles;
        }

        IEnumerable<FileInfo[]> ReturnFiles(DirectoryInfo[] dirList, string fileSearchPattern)
        {
            foreach (DirectoryInfo dir in dirList)
            {
                yield return dir.GetFiles(fileSearchPattern, SearchOption.TopDirectoryOnly);
            }
        }
    }
}
