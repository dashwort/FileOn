using System.Text.Json.Serialization;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Services.HostedServices;
using WebApi.Services.Interface;
using WebApi.Services.Transient;

var builder = WebApplication.CreateBuilder(args);

// add services to DI container
{
    var services = builder.Services;
    var env = builder.Environment;
 
    services.AddDbContext<DataContext>();
    services.AddCors();
    services.AddControllers().AddJsonOptions(x =>
    {
        // serialize enums as strings in api responses (e.g. Role)
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // ignore omitted parameters on models to enable optional params (e.g. User update)
        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
    services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    // configure DI for application services
    services.AddTransient<IFileService, FileService>();
    services.AddTransient<IDirectoryService, DirectoryService>();
    services.AddTransient<IMonitoredFolderService, MonitoredFolderService>();

    // add the directory monitor and starts it running
    services.AddHostedService<DirectoryMonitor>()
    .AddSingleton<DirectoryMonitor>(x => x
        .GetServices<IHostedService>()
        .OfType<DirectoryMonitor>()
        .First());

    services.AddHostedService<FileTransferService>()
    .AddSingleton<FileTransferService>(x => x
        .GetServices<IHostedService>()
        .OfType<FileTransferService>()
        .First());

    services.AddHostedService<UniqueFileService>()
    .AddSingleton<UniqueFileService>(x => x
        .GetServices<IHostedService>()
        .OfType<UniqueFileService>()
        .First());

}

var app = builder.Build();


// Instantiate static properties on first launch
var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<IFileService>();



// configure HTTP request pipeline
{
    // global cors policy
    app.UseCors(x => x
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

    // global error handler
    app.UseMiddleware<ErrorHandlerMiddleware>();

    app.MapControllers();
}

app.Run("http://localhost:4000");