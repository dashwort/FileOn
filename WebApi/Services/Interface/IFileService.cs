using WebApi.Entities;
using WebApi.Models.FFiles;

namespace WebApi.Services.Interface
{
    public interface IFileService
    {
        DirectoryInfo FileArchiveLocation { get; set; }

        event EventHandler FileChanged;

        void Create(CreateRequest model);
        void Delete(int id);
        IEnumerable<FFile> GetAll();
        FFile GetById(int id);
    }
}