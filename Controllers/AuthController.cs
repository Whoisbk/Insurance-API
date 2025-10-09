using InsuranceClaimsAPI.Models.DTOs.Auth;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequest)
        {
            try
            {
                if (!registerRequest.AcceptTerms)
                {
                    return BadRequest(new { message = "You must accept the terms and conditions" });
                }

                if (await _authService.EmailExistsAsync(registerRequest.Email))
                {
                    return BadRequest(new { message = "Email already exists" });
                }

                var result = await _authService.RegisterAsync(registerRequest);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration");
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            try
            {
                var result = await _authService.LoginAsync(loginRequest);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user login");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }


        [HttpPost("add-user")]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequestDto addUserRequest)
        {
            try
            {
                if (await _authService.EmailExistsAsync(addUserRequest.Email))
                {
                    return BadRequest(new { message = "Email already exists" });
                }

                var result = await _authService.AddUserAsync(addUserRequest);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding user");
                return StatusCode(500, new { message = "An error occurred while adding user" });
            }
        }

    }

}
