using WebApi.Entities;

namespace WebApi.Services.Interface
{
    public interface IMonitoredFolderService
    {
        FolderToMonitor CreateFolderToMonitor(DirectoryInfo folder);
        Task ScanMonitoredFolder(FolderToMonitor folder);
    }
}