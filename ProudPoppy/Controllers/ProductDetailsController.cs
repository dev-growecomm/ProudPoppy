using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProudPoppy.Data;
using ProudPoppy.Models;
using ShopifySharp;
using ShopifySharp.Filters;
using ShopifySharp.Lists;

namespace ProudPoppy.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductDetailsController : Controller
    {
        private readonly ProudPoppyContext _context;
        private readonly IConfiguration _configuration;

        public ProductDetailsController(ProudPoppyContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: ProductDetails
        public async Task<IActionResult> Index()
        {
            return View(await _context.ProductDetails.ToListAsync());
        }

        // GET: ProductDetails/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.ProductDetails == null)
            {
                return NotFound();
            }

            var productDetails = await _context.ProductDetails
                .FirstOrDefaultAsync(m => m.id == id);
            if (productDetails == null)
            {
                return NotFound();
            }

            return View(productDetails);
        }

        // GET: ProductDetails/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ProductDetails/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,ProductId,VariantIds,SKU,Name,Description,Brand,Category,Tags,SalePrice,CostPrice,RRP,Size,Colour,Status")] ProductDetails productDetails)
        {
            if (ModelState.IsValid)
            {
                _context.Add(productDetails);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(productDetails);
        }

        // GET: ProductDetails/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.ProductDetails == null)
            {
                return NotFound();
            }

            var productDetails = await _context.ProductDetails.FindAsync(id);
            if (productDetails == null)
            {
                return NotFound();
            }
            return View(productDetails);
        }

        // POST: ProductDetails/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,ProductId,VariantIds,SKU,Name,Description,Brand,Category,Tags,SalePrice,CostPrice,RRP,Size,Colour,Status")] ProductDetails productDetails)
        {
            if (id != productDetails.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productDetails);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductDetailsExists(productDetails.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(productDetails);
        }

        // GET: ProductDetails/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.ProductDetails == null)
            {
                return NotFound();
            }

            var productDetails = await _context.ProductDetails
                .FirstOrDefaultAsync(m => m.id == id);
            if (productDetails == null)
            {
                return NotFound();
            }

            return View(productDetails);
        }

        // POST: ProductDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.ProductDetails == null)
            {
                return Problem("Entity set 'ProudPoppyContext.ProductDetails'  is null.");
            }
            var productDetails = await _context.ProductDetails.FindAsync(id);
            if (productDetails != null)
            {
                _context.ProductDetails.Remove(productDetails);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductDetailsExists(int id)
        {
            return _context.ProductDetails.Any(e => e.id == id);
        }

        public async Task<IActionResult> ExistingProducts()
        {
            return View("ExistingProducts");
        }

        [HttpPost]
        public async Task<IActionResult> SaveExistingProducts(string status)
        {
            string shopifyUrl = _configuration.GetSection("sopify:shopifyUrl").Value;
            string shopAccessToken = _configuration.GetSection("sopify:shopAccessToken").Value;

            var productService = new ProductService(shopifyUrl, shopAccessToken);

            var filter = new ProductListFilter()
            {
                Status = status,
                Limit = 250
            };

            var products = await productService.ListAsync(filter);

            if (products != null)
            {

                await SaveProducts(products);

                if (products.HasNextPage)
                {
                    var nextFilter = products.GetNextPageFilter(250);
                    var nextProducts = await productService.ListAsync(nextFilter);
                    await SaveProducts(nextProducts);

                    if (nextProducts.HasNextPage)
                    {
                        var nextToNextFilter = nextProducts.GetNextPageFilter(250);
                        var nextToNextProducts = await productService.ListAsync(nextToNextFilter);
                        await SaveProducts(nextToNextProducts);

                        if (nextToNextProducts.HasNextPage)
                        {
                            var nextToNextToNextFilter = nextToNextProducts.GetNextPageFilter(250);
                            var nextToNextToNextProducts = await productService.ListAsync(nextToNextToNextFilter);
                            await SaveProducts(nextToNextToNextProducts);

                            if (nextToNextToNextProducts.HasNextPage)
                            {
                                var next5Filter = nextToNextToNextProducts.GetNextPageFilter(250);
                                var next5Products = await productService.ListAsync(next5Filter);
                                await SaveProducts(next5Products);

                                if (next5Products.HasNextPage)
                                {
                                    var next6Filter = next5Products.GetNextPageFilter(250);
                                    var next6Products = await productService.ListAsync(next6Filter);
                                    await SaveProducts(next6Products);

                                    if (next6Products.HasNextPage)
                                    {
                                        var next7Filter = next6Products.GetNextPageFilter(250);
                                        var next7Products = await productService.ListAsync(next7Filter);
                                        await SaveProducts(next7Products);

                                        if (next7Products.HasNextPage)
                                        {
                                            var next8Filter = next7Products.GetNextPageFilter(250);
                                            var next8Products = await productService.ListAsync(next8Filter);
                                            await SaveProducts(next8Products);
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }

            return View("ExistingProducts");
        }

        private async Task SaveProducts(ListResult<Product> products)
        {
            foreach (var product in products.Items)
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
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
