namespace WebApi.Models.FFiles;

using System.ComponentModel.DataAnnotations;
using WebApi.Entities;

public class CreateRequest
{
    public CreateRequest()
    {

    }

    public CreateRequest(string pullpath)
    {
        FullPath = pullpath;
    }

    public int Id { get; set; }
    public string Name { get; set; }

    [Required]
    public string FullPath { get; set; }
    public string ParentFolder { get; set; }
    public string Hash { get; set; }

    public string CreationTime { get; set; }
    public string Extension { get; set; }

    public string ArchivePath  { get; set; }

    public bool Iszip { get; set; }

}