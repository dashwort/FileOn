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

    public DbSet<FFile> FFiles { get; set; }
    public DbSet<CopyJob> CopyJobs { get; set; }

    public DbSet<FFolder> FFolders { get; set; }
}