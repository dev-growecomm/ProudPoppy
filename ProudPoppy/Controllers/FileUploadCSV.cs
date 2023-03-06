using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using ShopifySharp;
using System.Data;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using System.Web;
using System.Reflection;
using ProudPoppy.Models;

namespace ProudPoppy.Controllers
{
    public class FileUploadCSV : Controller
    {
        IConfiguration _configuration;

        public FileUploadCSV(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UploadCSV(IFormFile postedFile)
        {
            if (postedFile != null)
            {
                if (postedFile.FileName.EndsWith(".csv"))
                {
                    string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FileUpload");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    string fileName = Path.GetFileName(postedFile.FileName);
                    string filePath = Path.Combine(path, fileName);
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        postedFile.CopyTo(stream);
                    }

                    List<ProductIngestCsv> fileIngests = new List<ProductIngestCsv>();

                    using (var reader = new StreamReader(filePath, encoding: Encoding.Latin1, true))
                    {
                        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                        {
                            fileIngests = csv.GetRecords<ProductIngestCsv>().ToList();
                        }
                    }

                    //foreach (var item in fileIngests)
                    //{
                    //    item.Description = HttpUtility.HtmlDecode(item.Description);
                    //}
                    return View("Index", fileIngests);
                }
                else
                {
                    ViewBag.ErrorMessage += string.Format("<b>{0}</b> file format is not supported. Please upload valid CSV file.<br />", postedFile.FileName);
                }
                
            }

            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UploadToShopify([FromBody] List<ProductIngestCsv> uploadDataList)
        {
            try
            {
                string shopifyUrl = _configuration.GetSection("sopify:shopifyUrl").Value;
                string shopAccessToken = _configuration.GetSection("sopify:shopAccessToken").Value;

                var service = new ProductService(shopifyUrl, shopAccessToken);

                //var productDetails = await service.GetAsync(7887331098860);

                foreach (var item in uploadDataList)
                {
                    bool isRecordPresent = CheckIsRecordExist(item);

                    if (!isRecordPresent)
                    {
                        var variants = new List<ProductVariant>();

                        foreach (var colour in item.Colour.Split(","))
                        {
                            foreach (var size in item.Size.Split(","))
                            {
                                variants.Add(new ProductVariant
                                {
                                    SKU = item.SKU,
                                    Price = string.IsNullOrEmpty(item.SalePrice) ? null : Convert.ToDecimal(item.SalePrice),
                                    CompareAtPrice = string.IsNullOrEmpty(item.RRP) ? null : Convert.ToDecimal(item.RRP),
                                    Option1 = colour,
                                    Option2 = size,
                                });
                            }
                        }

                        var productOptions = new List<ProductOption>();

                        productOptions.Add(new ProductOption
                        {
                            Name = "Colour",
                            Values = item.Colour.Split(','),
                            Position = 1
                        });

                        productOptions.Add(new ProductOption
                        {
                            Name = "Size",
                            Values = item.Size.Split(','),
                            Position = 2
                        });

                        var product = new Product()
                        {
                            Title = item.Name,
                            BodyHtml = HttpUtility.HtmlEncode(item.Description),
                            Vendor = item.Brand,
                            ProductType = item.Category,
                            Tags = item.Tags,
                            Status = item.Status.ToLower(),
                            Variants = variants,
                            Options = productOptions,
                            PublishedScope = "global"
                        };

                        product = await service.CreateAsync(product);

                        var inventory = new Inventory();

                        List<string> variantIds = new List<string>();
                        foreach (var variant in product.Variants)
                        {
                            variantIds.Add($"{variant.Id.Value}_{variant.Option1}");
                            inventory.InventoryItems = new InventoryItems
                            {
                                id = variant.InventoryItemId.Value,
                                sku = variant.SKU,
                                cost = Convert.ToDecimal(item.CostPrice),
                                tracked = true
                            };

                            string jsonData = JsonConvert.SerializeObject(inventory);

                            var request = new RestRequest($"{shopifyUrl}/admin/api/2023-01/inventory_items/{inventory.InventoryItems.id}.json", Method.Put)
                            .AddHeader("X-Shopify-Access-Token", shopAccessToken)
                                             .AddJsonBody(jsonData);

                            var _restClient = new RestClient().AddDefaultHeader("Content-Type", "application/json");

                            var response = _restClient.Put<dynamic>(request);
                        }

                        SaveRecordInDb(item, product.Id.Value, variantIds);
                    }
                }

                return View("Index");
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }

        }

        private bool CheckIsRecordExist(ProductIngestCsv item)
        {
            bool isRecordPresent = false;
            var ConnectionString = _configuration.GetConnectionString("ShopifyProductUploadDbContext");
            using (MySqlConnection mConnection = new MySqlConnection(ConnectionString))
            {
                mConnection.Open();
                using (MySqlCommand myCmd = new MySqlCommand($"Select id from product_details where `SKU`='{item.SKU}'", mConnection))
                {
                    myCmd.CommandType = CommandType.Text;
                    var result = myCmd.ExecuteReader();
                    if (result.HasRows)
                    {
                        isRecordPresent = true;
                    }
                }
            }
            return isRecordPresent;
        }

        private void SaveRecordInDb(ProductIngestCsv item, long productId, List<string> variantIds)
        {
            List<string> Rows = new List<string>();
            Rows.Add(string.Format("'{0}','{1}','{2}','{3}','{4}','{5}'", MySqlHelper.EscapeString(item.SKU),
                MySqlHelper.EscapeString(item.Name), MySqlHelper.EscapeString(item.Description), MySqlHelper.EscapeString(item.Brand),
                MySqlHelper.EscapeString(item.Category), MySqlHelper.EscapeString(item.Tags)));

            Rows.Add(string.Format("'{0}','{1}','{2}','{3}','{4}','{5}'", MySqlHelper.EscapeString(item.SalePrice),
                MySqlHelper.EscapeString(item.CostPrice), MySqlHelper.EscapeString(item.CostPrice), MySqlHelper.EscapeString(item.Size),
                MySqlHelper.EscapeString(item.Colour), MySqlHelper.EscapeString(item.Status)));

            Rows.Add(string.Format("'{0}','{1}','{2}', '{3}'", MySqlHelper.EscapeString(DateTime.Now.ToString()),
                MySqlHelper.EscapeString(DateTime.Now.ToString()), productId, string.Join(", ", variantIds)));

            StringBuilder sCommand = new StringBuilder("INSERT INTO product_details (`SKU`, `Name`, `Description`, " +
                $"`Brand`, `Category`, `Tags`, `SalePrice`, `CostPrice`, `RRP`, `Size`, `Colour`, `Status`, `DateCreated`, `DateLastModified`, `ProductId`, `VariantIds`) VALUES (");

            var ConnectionString = _configuration.GetConnectionString("ShopifyProductUploadDbContext");
            using (MySqlConnection mConnection = new MySqlConnection(ConnectionString))
            {
                sCommand.Append(string.Join(",", Rows));
                sCommand.Append(");");
                mConnection.Open();
                using (MySqlCommand myCmd = new MySqlCommand(sCommand.ToString(), mConnection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
