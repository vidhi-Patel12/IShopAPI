using ECommerceAPI.Interface;
using ECommerceAPI.Models;
using ECommerceAPI.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using Swashbuckle.AspNetCore.Annotations;
using System;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ECommerceAPI.Controllers
{
    [Route("admin/v1/coupan")]
    [ApiController]
    public class CoupanController : ControllerBase
    {
        private readonly ICoupan _coupan;

        public CoupanController(ICoupan coupanRepository)
        {
            _coupan = coupanRepository;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get All Coupans.")]
        public async Task<IActionResult> GetCoupansAsync()
        {
            var coupons = await _coupan.GetCoupansAsync();
            return Ok(coupons);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get coupan by Id.")]
        public async Task<IActionResult> GetCoupanById(int id)
        {
            var coupan = await _coupan.GetCoupanByIdAsync(id);

            if (coupan == null)
                return NotFound($"Coupan with ID {id} not found.");

            return Ok(coupan);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Create new coupan and update coupan.")]
        public async Task<IActionResult> AddOrUpdateCoupan([FromBody] Coupan model)
        {
            try
            {
                if (model.ExpiryDate == default)
                    return BadRequest("Invalid expiry date");

                var message = await _coupan.AddOrUpdateCoupanAsync(model);
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete coupan by Id.")]
        public async Task<IActionResult> DeleteCoupan(int id)
        {
            bool result = await _coupan.DeleteCoupanAsync(id);

            if (result)
                return Ok(new { message = "Coupan deleted successfully." });

            return NotFound(new { message = "Coupan not found." });
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateCoupan(string coupanCode)
        {
            try
            {
                var discount = await _coupan.ValidateCoupanAsync(coupanCode);

                if (discount.HasValue)
                {
                    return Ok(new { success = true, discount = discount.Value });
                }
                else
                {
                    return NotFound(new { success = false, message = "Invalid or expired coupon code." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
