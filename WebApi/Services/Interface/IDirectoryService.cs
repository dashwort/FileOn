using WebApi.Entities;
using WebApi.Services.HostedServices.models;

namespace WebApi.Services.Interface
{
    public interface IDirectoryService
    {
        FFolder Create(DirectoryInfo folder);
        void Delete(int id);
        IEnumerable<FFolder> GetAll();
        FFolder GetById(int id);
        void CreateFolder(DirectoryInfo folder);
        Task ScanForFFolderChanges(FFolder folder);
        Task<FolderInUseEvent> RaiseFolderEvent(FileInfo file);
    }
}