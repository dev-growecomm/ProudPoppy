using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProudPoppy.Models
{
    [Table("user")]
    public class User
    {
        [Key]
        public string username { get; set; }
        public string password { get; set; }
    }
}
