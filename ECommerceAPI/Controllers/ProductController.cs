using ECommerceAPI.Interface;
using ECommerceAPI.Models;
using ECommerceAPI.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace ECommerceAPI.Controllers
{
    [Route("admin/v1/product")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProduct _product;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductController(IProduct product, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _product = product;
            _env = env;
            _httpContextAccessor = httpContextAccessor;

        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all products.")]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _product.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get product & productimage details by Id.")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int? productsImageId)
        {
            var product = await _product.GetProductByIdAsync(id, productsImageId);

            if (product == null || (productsImageId.HasValue && product.ProductImages.Count == 0))
            {
                return NotFound(new { message = "Product or product image not found" });
            }

            return Ok(product);
        }

        [HttpPost("create")]
        [SwaggerOperation(Summary = "Save products & productsImage.")]
        public async Task<IActionResult> Create([FromForm] string productList, IFormFile largeImageFile, IFormFile mediumImageFile, IFormFile smallImageFile, [FromForm] string userId)
        {
            if (string.IsNullOrWhiteSpace(productList))
                return BadRequest("Product list is required.");

            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("User not logged in.");

            List<Products> products;
            try
            {
                products = JsonConvert.DeserializeObject<List<Products>>(productList);
                if (products == null || !products.Any())
                    return BadRequest("Product list is empty.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Invalid product list format.", details = ex.Message });
            }

            try
            {
                var productId = await _product.CreateProductsAsync(products, largeImageFile, mediumImageFile, smallImageFile, userId);

                if (productId > 0)
                    return Ok(new { success = true, productId });
                else
                    return BadRequest(new { success = false, message = "No products were created." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal error", details = ex.Message });
            }
        }


        [HttpPost("update")]
        [SwaggerOperation(Summary = "Update products & product images.")]
        public async Task<IActionResult> Update([FromForm] string productList, IFormFile largeImageFile, IFormFile mediumImageFile, IFormFile smallImageFile, [FromForm] string userId)
        {
            if (string.IsNullOrWhiteSpace(productList))
                return BadRequest("Product list is required.");

            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("User not logged in.");

            List<Products> products;
            try
            {
                products = JsonConvert.DeserializeObject<List<Products>>(productList);
                if (products == null || !products.Any())
                    return BadRequest("Product list is empty.");              
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Invalid product list format.", details = ex.Message });
            }

            try
            {
                var updatedProductIds = await _product.UpdateProductsAsync(products, largeImageFile, mediumImageFile, smallImageFile, userId);

                if (updatedProductIds.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Products updated.",
                        productId = updatedProductIds.First() // Send the first one, or all as needed
                    });
                }
                else
                {
                    return NotFound(new { success = false, message = "No products updated." });
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal error", details = ex.Message });
            }
        }



        [HttpPost("delete")]
        [SwaggerOperation(Summary = "Delete product by productid & productsimageid.")]
        public async Task<IActionResult> DeleteProductImage([FromQuery] int id, [FromQuery] int productsImageId)
        {
            try
            {
                var result = await _product.DeleteProductImageAsync(id, productsImageId, _env.WebRootPath);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

    }
}