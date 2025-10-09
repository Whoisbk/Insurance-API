using InsuranceClaimsAPI.Models.DTOs.Auth;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IAuthService authService, ILogger<UsersController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet("firebase/{firebaseUid}")]
        public async Task<IActionResult> GetUserByFirebaseUid(string firebaseUid)
        {
            try
            {
                var user = await _authService.GetUserByFirebaseUidAsync(firebaseUid);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user by Firebase UID: {FirebaseUid}", firebaseUid);
                return StatusCode(500, new { message = "An error occurred while retrieving user" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInsurers()
        {
            try
            {
                var users = await _authService.GetInsurersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting insurers");
                return StatusCode(500, new { message = "An error occurred while retrieving insurers" });
            }
        }
    }
}
