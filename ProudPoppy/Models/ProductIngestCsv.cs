using CsvHelper.Configuration.Attributes;
using FileHelpers;

namespace ProudPoppy.Models
{
    [DelimitedRecord(",")]
    [IgnoreFirst()]
    public class ProductIngestCsv
    {
        public string SKU { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }

        [Name("Sale Price")]
        public string SalePrice { get; set; }
        [Name("Cost Price")]
        public string CostPrice { get; set; }
        public string RRP { get; set; }
        public string Size { get; set; }
        public string Colour { get; set; }
        public string Status { get; set; }
    }

    public class UploadData
    {
        public string SKU { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }

        [Name("Sale Price")]
        public string SalePrice { get; set; }
        [Name("Cost Price")]
        public string CostPrice { get; set; }
        public string RRP { get; set; }
        public string Size { get; set; }
        public string Colour { get; set; }
        public string Status { get; set; }
    }
}
