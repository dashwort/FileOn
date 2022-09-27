﻿using FileOnLib;
using System.Text.Json.Serialization;
using WebApi.Helpers;
using WebApi.Services;
using WebApi.Services.Interface;

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

    // add the directory monitor and starts it running
    services.AddHostedService<DirectoryMonitor>()
    .AddSingleton<DirectoryMonitor>(x => x
        .GetServices<IHostedService>()
        .OfType<DirectoryMonitor>()
        .First());

}

var app = builder.Build();


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