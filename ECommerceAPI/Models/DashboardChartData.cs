namespace ECommerceAPI.Models
{
    public class DashboardChartData
    {
        public List<string> Labels { get; set; }
        public List<double> ProductData { get; set; }
        public List<double> OrderData { get; set; }
        public List<double> RevenueData { get; set; }
        public List<double> DuePaymentData { get; set; }
    }
}
