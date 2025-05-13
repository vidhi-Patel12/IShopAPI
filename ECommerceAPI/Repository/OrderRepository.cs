using ECommerceAPI.Interface;
using ECommerceAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ECommerceAPI.Repository
{
    public class OrderRepository : IOrder
    {
        private readonly string _connectionString;

        public OrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<OrderDetails>> GetOrdersAsync(string orderId = null)
        {
            var orders = new List<OrderDetails>();

            using SqlConnection conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand("GetOrdersWithDetails", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            //cmd.Parameters.AddWithValue("@IShopId", (object?)shopId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderId", (object?)orderId ?? DBNull.Value);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

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

            return orders;
        }

        public async Task<IEnumerable<OrderDetails>> GetOrderDetailsByIdAsync(string orderId = null)
        {
            var orders = new List<OrderDetails>();

            using SqlConnection conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand("GetOrdersWithDetails", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            //cmd.Parameters.AddWithValue("@IShopId", (object?)shopId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderId", (object?)orderId ?? DBNull.Value);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(new OrderDetails
                {
                    OrderId = reader["OrderId"].ToString(),
                    IShopId = Convert.ToInt32(reader["IShopId"]),
                    OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                    PaymentMode = reader["PaymentMode"].ToString(),
                    TotalAmount = Convert.ToDouble(reader["TotalAmount"]),
                    OrderAmount = Convert.ToDouble(reader["OrderAmount"]),
                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                    Shipping = Convert.ToBoolean(reader["Shipping"]),
                    Tax = Convert.ToDouble(reader["Tax"]),
                    DelivaryCharge = Convert.ToDouble(reader["DelivaryCharge"]),
                    FinalAmount = Convert.ToDouble(reader["FinalAmount"]),
                    PromoAmount = Convert.ToDouble(reader["PromoAmount"]),
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
                    ZipCode = Convert.ToInt32(reader["IShopId"]),
                    Mobile = reader["Mobile"] != DBNull.Value ? Convert.ToInt64(reader["Mobile"]) : 0
                });
            }

            return orders;
        }

    }
}
