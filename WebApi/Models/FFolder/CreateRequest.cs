using System.ComponentModel.DataAnnotations;
using WebApi.Entities;

namespace WebApi.Models.FFolder
{
    public class CreateRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [Required]
        public string Path { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
        public List<FFile> Files { get; set; }
    }
}
