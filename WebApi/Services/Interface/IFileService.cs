using WebApi.Entities;
using WebApi.Models.FFiles;

namespace WebApi.Services.Interface
{
    public interface IFileService
    {
        static DirectoryInfo FileArchiveLocation { get; set; }

        void Create(CreateRequest model);
        void Delete(int id);
        IEnumerable<FFile> GetAll();
        FFile GetById(int id);
    }
}