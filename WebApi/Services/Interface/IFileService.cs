using WebApi.Entities;
using WebApi.Models.FFiles;

namespace WebApi.Services.Interface
{
    public interface IFileService
    {
        void Create(CreateRequest model);
        void CreateCopyJob(FFile file);
        void Delete(int id);
        IEnumerable<FFile> GetAll();
        FFile GetById(int id);
    }
}