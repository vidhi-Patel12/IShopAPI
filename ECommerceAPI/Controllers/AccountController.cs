using ECommerceAPI.Interface;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ECommerceAPI.Controllers
{
    [Route("account/v1")]
    [ApiController]
    public class AccountController : ControllerBase
    {

        private readonly IAccount _repository;
        private readonly SmsService _smsService;

        public AccountController(IAccount repository, SmsService smsService)
        {
            _repository = repository;
            _smsService = smsService;

        }

        [HttpPost("register")]
        [SwaggerOperation(Summary = "Register for new login.")]
        public IActionResult Register([FromBody] Register model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (_repository.MobileExists(model.Mobile))
            {
                ModelState.AddModelError("Mobile", "This mobile number is already registered.");
                return BadRequest(ModelState);
            }

            model.Role = 1;
            model.IsActive = true;

            _repository.Add(model);
            _repository.Save();

            return Ok(new { message = "Registration successful" });
        }


        [HttpPost("loginwithotp")]
        [SwaggerOperation(Summary = "Login with OTP.")]
        public IActionResult LoginWithOTP([FromBody] long mobile)
        {
            var user = _repository.GetUserByMobile(mobile);

            if (user == null)
            {
                return NotFound(new { message = "Mobile number not registered." });
            }

            int otp = new Random().Next(100000, 999999);

            var loginEntry = new Login
            {
                IShopId = user.IShopId,
                OTP = otp,
                IsValid = true,
                GeneratedAt = DateTime.Now
            };

            _repository.SaveOTP(loginEntry);
            _repository.SaveChanges();

            bool otpSent = _smsService.SendSmsOTP(mobile, otp);

            if (!otpSent)
            {
                return StatusCode(500, new { message = "Failed to send OTP. Please try again." });
            }

            return Ok(new { message = "OTP sent successfully.", mobile = mobile });
        }

        [HttpPost("verifyotp")]
        [SwaggerOperation(Summary = "Verify OTP.")]

        public IActionResult VerifyOTP([FromBody] VerifyOtpRequest request)
        {
            var user = _repository.GetUserByMobile(request.Mobile);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var loginEntry = _repository.GetLatestValidLoginEntry(user.IShopId);
            if (loginEntry == null || loginEntry.OTP != request.OTP)
            {
                return BadRequest(new { message = "Invalid or expired OTP." });
            }

            if (DateTime.Now > loginEntry.GeneratedAt.AddMinutes(5))
            {
                _repository.InvalidateOTP(loginEntry);
                _repository.SaveChanges();
                return BadRequest(new { message = "OTP expired. Please request a new one." });
            }

            // OTP is valid
            _repository.InvalidateOTP(loginEntry);
            _repository.SaveChanges();

            // Return role or session token info
            return Ok(new
            {
                message = "OTP verified successfully.",
                shopId = user.IShopId,
                role = user.Role
            });
        }

        [HttpPost("expireotp")]
        public IActionResult ExpireOtp()
        {
            _repository.ExpireOtp();
            _repository.SaveChanges();

            return Ok(new { message = "Expired OTPs invalidated successfully." });
        }


        [HttpPost("loginwithpassword")]
        [SwaggerOperation(Summary = "Login with Password.")]

        public IActionResult LoginWithPassword([FromBody] LoginRequest request)
        {
            var user = _repository.GetUserByMobile(request.Mobile);
            if (user == null)
            {
                return NotFound(new { message = "Mobile number not registered." });
            }

            if (user.Password != request.Password)
            {
                return BadRequest(new { message = "Invalid password." });
            }

            return Ok(new
            {
                message = "Login successful",
                shopId = user.IShopId,
                role = user.Role
            });
        }


        [HttpPost("logout")]
        [SwaggerOperation(Summary = "Logout user.")]
        public IActionResult Logout()
        {
            // Optionally, log the logout event
            if (HttpContext.Session.GetInt32("IShopId") is int shopId)
            {
                _repository.LogUserLogout(shopId);
                _repository.SaveChanges();
            }

            // Clear session and cookies
            HttpContext.Session.Clear();
            Response.Cookies.Delete("IShopId");
            Response.Cookies.Delete("cartItems");

            // Return JavaScript to clear localStorage and redirect
            string js = @"
    <script>
        localStorage.removeItem('cartItems');
        localStorage.removeItem('checkoutItems');
        localStorage.removeItem('selectedAddress');
        localStorage.removeItem('orderPlaced');
        localStorage.removeItem('cartSynced');
        window.location.href = '/Account/Login';
    </script>";

            return Content(js, "text/html");
        }


        public class LoginRequest
        {
            public long Mobile { get; set; }
            public string Password { get; set; }
        }

    }
}
