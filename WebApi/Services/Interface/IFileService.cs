using WebApi.Entities;
using WebApi.Models.FFiles;

namespace WebApi.Services.Interface
{
    public interface IFileService
    {
        void Create(FFileCreateRequest model);
        void Delete(int id);
        IEnumerable<FFile> GetAll();
        FFile GetById(int id);
        void CreateFFile(FileInfo f);
        void ScanForChanges(FileInfo f);
    }
}