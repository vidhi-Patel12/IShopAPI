using Braintree;
using ECommerceAPI.Interface;
using ECommerceAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Transactions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ECommerceAPI.Repository.User
{
    public class UserRepository : IUser
    {
        private readonly string _connectionString;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBraintreeGateway _braintreeGateway;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiUrl;
        private readonly string _apiKey;

        public UserRepository(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IBraintreeGateway braintreeGateway)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _httpContextAccessor = httpContextAccessor;
            _braintreeGateway = braintreeGateway;

            httpClientFactory = httpClientFactory;
            _apiUrl = configuration["Tracking:ApiUrl"]; // e.g., "https://api.17track.net/track"
            _apiKey = configuration["Tracking:ApiKey"];

        }

        public async Task<List<Products>> GetProductsWithImagesAsync()
        {
            List<Products> products = new List<Products>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetProductsAndImages", conn))
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
                                    Slug = reader.GetString(reader.GetOrdinal("Slug")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                    ProductImages = new List<ProductsImage>()
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("ProductsImageId")))
                            {
                                var image = new ProductsImage
                                {
                                    ProductsImageId = reader.GetInt32(reader.GetOrdinal("ProductsImageId")),
                                    ProductId = productId,
                                    Type = reader.GetString(reader.GetOrdinal("Type")),
                                    Color = reader.GetString(reader.GetOrdinal("Color")),
                                    LargeImage = reader.GetString(reader.GetOrdinal("LargeImage")),
                                    MediumImage = reader.GetString(reader.GetOrdinal("MediumImage")),
                                    Description = reader.GetString(reader.GetOrdinal("Description")),
                                    Quantity = reader.GetDouble(reader.GetOrdinal("Quantity")),
                                    MRP = reader.GetDouble(reader.GetOrdinal("MRP")),
                                    Discount = reader.GetInt32(reader.GetOrdinal("Discount")),
                                    Price = reader.GetDouble(reader.GetOrdinal("Price")),
                                    ArrivingDays = reader.GetInt32(reader.GetOrdinal("ArrivingDays")),
                                    TypeSlug = reader.GetString(reader.GetOrdinal("TypeSlug")),
                                    ColorSlug = reader.GetString(reader.GetOrdinal("ColorSlug")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                                };

                                productDict[productId].ProductImages.Add(image);
                            }
                        }

                        products = productDict.Values.ToList();
                    }
                }
            }

            return products;
        }

        public async Task<List<Products>> GetProductsByImageIdAsync(int productImageId)
        {
            List<Products> products = new List<Products>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetProductsAndImages", conn))
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
                                var imageId = reader.GetInt32(reader.GetOrdinal("ProductsImageId"));

                                if (imageId == productImageId)
                                {
                                    var image = new ProductsImage
                                    {
                                        ProductsImageId = imageId,
                                        ProductId = productId,
                                        Type = reader.GetString(reader.GetOrdinal("Type")),
                                        Color = reader.GetString(reader.GetOrdinal("Color")),
                                        LargeImage = reader.GetString(reader.GetOrdinal("LargeImage")),
                                        Description = reader.GetString(reader.GetOrdinal("Description")),
                                        Quantity = reader.GetDouble(reader.GetOrdinal("Quantity")),
                                        MRP = reader.GetDouble(reader.GetOrdinal("MRP")),
                                        Discount = reader.GetInt32(reader.GetOrdinal("Discount")),
                                        Price = reader.GetDouble(reader.GetOrdinal("Price")),
                                        ArrivingDays = reader.GetInt32(reader.GetOrdinal("ArrivingDays")),
                                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                                    };

                                    productDict[productId].ProductImages.Add(image);
                                }
                            }
                        }

                        products = productDict.Values
                            .Where(p => p.ProductImages.Any())
                            .ToList();
                    }
                }
            }

            return products;
        }



        public async Task<int> GetProductImageIdAsync(string slug, string typeSlug, string colorSlug)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = @"
                SELECT pi.ProductsImageId 
                FROM Products p
                INNER JOIN ProductsImage pi ON p.ProductId = pi.ProductId
                WHERE p.Slug = @Slug
                AND pi.TypeSlug = @TypeSlug
                AND pi.ColorSlug = @ColorSlug";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Slug", slug);
                    cmd.Parameters.AddWithValue("@TypeSlug", typeSlug);
                    cmd.Parameters.AddWithValue("@ColorSlug", colorSlug);

                    var result = await cmd.ExecuteScalarAsync();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public async Task<Products> GetProductWithImagesAsync(string slug, string typeSlug, string colorSlug)
        {
            Products product = null;
            List<ProductsImage> productImages = new List<ProductsImage>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetProductsAndImages", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Slug", slug);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (product == null)
                            {
                                product = new Products
                                {
                                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                    Name = reader.GetString(reader.GetOrdinal("Name")),
                                    Slug = reader.GetString(reader.GetOrdinal("Slug")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                    ProductImages = new List<ProductsImage>()
                                };
                            }
                        }
                    }
                }

                if (product == null) return null;

                using (SqlCommand cmd = new SqlCommand("GetProductsAndImages", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                    cmd.Parameters.AddWithValue("@TypeSlug", typeSlug);
                    cmd.Parameters.AddWithValue("@ColorSlug", colorSlug);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var image = new ProductsImage
                            {
                                ProductsImageId = reader.GetInt32(reader.GetOrdinal("ProductsImageId")),
                                ProductId = product.ProductId,
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
                                TypeSlug = reader.GetString(reader.GetOrdinal("TypeSlug")),
                                ColorSlug = reader.GetString(reader.GetOrdinal("ColorSlug")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            };
                            productImages.Add(image);
                        }
                    }
                }
            }

            product.ProductImages = productImages;
            return product;
        }

        public async Task<List<Products>> GetRelatedProductsAsync(int productId, string slug, string typeSlug, string colorSlug)
        {
            List<Products> relatedProducts = new List<Products>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetRelatedProducts", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    cmd.Parameters.AddWithValue("@Slug", slug);
                    cmd.Parameters.AddWithValue("@TypeSlug", typeSlug);
                    cmd.Parameters.AddWithValue("@ColorSlug", colorSlug);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int prodId = reader.GetInt32(reader.GetOrdinal("ProductId"));

                            if (prodId == productId) continue;

                            var existing = relatedProducts.FirstOrDefault(p => p.ProductId == prodId);
                            if (existing == null)
                            {
                                existing = new Products
                                {
                                    ProductId = prodId,
                                    Name = reader.GetString(reader.GetOrdinal("Name")),
                                    Slug = reader.GetString(reader.GetOrdinal("Slug")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                    ProductImages = new List<ProductsImage>()
                                };
                                relatedProducts.Add(existing);
                            }

                            if (reader["ProductsImageId"] != DBNull.Value)
                            {
                                var img = new ProductsImage
                                {
                                    ProductsImageId = reader.GetInt32(reader.GetOrdinal("ProductsImageId")),
                                    MediumImage = reader.GetString(reader.GetOrdinal("MediumImage")),
                                    Price = reader.GetDouble(reader.GetOrdinal("Price")),
                                    Color = reader.GetString(reader.GetOrdinal("Color")),
                                    TypeSlug = reader.GetString(reader.GetOrdinal("TypeSlug")),
                                    ColorSlug = reader.GetString(reader.GetOrdinal("ColorSlug"))
                                };
                                existing.ProductImages.Add(img);
                            }
                        }
                    }
                }
            }

            return relatedProducts;
        }


        public async Task<List<ShoppingCart>> GetCartItemsAsync(string iShopId)
        {
            List<ShoppingCart> cartItems = new List<ShoppingCart>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("SELECT * FROM ShoppingCart WHERE IShopId = @IShopId", conn))
                {
                    cmd.Parameters.AddWithValue("@IShopId", iShopId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            cartItems.Add(new ShoppingCart
                            {
                                IShopId = reader.GetInt32(reader.GetOrdinal("IShopId")),
                                ArrivingDays = reader.IsDBNull(reader.GetOrdinal("ArrivingDays")) ? 0 : reader.GetInt32(reader.GetOrdinal("ArrivingDays")),
                                Color = reader["Color"]?.ToString() ?? "",
                                Description = reader["Description"]?.ToString() ?? "",
                                Image = reader["Image"]?.ToString() ?? "",
                                Name = reader["Name"]?.ToString() ?? "",
                                Price = reader.IsDBNull(reader.GetOrdinal("Price")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("Price")),
                                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                ProductsImageId = reader.GetInt32(reader.GetOrdinal("ProductsImageId")),
                                Quantity = reader.IsDBNull(reader.GetOrdinal("Quantity")) ? 0 : reader.GetDouble(reader.GetOrdinal("Quantity")),
                                Total = reader.IsDBNull(reader.GetOrdinal("Total")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("Total")),
                                Type = reader["Type"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return cartItems;
        }

        public async Task<bool> SaveCartAsync(string? userId, List<ShoppingCart> cartItems)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Guest user: store cart in cookie
                var response = _httpContextAccessor.HttpContext.Response;
                response.Cookies.Append("cartItems", JsonSerializer.Serialize(cartItems),
                    new CookieOptions { Expires = DateTime.UtcNow.AddDays(7) });

                return true;
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                foreach (var item in cartItems)
                {
                    using (SqlCommand cmd = new SqlCommand("SaveOrUpdateCart", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IShopId", userId);
                        cmd.Parameters.AddWithValue("@ArrivingDays", item.ArrivingDays);
                        cmd.Parameters.AddWithValue("@Color", item.Color ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description", item.Description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Image", item.Image ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Name", item.Name ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Price", item.Price);
                        cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                        cmd.Parameters.AddWithValue("@ProductsImageId", item.ProductsImageId);
                        cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                        cmd.Parameters.AddWithValue("@Total", item.Total);
                        cmd.Parameters.AddWithValue("@Type", item.Type ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            return true;
        }


        public async Task<bool> CartItemExistsAsync(string iShopId, int productsImageId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("CheckCartItemExists", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IShopId", iShopId);
                    cmd.Parameters.AddWithValue("@ProductsImageId", productsImageId);

                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        public async Task<bool> DeleteCartItemAsync(string userId, int productId, int productsImageId)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand("DeleteShoppingCart", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@IShopId", userId);
            cmd.Parameters.AddWithValue("@ProductId", productId);
            cmd.Parameters.AddWithValue("@ProductsImageId", productsImageId);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAllCartItemsAsync(string userId)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand("DELETE FROM ShoppingCart WHERE IShopId = @IShopId", conn);
            cmd.Parameters.AddWithValue("@IShopId", userId);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<List<DelivaryAddresses>> GetAddressesByShopIdAsync(string shopId)
        {
            var addresses = new List<DelivaryAddresses>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetAddressesByShopId", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IShopId", shopId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            addresses.Add(new DelivaryAddresses
                            {
                                AddressId = reader.GetInt32(0),
                                FullName = reader.GetString(1),
                                Mobile = reader.GetInt64(2),
                                Address = reader.GetString(3),
                                City = reader.GetString(4),
                                State = reader.GetString(5),
                                ZipCode = reader.GetInt32(6),
                                Country = reader.GetString(7),
                                IsActive = reader.GetBoolean(8)
                            });
                        }
                    }
                }
            }

            return addresses;
        }

        public async Task<int> SaveAddressAsync(DelivaryAddresses model)
        {
            int shopId;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // 🔍 Fetch IShopId by Mobile
            using (var fetchCmd = new SqlCommand("SELECT IShopId FROM Register WHERE Mobile = @Mobile", conn))
            {
                fetchCmd.Parameters.AddWithValue("@Mobile", model.Mobile);
                object result = await fetchCmd.ExecuteScalarAsync();
                if (result == null)
                {
                    throw new Exception("IShopId not found for the given mobile number.");
                }
                shopId = Convert.ToInt32(result);
            }

            // 🔄 Save or update address
            using (var cmd = new SqlCommand("SaveDeliveryAddress", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                var addressIdParam = new SqlParameter("@AddressId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = model.AddressId > 0 ? model.AddressId : 0
                };

                cmd.Parameters.Add(addressIdParam);
                cmd.Parameters.AddWithValue("@IShopId", shopId);
                cmd.Parameters.AddWithValue("@FullName", model.FullName);
                cmd.Parameters.AddWithValue("@Mobile", model.Mobile);
                cmd.Parameters.AddWithValue("@Country", model.Country);
                cmd.Parameters.AddWithValue("@State", model.State);
                cmd.Parameters.AddWithValue("@City", model.City);
                cmd.Parameters.AddWithValue("@ZipCode", model.ZipCode);
                cmd.Parameters.AddWithValue("@Address", model.Address);
                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                await cmd.ExecuteNonQueryAsync();

                return Convert.ToInt32(addressIdParam.Value);
            }
        }

        public async Task<bool> SaveOrdersAsync(List<Orders> orders, int shopId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                foreach (var order in orders)
                {
                    using (SqlCommand cmd = new SqlCommand("SaveOrder", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@OrderId", order.OrderId);
                        cmd.Parameters.AddWithValue("@IShopId", shopId);
                        cmd.Parameters.AddWithValue("@ProductId", order.ProductId);
                        cmd.Parameters.AddWithValue("@ProductsImageId", order.ProductsImageId);
                        cmd.Parameters.AddWithValue("@OrderQty", order.OrderQty);
                        cmd.Parameters.AddWithValue("@TotalAmount", order.TotalAmount);
                        cmd.Parameters.AddWithValue("@IsActive", true);
                        cmd.Parameters.AddWithValue("@Shipping", false);
                        cmd.Parameters.AddWithValue("@CreatedDateTime", DateTime.Now);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            return true;
        }

        public async Task<(bool Success, string? Message, string? PaymentMode, string? OrderId)>
         SaveCheckoutAsync(Checkout checkout, int shopId, HttpContext httpContext)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                var orderDate = DateTime.Now;

                using (SqlCommand cmd = new SqlCommand("SaveCheckout", conn, transaction))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@OrderId", checkout.OrderId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IShopId", shopId);
                    cmd.Parameters.AddWithValue("@AddressId", checkout.AddressId);
                    cmd.Parameters.AddWithValue("@PaymentMode", checkout.PaymentMode);
                    cmd.Parameters.AddWithValue("@OrderDate", orderDate);
                    cmd.Parameters.AddWithValue("@TotalAmount", checkout.TotalAmount);
                    cmd.Parameters.AddWithValue("@Tax", checkout.Tax);
                    cmd.Parameters.AddWithValue("@DelivaryCharge", checkout.DelivaryCharge);
                    cmd.Parameters.AddWithValue("@FinalAmount", checkout.FinalAmount);
                    cmd.Parameters.AddWithValue("@PromoAmount", checkout.PromoAmount);
                    cmd.Parameters.AddWithValue("@OrderAmount", checkout.OrderAmount);
                    cmd.Parameters.AddWithValue("@IsActive", true);

                    await cmd.ExecuteNonQueryAsync();
                }

                using (SqlCommand cmdStock = new SqlCommand("UpdateStockAfterCheckout", conn, transaction))
                {
                    cmdStock.CommandType = CommandType.StoredProcedure;
                    cmdStock.Parameters.AddWithValue("@OrderId", checkout.OrderId);
                    await cmdStock.ExecuteNonQueryAsync();
                }

                transaction.Commit();

                httpContext.Session.SetString("PaymentMode", checkout.PaymentMode);
                httpContext.Session.SetString("OrderId", checkout.OrderId);
                httpContext.Response.Cookies.Append("IShopId", shopId.ToString());
                httpContext.Session.SetInt32("IShopId", shopId);

                return (true, "Checkout saved successfully!", checkout.PaymentMode, checkout.OrderId);
            }
            catch (Exception)
            {
                try
                {
                    string fetchQuery = "SELECT TOP 1 OrderId FROM Orders WHERE IShopId = @IShopId ORDER BY Id DESC";
                    string? orderId = null;

                    using (SqlCommand fetchCmd = new SqlCommand(fetchQuery, conn, transaction))
                    {
                        fetchCmd.Parameters.AddWithValue("@IShopId", shopId);
                        object result = await fetchCmd.ExecuteScalarAsync();
                        if (result != null)
                            orderId = result.ToString();
                    }

                    if (!string.IsNullOrEmpty(orderId))
                    {
                        using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM Orders WHERE OrderId = @OrderId", conn, transaction))
                        {
                            deleteCmd.Parameters.AddWithValue("@OrderId", orderId);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return (false, "Checkout failed, order deleted.", null, null);
                    }

                    transaction.Rollback();
                    return (false, "Checkout failed. No matching order found.", null, null);
                }
                catch (Exception deleteEx)
                {
                    return (false, $"Checkout failed. Deletion error: {deleteEx.Message}", null, null);
                }
            }
        }
        public async Task<List<Coupan>> GetValidCouponsAsync(double totalAmount)
        {
            List<Coupan> coupons = new List<Coupan>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(@"SELECT * FROM Coupan 
                                                     WHERE IsActive = 1 
                                                       AND ExpiryDate >= CAST(GETDATE() AS DATE) 
                                                       AND ValidAmount >= @ValidAmount", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@ValidAmount", totalAmount);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            coupons.Add(new Coupan
                            {
                                CoupanId = reader.GetInt32(reader.GetOrdinal("CoupanId")),
                                CoupanName = reader.GetString(reader.GetOrdinal("CoupanName")),
                                CoupanType = reader.GetString(reader.GetOrdinal("CoupanType")),
                                CoupanCode = reader.GetString(reader.GetOrdinal("CoupanCode")),
                                Discount = reader.GetDouble(reader.GetOrdinal("Discount")),
                                ExpiryDate = reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            });
                        }
                    }
                }
            }

            return coupons;
        }


        public async Task<List<OrderDetails>> GetOrdersAsync(int iShopId)
        {
            var orders = new List<OrderDetails>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetOrdersWithDetails", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IShopId", iShopId);
                    cmd.Parameters.AddWithValue("@OrderId", DBNull.Value);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orders.Add(new OrderDetails
                            {
                                OrderId = reader["OrderId"].ToString(),
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                PaymentMode = reader["PaymentMode"].ToString(),
                                OrderAmount = Convert.ToDouble(reader["OrderAmount"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                Shipping = Convert.ToBoolean(reader["Shipping"])
                            });
                        }
                    }
                }
            }

            return orders;
        }

        public async Task<List<OrderDetails>> GetOrderDetailsAsync(int shopId, string orderId)
        {
            var orders = new List<OrderDetails>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetOrdersWithDetails", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IShopId", shopId);
                    cmd.Parameters.AddWithValue("@OrderId", orderId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orders.Add(new OrderDetails
                            {
                                OrderId = reader["OrderId"].ToString(),
                                Shipping = Convert.ToBoolean(reader["Shipping"]),
                                IShopId = Convert.ToInt32(reader["IShopId"]),
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                PaymentMode = reader["PaymentMode"].ToString(),
                                TotalAmount = Convert.ToDouble(reader["TotalAmount"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                Tax = Convert.ToDouble(reader["Tax"]),
                                DelivaryCharge = Convert.ToDouble(reader["DelivaryCharge"]),
                                FinalAmount = Convert.ToDouble(reader["FinalAmount"]),
                                PromoAmount = Convert.ToDouble(reader["PromoAmount"]),
                                OrderAmount = Convert.ToDouble(reader["OrderAmount"]),
                                ProductName = reader["ProductName"].ToString(),
                                ProductsImageId = Convert.ToInt32(reader["ProductsImageId"]),
                                LargeImage = reader["LargeImage"].ToString(),
                                Type = reader["Type"].ToString(),
                                Color = reader["Color"].ToString(),
                                FullName = reader["FullName"].ToString(),
                                Address = reader["Address"].ToString(),
                                City = reader["City"].ToString(),
                                State = reader["State"].ToString(),
                                Country = reader["Country"].ToString(),
                                ZipCode = Convert.ToInt32(reader["ZipCode"]), // Fix this line
                                Mobile = reader["Mobile"] != DBNull.Value ? Convert.ToInt64(reader["Mobile"]) : 0
                            });
                        }
                    }
                }
            }

            return orders;
        }

        public async Task<bool> CancelOrderAsync(string orderId)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();
            try
            {
                // Cancel Checkout
                using (SqlCommand cmd = new SqlCommand("UPDATE Checkout SET IsActive = 0 WHERE OrderId = @OrderId", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Cancel Orders
                using (SqlCommand cmd = new SqlCommand("UPDATE Orders SET IsActive = 0 WHERE OrderId = @OrderId", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }
        public async Task<bool> CheckStockAvailabilityAsync(int productImageId, int requestedQty)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("CheckStockAvailability", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductsImageId", productImageId);
                    cmd.Parameters.AddWithValue("@RequestedQty", requestedQty);

                    var outputParam = new SqlParameter("@StockAvailable", SqlDbType.Bit)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    await cmd.ExecuteNonQueryAsync();

                    return Convert.ToBoolean(outputParam.Value);
                }
            }
        }

        public async Task<List<Products>> SearchProductsAsync(string query)
        {
            List<Products> products = new List<Products>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetSearchProducts", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Slug", query);
                    cmd.Parameters.AddWithValue("@TypeSlug", query);
                    cmd.Parameters.AddWithValue("@ColorSlug", query);

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
                                    Slug = reader.GetString(reader.GetOrdinal("Slug")),
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
                                    SmallImage = reader.GetString(reader.GetOrdinal("SmallImage")),
                                    Description = reader.GetString(reader.GetOrdinal("Description")),
                                    Quantity = reader.GetDouble(reader.GetOrdinal("Quantity")),
                                    MRP = reader.GetDouble(reader.GetOrdinal("MRP")),
                                    Discount = reader.GetInt32(reader.GetOrdinal("Discount")),
                                    Price = reader.GetDouble(reader.GetOrdinal("Price")),
                                    ArrivingDays = reader.GetInt32(reader.GetOrdinal("ArrivingDays")),
                                    TypeSlug = reader.GetString(reader.GetOrdinal("TypeSlug")),
                                    ColorSlug = reader.GetString(reader.GetOrdinal("ColorSlug")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                                });
                            }
                        }

                        products = productDict.Values.ToList();
                    }
                }
            }

            return products;
        }

        public async Task<Register> GetUserByShopIdAsync(int iShopId)
        {
            Register user = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("Select * from Register where IShopId = @IShopId", conn))
                {
                    cmd.Parameters.AddWithValue("@IShopId", iShopId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            user = new Register
                            {
                                IShopId = reader.GetInt32(reader.GetOrdinal("IShopId")),
                                FirstName = reader["FirstName"]?.ToString(),
                                LastName = reader["LastName"]?.ToString(),
                                Mobile = reader.GetInt64(reader.GetOrdinal("Mobile")),
                                Password = reader["Password"]?.ToString(),
                                Birthdate = reader["BirthDate"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["BirthDate"])) : (DateOnly?)null,
                                Email = reader["Email"]?.ToString(),
                                Role = reader.GetInt32(reader.GetOrdinal("Role"))
                            };
                        }
                    }
                }
            }

            return user;
        }

        public async Task<string> GetPasswordAsync(int iShopId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("SELECT Password FROM Register WHERE IShopId = @IShopId", conn);
            cmd.Parameters.AddWithValue("@IShopId", iShopId);

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }

        public async Task<bool> UpdateUserAsync(int iShopId, Register model)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("UpdateAccount", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IShopId", iShopId);
            cmd.Parameters.AddWithValue("@FirstName", model.FirstName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LastName", model.LastName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Mobile", model.Mobile);
            cmd.Parameters.AddWithValue("@Email", model.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Password", model.Password ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BirthDate", model.Birthdate?.ToDateTime(TimeOnly.MinValue) ?? (object)DBNull.Value);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<int> SaveAddressAsync(int iShopId, DelivaryAddresses model, string formMode)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var command = new SqlCommand("SaveDeliveryAddress", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Use 0 if adding, otherwise trust the model's AddressId (for update)
            int addressIdValue = (formMode == "Add Address" || model.AddressId == 0) ? 0 : model.AddressId;

            var addressIdParam = new SqlParameter("@AddressId", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = addressIdValue
            };
            command.Parameters.Add(addressIdParam);

            command.Parameters.AddWithValue("@IShopId", iShopId);
            command.Parameters.AddWithValue("@FullName", model.FullName);
            command.Parameters.AddWithValue("@Mobile", model.Mobile);
            command.Parameters.AddWithValue("@Country", model.Country);
            command.Parameters.AddWithValue("@State", model.State);
            command.Parameters.AddWithValue("@City", model.City);
            command.Parameters.AddWithValue("@ZipCode", model.ZipCode);
            command.Parameters.AddWithValue("@Address", model.Address);
            command.Parameters.AddWithValue("@IsActive", true);

            await command.ExecuteNonQueryAsync();

            return Convert.ToInt32(addressIdParam.Value); // return final AddressId
        }
        public async Task<bool> SoftDeleteAddressAsync(int addressId)
        {
            if (addressId <= 0) return false;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("UPDATE DelivaryAddresses SET IsActive = 0 WHERE AddressId = @AddressId", connection))
                {
                    command.Parameters.AddWithValue("@AddressId", addressId);
                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        public async Task<List<OrderDetailsDto>> GetOrderDetailsByOrderIdAsync(Guid orderId)
        {
            var orderDetails = new List<OrderDetailsDto>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("GetOrderDetailsByOrderId", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@OrderId", SqlDbType.UniqueIdentifier) { Value = orderId });

                await conn.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        orderDetails.Add(new OrderDetailsDto
                        {
                            OrderId = reader["OrderId"].ToString(),
                            AddressId = Convert.ToInt32(reader["AddressId"]),
                            PaymentMode = reader["PaymentMode"]?.ToString(),
                            OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                            DelivaryCharge = (float)reader.GetDouble(reader.GetOrdinal("DelivaryCharge")),
                            PromoAmount = (float)reader.GetDouble(reader.GetOrdinal("PromoAmount")),
                            OrderAmount = (float)reader.GetDouble(reader.GetOrdinal("OrderAmount")),
                            FullName = reader["FullName"]?.ToString(),
                            Mobile = reader["Mobile"]?.ToString(),
                            Address = reader["Address"]?.ToString(),
                            City = reader["City"]?.ToString(),
                            State = reader["State"]?.ToString(),
                            Country = reader["Country"]?.ToString(),
                            Zipcode = reader["Zipcode"]?.ToString(),
                            ProductsImageId = Convert.ToInt32(reader["ProductsImageId"]),
                            OrderQty = Convert.ToInt32(reader["OrderQty"]),
                            TotalAmount = (float)reader.GetDouble(reader.GetOrdinal("TotalAmount")),
                            ProductId = Convert.ToInt32(reader["ProductId"]),
                            Type = reader["Type"]?.ToString(),
                            Color = reader["Color"]?.ToString(),
                            Price = (float)reader.GetDouble(reader.GetOrdinal("Price")),
                            ProductName = reader["ProductName"]?.ToString(),
                            Email = reader["Email"]?.ToString(),
                            PaymentModeInPaymentTable = reader["PaymentModeInPaymentTable"]?.ToString(),
                            PaymentAmount = (float)reader.GetDouble(reader.GetOrdinal("PaymentAmount")),
                            PaymentDate = reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
                            PaymentId = Convert.ToInt32(reader["Id"])
                        });
                    }
                }
            }

            return orderDetails;
        }

        public async Task<(string OrderId, decimal OrderAmount, string PaymentMode)> GetOrderDetailsAsync(string orderId = null)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string query = string.IsNullOrEmpty(orderId)
                ? "SELECT TOP 1 OrderId, OrderAmount, PaymentMode FROM Checkout ORDER BY CheckoutId DESC"
                : "SELECT OrderId, OrderAmount, PaymentMode FROM Checkout WHERE OrderId = @OrderId";

            using var cmd = new SqlCommand(query, conn);

            if (!string.IsNullOrEmpty(orderId))
                cmd.Parameters.AddWithValue("@OrderId", orderId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    reader.GetString(0),
                    (decimal)reader.GetDouble(1),
                    reader.GetString(2)
                );
            }

            return (null, 0, null);
        }

        public async Task<bool> IsPaymentDoneAsync(string orderId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT TransactionId, Status FROM Payment WHERE OrderId = @OrderId";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var status = reader["Status"].ToString();
                var txnId = reader["TransactionId"].ToString();
                return !string.IsNullOrEmpty(txnId) && status == "Success";
            }

            return false;
        }

        public async Task SaveCashPaymentAsync(string orderId, decimal amount)
        {
            string transactionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            string paymentStatus = "Pending";
            DateTime paymentDate = DateTime.Now;

            using SqlConnection conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand("SavePaymentAndUpdateOrder", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@TransactionId", transactionId);
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            cmd.Parameters.AddWithValue("@PaymentMode", "COD");
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@Status", paymentStatus);
            cmd.Parameters.AddWithValue("@PaymentDate", paymentDate);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<(bool IsSuccess, string TransactionId, string Status, string RedirectUrl, string ErrorMessage)> ProcessPaymentAsync(PaymentRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OrderId) || string.IsNullOrEmpty(request.Nonce))
                return (false, null, null, null, "Invalid payment request");

            string paymentStatus;
            string transactionId = null;
            double orderAmount = 0;
            int iShopId = 0;
            DateTime paymentDate = DateTime.Now;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Check if payment exists
            using (var checkCmd = new SqlCommand("SELECT TransactionId, Status FROM Payment WHERE OrderId = @OrderId", conn))
            {
                checkCmd.Parameters.AddWithValue("@OrderId", request.OrderId);
                using var reader = await checkCmd.ExecuteReaderAsync();
                if (reader.Read() && reader["Status"].ToString() == "Success")
                    return (false, null, null, null, "Payment is already done for this order.");
            }

            // Fetch order info
            using (var cmd = new SqlCommand("SELECT TOP 1 IShopId, OrderAmount FROM Checkout WHERE OrderId = @OrderId ORDER BY CheckoutId DESC", conn))
            {
                cmd.Parameters.AddWithValue("@OrderId", request.OrderId);
                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    iShopId = reader.GetInt32(0);
                    orderAmount = reader.GetDouble(1);
                }
                else
                    return (false, null, null, null, "Order not found");
            }

            // Braintree transaction
            var transactionRequest = new TransactionRequest
            {
                Amount = (decimal)orderAmount,
                PaymentMethodNonce = request.Nonce,
                Options = new TransactionOptionsRequest { SubmitForSettlement = true }
            };

            var result = await _braintreeGateway.Transaction.SaleAsync(transactionRequest);

            paymentStatus = result.IsSuccess() ? "Success"
                          : result.Transaction?.Status == Braintree.TransactionStatus.SUBMITTED_FOR_SETTLEMENT ? "Pending"
                          : "Failed";

            transactionId = result.IsSuccess() ? result.Target.Id : result.Transaction?.Id;

            // Save payment and update order
            using (var cmd = new SqlCommand("SavePaymentAndUpdateOrder", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TransactionId", transactionId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@OrderId", request.OrderId);
                cmd.Parameters.AddWithValue("@PaymentMode", request.PaymentMode);
                cmd.Parameters.AddWithValue("@Amount", orderAmount);
                cmd.Parameters.AddWithValue("@Status", paymentStatus);
                cmd.Parameters.AddWithValue("@PaymentDate", paymentDate);

                await cmd.ExecuteNonQueryAsync();
            }

            var redirectUrl = $"/Home/GenerateAndSendInvoice?orderId={request.OrderId}";

            return paymentStatus == "Failed"
                ? (false, transactionId, paymentStatus, null, result.Message ?? "Payment failed")
                : (true, transactionId, paymentStatus, redirectUrl, null);
        }


        public async Task<(bool Success, string Status, List<JObject> Checkpoints, string ErrorMessage)> TrackPackageAsync(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                return (false, null, null, "Tracking number is required.");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("17token", _apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestData = new { numbers = new[] { trackingNumber } };
            var jsonData = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_apiUrl, content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, null, null, $"Request failed: {response.StatusCode}");

            try
            {
                JObject trackingData = JObject.Parse(result);

                if (trackingData["data"] is JArray dataArray && dataArray.Count > 0)
                {
                    var trackInfo = dataArray[0]?["track"];
                    var status = trackInfo?["latest_status"]?.ToString() ?? "Unknown";
                    var checkpoints = trackInfo?["z1"]?.ToObject<List<JObject>>() ?? new List<JObject>();
                    return (true, status, checkpoints, null);
                }
                else
                {
                    return (false, null, null, "No tracking data found.");
                }
            }
            catch (Exception ex)
            {
                return (false, null, null, $"Error parsing response: {ex.Message}");
            }
        }

    }
}