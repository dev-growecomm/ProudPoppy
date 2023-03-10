using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProudPoppy.Data;
using ProudPoppy.Models;
using ShopifySharp;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ProudPoppy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ProudPoppyContext _context;
        private readonly IConfiguration _configuration;

        public ProductController(ProudPoppyContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/<ProductController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<ProductController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ProductController>
        [HttpPost]
        public async Task<bool> AddProduct([FromBody] Product product)
        {
            var isProductExist = _context.ProductDetails.Any(e => e.ProductId == product.Id);
            if (!isProductExist)
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

                var productDetails = new ProductDetails
                {
                    ProductId = product.Id.Value,
                    VariantIds = variantIds,
                    SKU = product.Variants.Any() ? product.Variants.First().SKU : null,
                    Name = product.Title,
                    Description = product.BodyHtml,
                    Brand = product.Vendor,
                    Category = product.ProductType,
                    Tags = product.Tags,
                    SalePrice = product.Variants.Any() ? product.Variants.First().Price.ToString() : null,
                    CostPrice = product.Variants.Any() ? product.Variants.First().Price.ToString() : null,
                    RRP = product.Variants.Any() ? product.Variants.First().CompareAtPrice.ToString() : null,
                    Size = sizes,
                    Colour = colours.Any() ? string.Join(",", colours) : null,
                    Status = product.Status,
                    DateCreated = DateTime.Now.ToString(),
                    DateLastModified = DateTime.Now.ToString(),
                };
                _context.Add(productDetails);

                return await _context.SaveChangesAsync() > 0;
            }

            return false;
        }

        // PUT api/<ProductController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ProductController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
