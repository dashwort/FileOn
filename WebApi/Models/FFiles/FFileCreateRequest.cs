namespace WebApi.Models.FFiles;

using System.ComponentModel.DataAnnotations;
using WebApi.Entities;

public class FFileCreateRequest
{
    public FFileCreateRequest()
    {

    }

    public FFileCreateRequest(string fullpath)
    {
        FullPath = fullpath;
    }

    public int Id { get; set; }
    public string Name { get; set; }

    [Required]
    public string FullPath { get; set; }
    public string ParentFolder { get; set; }

    public FFolder FFolder { get; set; }
    public string Hash { get; set; }

    public DateTime CreationTime { get; set; }
    public DateTime LastModified { get; set; }
    public long Size { get; set; }
    public string Extension { get; set; }

    public string ArchivePath  { get; set; }

    public bool Iszip { get; set; }

}