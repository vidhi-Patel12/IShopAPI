using ECommerceAPI.Interface;
using ECommerceAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Logging;
using System.Data;

namespace ECommerceAPI.Repository
{
    public class ProductRepository : IProduct
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        public ProductRepository(IConfiguration config, IWebHostEnvironment env)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _env = env;
        }

        public async Task<List<Products>> GetAllProductsAsync()
        {
            var productsList = new List<Products>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetAllProducts", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        Dictionary<int, Products> productDict = new Dictionary<int, Products>();

                        while (await reader.ReadAsync())
                        {
                            int productId = reader.GetInt32(reader.GetOrdinal("ProductId"));

                            if (!productDict.ContainsKey(productId))
                            {
                                productDict[productId] = new Products
                                {
                                    ProductId = productId,
                                    Name = reader.GetString(reader.GetOrdinal("Name")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                    ProductImages = new List<ProductsImage>()
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("ProductsImageId")))
                            {
                                productDict[productId].ProductImages.Add(new ProductsImage
                                {
                                    ProductsImageId = reader.GetInt32(reader.GetOrdinal("ProductsImageId")),
                                    ProductId = productId,
                                    Type = reader.GetString(reader.GetOrdinal("Type")),
                                    Color = reader.GetString(reader.GetOrdinal("Color")),
                                    LargeImage = reader["LargeImage"].ToString(),
                                    MediumImage = reader["MediumImage"].ToString(),
                                    SmallImage = reader["SmallImage"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Quantity = Convert.ToDouble(reader["Quantity"]),
                                    MRP = Convert.ToDouble(reader["MRP"]),
                                    Discount = Convert.ToInt32(reader["Discount"]),
                                    Price = Convert.ToDouble(reader["Price"]),
                                    ArrivingDays = Convert.ToInt32(reader["ArrivingDays"]),
                                    IsActive = Convert.ToBoolean(reader["ImageIsActive"])
                                });
                            }
                        }

                        productsList = productDict.Values.ToList();
                    }
                }
            }

            return productsList;
        }

        public async Task<Products> GetProductByIdAsync(int productId, int? productsImageId = null)
        {
            Products product = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetProductById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductId", productId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        Dictionary<int, Products> productDict = new Dictionary<int, Products>();

                        while (await reader.ReadAsync())
                        {
                            int pid = reader.GetInt32(reader.GetOrdinal("ProductId"));

                            if (!productDict.ContainsKey(pid))
                            {
                                productDict[pid] = new Products
                                {
                                    ProductId = pid,
                                    Name = reader.GetString(reader.GetOrdinal("Name")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                    ProductImages = new List<ProductsImage>()
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("ProductsImageId")))
                            {
                                var image = new ProductsImage
                                {
                                    ProductsImageId = reader.GetInt32(reader.GetOrdinal("ProductsImageId")),
                                    ProductId = pid,
                                    Type = reader.GetString(reader.GetOrdinal("Type")),
                                    Color = reader.GetString(reader.GetOrdinal("Color")),
                                    LargeImage = reader.GetString(reader.GetOrdinal("LargeImage")),
                                    MediumImage = reader.GetString(reader.GetOrdinal("MediumImage")),
                                    SmallImage = reader.GetString(reader.GetOrdinal("SmallImage")),
                                    Description = reader.GetString(reader.GetOrdinal("Description")),
                                    Quantity = reader.GetDouble(reader.GetOrdinal("Quantity")),
                                    MRP = reader.GetDouble(reader.GetOrdinal("MRP")),
                                    Discount = reader.GetInt32(reader.GetOrdinal("Discount")),
                                    Price = reader.GetDouble(reader.GetOrdinal("Price")),
                                    ArrivingDays = reader.GetInt32(reader.GetOrdinal("ArrivingDays")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("ImageIsActive"))
                                };

                                productDict[pid].ProductImages.Add(image);
                            }
                        }

                        product = productDict.Values.FirstOrDefault();

                        if (product != null && productsImageId.HasValue)
                        {
                            product.ProductImages = product.ProductImages
                                .Where(img => img.ProductsImageId == productsImageId.Value)
                                .ToList();
                        }
                    }
                }
            }

            return product;
        }


        public async Task<int> CreateProductsAsync(List<Products> productList, IFormFile largeImageFile, IFormFile mediumImageFile, IFormFile smallImageFile, string userId)
        {
            foreach (var model in productList)
            {
                int productId = 0;

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string slug = SlugHelper.GenerateSlug(model.Name);
                    string typeSlug = SlugHelper.GenerateSlug(model.Type);
                    string colorSlug = SlugHelper.GenerateSlug(model.Color);

                    using (SqlCommand checkCmd = new SqlCommand("SELECT ProductId FROM Products WHERE Name = @Name", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Name", model.Name);
                        object result = await checkCmd.ExecuteScalarAsync();
                        productId = result != null ? Convert.ToInt32(result) : 0;
                    }

                    if (productId == 0)
                    {
                        // Insert the product into the database if it doesn't exist yet
                        using (SqlCommand cmd = new SqlCommand("AddProduct", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Name", model.Name);
                            cmd.Parameters.AddWithValue("@Slug", slug);
                            cmd.Parameters.AddWithValue("@IsActive", true);
                            cmd.Parameters.AddWithValue("@CreatedBy", userId);
                            cmd.Parameters.AddWithValue("@CreatedDateTime", DateTime.Now);
                            cmd.Parameters.AddWithValue("@UpdatedBy", DBNull.Value);
                            cmd.Parameters.AddWithValue("@UpdatedDateTime", DBNull.Value);

                            SqlParameter outputIdParam = new SqlParameter("@NewProductId", SqlDbType.Int)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(outputIdParam);

                            await cmd.ExecuteNonQueryAsync();
                            productId = (int)outputIdParam.Value;
                        }
                    }

                    // Save images and product info
                    string basePath = Path.Combine(_env.WebRootPath, "uploads", "Products", productId.ToString());

                    // Ensure folders exist and save files
                    async Task SaveImage(string subfolder, string filename, IFormFile file)
                    {
                        if (file != null && !string.IsNullOrEmpty(filename))
                        {
                            string dir = Path.Combine(basePath, subfolder);
                            Directory.CreateDirectory(dir);

                            string filePath = Path.Combine(dir, filename);
                            using var stream = new FileStream(filePath, FileMode.Create);
                            await file.CopyToAsync(stream);
                        }
                    }

                    await SaveImage("Large", model.LargeImage, largeImageFile);
                    await SaveImage("Medium", model.MediumImage, mediumImageFile);
                    await SaveImage("Small", model.SmallImage, smallImageFile);

                    using (SqlCommand cmd = new SqlCommand("AddProductImage", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.Parameters.AddWithValue("@Type", model.Type);
                        cmd.Parameters.AddWithValue("@Color", model.Color);
                        cmd.Parameters.AddWithValue("@LargeImage", $"/uploads/Products/{productId}/Large/{Path.GetFileName(model.LargeImage)}");
                        cmd.Parameters.AddWithValue("@MediumImage", $"/uploads/Products/{productId}/Medium/{Path.GetFileName(model.MediumImage)}");
                        cmd.Parameters.AddWithValue("@SmallImage", $"/uploads/Products/{productId}/Small/{Path.GetFileName(model.SmallImage)}");
                        cmd.Parameters.AddWithValue("@Description", model.Description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Quantity", model.Quantity);
                        cmd.Parameters.AddWithValue("@MRP", model.MRP);
                        cmd.Parameters.AddWithValue("@Discount", model.Discount);
                        cmd.Parameters.AddWithValue("@Price", model.MRP - (model.MRP * model.Discount / 100));
                        cmd.Parameters.AddWithValue("@ArrivingDays", model.ArrivingDays);
                        cmd.Parameters.AddWithValue("@TypeSlug", typeSlug);
                        cmd.Parameters.AddWithValue("@ColorSlug", colorSlug);
                        cmd.Parameters.AddWithValue("@IsActive", true);
                        cmd.Parameters.AddWithValue("@CreatedBy", userId);
                        cmd.Parameters.AddWithValue("@CreatedDateTime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@UpdatedBy", DBNull.Value);
                        cmd.Parameters.AddWithValue("@UpdatedDateTime", DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return productId;  // Return the productId after creating the product.
            }

            return 0; // If no product was created, return 0
        }

        public async Task<List<int>> UpdateProductsAsync(List<Products> productList, IFormFile largeImageFile, IFormFile mediumImageFile, IFormFile smallImageFile, string userId)
        {
            List<int> updatedProductIds = new();

            foreach (var model in productList)
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string slug = SlugHelper.GenerateSlug(model.Name);
                    string typeSlug = SlugHelper.GenerateSlug(model.Type);
                    string colorSlug = SlugHelper.GenerateSlug(model.Color);

                    using (SqlCommand cmd = new SqlCommand(@"
                UPDATE Products 
                SET 
                    Name = @Name,
                    Slug = @Slug,
                    IsActive = @IsActive,
                    UpdatedBy = @UpdatedBy,
                    UpdatedDateTime = @UpdatedDateTime 
                WHERE ProductId = @ProductId", conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                        cmd.Parameters.AddWithValue("@Name", model.Name);
                        cmd.Parameters.AddWithValue("@Slug", slug);
                        cmd.Parameters.AddWithValue("@IsActive", true);
                        cmd.Parameters.AddWithValue("@UpdatedBy", userId);
                        cmd.Parameters.AddWithValue("@UpdatedDateTime", DateTime.Now);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    string basePath = Path.Combine(_env.WebRootPath, "uploads", "Products", model.ProductId.ToString());

                    async Task SaveImage(string subfolder, string filename, IFormFile file)
                    {
                        if (file != null && !string.IsNullOrEmpty(filename))
                        {
                            string dir = Path.Combine(basePath, subfolder);
                            Directory.CreateDirectory(dir);

                            string filePath = Path.Combine(dir, filename);
                            using var stream = new FileStream(filePath, FileMode.Create);
                            await file.CopyToAsync(stream);
                        }
                    }

                    await SaveImage("Large", model.LargeImage, largeImageFile);
                    await SaveImage("Medium", model.MediumImage, mediumImageFile);
                    await SaveImage("Small", model.SmallImage, smallImageFile);

                    using (SqlCommand cmd = new SqlCommand(@"UPDATE ProductsImage SET Type = @Type,Color = @Color,LargeImage = @LargeImage,MediumImage = @MediumImage,SmallImage = @SmallImage,
                    Description = @Description,Quantity = @Quantity,MRP = @MRP,Discount = @Discount,Price = @Price,ArrivingDays = @ArrivingDays,TypeSlug = @TypeSlug,ColorSlug = @ColorSlug,
                    IsActive = @IsActive,UpdatedBy = @UpdatedBy,UpdatedDateTime = @UpdatedDateTime WHERE ProductId = @ProductId AND ProductsImageId = @ProductsImageId", conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                        cmd.Parameters.AddWithValue("@ProductsImageId", model.ProductsImageId); 
                        cmd.Parameters.AddWithValue("@Type", model.Type);
                        cmd.Parameters.AddWithValue("@Color", model.Color);
                        cmd.Parameters.AddWithValue("@LargeImage", $"/uploads/Products/{model.ProductId}/Large/{Path.GetFileName(model.LargeImage)}");
                        cmd.Parameters.AddWithValue("@MediumImage", $"/uploads/Products/{model.ProductId}/Medium/{Path.GetFileName(model.MediumImage)}");
                        cmd.Parameters.AddWithValue("@SmallImage", $"/uploads/Products/{model.ProductId}/Small/{Path.GetFileName(model.SmallImage)}");
                        cmd.Parameters.AddWithValue("@Description", model.Description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Quantity", model.Quantity);
                        cmd.Parameters.AddWithValue("@MRP", model.MRP);
                        cmd.Parameters.AddWithValue("@Discount", model.Discount);
                        cmd.Parameters.AddWithValue("@Price", model.MRP - (model.MRP * model.Discount / 100));
                        cmd.Parameters.AddWithValue("@ArrivingDays", model.ArrivingDays);
                        cmd.Parameters.AddWithValue("@TypeSlug", typeSlug);
                        cmd.Parameters.AddWithValue("@ColorSlug", colorSlug);
                        cmd.Parameters.AddWithValue("@IsActive", true);
                        cmd.Parameters.AddWithValue("@UpdatedBy", userId);
                        cmd.Parameters.AddWithValue("@UpdatedDateTime", DateTime.Now);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    updatedProductIds.Add(model.ProductId);
                }
            }

    return updatedProductIds;
        }







        public async Task<List<string>> GetImagePathsAsync(int productsImageId)
        {
            var imagePaths = new List<string>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT LargeImage, MediumImage, SmallImage FROM ProductsImage WHERE ProductsImageId = @ProductsImageId", conn);
            cmd.Parameters.AddWithValue("@ProductsImageId", productsImageId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                imagePaths.Add(reader["LargeImage"]?.ToString());
                imagePaths.Add(reader["MediumImage"]?.ToString());
                imagePaths.Add(reader["SmallImage"]?.ToString());
            }

            return imagePaths;
        }

        public async Task SoftDeleteProductImageAsync(int productsImageId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("UPDATE ProductsImage SET IsActive = 0 WHERE ProductsImageId = @ProductsImageId", conn);
            cmd.Parameters.AddWithValue("@ProductsImageId", productsImageId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> GetActiveImageCountAsync(int productId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT COUNT(*) FROM ProductsImage WHERE ProductId = @ProductId AND IsActive = 1", conn);
            cmd.Parameters.AddWithValue("@ProductId", productId);
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task SoftDeleteProductAsync(int productId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("UPDATE Products SET IsActive = 0 WHERE ProductId = @ProductId", conn);
            cmd.Parameters.AddWithValue("@ProductId", productId);
            await cmd.ExecuteNonQueryAsync();
        }


        public async Task<bool> DeleteProductImageAsync(int productId, int productsImageId, string rootPath)
        {
            var imagePaths = await GetImagePathsAsync(productsImageId);
            await SoftDeleteProductImageAsync(productsImageId);

            int activeCount = await GetActiveImageCountAsync(productId);
            if (activeCount == 0)
            {
                await SoftDeleteProductAsync(productId);
            }

            foreach (var path in imagePaths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    var fullPath = Path.Combine(rootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }
            }

            var productFolder = Path.Combine(rootPath, "uploads", "Products", productId.ToString());
            if (Directory.Exists(productFolder))
            {
                try
                {
                    foreach (var file in Directory.GetFiles(productFolder))
                        File.Delete(file);

                    foreach (var dir in Directory.GetDirectories(productFolder))
                        Directory.Delete(dir, true);

                    Directory.Delete(productFolder, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting folder: {ex.Message}");
                }
            }

            return true;
        }
    }
}