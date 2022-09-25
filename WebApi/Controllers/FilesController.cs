namespace WebApi.Controllers;

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models.FFiles;
using WebApi.Services;
using WebApi.Services.Interface;

[ApiController]
[Route("[controller]")]
public class FilesController : ControllerBase
{
    private IFileService _fileService;
    private IMapper _mapper;

    public FilesController(IFileService fileService, IMapper mapper)
    {
        _fileService = fileService;
        _mapper = mapper;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var files = _fileService.GetAll();
        return Ok(files);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var file = _fileService.GetById(id);
        return Ok(file);
    }

    [HttpPost]
    public IActionResult Create(CreateRequest model)
    {
        _fileService.Create(model);
        return Ok(new { message = "File created" });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        _fileService.Delete(id);
        return Ok(new { message = "File deleted" });
    }
}