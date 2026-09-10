using System.Security.Claims;
using Hotel_Management_System_Backend_dotNet.DTOs.Auth;
using Hotel_Management_System_Backend_dotNet.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Management_System_Backend_dotNet.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;


        // ---------------------------------------------------------
        // REGISTER
        // it will try to register the user and if successful, it will automatically log the user in.
        // ---------------------------------------------------------

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
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

            if (!request.AcceptTerms)
            {
                return BadRequest(new
                {
                    message = "You must accept the terms and conditions to register."
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
                    errors = result.Errors.Select(error => new
                    {
                        code = error.Code,
                        description = error.Description
                    })
                });
            }

            // Automatically log the newly registered user in.
            var signInResult = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: false,
                lockoutOnFailure: false
            );

            if (!signInResult.Succeeded)
            {
                return StatusCode(500, new
                {
                    message = "Account was created, but automatic login failed."
                });
            }

            return Ok(new
            {
                message = "Registration successful.",
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    userName = user.UserName,
                    email = user.Email
                }
            });

        }

        // ---------------------------------------------------------
        // LOGIN
        // ---------------------------------------------------------

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            if (
                request is null ||
                string.IsNullOrWhiteSpace(request.EmailOrUserName) ||
                string.IsNullOrWhiteSpace(request.Password)
            )
            {
                return BadRequest(new
                {
                    message = "Username/email and password are required."
                });
            }

            ApplicationUser? user = null;

            if (request.EmailOrUserName.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(
                    request.EmailOrUserName
                );
            }

            user ??= await _userManager.FindByNameAsync(
                request.EmailOrUserName
            );

            if (user is null)
            {
                return Unauthorized(new
                {
                    message = "Invalid username/email "
                });
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                request.RememberMe,
                //for security reasons, we will lock the user out after 5 failed attempts for 5 minutes
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                return Ok(new
                {
                    message = "Login successful."
                });
            }

            if (result.IsLockedOut)
            {
                return Unauthorized(new
                {
                    message = "Your account is temporarily locked. Please try again later."
                });
            }

            if (result.IsNotAllowed)
            {
                return Unauthorized(new
                {
                    message = "You are not allowed to log in."
                });
            }

            return Unauthorized(new
            {
                message = "Invalid username/email or password."
            });
        }

        //---------------------------------------------------------
        // Who am I 
        // ---------------------------------------------------------
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Unauthorized();
            }

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Unauthorized();
            }

            // TODO: Roles shall be implemented in the future. For now, we will not return roles in the response.

            // var roles =
            //     await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                userName = user.UserName,
                email = user.Email,
                // roles
            });
        }


        // ---------------------------------------------------------
        // LOGOUT
        // ---------------------------------------------------------

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return Ok(new
            {
                message = "Logout successful."
            });
        }

    }
}
