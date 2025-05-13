using ECommerceAPI.Interface;
using ECommerceAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ECommerceAPI.Repository
{
    public class CoupanRepository : ICoupan
    {
        private readonly string _connectionString;

        public CoupanRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<Coupan>> GetCoupansAsync()
        {
            var coupons = new List<Coupan>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Coupan WHERE IsActive = 1", conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            coupons.Add(new Coupan
                            {
                                CoupanId = reader.GetInt32(reader.GetOrdinal("CoupanId")),
                                CoupanName = reader.GetString(reader.GetOrdinal("CoupanName")),
                                CoupanType = reader["CoupanType"] as string,
                                CoupanCode = reader["CoupanCode"] as string,
                                Discount = reader.GetDouble(reader.GetOrdinal("Discount")),
                                ExpiryDate = reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
                                ValidAmount = reader.GetDouble(reader.GetOrdinal("ValidAmount")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            });
                        }
                    }
                }
            }

            return coupons;
        }

        public async Task<Coupan> GetCoupanByIdAsync(int id)
        {
            Coupan model = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetCoupanById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CoupanId", id);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            model = new Coupan
                            {
                                CoupanId = reader.GetInt32(reader.GetOrdinal("CoupanId")),
                                CoupanName = reader.GetString(reader.GetOrdinal("CoupanName")),
                                CoupanType = reader["CoupanType"] as string,
                                CoupanCode = reader["CoupanCode"] as string,
                                Discount = reader.GetDouble(reader.GetOrdinal("Discount")),
                                ExpiryDate = reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
                                ValidAmount = reader.GetDouble(reader.GetOrdinal("ValidAmount")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            };
                        }
                    }
                }
            }

            return model;
        }

        public async Task<string> AddOrUpdateCoupanAsync(Coupan model)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("AddUpdateCoupan", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@CoupanId", model.CoupanId != 0 ? model.CoupanId : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CoupanName", model.CoupanName);
                    cmd.Parameters.AddWithValue("@CoupanType", model.CoupanType);
                    cmd.Parameters.AddWithValue("@Discount", model.Discount);
                    cmd.Parameters.AddWithValue("@ExpiryDate", model.ExpiryDate.Date);
                    cmd.Parameters.AddWithValue("@ValidAmount", model.ValidAmount);
                    cmd.Parameters.AddWithValue("@IsActive", true);

                    var result = await cmd.ExecuteReaderAsync();
                    string message = "Success";

                    if (await result.ReadAsync())
                    {
                        message = result["Message"].ToString();
                    }

                    return message;
                }
            }
        }

        public async Task<bool> DeleteCoupanAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("UPDATE Coupan SET IsActive = 0 WHERE CoupanId = @CoupanId", conn))
                {
                    cmd.Parameters.AddWithValue("@CoupanId", id);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        public async Task<double?> ValidateCoupanAsync(string coupanCode)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Discount FROM Coupan WHERE CoupanCode = @CoupanCode AND IsActive = 1 AND ExpiryDate >= GETDATE()", conn))
                {
                    cmd.Parameters.AddWithValue("@CoupanCode", coupanCode);

                    var discount = await cmd.ExecuteScalarAsync();
                    return discount != null ? Convert.ToDouble(discount) : (double?)null;
                }
            }
        }

    }
}