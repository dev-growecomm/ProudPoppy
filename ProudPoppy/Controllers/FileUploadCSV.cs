using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using ShopifySharp;
using System.Data;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using System.Reflection;
using ProudPoppy.Models;
using Microsoft.AspNetCore.Authorization;
using ProudPoppy.Data;
using Microsoft.CodeAnalysis;

namespace ProudPoppy.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FileUploadCSV : Controller
    {
        IConfiguration _configuration;
        private readonly ProudPoppyContext _context;

        public FileUploadCSV(IConfiguration configuration, ProudPoppyContext context)
        {
            _configuration = configuration;
            _context = context;
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

                    using (var reader = new StreamReader(filePath, encoding: Encoding.Latin1, false))
                    {
                        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                        {
                            fileIngests = csv.GetRecords<ProductIngestCsv>().ToList();
                        }
                    }

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



                foreach (var item in uploadDataList)
                {
                    var productDetails = CheckIsRecordExist(item);

                    if (productDetails != null && productDetails.ProductId > 0)
                    {
                        var variants = new List<ProductVariant>();
                        var productOptions = new List<ProductOption>();

                        if (!string.IsNullOrWhiteSpace(item.Size))
                        {
                            //foreach (var colour in item.Colour.Split(","))
                            //{
                            foreach (var size in item.Size.Split(","))
                            {
                                variants.Add(new ProductVariant
                                {
                                    SKU = item.SKU,
                                    Price = string.IsNullOrEmpty(item.SalePrice) ? null : Convert.ToDecimal(item.SalePrice),
                                    CompareAtPrice = string.IsNullOrEmpty(item.RRP) ? null : Convert.ToDecimal(item.RRP),
                                    Option1 = size,
                                    //Option2 = colour,
                                });
                            }
                            //}

                            productOptions.Add(new ProductOption
                            {
                                Name = "Size",
                                Values = item.Size.Split(','),
                                Position = 1
                            });

                            //productOptions.Add(new ProductOption
                            //{
                            //    Name = "Color",
                            //    Values = item.Colour.Split(','),
                            //    Position = 2
                            //});
                        }
                        var product = await service.GetAsync(productDetails.ProductId);

                        if (!string.IsNullOrWhiteSpace(item.Name))
                            product.Title = item.Name;

                        if (!string.IsNullOrWhiteSpace(item.Description))
                        {
                            StringBuilder stringBuilder = new StringBuilder();
                            stringBuilder.Append(item.Description);
                            stringBuilder.Append(product.BodyHtml);
                            product.BodyHtml = stringBuilder.ToString();
                        }

                        if (!string.IsNullOrWhiteSpace(item.Brand))
                            product.Vendor = item.Brand;

                        if (!string.IsNullOrWhiteSpace(item.Category))
                            product.ProductType = item.Category;

                        if (!string.IsNullOrWhiteSpace(item.Tags))
                            product.Tags = item.Tags;

                        if (!string.IsNullOrWhiteSpace(item.Status.ToLower()))
                            product.Status = item.Status.ToLower();

                        if (variants.Any())
                        {
                            var existingVariants = product.Variants;
                            variants.AddRange(existingVariants);
                            product.Variants = variants;
                        }

                        if (productOptions.Any())
                            product.Options = productOptions;

                        product.PublishedScope = "global";

                        product = await service.UpdateAsync(productDetails.ProductId, product);

                        if (!string.IsNullOrWhiteSpace(item.CostPrice))
                        {
                            var inventory = new Inventory();

                            foreach (var variant in product.Variants)
                            {
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
                        }

                        await UpdateRecordInDb(productDetails, product, item.CostPrice);
                    }
                }

                return View("Index");
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }

        }

        private ProductDetails CheckIsRecordExist(ProductIngestCsv item)
        {
            ProductDetails productDetails = _context.ProductDetails.FirstOrDefault(e => e.SKU.Contains(item.SKU));
            return productDetails;
        }

        private async Task UpdateRecordInDb(ProductDetails productDetails, Product product, string costPrice)
        {
            string variantIds = string.Empty;
            string sizes = string.Empty;

            List<string> colours = new List<string>();

            if (product.Variants.Any())
            {
                variantIds = string.Join(",", product.Variants.Select(x => x.Id.Value).ToArray());
                sizes = string.Join(",", product.Variants.Select(x => x.Option1).ToArray());

                foreach (var item in product.Variants)
                {
                    if (item.Option2 != null)
                    {
                        colours.Add(item.Option2);
                    }
                }
            }

            productDetails.VariantIds = variantIds;
            productDetails.SKU = product.Variants.Any() ? product.Variants.First().SKU : null;
            productDetails.Name = product.Title;
            productDetails.Description = product.BodyHtml;
            productDetails.Brand = product.Vendor;
            productDetails.Category = product.ProductType;
            productDetails.Tags = product.Tags;
            productDetails.SalePrice = product.Variants.Any() ? product.Variants.First().Price.ToString() : null;
            productDetails.CostPrice = costPrice;
            productDetails.RRP = product.Variants.Any() ? product.Variants.First().CompareAtPrice.ToString() : null;
            productDetails.Size = sizes;
            productDetails.Colour = colours.Any() ? string.Join(",", colours) : null;
            productDetails.Status = product.Status;
            productDetails.DateLastModified = DateTime.Now.ToString();

            _context.Update(productDetails);
            await _context.SaveChangesAsync();
        }
    }
}
