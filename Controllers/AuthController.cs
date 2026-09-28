using System.Security.Claims;
using Hotel_Management_System_Backend_dotNet.DTOs.Auth;
using Hotel_Management_System_Backend_dotNet.Entities;
using Hotel_Management_System_Backend_dotNet.Entities.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Management_System_Backend_dotNet.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration) : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly IConfiguration _configuration = configuration;


        // ---------------------------------------------------------
        // REGISTER
        // it will try to register the user and if successful, it will automatically log the user in.
        // also it will assign the user to the "Guest" role by default.
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

            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Guest);
            if (!roleResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Failed to assign role to the user.",
                    errors = roleResult.Errors.Select(error => new
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
                    email = user.Email
                }
            });

        }

        // ---------------------------------------------------------
        // NORMAL EMAIL/PASSWORD LOGIN
        // ---------------------------------------------------------

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            if (
                request is null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password)
            )
            {
                return BadRequest(new
                {
                    message = "Email and password are required."
                });
            }

            ApplicationUser? user = null;

            user = await _userManager.FindByEmailAsync(
                request.Email
            );


            if (user is null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email "
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
                message = "Invalid email or password."
            });
        }
        // ---------------------------------------------------------
        // START EXTERNAL LOGIN
        //
        // GET:
        // /api/auth/external/google
        // /api/auth/external/facebook
        // /api/auth/external/linkedin
        // /api/auth/external/github
        // ---------------------------------------------------------

        [AllowAnonymous]
        [HttpGet("external/{provider}")]
        public IActionResult ExternalLogin(
            string provider
        )
        {
            var scheme =
                NormalizeProviderScheme(provider);

            if (scheme is null)
            {
                return BadRequest(new
                {
                    message =
                        "Unsupported external login provider."
                });
            }

            var redirectUri =
                "/api/auth/external-callback";

            var properties =
                _signInManager.ConfigureExternalAuthenticationProperties(
                    scheme,
                    redirectUri
                );

            return Challenge(
                properties,
                scheme
            );
        }


        // ---------------------------------------------------------
        // EXTERNAL LOGIN CALLBACK
        //
        // The OAuth provider redirects here after successful
        // authentication.
        // ---------------------------------------------------------

        [AllowAnonymous]
        [HttpGet("external-callback")]
        public async Task<IActionResult> ExternalCallback()
        {
            var info =
                await _signInManager.GetExternalLoginInfoAsync();

            if (info is null)
            {
                return ExternalLoginFailure(
                    "The external login information could not be read."
                );
            }

            var provider =
                info.LoginProvider;

            var providerKey =
                info.ProviderKey;

            var email =
                info.Principal.FindFirstValue(
                    ClaimTypes.Email
                );

            var fullName =
                info.Principal.FindFirstValue(
                    ClaimTypes.Name
                );

            var profilePicture =
                GetProfilePicture(
                    info.Principal
                );

            if (string.IsNullOrWhiteSpace(email))
            {
                await _signInManager.SignOutAsync();

                return ExternalLoginFailure(
                    $"The {provider} account did not provide an email address. " +
                    "Please make sure email permission is enabled."
                );
            }

            // -----------------------------------------------------
            // STEP 1:
            // Does this exact Google/Facebook/etc login already
            // belong to a Velora account?
            // -----------------------------------------------------

            var user =
                await _userManager.FindByLoginAsync(
                    provider,
                    providerKey
                );

            if (user is not null)
            {
                await _signInManager.SignInAsync(
                    user,
                    isPersistent: true
                );

                return RedirectToFrontend();
            }


            // -----------------------------------------------------
            // STEP 2:
            // Does a Velora account already exist with this email?
            // -----------------------------------------------------

            user =
                await _userManager.FindByEmailAsync(
                    email
                );

            if (user is not null)
            {
                // -------------------------------------------------
                // We automatically link the provider to the
                // existing account.
                //
                // This means:
                //
                // existing password account
                // +
                // same verified social email
                // =
                // one Velora account with multiple login methods.
                // -------------------------------------------------

                var existingLogins =
                    await _userManager.GetLoginsAsync(user);

                var alreadyLinked =
                    existingLogins.Any(
                        login =>
                            login.LoginProvider == provider &&
                            login.ProviderKey == providerKey
                    );

                if (!alreadyLinked)
                {
                    var addLoginResult =
                        await _userManager.AddLoginAsync(
                            user,
                            new UserLoginInfo(
                                provider,
                                providerKey,
                                provider
                            )
                        );

                    if (!addLoginResult.Succeeded)
                    {
                        await _signInManager.SignOutAsync();

                        return ExternalLoginFailure(
                            "The social account could not be linked."
                        );
                    }
                }

                // Update profile data if the provider has a
                // picture and our account doesn't already have one.
                var profileChanged = false;

                if (
                    !string.IsNullOrWhiteSpace(profilePicture) &&
                    string.IsNullOrWhiteSpace(
                        user.ProfilePictureUrl
                    )
                )
                {
                    user.ProfilePictureUrl =
                        profilePicture;

                    profileChanged = true;
                }

                if (
                    string.IsNullOrWhiteSpace(user.FullName) &&
                    !string.IsNullOrWhiteSpace(fullName)
                )
                {
                    user.FullName = fullName;
                    profileChanged = true;
                }

                if (profileChanged)
                {
                    await _userManager.UpdateAsync(user);
                }

                await _signInManager.SignInAsync(
                    user,
                    isPersistent: true
                );

                return RedirectToFrontend();
            }


            // -----------------------------------------------------
            // STEP 3:
            // No account exists.
            //
            // Create a new Velora Guest account.
            // -----------------------------------------------------

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName =
                    string.IsNullOrWhiteSpace(fullName)
                        ? email.Split('@')[0]
                        : fullName,
                ProfilePictureUrl =
                    profilePicture,
                CreatedAt = DateTime.UtcNow
            };

            var createResult =
                await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                await _signInManager.SignOutAsync();

                return ExternalLoginFailure(
                    "Your Velora account could not be created."
                );
            }


            // -----------------------------------------------------
            // Assign Guest role
            // -----------------------------------------------------

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    Roles.Guest
                );

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                await _signInManager.SignOutAsync();

                return ExternalLoginFailure(
                    "Your account was created but the Guest role could not be assigned."
                );
            }


            // -----------------------------------------------------
            // Link external provider
            // -----------------------------------------------------

            var loginResult =
                await _userManager.AddLoginAsync(
                    user,
                    new UserLoginInfo(
                        provider,
                        providerKey,
                        provider
                    )
                );

            if (!loginResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                await _signInManager.SignOutAsync();

                return ExternalLoginFailure(
                    "Your account was created but the social login could not be linked."
                );
            }


            // -----------------------------------------------------
            // Log the user into Velora.
            // -----------------------------------------------------

            await _signInManager.SignInAsync(
                user,
                isPersistent: true
            );

            return RedirectToFrontend();
        }


        // ---------------------------------------------------------
        // GET CURRENT USER
        // ---------------------------------------------------------

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (userId is null)
            {
                return Unauthorized();
            }

            var user =
                await _userManager.FindByIdAsync(
                    userId
                );

            if (user is null)
            {
                return Unauthorized();
            }

            var roles =
                await _userManager.GetRolesAsync(user);

            var logins =
                await _userManager.GetLoginsAsync(user);

            return Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                profilePictureUrl =
                    user.ProfilePictureUrl,
                roles,
                providers =
                    logins
                        .Select(login => login.LoginProvider)
                        .Distinct()
                        .ToArray()
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


        // =========================================================
        // HELPERS
        // =========================================================

        private string? NormalizeProviderScheme(
            string provider
        )
        {
            return provider.Trim().ToLowerInvariant() switch
            {
                "google" =>
                    GoogleDefaults.AuthenticationScheme,

                "facebook" =>
                    FacebookDefaults.AuthenticationScheme,

                "linkedin" =>
                    "LinkedIn",

                "github" =>
                    "GitHub",

                _ => null
            };
        }


        private string? GetProfilePicture(
            ClaimsPrincipal principal
        )
        {
            // -----------------------------------------------------
            // Google:
            // urn:velora:profile_picture
            //
            // LinkedIn:
            // urn:velora:profile_picture
            //
            // GitHub:
            // urn:velora:profile_picture
            // -----------------------------------------------------

            var picture =
                principal.FindFirst(
                    "urn:velora:profile_picture"
                )?.Value;

            if (!string.IsNullOrWhiteSpace(picture))
            {
                return picture;
            }


            // -----------------------------------------------------
            // Facebook
            //
            // The Facebook handler's default claim mapping doesn't
            // map the nested picture object to ClaimTypes.Picture.
            //
            // Therefore we try the standard picture claim too.
            // -----------------------------------------------------

            picture =
                principal.FindFirst(
                    "urn:facebook:picture"
                )?.Value;

            return picture;
        }


        private RedirectResult RedirectToFrontend()
        {
            return Redirect(
                $"{GetFrontendBaseUrl()}/"
            );
        }

        private RedirectResult ExternalLoginFailure(string message)
        {
            var encodedMessage = Uri.EscapeDataString(message);

            // The login UI is a modal, not a route, so return to the home page
            // and let the frontend open the modal and show the message.
            var url = $"{GetFrontendBaseUrl()}/?authError={encodedMessage}";

            return Redirect(url);
        }

        private string GetFrontendBaseUrl()
        {
            var baseUrl = _configuration["Frontend:BaseUrl"]
                ?? "http://localhost:3000";

            return baseUrl.TrimEnd('/');
        }
    }
}