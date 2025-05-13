using ECommerceAPI.Interface;
using ECommerceAPI.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ECommerceAPI.Controllers
{
    [Route("admin/v1/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboard _dashboard;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboard dashboard, ILogger<DashboardController> logger)
        {
            _dashboard= dashboard;
            _logger = logger;
        }

        [HttpGet("chart-data")]
        public async Task<IActionResult> GetDashboardChartData(string timeRange = "day")
        {
            try
            {
                var authId = Request.Cookies["IShopId"];
                var data = await _dashboard.GetDashboardChartDataAsync(timeRange, authId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dashboard chart data");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
