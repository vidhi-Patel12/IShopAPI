using ECommerceAPI.Interface;
using ECommerceAPI.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ECommerceAPI.Controllers
{
    [Route("admin/v1/order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrder _order;

        public OrderController(IOrder order)
        {
            _order = order;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get All Orders.")]
        public async Task<IActionResult> GetOrders([FromQuery] string orderId = null)
        {
            var orders = await _order.GetOrdersAsync(orderId);
            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        [SwaggerOperation(Summary = "Get order by orderId.")]
        public async Task<IActionResult> GetOrderDetailsById(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return BadRequest("Invalid order ID.");
            }

            var orders = await _order.GetOrderDetailsByIdAsync(orderId);

            if (!orders.Any())
            {
                return NotFound("No order found with the given ID.");
            }

            return Ok(orders);
        }
    }
}
