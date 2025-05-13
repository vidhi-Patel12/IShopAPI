using ECommerceAPI.Models;

namespace ECommerceAPI.Interface
{
    public interface IOrder
    {
        Task<IEnumerable<OrderDetails>> GetOrdersAsync(string orderId = null);
        Task<IEnumerable<OrderDetails>> GetOrderDetailsByIdAsync(string orderId = null);

    }
}
