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
        void CreateFolderToMonitor(string f);
        void Delete(int id);
        IEnumerable<FFolder> GetAll();
        FFolder GetById(int id);
        void ScanForFFolderChanges(DirectoryInfo fo);
        void ScanForFFolderChanges(FFolder fo);
        void ScanMonitoredFolder(FolderToMonitor folder);
        void ScanMonitoredFolder(string folder);
    }
}