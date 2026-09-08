using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using AzureBlobExplorer.Models;
using AzureBlobExplorer.Services;
using AzureBlobExplorer.Data;
using Microsoft.EntityFrameworkCore;

namespace AzureBlobExplorer.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAccessPolicyService _accessPolicyService;
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAccessPolicyService accessPolicyService,
            IAuditService auditService,
            ApplicationDbContext dbContext,
            ILogger<AdminController> logger)
        {
            _accessPolicyService = accessPolicyService;
            _auditService = auditService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet("audit-logs")]
        public async Task<ActionResult<List<AuditLog>>> GetAuditLogs([FromQuery] int days = 30)
        {
            try
            {
                var logs = await _auditService.GetAllAuditLogsAsync(days);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("audit-logs/user/{userId}")]
        public async Task<ActionResult<List<AuditLog>>> GetUserAuditLogs(string userId, [FromQuery] int days = 30)
        {
            try
            {
                var logs = await _auditService.GetUserAuditLogsAsync(userId, days);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user audit logs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("audit-logs/container/{container}")]
        public async Task<ActionResult<List<AuditLog>>> GetContainerAuditLogs(string container, [FromQuery] int days = 30)
        {
            try
            {
                var logs = await _auditService.GetContainerAuditLogsAsync(container, days);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving container audit logs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("access-policy")]
        public async Task<ActionResult> CreateAccessPolicy([FromBody] AccessPolicy policy)
        {
            try
            {
                if (string.IsNullOrEmpty(policy.UserId) || string.IsNullOrEmpty(policy.ContainerName))
                {
                    return BadRequest("UserId and ContainerName are required");
                }

                await _accessPolicyService.CreatePolicyAsync(policy);
                
                _logger.LogInformation($"Access policy created for user {policy.UserId} on container {policy.ContainerName}");
                return Ok(new { message = "Access policy created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating access policy");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("access-policy/{policyId}")]
        public async Task<ActionResult> UpdateAccessPolicy(int policyId, [FromBody] AccessPolicy policy)
        {
            try
            {
                policy.Id = policyId;
                await _accessPolicyService.UpdatePolicyAsync(policy);
                
                _logger.LogInformation($"Access policy {policyId} updated");
                return Ok(new { message = "Access policy updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating access policy");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("access-policy/{policyId}")]
        public async Task<ActionResult> DeleteAccessPolicy(int policyId)
        {
            try
            {
                await _accessPolicyService.DeletePolicyAsync(policyId);
                
                _logger.LogInformation($"Access policy {policyId} deleted");
                return Ok(new { message = "Access policy deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting access policy");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<UserProfile>>> GetUsers()
        {
            try
            {
                var users = await _dbContext.Users.ToListAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("users/{userId}")]
        public async Task<ActionResult<UserProfile>> GetUser(string userId)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("users")]
        public async Task<ActionResult> CreateUser([FromBody] UserProfile userProfile)
        {
            try
            {
                if (string.IsNullOrEmpty(userProfile.Id) || string.IsNullOrEmpty(userProfile.Email))
                {
                    return BadRequest("Id and Email are required");
                }

                _dbContext.Users.Add(userProfile);
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation($"User {userProfile.Email} created");
                return Ok(new { message = "User created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("users/{userId}")]
        public async Task<ActionResult> UpdateUser(string userId, [FromBody] UserProfile userProfile)
        {
            try
            {
                var existingUser = await _dbContext.Users.FindAsync(userId);
                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.DisplayName = userProfile.DisplayName;
                existingUser.Email = userProfile.Email;
                existingUser.Role = userProfile.Role;
                existingUser.IsActive = userProfile.IsActive;
                existingUser.LastLogin = userProfile.LastLogin;

                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation($"User {userId} updated");
                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
