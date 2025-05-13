using ECommerceAPI.Models;

namespace ECommerceAPI.Interface
{
    public interface IDashboard
    {
        Task<DashboardChartData> GetDashboardChartDataAsync(string timeRange, string authId);
    }
}
