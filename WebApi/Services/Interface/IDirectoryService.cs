using WebApi.Entities;
using WebApi.Services.HostedServices.models;

namespace WebApi.Services.Transient
{
    public interface IDirectoryService
    {
        FFolder Create(DirectoryInfo folder);
        FFolder CreateFolder(DirectoryInfo folder);
        void Delete(int id);
        Task<FFolder> FindFFolder(DirectoryInfo dir);
        IEnumerable<FFolder> GetAll();
        FFolder GetById(int id);
        Task RaiseFolderEvent(FileInfo file);
        Task ScanForFFolderChanges(FFolder folder);
    }
}