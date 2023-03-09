using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProudPoppy.Models
{
    [Table("product_details")]
    public class ProductDetails
    {
        [Key]
        public new int id { get; set; }
        public long ProductId { get; set; }
        public string VariantIds { get; set; }
        public string SKU { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }
        public string SalePrice { get; set; }
        public string CostPrice { get; set; }
        public string RRP { get; set; }
        public string Size { get; set; }
        public string Colour { get; set; }
        public string Status { get; set; }
        public string DateCreated { get; set; }
        public string DateLastModified { get; set; }
    }
}
