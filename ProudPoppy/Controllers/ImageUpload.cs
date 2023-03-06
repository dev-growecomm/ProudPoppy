using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using ProudPoppy.Models;
using ShopifySharp;
using System.Data;
using System.Net;

namespace ProudPoppy.Controllers
{
    public class ImageUpload : Controller
    {
        IConfiguration _configuration;

        public ImageUpload(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: ImageUpload
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadImages(ImageDetails postedFile)
        {
            try
            {
                if (postedFile.ImageFile != null)
                {
                    string bucketName = _configuration.GetSection("AWS:bucketName").Value;
                    string awsAccessKey = _configuration.GetSection("AWS:awsAccessKey").Value;
                    string awsSecretKey = _configuration.GetSection("AWS:awsSecretKey").Value;
                    string awsRegion = _configuration.GetSection("AWS:awsRegion").Value;
                    string prefix = _configuration.GetSection("AWS:prefix").Value;

                    var region = RegionEndpoint.GetBySystemName(awsRegion);
                    IAmazonS3 _s3Client = new AmazonS3Client(awsAccessKey, awsSecretKey, region);

                    var bucketExists = await _s3Client.DoesS3BucketExistAsync(bucketName);
                    if (!bucketExists)
                    {
                        ViewBag.ErrorMessage = $"Bucket name <b>{bucketName}</b> does not exist.";
                        return View("Index");
                    }
                    foreach (var item in postedFile.ImageFile)
                    {
                        var request = new PutObjectRequest()
                        {
                            BucketName = bucketName,
                            Key = string.IsNullOrEmpty(prefix) ? $"{item.FileName}" : $"{prefix?.TrimEnd('/')}/{item.FileName}",
                            InputStream = item.OpenReadStream()
                        };
                        request.Metadata.Add("Content-Type", item.ContentType);
                        var result = await _s3Client.PutObjectAsync(request);

                        if (result.HttpStatusCode == HttpStatusCode.OK)
                        {
                            var imageUrl = $"https://{bucketName}.s3.{awsRegion}.amazonaws.com/{prefix?.TrimEnd('/')}/{item.FileName}";

                            var fileNameSplit = item.FileName.Split("_");
                            string sku = fileNameSplit[0];
                            string variantColour = fileNameSplit[1];
                            string position = fileNameSplit[2].Split(".")[0];

                            var productId = new object();

                            var ConnectionString = _configuration.GetConnectionString("ShopifyProductUploadDbContext");
                            using (MySqlConnection mConnection = new MySqlConnection(ConnectionString))
                            {
                                string query = $"Select ProductId from product_details where SKU='{sku}'";
                                mConnection.Open();
                                using (MySqlCommand myCmd = new MySqlCommand(query, mConnection))
                                {
                                    myCmd.CommandType = CommandType.Text;
                                    productId = myCmd.ExecuteScalar();
                                }
                            }

                            string shopifyUrl = _configuration.GetSection("sopify:shopifyUrl").Value;
                            string shopAccessToken = _configuration.GetSection("sopify:shopAccessToken").Value;
                            if (productId != null && !string.IsNullOrEmpty(productId.ToString()))
                            {
                                var productImageService = new ProductImageService(shopifyUrl, shopAccessToken);

                                long productIdToPass = Convert.ToInt64(productId);
                                var image = await productImageService.CreateAsync(productIdToPass, new ProductImage()
                                {
                                    Metafields = new List<MetaField>()
                                {
                                    new MetaField()
                                    {
                                        Key = "alt",
                                        Value = item.FileName,
                                        Type = "string",
                                        Namespace = "tags"
                                    }
                                },
                                    Src = imageUrl,
                                    Alt = item.FileName,
                                    Position = Convert.ToInt32(position)
                                });

                                if (Convert.ToInt32(position) == 1)
                                {
                                    var variantIds = new object();
                                    using (MySqlConnection mConnection = new MySqlConnection(ConnectionString))
                                    {
                                        string queryToGetVariantIds = $"Select VariantIds from product_details where SKU='{sku}'";
                                        mConnection.Open();
                                        using (MySqlCommand myCmd = new MySqlCommand(queryToGetVariantIds, mConnection))
                                        {
                                            myCmd.CommandType = CommandType.Text;
                                            variantIds = myCmd.ExecuteScalar();
                                        }
                                    }
                                    if (variantIds != null && !string.IsNullOrEmpty(variantIds.ToString()))
                                    {
                                        string[] variantIdList = variantIds.ToString().Split(",");

                                        foreach (var variantId in variantIdList)
                                        {
                                            var variant = variantId.Split("_");
                                            string variantIdName = variant[0];
                                            string variantColourName = variant[1];

                                            if (variantColourName == variantColour)
                                            {
                                                var productVariantService = new ProductVariantService(shopifyUrl, shopAccessToken);
                                                var variantDetails = await productVariantService.UpdateAsync(Convert.ToInt64(variantIdName), new ProductVariant()
                                                {
                                                    ImageId = image.Id
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (result.HttpStatusCode == HttpStatusCode.OK)
                            ViewBag.SuccessMessage += string.Format("<b>{0}</b> uploaded.<br />", item.FileName);
                        else
                            ViewBag.ErrorMessage += string.Format("<b>{0}</b> failed to upload.<br />", item.FileName);
                    }

                    return View("Index");
                }

                return View("Index");
            }
            catch (Exception ex)
            {

                throw ex.InnerException;
            }

        }

        // GET: ImageUpload/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }
    }
}
