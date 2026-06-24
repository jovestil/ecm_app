using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;

namespace Mathy.ELM.Api.Controllers;

/// <summary>
/// Authentication and user management endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserContextService _userContextService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEcmLogger _ecmLogger;

    public AuthController(IUserContextService userContextService, ILogger<AuthController> logger, IConfiguration configuration, IEcmLogger ecmLogger)
    {
        _userContextService = userContextService;
        _logger = logger;
        _configuration = configuration;
        _ecmLogger = ecmLogger;
    }

    /// <summary>
    /// Get current user information from JWT token
    /// </summary>
    /// <returns>Current user information including roles and companies</returns>
    /// <response code="200">Returns user information</response>
    /// <response code="401">If the token is invalid or expired</response>
    /// <response code="500">If there's an internal server error</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<object> GetCurrentUser()
    {
        try
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("User is not authenticated");
                _ecmLogger.LogAuthentication(false, "GetCurrentUser", "Unknown", HttpContext.Connection.RemoteIpAddress?.ToString(), "User is not authenticated");
                return Unauthorized(new
                {
                    success = false,
                    error = "NOT_AUTHENTICATED",
                    message = "User is not authenticated",
                    details = "The request does not contain a valid authentication token"
                });
            }

            // Try to get user information
            string userId = null;
            string email = null;
            string name = null;
            List<string> companies = null;
            
            try
            {
                userId = _userContextService.GetUserId();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user ID");
            }

            try
            {
                email = _userContextService.GetUserEmail();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user email");
            }

            try
            {
                name = _userContextService.GetUserName();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user name");
            }

            try
            {
                name = _userContextService.GetUserDisplayName();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user display name");
            }

            try
            {
                companies = _userContextService.GetUserCompanies();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user companies");
                companies = new List<string>();
            }

            var userInfo = new
            {
                userId = userId ?? "Unable to retrieve user ID",
                email = email ?? "Unable to retrieve email",
                name = name ?? "Unable to retrieve name",
                companies = companies ?? new List<string>(),
                roles = User.Claims.Where(c => c.Type == "roles" || c.Type == "role")
                    .Select(c => c.Value).ToList(),
                isSystemAdmin = _userContextService.IsInRole("SystemAdmin"),
                isHRAdmin = _userContextService.IsInRole("HRAdmin"),
                isManager = _userContextService.IsInRole("Manager")
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            _ecmLogger.LogAuthentication(true, "GetCurrentUser", email ?? userId ?? "Unknown", ipAddress, null);

            return Ok(new
            {
                success = true,
                data = userInfo
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            _ecmLogger.LogAuthentication(false, "GetCurrentUser", "Unknown", ipAddress, ex.Message);
            return Unauthorized(new
            {
                success = false,
                error = "UNAUTHORIZED_ACCESS",
                message = "Access denied",
                details = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user info");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            _ecmLogger.LogAuthentication(false, "GetCurrentUser", "Unknown", ipAddress, ex.Message);
            return StatusCode(500, new
            {
                success = false,
                error = "INTERNAL_SERVER_ERROR",
                message = "An error occurred while retrieving user information",
                details = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// Validate token and return basic user info
    /// </summary>
    /// <returns>Token validation result</returns>
    /// <response code="200">Token is valid</response>
    /// <response code="401">Token is invalid or expired</response>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> ValidateToken()
    {
        try
        {
            var isValid = User.Identity?.IsAuthenticated ?? false;

            if (!isValid)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                _ecmLogger.LogAuthentication(false, "ValidateToken", "Unknown", ipAddress, "Token is invalid");
                return Unauthorized(new
                {
                    success = false,
                    message = "Token is invalid"
                });
            }

            var userId = _userContextService.GetUserId();
            var email = _userContextService.GetUserEmail();
            var ipAddress2 = HttpContext.Connection.RemoteIpAddress?.ToString();
            _ecmLogger.LogAuthentication(true, "ValidateToken", email ?? userId, ipAddress2, null);

            return Ok(new
            {
                success = true,
                message = "Token is valid",
                data = new
                {
                    userId = userId,
                    email = email,
                    authenticated = true
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            _ecmLogger.LogAuthentication(false, "ValidateToken", "Unknown", ipAddress, ex.Message);
            return Unauthorized(new
            {
                success = false,
                message = "Token validation failed"
            });
        }
    }

    /// <summary>
    /// Get user's accessible companies
    /// </summary>
    /// <returns>List of companies the user has access to</returns>
    /// <response code="200">Returns list of companies</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="500">If there's an error retrieving companies</response>
    [HttpGet("companies")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<object> GetUserCompanies()
    {
        try
        {
            var companies = _userContextService.GetUserCompanies();
            
            return Ok(new
            {
                success = true,
                data = companies
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user companies");
            return StatusCode(500, new
            {
                success = false,
                message = "Error retrieving companies"
            });
        }
    }

    /// <summary>
    /// Health check endpoint that doesn't require authentication
    /// </summary>
    /// <returns>Service health status</returns>
    /// <response code="200">Service is healthy</response>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            success = true,
            message = "Authentication service is healthy",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Dummy test endpoint that doesn't require authentication
    /// </summary>
    /// <returns>Test data</returns>
    /// <response code="200">Returns test data</response>
    [HttpGet("test")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> Test()
    {
        return Ok(new
        {
            success = true,
            message = "Test endpoint working",
            data = new
            {
                userId = "test-user-123",
                email = "test@example.com",
                name = "Test User",
                roles = new[] { "User", "TestRole" },
                companies = new[] { "001", "002" },
                isAuthenticated = false,
                timestamp = DateTime.UtcNow
            }
        });
    }
}