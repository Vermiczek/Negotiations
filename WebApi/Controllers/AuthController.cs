using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Negotiations.Models.DTOs;
using Negotiations.Services;

namespace Negotiations.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Register a new seller account
        /// </summary>
        /// <remarks>
        /// Creates a new user account with seller role.
        /// </remarks>
        /// <param name="request">User registration details</param>
        /// <returns>Authentication token and user information</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _userService.RegisterAsync(request, "seller");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Register a new admin account
        /// </summary>
        /// <remarks>
        /// Creates a new user account with admin role. This endpoint would typically have additional security.
        /// </remarks>
        /// <param name="request">User registration details</param>
        /// <returns>Authentication token and user information</returns>
        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _userService.RegisterAsync(request, "admin");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        /// <summary>
        /// Authenticate a user
        /// </summary>
        /// <remarks>
        /// Authenticates a user with username and password, returning a JWT token for API access.
        /// </remarks>
        /// <param name="request">Login credentials</param>
        /// <returns>Authentication token and user information</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _userService.LoginAsync(request);
            
            if (response == null)
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            return Ok(response);
        }
    }
}