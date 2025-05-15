using ECommerceAPI.Models;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace ECommerceAPI.Interface
{
    public interface IUser
    {
        Task<List<Products>> GetProductsWithImagesAsync();
        Task<List<Products>> GetProductsByImageIdAsync(int productImageId);

        Task<int> GetProductImageIdAsync(string slug, string typeSlug, string colorSlug);
        Task<Products> GetProductWithImagesAsync(string slug, string typeSlug, string colorSlug);
        Task<List<Products>> GetRelatedProductsAsync(int productId, string slug, string typeSlug, string colorSlug);

        Task<List<ShoppingCart>> GetCartItemsAsync(string iShopId);

        Task<bool> SaveCartAsync(string? userId, List<ShoppingCart> cartItems);

        Task<bool> CartItemExistsAsync(string iShopId, int productsImageId);

        Task<bool> DeleteCartItemAsync(string userId, int productId, int productsImageId);
        Task<bool> DeleteAllCartItemsAsync(string userId);

        Task<List<DelivaryAddresses>> GetAddressesByShopIdAsync(string shopId);

        Task<int> SaveAddressAsync(DelivaryAddresses model);
        Task<bool> SaveOrdersAsync(List<Orders> orders, int shopId);
        Task<(bool Success, string? Message, string? PaymentMode, string? OrderId)>
                SaveCheckoutAsync(Checkout checkout, int shopId, HttpContext httpContext);

        Task<List<Coupan>> GetValidCouponsAsync(double totalAmount);
        Task<List<OrderDetails>> GetOrdersAsync(int iShopId);
        Task<List<OrderDetails>> GetOrderDetailsAsync(int shopId, string orderId);

        Task<bool> CancelOrderAsync(string orderId);
        Task<bool> CheckStockAvailabilityAsync(int productImageId, int requestedQty);
        Task<List<Products>> SearchProductsAsync(string query);
        Task<Register> GetUserByShopIdAsync(int iShopId);
        Task<string> GetPasswordAsync(int iShopId);
        Task<bool> UpdateUserAsync(int iShopId, Register model);
        Task<int> SaveAddressAsync(int iShopId, DelivaryAddresses model, string formMode);

        Task<bool> SoftDeleteAddressAsync(int addressId);

        Task<List<OrderDetailsDto>> GetOrderDetailsByOrderIdAsync(Guid orderId);


        Task<(string OrderId, decimal OrderAmount, string PaymentMode)> GetOrderDetailsAsync(string orderId = null);
        Task<bool> IsPaymentDoneAsync(string orderId);
        Task SaveCashPaymentAsync(string orderId, decimal amount);
        Task<(bool IsSuccess, string TransactionId, string Status, string RedirectUrl, string ErrorMessage)> ProcessPaymentAsync(PaymentRequest request);

        Task<(bool Success, string Status, List<JObject> Checkpoints, string ErrorMessage)> TrackPackageAsync(string trackingNumber);

    }
}
