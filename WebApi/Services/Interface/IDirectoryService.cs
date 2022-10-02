using WebApi.Entities;
using WebApi.Models.FFolder;

namespace WebApi.Services.Interface
{
    public interface IDirectoryService
    {
        void Create(CreateRequest model);
        void Create(DirectoryInfo folder);
        void Create(string folderPath);
        CopyJob CreateCopyJob(FFile file);
        void Delete(int id);
        IEnumerable<FFolder> GetAll();
        FFolder GetById(int id);
        void ScanForChanges(FFolder fo);

        void ScanForChanges(DirectoryInfo fo);
    }
}