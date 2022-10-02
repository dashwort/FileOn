using AutoMapper;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models.FFolder;

namespace WebApi.Services
{
    public class DirectoryService
    {

        DataContext _context;
        readonly IMapper _mapper;
        IConfiguration _configuration;

        public DirectoryService(DataContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
        }


        public IEnumerable<FFolder> GetAll()
        {
            return _context.FFolders;
        }

        public FFolder GetById(int id)
        {
            return getFFolder(id);
        }

        public void Create(CreateRequest model)
        {
            //
        }

        public void Create(string folderPath)
        {
            
        }

        public void Delete(int id)
        {
            var ffile = getFFolder(id);
            _context.FFolders.Remove(ffile);
            _context.SaveChanges();
        }

        // helper methods

        private FFolder getFFolder(int id)
        {
            var file = _context.FFolders.Find(id);
            if (file == null) throw new KeyNotFoundException("ffile not found");
            return file;
        }

    }
}
