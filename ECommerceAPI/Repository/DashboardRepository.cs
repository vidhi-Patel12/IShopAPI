using ECommerceAPI.Interface;
using ECommerceAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace ECommerceAPI.Repository
{
    public class DashboardRepository : IDashboard
    {
        private readonly IConfiguration _config;

        public DashboardRepository(IConfiguration config)
        {
            _config = config;
        }

        public async Task<DashboardChartData> GetDashboardChartDataAsync(string timeRange, string authId)
        {
            var labels = new List<string>();
            var productDict = new Dictionary<string, double>();
            var orderDict = new Dictionary<string, double>();
            var revenueDict = new Dictionary<string, double>();
            var dueDict = new Dictionary<string, double>();

            //if (string.IsNullOrEmpty(authId))
            //    throw new ArgumentException("Invalid shop ID");

            if (timeRange == "day")
            {
                int days = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
                for (int i = 1; i <= days; i++)
                {
                    string label = i.ToString();
                    labels.Add(label);
                    productDict[label] = 0;
                    orderDict[label] = 0;
                    revenueDict[label] = 0;
                    dueDict[label] = 0;
                }
            }
            else if (timeRange == "week")
            {
                var calendar = CultureInfo.CurrentCulture.Calendar;
                var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
                var end = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1);

                var added = new HashSet<string>();
                var current = start;

                while (current <= end)
                {
                    int week = calendar.GetWeekOfYear(current, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    string label = $"Week {week}, {current.Year}";
                    if (!added.Contains(label))
                    {
                        labels.Add(label);
                        added.Add(label);
                        productDict[label] = 0;
                        orderDict[label] = 0;
                        revenueDict[label] = 0;
                        dueDict[label] = 0;
                    }
                    current = current.AddDays(7);
                }
            }

            else if (timeRange == "month")
            {
                for (int i = 1; i <= 12; i++)
                {
                    string label = new DateTime(DateTime.Now.Year, i, 1).ToString("MMM");
                    labels.Add(label);
                    productDict[label] = 0;
                    orderDict[label] = 0;
                    revenueDict[label] = 0;
                    dueDict[label] = 0;
                }
            }
            else if (timeRange == "year")
            {
                int year = DateTime.Now.Year;
                for (int i = year - 9; i <= year; i++)
                {
                    string label = i.ToString();
                    labels.Add(label);
                    productDict[label] = 0;
                    orderDict[label] = 0;
                    revenueDict[label] = 0;
                    dueDict[label] = 0;
                }
            }

            string connectionString = _config.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("GetDashboardData", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@TimeRange", timeRange.ToLower());

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int year = reader.GetInt32(0);
                string label;

                if (timeRange == "day")
                {
                    var date = reader.GetDateTime(1);
                    label = date.Day.ToString();
                }
                else if (timeRange == "week")
                {
                    int week = reader.GetInt32(1);
                    label = $"Week {week}, {year}";
                }
                else if (timeRange == "month")
                {
                    int month = reader.GetInt32(1);
                    label = new DateTime(year, month, 1).ToString("MMM");
                }
                else if (timeRange == "year")
                {
                    label = year.ToString();
                }
                else
                {
                    continue;
                }

                // Safe conversion
                double productCount, orderCount, revenue, duePayment = 0;
                if (timeRange == "year")
                {
                    productCount = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1));
                    orderCount = reader.IsDBNull(2) ? 0 : Convert.ToDouble(reader.GetValue(2));
                    revenue = reader.IsDBNull(3) ? 0 : Convert.ToDouble(reader.GetValue(3));
                    duePayment = reader.IsDBNull(4) ? 0 : Convert.ToDouble(reader.GetValue(4));
                }
                else
                {
                    productCount = reader.IsDBNull(2) ? 0 : Convert.ToDouble(reader.GetValue(2));
                    orderCount = reader.IsDBNull(3) ? 0 : Convert.ToDouble(reader.GetValue(3));
                    revenue = reader.IsDBNull(4) ? 0 : Convert.ToDouble(reader.GetValue(4));
                    duePayment = reader.IsDBNull(5) ? 0 : Convert.ToDouble(reader.GetValue(5));
                }

                if (productDict.ContainsKey(label))
                {
                    productDict[label] = productCount;
                    orderDict[label] = orderCount;
                    revenueDict[label] = revenue;
                    dueDict[label] = duePayment;
                }
            }

            return new DashboardChartData
            {
                Labels = labels,
                ProductData = labels.Select(l => productDict[l]).ToList(),
                OrderData = labels.Select(l => orderDict[l]).ToList(),
                RevenueData = labels.Select(l => revenueDict[l]).ToList(),
                DuePaymentData = labels.Select(l => dueDict[l]).ToList()
            };
        }
    }

}