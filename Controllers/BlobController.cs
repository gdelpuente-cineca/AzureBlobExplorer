using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using AzureBlobExplorer.Models;
using AzureBlobExplorer.Services;

namespace AzureBlobExplorer.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BlobController : ControllerBase
    {
        private readonly IAzureBlobService _blobService;
        private readonly IAuditService _auditService;
        private readonly IAccessPolicyService _accessPolicyService;
        private readonly ILogger<BlobController> _logger;

        public BlobController(
            IAzureBlobService blobService,
            IAuditService auditService,
            IAccessPolicyService accessPolicyService,
            ILogger<BlobController> logger)
        {
            _blobService = blobService;
            _auditService = auditService;
            _accessPolicyService = accessPolicyService;
            _logger = logger;
        }

        [HttpGet("containers")]
        public async Task<ActionResult<List<string>>> GetContainers()
        {
            try
            {
                var userId = User.GetObjectId();
                var containers = await _blobService.ListContainersAsync(userId);
                return Ok(containers);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing containers");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("containers/{container}/blobs")]
        public async Task<ActionResult<List<BlobItem>>> GetBlobs(string container, [FromQuery] string? prefix)
        {
            try
            {
                var userId = User.GetObjectId();
                var blobs = await _blobService.ListBlobsAsync(container, prefix, userId);
                return Ok(blobs);
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditService.LogActionAsync(User.GetObjectId(), "Browse", container, null, 
                    isSuccessful: false, errorMessage: "Access Denied");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error listing blobs in container {container}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadBlob([FromQuery] string container, [FromQuery] string blob)
        {
            try
            {
                var userId = User.GetObjectId();
                
                // Verify access
                var hasAccess = await _accessPolicyService.HasAccessAsync(userId, container, blob, BlobPermission.Read);
                if (!hasAccess)
                {
                    await _auditService.LogActionAsync(userId, "Download", container, blob, 
                        isSuccessful: false, errorMessage: "Access Denied");
                    return Forbid();
                }

                // Get blob properties for size
                var properties = await _blobService.GetBlobPropertiesAsync(container, blob);
                
                // Download blob
                var stream = await _blobService.DownloadBlobAsync(container, blob);

                // Log the download
                await _auditService.LogActionAsync(userId, "Download", container, blob, 
                    fileSize: properties?.Size, isSuccessful: true);

                _logger.LogInformation($"User {userId} downloaded blob {blob} from container {container}");
                
                return File(stream, "application/octet-stream", Path.GetFileName(blob));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading blob {blob} from container {container}");
                await _auditService.LogActionAsync(User.GetObjectId(), "Download", container, blob, 
                    isSuccessful: false, errorMessage: ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("upload")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UploadBlob([FromQuery] string container, [FromForm] IFormFile file, [FromQuery] string? prefix = "")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("File is required");
                }

                var userId = User.GetObjectId();

                // Verify access
                var hasAccess = await _accessPolicyService.HasAccessAsync(userId, container, prefix, BlobPermission.Write);
                if (!hasAccess)
                {
                    await _auditService.LogActionAsync(userId, "Upload", container, file.FileName, 
                        isSuccessful: false, errorMessage: "Access Denied");
                    return Forbid();
                }

                await _blobService.UploadBlobAsync(container, file, prefix);

                // Log the upload
                await _auditService.LogActionAsync(userId, "Upload", container, file.FileName, 
                    fileSize: file.Length, isSuccessful: true);

                _logger.LogInformation($"User {userId} uploaded blob {file.FileName} to container {container}");
                
                return Ok(new { message = "File uploaded successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading blob to container {container}");
                await _auditService.LogActionAsync(User.GetObjectId(), "Upload", container, file?.FileName, 
                    isSuccessful: false, errorMessage: ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("delete")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteBlob([FromQuery] string container, [FromQuery] string blob)
        {
            try
            {
                var userId = User.GetObjectId();

                // Verify access
                var hasAccess = await _accessPolicyService.HasAccessAsync(userId, container, blob, BlobPermission.Delete);
                if (!hasAccess)
                {
                    await _auditService.LogActionAsync(userId, "Delete", container, blob, 
                        isSuccessful: false, errorMessage: "Access Denied");
                    return Forbid();
                }

                await _blobService.DeleteBlobAsync(container, blob);

                // Log the deletion
                await _auditService.LogActionAsync(userId, "Delete", container, blob, isSuccessful: true);

                _logger.LogInformation($"User {userId} deleted blob {blob} from container {container}");
                
                return Ok(new { message = "Blob deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting blob {blob} from container {container}");
                await _auditService.LogActionAsync(User.GetObjectId(), "Delete", container, blob, 
                    isSuccessful: false, errorMessage: ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
