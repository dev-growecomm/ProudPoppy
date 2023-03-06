using Newtonsoft.Json;

namespace ProudPoppy.Models
{
    public class Inventory
    {
        [JsonProperty("inventory_item")]
        public InventoryItems InventoryItems { get; set; }
    }

    public class InventoryItems
    {
        public long id { get; set; }
        public string sku { get; set; }   
        public decimal? cost { get; set; }
        public bool tracked { get; set; }
    }
}   
