using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace ProudPoppy.Models
{
    public class ImageDetails
    {
        public int id { get; set; }

        public string ImageName { get; set; }

        public List<IFormFile> ImageFile { get; set; }
    }
}
