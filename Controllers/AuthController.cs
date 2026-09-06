using Hotel_Management_System_Backend_dotNet.DTOs.Auth;
using Hotel_Management_System_Backend_dotNet.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Management_System_Backend_dotNet.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(UserManager<ApplicationUser> userManager) : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.ConfirmPassword) )
            {
                return BadRequest(new
                {
                    message = "Full Name, Email, and Password are required."
                });
            }
            if (string.Equals(request.Password, request.ConfirmPassword) is false)
            {
                return BadRequest(new
                {
                    message = "Password and Confirm Password do not match."
                });
            }
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                return Conflict(new
                {
                    message = "An Account with this Email already exists."
                });
            }

            var user = new ApplicationUser
            {
                FullName = request.FullName,
                UserName = request.Email,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Registration failed.",
                    errors = result.Errors
                });
            }

            return Ok(new { Message = "Registration Successful" });

        }

    }
}
