namespace WebApi.Helpers;

using Microsoft.EntityFrameworkCore;
using WebApi.Entities;

public class DataContext : DbContext
{
    protected readonly IConfiguration Configuration;

    public DataContext(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // connect to sqlite database
        options.UseSqlite(Configuration.GetConnectionString("WebApiDatabase"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
    }

    public DbSet<FFile> FFiles { get; set; }
    public DbSet<CopyJob> CopyJobs { get; set; }
    public DbSet<FFolder> FFolders { get; set; }
    public DbSet<FolderToMonitor> FoldersToMonitor { get; set; }
    public DbSet<FolderScanJob> FoldersScanJob { get; set; }
}