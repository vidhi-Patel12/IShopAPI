namespace ECommerceAPI.Models
{
    public class DashboardDataItem
    {
        public int Year { get; set; }
        public int TimeValue { get; set; } 
        public DateTime? Date { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
        public double Revenue { get; set; }
        public double DuePayment { get; set; }
    }
}
