using ECommerceAPI.Interface;
using ECommerceAPI.Models;
using ECommerceAPI.Repository;
using ECommerceAPI.Repository.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers.User
{
    [Route("user/v1")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUser _user;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserController(IUser userRepository, IHttpContextAccessor httpContextAccessor)
        {
            _user = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet("limited")]
        public async Task<IActionResult> GetLimitedProducts()
        {
            try
            {
                var products = await _user.GetProductsWithImagesAsync();
                var result = products.Count > 4 ? products.Take(4).ToList() : products;
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _user.GetProductsWithImagesAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("quickview/{productImageId}")]
        public async Task<IActionResult> QuickViewByImageId(int productImageId)
        {
            try
            {
                var result = await _user.GetProductsByImageIdAsync(productImageId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [HttpGet("productdetails/{slug}/{typeSlug}/{colorSlug}")]
        public async Task<IActionResult> GetProductDetails(string slug, string typeSlug, string colorSlug)
        {
            if (string.IsNullOrEmpty(slug)) return BadRequest("Missing slug.");

            int productImageId = await _user.GetProductImageIdAsync(slug, typeSlug, colorSlug);
            if (productImageId == 0) return NotFound("Product image not found.");

            var product = await _user.GetProductWithImagesAsync(slug, typeSlug, colorSlug);
            if (product == null) return NotFound("Product not found.");

            var relatedProducts = await _user.GetRelatedProductsAsync(product.ProductId, slug, typeSlug, colorSlug);

            return Ok(new
            {
                ProductImageId = productImageId,
                Product = product,
                RelatedProducts = relatedProducts
            });
        }

        [HttpGet("getcart")]
        public async Task<IActionResult> GetCart()
        {
            string? iShopId = Request.Cookies["IShopId"];

            if (string.IsNullOrEmpty(iShopId))
            {
                return BadRequest(new { success = false, message = "IShopId cookie not found" });
            }

            var cartItems = await _user.GetCartItemsAsync(iShopId);

            return Ok(new { success = true, cartItems });
        }

        [HttpPost("savecart")]
        public async Task<IActionResult> SaveCart([FromBody] List<ShoppingCart> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
                return BadRequest(new { success = false, message = "Cart is empty." });

            var userId = _httpContextAccessor.HttpContext.Request.Cookies["IShopId"];

            try
            {
                var result = await _user.SaveCartAsync(userId, cartItems);
                return Ok(new
                {
                    success = result,
                    message = result
                        ? (!string.IsNullOrEmpty(userId) ? "Cart saved to database." : "Cart saved in cookies.")
                        : "Failed to save cart."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Server error.", error = ex.Message });
            }
        }

        [HttpPost("checkcartitem")]
        public async Task<IActionResult> CheckCartItem([FromBody] ShoppingCart item)
        {
            try
            {
                string? userId = _httpContextAccessor.HttpContext?.Request.Cookies["IShopId"];
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { exists = false, message = "User not logged in." });
                }

                bool exists = await _user.CartItemExistsAsync(userId, item.ProductsImageId);
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { exists = false, error = ex.Message });
            }
        }

        [HttpPost("deletecartitem")]
        public async Task<IActionResult> DeleteCartItem([FromBody] CartItemDeleteRequest request)
        {
            string? userId = Request.Cookies["IShopId"];

            if (request == null || request.ProductId == 0 || request.ProductsImageId == 0)
            {
                bool deletedAll = await _user.DeleteAllCartItemsAsync(userId);
                return Ok(new
                {
                    success = deletedAll,
                    message = deletedAll ? "All items deleted successfully." : "No items found to delete."
                });
            }

            // Delete a specific item
            bool deleted = await _user.DeleteCartItemAsync(userId, request.ProductId ?? 0, request.ProductsImageId);
            return Ok(new
            {
                success = deleted,
                message = deleted ? "Item deleted successfully." : "Failed to delete item."
            });
        }

        [HttpGet("getaddresses")]
        public async Task<IActionResult> GetAddresses()
        {
            var shopId = Request.Cookies["IShopId"];

            if (string.IsNullOrEmpty(shopId))
                return BadRequest(new { success = false, message = "Invalid or missing IShopId in cookie." });

            var addresses = await _user.GetAddressesByShopIdAsync(shopId);

            if (addresses == null || addresses.Count == 0)
                return NotFound(); // No need to wrap response

            return Ok(addresses); //  Return just the list
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveAddress([FromBody] DelivaryAddresses model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid address data." });

            try
            {
                int addressId = await _user.SaveAddressAsync(model);

                return Ok(new
                {
                    success = true,
                    message = model.AddressId > 0 ? "Address updated successfully!" : "Address added successfully!",
                    addressId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error saving address", error = ex.Message });
            }
        }

        [HttpPost("saveorder")]
        public async Task<IActionResult> SaveOrder([FromBody] List<Orders> orders)
        {
            string shopIdString = Request.Cookies["IShopId"];
            if (string.IsNullOrEmpty(shopIdString) || !int.TryParse(shopIdString, out int shopId))
            {
                return BadRequest(new { success = false, message = "Invalid or missing IShopId in cookie." });
            }

            if (orders == null || orders.Count == 0)
                return BadRequest(new { success = false, message = "No order items provided." });

            bool result = await _user.SaveOrdersAsync(orders, shopId);
            if (!result)
                return StatusCode(500, new { success = false, message = "Failed to save orders." });

            return Ok(new { success = true, message = "Order saved successfully!" });
        }


        [HttpPost("savecheckout")]
        public async Task<IActionResult> SaveCheckout([FromBody] Checkout checkout)
        {
            if (checkout == null)
                return BadRequest(new { success = false, message = "Invalid checkout data." });

            if (!Request.Cookies.TryGetValue("IShopId", out var shopIdStr) || !int.TryParse(shopIdStr, out var shopId))
                return BadRequest(new { success = false, message = "Missing or invalid IShopId in cookies." });

            var (success, message, paymentMode, orderId) =
                await _user.SaveCheckoutAsync(checkout, shopId, HttpContext);

            if (success)
                return Ok(new { success = true, message, paymentMode, orderId });

            return BadRequest(new { success = false, message });
        }
        [HttpGet("Coupan/{total}")]
        public async Task<IActionResult> Coupan(double total)
        {
            var coupons = await _user.GetValidCouponsAsync(total);

            if (coupons == null || coupons.Count == 0)
                return NotFound(new { success = false, message = "No coupons found." });

            return Ok(coupons);
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            string shopIdString = Request.Cookies["IShopId"];

            if (string.IsNullOrEmpty(shopIdString) || !int.TryParse(shopIdString, out int iShopId))
            {
                return BadRequest(new { success = false, message = "Invalid or missing IShopId." });
            }

            var orders = await _user.GetOrdersAsync(iShopId);

            if (orders == null || !orders.Any())
                return NotFound(new { success = false, message = "No orders found." });

            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(string orderId)
        {
            var shopIdString = Request.Cookies["IShopId"];

            if (string.IsNullOrEmpty(shopIdString) || !int.TryParse(shopIdString, out int shopId))
                return BadRequest(new { success = false, message = "Invalid or missing IShopId." });

            var orderDetails = await _user.GetOrderDetailsAsync(shopId, orderId);

            if (orderDetails == null || !orderDetails.Any())
                return NotFound(new { success = false, message = "No order found with the given ID." });

            return Ok(orderDetails);
        }

        [HttpPost("cancelorder")]
        public async Task<IActionResult> CancelOrder([FromQuery] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Order ID is required.");

            var success = await _user.CancelOrderAsync(id);

            if (!success)
                return BadRequest("Failed to cancel order.");

            return Ok(new { message = "Order cancelled successfully." });
        }

        [HttpGet("CheckAvailability")]
        public async Task<IActionResult> CheckStockAvailability(int productImageId, int requestedQty)
        {
            bool isAvailable = await _user.CheckStockAvailabilityAsync(productImageId, requestedQty);

            if (!isAvailable)
            {
                return BadRequest(new { success = false, message = "Out of Stock!" });
            }

            return Ok(new { success = true, message = "Stock Available!" });
        }

        [HttpGet("searchproduct")]
        public async Task<IActionResult> SearchProduct([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { success = false, message = "Search query is required." });

            var products = await _user.SearchProductsAsync(query);

            if (products == null || products.Count == 0)
                return NotFound(new { success = false, message = "No products found." });

            return Ok(products);
        }

        [HttpGet("MyAccount")]
        public async Task<IActionResult> MyAccount([FromQuery] int iShopId)
        {
            var user = await _user.GetUserByShopIdAsync(iShopId);

            if (user == null)
            {
                return NotFound(new { message = "User profile not found." });
            }

            return Ok(user);
        }

        [HttpPost("MyAccount")]
        public async Task<IActionResult> UpdateAccount([FromQuery] int iShopId, [FromBody] Register model)
        {
            
            string oldPassword = await _user.GetPasswordAsync(iShopId);

            if (oldPassword == model.Password)
            {
                return BadRequest(new { message = "New password cannot be the same as the old password." });
            }

            bool updated = await _user.UpdateUserAsync(iShopId, model);

            if (!updated)
            {
                return StatusCode(500, new { message = "Update failed." });
            }

            return Ok(new { message = "Account updated successfully!" });
        }

        [HttpPost("saveaddress")]
        public async Task<IActionResult> SaveAddress([FromQuery] int iShopId, [FromQuery] string formMode, [FromBody] DelivaryAddresses model)
        {
            if (iShopId <= 0 || string.IsNullOrWhiteSpace(formMode))
            {
                return BadRequest(new { success = false, message = "Invalid input." });
            }

            int newAddressId = await _user.SaveAddressAsync(iShopId, model, formMode);

            return Ok(new
            {
                success = true,
                message = formMode == "Add Address" ? "Address added successfully!" : "Address updated successfully!",
                addressId = newAddressId
            });
        }

        [HttpDelete("deleteaddress/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid address ID.");

            var success = await _user.SoftDeleteAddressAsync(id);

            if (!success)
                return NotFound("Address not found or already inactive.");

            return Ok("Address deleted successfully.");
        }

        [HttpGet("orderdetails/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(Guid orderId)
        {
            var details = await _user.GetOrderDetailsByOrderIdAsync(orderId);

            if (details == null || details.Count == 0)
                return NotFound(new { success = false, message = "No order details found." });

            return Ok(details);
        }


        [HttpGet("payment")]
        public async Task<IActionResult> GetPaymentDetails([FromQuery] string orderId)
        {
            var (finalOrderId, amount, paymentMode) = await _user.GetOrderDetailsAsync(orderId);

            if (string.IsNullOrEmpty(finalOrderId))
                return NotFound(new { message = "No order found." });

            var isPaid = await _user.IsPaymentDoneAsync(finalOrderId);

            return Ok(new
            {
                OrderId = finalOrderId,
                OrderAmount = amount,
                PaymentMode = paymentMode,
                IsPaymentDone = isPaid,
                Message = isPaid ? "Payment already completed." : ""
            });
        }

        [HttpPost("savecashpayment")]
        public async Task<IActionResult> SaveCashPayment([FromQuery] string orderId, [FromQuery] decimal amount)
        {
            if (string.IsNullOrEmpty(orderId) || amount <= 0)
                return BadRequest("Invalid input.");

            try
            {
                await _user.SaveCashPaymentAsync(orderId, amount);
                return Ok(new { message = "Cash payment  successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        [HttpPost("braintree/checkout")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            var (success, transactionId, status, redirectUrl, errorMessage) = await _user.ProcessPaymentAsync(request);

            if (!success)
                return BadRequest(new { success = false, error = errorMessage });

            return Ok(new
            {
                success = true,
                transactionId,
                status,
                redirectUrl,
                message = status == "Pending" ? "Payment is pending. Please wait for confirmation." : null
            });
        }

        [HttpPost("trackorder")]
        public async Task<IActionResult> Track([FromBody] string trackingNumber)
        {
            var (success, status, checkpoints, errorMessage) = await _user.TrackPackageAsync(trackingNumber);

            if (!success)
                return BadRequest(new { success = false, error = errorMessage });

            return Ok(new
            {
                success = true,
                status,
                checkpoints
            });
        }


    }
}
