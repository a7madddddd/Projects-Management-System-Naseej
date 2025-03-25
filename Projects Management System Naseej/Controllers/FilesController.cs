using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Projects_Management_System_Naseej.DTOs.FileDTOs;
using Projects_Management_System_Naseej.Repositories;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Projects_Management_System_Naseej.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Net.Mime;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using Projects_Management_System_Naseej.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Projects_Management_System_Naseej.DTOs.GoogleUserDto;
using Projects_Management_System_Naseej.DTOs.RoleDTOs;
using Google;
using Google.Apis.Drive.v3;
namespace Projects_Management_System_Naseej.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IFileRepository _fileRepository;
        private readonly ILogger<FilesController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly MyDbContext _context;
        private readonly MicrosoftGraphService _graphService;
        private readonly GoogleDriveService _googleDriveService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FilesController(IFileRepository fileRepository, ILogger<FilesController> logger, IConfiguration configuration, IWebHostEnvironment environment, MyDbContext context,
            MicrosoftGraphService graphService, GoogleDriveService googleDriveService, IHttpContextAccessor httpContextAccessor)
        {
            _fileRepository = fileRepository;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _graphService = graphService;
            _googleDriveService = googleDriveService;
            _httpContextAccessor = httpContextAccessor;

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FileDTO>>> GetAllFiles()
        {
            try
            {
                var files = await _fileRepository.GetFilesAsync();
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files");
                return StatusCode(500, "An error occurred while retrieving files.");
            }
        }

        [HttpGet("{fileId}")]
        public async Task<ActionResult<FileDTO>> GetFileById(int fileId)
        {
            try
            {
                var file = await _fileRepository.GetFileByIdAsync(fileId);
                if (file == null)
                {
                    return NotFound($"File with ID {fileId} not found.");
                }
                return Ok(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file with ID {FileId}", fileId);
                return StatusCode(500, "An error occurred while retrieving the file.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<FileDTO>> UploadFile(IFormFile file, [FromForm] CreateFileDTO createFileDTO)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("File is required.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                int uploadedBy;

                // First try to get userId from the token if authenticated
                if (User.Identity.IsAuthenticated)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                                      User.FindFirst("UserId") ??
                                      User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out uploadedBy))
                    {
                        _logger.LogInformation("Using user ID {UserId} from token", uploadedBy);
                    }
                    else
                    {
                        // For debugging purposes, log all claims
                        _logger.LogWarning("Could not find user ID in token. Claims: {Claims}",
                            string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}")));

                        // Fallback to the form data
                        uploadedBy = createFileDTO.UserId ?? 0;
                        _logger.LogInformation("Falling back to form data user ID: {UserId}", uploadedBy);
                    }
                }
                else
                {
                    // Not authenticated, use the user ID from form data
                    uploadedBy = createFileDTO.UserId ?? 0;
                    _logger.LogInformation("User not authenticated, using form data user ID: {UserId}", uploadedBy);
                }

                if (uploadedBy <= 0)
                {
                    return BadRequest("Valid user ID is required either in the token or form data.");
                }

                var uploadedFile = await _fileRepository.UploadFileAsync(file, uploadedBy, createFileDTO.CategoryId, createFileDTO);
                return CreatedAtAction(nameof(GetFileById), new { fileId = uploadedFile.FileId }, uploadedFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, $"An error occurred while uploading the file: {ex.Message}");
            }
        }

        [Authorize(Policy = "Editor")]
        [HttpPut("{fileId}")]
        public async Task<ActionResult<FileDTO>> UpdateFile(int fileId, [FromForm] UpdateFileDTO updateFileDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Extract the token from the Authorization header
                var authHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized("Invalid or missing Authorization header");
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();

                // Validate the token
                var tokenService = new TokenService(_configuration["Jwt:Key"]);
                var principal = tokenService.ValidateToken(token);

                if (principal == null)
                {
                    return Unauthorized("Invalid token");
                }

                var userIdClaim = principal.FindFirst("UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int updatedBy))
                {
                    return BadRequest("Invalid user ID in the token.");
                }

                var updatedFile = await _fileRepository.UpdateFileAsync(
                    fileId,
                    updateFileDTO.File,
                    updatedBy,
                    updateFileDTO
                );

                if (updatedFile == null)
                {
                    return NotFound($"File with ID {fileId} not found.");
                }

                return Ok(updatedFile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the file.", details = ex.Message });
            }
        }


        [HttpPut("update/{fileId}")]
        public async Task<IActionResult> UpdateFile(int fileId, IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("File upload failed: No file or empty file");
                    return BadRequest(new { message = "No file uploaded" });
                }

                // Find the existing file
                var existingFile = await _context.Files.FindAsync(fileId);
                if (existingFile == null)
                {
                    _logger.LogWarning("File not found with ID: {FileId}", fileId);
                    return NotFound($"File with ID {fileId} not found");
                }

                // Validate file extension
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                string[] allowedExtensions = { ".xlsx", ".xls" };

                if (!allowedExtensions.Contains(fileExtension))
                {
                    _logger.LogWarning("Invalid file type: {FileExtension}", fileExtension);
                    return BadRequest(new
                    {
                        message = "Invalid file type",
                        allowedExtensions = allowedExtensions
                    });
                }

                // Use the existing file path
                var filePath = existingFile.FilePath;

                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(directory);

                // Save file, overwriting the existing one
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Verify file was saved
                var savedFileInfo = new FileInfo(filePath);
                _logger.LogInformation("Saved File Details:");
                _logger.LogInformation("Saved File Path: {FilePath}", filePath);
                _logger.LogInformation("Saved File Size: {FileSize} bytes", savedFileInfo.Length);

                // Update file record in database
                existingFile.FileName = Path.GetFileNameWithoutExtension(file.FileName);
                existingFile.FileExtension = fileExtension;
                existingFile.FileSize = file.Length;
                existingFile.LastModifiedDate = DateTime.UtcNow;

                // Save changes
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "File updated successfully",
                    fileId = existingFile.FileId,
                    fileName = existingFile.FileName + existingFile.FileExtension,
                    savedFileSize = file.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comprehensive error updating file with ID {FileId}", fileId);
                return StatusCode(500, new
                {
                    message = "A comprehensive error occurred while updating the file",
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        [HttpGet("excel-view/{fileName}")]
        public async Task<IActionResult> ExcelViewer(string fileName)
        {
            try
            {
                // Find the file in the database
                var file = await _context.Files
                    .FirstOrDefaultAsync(f => f.FileName + f.FileExtension == fileName);

                if (file == null)
                {
                    return NotFound($"File {fileName} not found in database.");
                }

                // Create a public file URL - this needs to be accessible from the internet
                var publicFileUrl = $"https://your-actual-public-domain.com/api/Files/serve/{Uri.EscapeDataString(fileName)}?view=true";

                // Create an Office Online viewer URL
                var viewerUrl = $"https://view.officeapps.live.com/op/view.aspx?src={Uri.EscapeDataString(publicFileUrl)}";

                // Redirect to the Office Online viewer
                return Redirect(viewerUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating Excel viewer URL: {fileName}");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        [HttpDelete("{fileId}")]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            try
            {
                var file = await _context.Files.FindAsync(fileId);
                if (file == null)
                {
                    return NotFound($"File with ID {fileId} not found.");
                }

                // Remove file from database
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();

                // Optionally, delete the physical file
                if (System.IO.File.Exists(file.FilePath))
                {
                    System.IO.File.Delete(file.FilePath);
                }

                return Ok(new
                {
                    message = "File deleted successfully",
                    fileId = fileId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file with ID: {fileId}");
                return StatusCode(500, "An error occurred while deleting the file.");
            }
        }

        //private async Task ValidateFile(IFormFile file)
        //{
        //    if (file.Length == 0)
        //    {
        //        throw new ArgumentException("File is empty.");
        //    }

        //    if (file.Length > 10 * 1024 * 1024) // 10 MB
        //    {
        //        throw new ArgumentException("File size exceeds the maximum allowed size of 10 MB.");
        //    }

        //    var allowedExtensions = new[] { ".pdf", ".xlsx", ".xls", ".docx", ".txt" };
        //    var fileExtension = Path.GetExtension(file.FileName).ToLower();
        //    if (!allowedExtensions.Contains(fileExtension))
        //    {
        //        throw new ArgumentException("Invalid file type. Allowed types are: PDF, Excel, Word, and Text.");
        //    }
        //}

        [HttpGet("serve/{fileName}")]
        public async Task<IActionResult> ServeFile(string fileName, [FromQuery] bool view = false)
        {
            try
            {
                // Find the file in the database
                var file = await _context.Files
                    .FirstOrDefaultAsync(f => f.FileName + f.FileExtension == fileName);
                if (file == null)
                {
                    return NotFound($"File {fileName} not found in database.");
                }

                // Normalize the file path
                var filePath = file.FilePath.Replace("wwwroot/wwwroot", "wwwroot");

                // Check if file exists
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"File not found on the server. Path: {filePath}");
                }

                // Determine content type based on file extension
                var contentType = GetContentType(fileName);

                // Create file stream
                var fileStream = System.IO.File.OpenRead(filePath);

                // Set content disposition based on view parameter
                if (view)
                {
                    Response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
                }
                else
                {
                    Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                }

                // Return the file
                return File(fileStream, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error serving file: {fileName}");
                return StatusCode(500, new
                {
                    message = "An error occurred while serving the file.",
                    details = ex.Message
                });
            }
        }


        private string GeneratePublicFileUrl(Models.File file)
        {
            // Ensure you have a publicly accessible base URL
            string baseUrl = _configuration["PublicBaseUrl"] ?? "https://localhost:44320";

            // Create a URL that points to your file serve endpoint
            string publicFileUrl = $"{baseUrl}/api/Files/serve/{Uri.EscapeDataString(file.FileName + file.FileExtension)}?view=true";

            return publicFileUrl;
        }


        [HttpPut("update-online/{fileId}")]
        public async Task<IActionResult> UpdateFileOnline(int fileId, IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                // Convert IFormFile to Stream
                using var fileStream = file.OpenReadStream();

                // Update or create file in OneDrive
                string onlineFileId = await _graphService.UpdateWordFileAsync(fileId.ToString(), fileStream);

                return Ok(new
                {
                    message = "File updated online successfully",
                    onlineFileId = onlineFileId
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }




        [AllowAnonymous]
        [HttpGet("open-online/{fileId}")]
        public async Task<IActionResult> OpenFileOnline(int fileId)
        {
            try
            {
                var file = await _context.Files.FindAsync(fileId);
                if (file == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "File not found in the system",
                        fileId = fileId
                    });
                }

                // Determine file extension
                string fileExtension = DetermineFileExtension(file);

                // Supported file types - limit to those we can actually display
                string[] supportedExtensions = {
                    ".pdf",  // PDFs can be viewed
                    ".docx", ".doc", // Try with Google Docs viewer
                    ".pptx", ".ppt", // Try with Google Docs viewer
                    ".xlsx", ".xls"  // Add Excel files
                };

                // Validate file extension
                if (string.IsNullOrEmpty(fileExtension) ||
                    !supportedExtensions.Contains(fileExtension))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "File type not supported for online viewing",
                        fileType = fileExtension ?? "unknown",
                        supportedTypes = supportedExtensions
                    });
                }

                // Generate fully qualified, publicly accessible URL
                string baseUrl = _configuration["AppSettings:PublicDomain"] ??
                                 $"{Request.Scheme}://{Request.Host}";
                string fullFileName = file.FileName + file.FileExtension;
                string encodedFileName = Uri.EscapeDataString(fullFileName);

                // Create public file URL
                string publicFileUrl = $"{baseUrl}/api/Files/serve-public/{encodedFileName}";

                // Return structured response with viewer URLs (no direct downloads)
                return Ok(new
                {
                    success = true,
                    originalFileUrl = publicFileUrl,
                    fileName = fullFileName,
                    fileType = fileExtension,
                    viewerUrls = GetViewerUrlsForFileType(publicFileUrl, fileExtension),
                    viewOnly = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Critical error preparing file",
                    details = ex.Message
                });
            }
        }
        private string DetermineFileExtension(Models.File file)
        {
            // Try multiple methods to get file extension
            string[] extensionSources = {
        file.FileExtension,  // First, try stored extension
        Path.GetExtension(file.FileName),  // Then, try extracting from filename
        Path.GetExtension(file.FileName + file.FileExtension)  // Combine and extract
    };

            foreach (var ext in extensionSources)
            {
                if (!string.IsNullOrEmpty(ext))
                {
                    // Ensure extension starts with a dot and is lowercase
                    return ext.StartsWith('.')
                        ? ext.ToLowerInvariant()
                        : '.' + ext.ToLowerInvariant();
                }
            }

            return null;
        }
        private List<string> GetViewerUrlsForFileType(string publicFileUrl, string fileExtension)
        {
            var viewerUrls = new List<string>();

            // Office and PDF viewers - Google Docs viewer and Office Online Viewer
            if (new[] { ".docx", ".doc", ".pptx", ".ppt", ".pdf" }.Contains(fileExtension))
            {
                // Google Docs viewer is more reliable for most scenarios and limits downloading
                viewerUrls.Add($"https://docs.google.com/viewer?url={Uri.EscapeDataString(publicFileUrl)}&embedded=true");
            }

            // Add Office Online Viewer for Excel files
            if (new[] { ".xlsx", ".xls" }.Contains(fileExtension))
            {
                viewerUrls.Add($"https://view.officeapps.live.com/op/view.aspx?src={Uri.EscapeDataString(publicFileUrl)}");
            }

            // Image viewers
            if (new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(fileExtension))
            {
                viewerUrls.Add(publicFileUrl);
            }

            // PDF direct URL for browser's built-in viewer
            if (fileExtension == ".pdf")
            {
                viewerUrls.Add(publicFileUrl);
            }

            // Add direct URL for all other supported types
            if (!viewerUrls.Contains(publicFileUrl))
            {
                viewerUrls.Add(publicFileUrl);
            }

            return viewerUrls;
        }



        [AllowAnonymous]
        [HttpGet("serve-public/{fileName}")]
        public async Task<IActionResult> ServePublicFile(string fileName)
        {
            try
            {
                var file = await _context.Files
                    .FirstOrDefaultAsync(f => f.FileName + f.FileExtension == fileName);

                if (file == null)
                {
                    return NotFound(new { success = false, message = "File not found" });
                }

                // Normalize file path
                var filePath = file.FilePath.Replace("wwwroot/wwwroot", "wwwroot");

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { success = false, message = "File not found on server" });
                }

                // Determine content type
                var contentType = GetContentType(fileName);
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                // Check if this is an Excel file - reject the request
                if (extension == ".xlsx" || extension == ".xls")
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Downloads have been disabled by administrator"
                    });
                }

                // Set strict security headers to prevent downloads
                Response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
                Response.Headers.Add("X-Content-Type-Options", "nosniff");
                Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
                Response.Headers.Add("Pragma", "no-cache");
                Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");

                // For PDF files, use special viewing settings
                if (extension == ".pdf")
                {
                    return ServeFileWithStrictViewing(filePath, "application/pdf", fileName);
                }

                // For all other files, serve with strict viewing settings
                return ServeFileWithStrictViewing(filePath, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving public file");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error serving file",
                    details = ex.Message
                });
            }
        }
        private IActionResult ServeFileWithStrictViewing(string filePath, string contentType, string fileName)
        {
            // Create file stream
            var fileStream = System.IO.File.OpenRead(filePath);

            // Add response header to prevent saving/downloading
            Response.Headers.Add("Content-Security-Policy", "default-src 'self'; script-src 'none'");
            Response.Headers.Add("Content-Disposition", "inline");

            // Return file with strict settings
            return File(fileStream, contentType, fileName, enableRangeProcessing: true);
        }   // Improved content type detection
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            var contentTypes = new Dictionary<string, string>
    {
        { ".pdf", "application/pdf" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".doc", "application/msword" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".xls", "application/vnd.ms-excel" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".txt", "text/plain" },
        { ".rtf", "application/rtf" }
    };

            return contentTypes.TryGetValue(extension, out var contentType)
                ? contentType
                : "application/octet-stream";
        }


        private IActionResult ServeFileWithInlineDisposition(string filePath, string contentType, string fileName)
        {
            // Create file stream
            var fileStream = System.IO.File.OpenRead(filePath);
            // Return file with inline content disposition
            return File(fileStream, contentType, fileName, enableRangeProcessing: true);
        }


        [HttpGet("view/{fileName}")]
        public async Task<IActionResult> ViewFile(string fileName)
        {
            try
            {
                // Find the file in the database
                var file = await _context.Files
                    .FirstOrDefaultAsync(f => f.FileName + f.FileExtension == fileName);

                if (file == null)
                {
                    return NotFound($"File {fileName} not found in database.");
                }

                // Use the full file path from the database
                var filePath = file.FilePath.Replace("wwwroot/wwwroot", "wwwroot");

                // Check if file exists
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"File not found on the server. Path: {filePath}");
                }

                // Determine content type based on file extension
                var contentType = GetContentType(fileName);
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

                // Special handling for Excel files
                if (fileExtension == ".xlsx" || fileExtension == ".xls")
                {
                    // For Excel files, we'll redirect to a publicly accessible URL
                    // First, we need to save the file to a temporary location or make it accessible via a URL

                    // This is a placeholder - you'll need to implement logic to generate a public URL for the file
                    var publicUrl = $"https://localhost:44320/temp/{fileName}";

                    // Redirect to Office Online Viewer
                    return Redirect($"https://view.officeapps.live.com/op/view.aspx?src={Uri.EscapeDataString(publicUrl)}");
                }

                // For other file types, continue with inline viewing
                Response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
                var fileStream = System.IO.File.OpenRead(filePath);
                return File(fileStream, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error viewing file: {fileName}");
                return StatusCode(500, $"An error occurred while viewing the file: {ex.Message}");
            }
        }












        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<FileDTO>>> GetFilesByCategory(int categoryId)
        {
            try
            {
                var files = await _fileRepository.GetFilesByCategoryAsync(categoryId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving files by category.");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FileDTO>>> SearchFiles([FromQuery] string keyword, [FromQuery] int? categoryId)
        {
            try
            {
                var files = await _fileRepository.SearchFilesAsync(keyword, categoryId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while searching for files.");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<FileDTO>>> GetUserFiles(int userId)
        {
            try
            {
                var files = await _fileRepository.GetUserFilesAsync(userId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving user files.");
            }
        }

        [HttpGet("download/{fileId}")]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            try
            {
                // Fetch file details
                var file = await _context.Files.FindAsync(fileId);
                if (file == null)
                {
                    return NotFound($"File with ID {fileId} not found.");
                }

                // Normalize the file path
                var filePath = file.FilePath.Replace("wwwroot/wwwroot", "wwwroot");

                // Check if file exists
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"File not found on the server. Path: {filePath}");
                }

                // Determine content type
                var fullFileName = file.FileName + file.FileExtension;
                var contentType = GetContentType(fullFileName);

                // Return file for download
                return PhysicalFile(filePath, contentType, fullFileName);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, $"Error downloading file with ID: {fileId}");
                return StatusCode(500, $"An error occurred while downloading the file: {ex.Message}");
            }
        }


        [HttpPost("{fileId}/permissions")]
        public async Task<IActionResult> SetFilePermissions(int fileId, [FromBody] Dictionary<int, bool> permissions)
        {
            try
            {
                await _fileRepository.SetFilePermissionsAsync(fileId, permissions);
                return NoContent();
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while setting file permissions.");
            }
        }

        [HttpGet("{fileId}/permissions")]
        public async Task<ActionResult<Dictionary<int, bool>>> GetFilePermissions(int fileId)
        {
            try
            {
                var permissions = await _fileRepository.GetFilePermissionsAsync(fileId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving file permissions.");
            }
        }

        [HttpPost("{fileId}/convert")]
        public async Task<ActionResult<FileConversionDTO>> ConvertFile(int fileId, [FromBody] string targetExtension)
        {
            try
            {
                var convertedFile = await _fileRepository.ConvertFileAsync(fileId, targetExtension);
                return Ok(convertedFile);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error converting file {fileId}");
                return StatusCode(500, "An unexpected error occurred while converting the file.");
            }
        }






















        private int GetCurrentUserId()
        {
            try
            {
                // Try to get user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }

                // If no claim found, try alternative methods
                var httpContext = _httpContextAccessor.HttpContext;
                var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                if (!string.IsNullOrEmpty(token))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

                    var userIdFromToken = jsonToken?.Claims
                        .FirstOrDefault(claim => claim.Type == "UserId" || claim.Type == ClaimTypes.NameIdentifier)?.Value;

                    if (int.TryParse(userIdFromToken, out int tokenUserId))
                    {
                        return tokenUserId;
                    }
                }

                // Fallback to a default user or throw an exception
                _logger.LogWarning("No authenticated user found. Using default user.");
                return GetDefaultUserId();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user ID");
                return GetDefaultUserId();
            }
        }

        private int GetDefaultUserId()
        {
            // Option 1: Return a specific default user ID
            // This should be a valid user ID in your database
            return 2; // Or any other default user ID

            // Option 2: Throw an exception if no default is acceptable
            // throw new UnauthorizedAccessException("No authenticated user found");
        }

        [HttpGet("list-files")]
        public async Task<IActionResult> ListFiles(
      [FromQuery] int PageNumber = 1,
      [FromQuery] int PageSize = 10,
      [FromQuery] string SearchQuery = "")
        {
            try
            {
                var query = new GoogleDriveListRequest
                {
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    SearchQuery = SearchQuery ?? ""
                };

                // Directly fetch files from Google Drive
                var files = await _googleDriveService.ListFilesAsync(query);

                // Fetch user details for the uploaded files
                var userIds = files.Select(f => f.UploadedBy).Distinct().ToList();
                var users = await _context.Users
                    .Where(u => userIds.Contains(u.UserId))
                    .Select(u => new { u.UserId, u.FirstName }) // Adjust based on your User model
                    .ToDictionaryAsync(u => u.UserId, u => u.FirstName);

                // Map to FileDTO 
                var fileDtos = files.Select(f => new FileDTO
                {
                    FileName = f.Name,
                    FileExtension = f.FileExtension,
                    UploadDate = f.CreatedTime ?? DateTime.UtcNow,
                    FileSize = f.Size,
                    GoogleDriveFileId = f.Id,
                    WebViewLink = f.WebViewLink,
                    MimeType = f.MimeType,
                    UploadedBy = f.UploadedBy,
                    UploadedByName = users.TryGetValue(f.UploadedBy, out var userName) ? userName : "Unknown"
                }).ToList();

                // Get total count of files
                var totalCount = await _googleDriveService.GetTotalFileCountAsync(SearchQuery);

                return Ok(new
                {
                    Files = fileDtos,
                    Pagination = new
                    {
                        CurrentPage = query.PageNumber,
                        PageSize = query.PageSize,
                        TotalCount = totalCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files from Google Drive");
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving files",
                    details = ex.Message
                });
            }
        }

        // MIME Type detection
        private string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        // Sanitize filename method (handle Arabic and special characters)
        private string SanitizeFileName(string fileName)
        {
            // Ensure fileName is not null
            if (string.IsNullOrEmpty(fileName))
            {
                return Guid.NewGuid().ToString();
            }

            // Remove or replace invalid characters
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            // Replace invalid characters with underscores
            string sanitizedFileName = invalidChars.Aggregate(fileName, (current, c) => current.Replace(c.ToString(), "_"));

            // Remove non-spacing marks and normalize
            var normalizedFileName = new string(
                sanitizedFileName
                    .Normalize(NormalizationForm.FormD)
                    .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    .ToArray()
            );

            // Truncate filename if it's too long
            int maxLength = 255;
            if (normalizedFileName.Length > maxLength)
            {
                string extension = Path.GetExtension(normalizedFileName);
                normalizedFileName = normalizedFileName.Substring(0, maxLength - extension.Length) + extension;
            }

            return normalizedFileName;
        }


        [HttpPost("upload-to-drive")]
        public async Task<IActionResult> UploadToDrive(IFormFile file)
        {
            try
            {
                // Validate file existence
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded" });
                }

                // Validate authentication first
                var currentUserId = GetCurrentUserId();

                // Sanitize filename
                string originalFileName = file.FileName;
                string sanitizedFileName = SanitizeFileName(originalFileName);

                // Prepare streams
                using var originalFileStream = file.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await originalFileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Upload to Google Drive
                string googleDriveFileId = await UploadToGoogleDrive(memoryStream, sanitizedFileName);

                // Reset stream position
                memoryStream.Position = 0;

                // Save to local storage
                string localFilePath = await SaveToLocalStorage(memoryStream, sanitizedFileName);

                // Prepare file entity
                var fileEntity = new Models.File
                {
                    FileName = Path.GetFileNameWithoutExtension(sanitizedFileName),
                    FileExtension = Path.GetExtension(sanitizedFileName),
                    FilePath = localFilePath,
                    FileSize = file.Length,
                    UploadDate = DateTime.UtcNow,
                    GoogleDriveFileId = googleDriveFileId,
                    IsSyncedWithGoogleDrive = true,
                    UploadedBy = currentUserId,
                    IsActive = true,
                    IsPublic = false
                };

                _context.Files.Add(fileEntity);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "File uploaded successfully",
                    fileName = sanitizedFileName,
                    googleDriveFileId = googleDriveFileId,
                    localFilePath = localFilePath
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized file upload attempt");
                return Unauthorized("You must be logged in to upload files");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during file upload");
                return StatusCode(500, new
                {
                    message = "Database error occurred",
                    details = ex.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file upload");
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred",
                    details = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }  // Validate file method
        private void ValidateFile(IFormFile file)
        {
            // Check file extension
            string[] allowedExtensions = {
                ".pdf", ".docx", ".xlsx", ".pptx",
                ".doc", ".xls", ".ppt", ".txt",
                ".csv", ".jpg", ".jpeg", ".png"
            };

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"File type {extension} is not allowed.");
            }

            // Check file size (e.g., max 50 MB)
            long maxFileSize = 50 * 1024 * 1024; // 50 MB
            if (file.Length > maxFileSize)
            {
                throw new ArgumentException($"File size exceeds the maximum allowed size of {maxFileSize / (1024 * 1024)} MB.");
            }
        }

        private async Task<string> UploadToGoogleDrive(Stream fileStream, string fileName)
        {
            try
            {
                // Ensure stream is at the beginning
                fileStream.Position = 0;

                string mimeType = GetMimeType(fileName);

                _logger.LogInformation($"Attempting to upload {fileName} to Google Drive");
                _logger.LogInformation($"MIME Type: {mimeType}");
                _logger.LogInformation($"Stream Length: {fileStream.Length}");

                string googleDriveFileId = await _googleDriveService.UploadFileAsync(
                    fileStream,
                    fileName,
                    mimeType
                );

                _logger.LogInformation($"Successfully uploaded {fileName} to Google Drive. File ID: {googleDriveFileId}");

                return googleDriveFileId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Detailed Google Drive upload error for {fileName}");

                // Log specific details about the exception
                _logger.LogError($"Exception Type: {ex.GetType().FullName}");
                _logger.LogError($"Exception Message: {ex.Message}");

                // If there's an inner exception, log its details
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception Type: {ex.InnerException.GetType().FullName}");
                    _logger.LogError($"Inner Exception Message: {ex.InnerException.Message}");
                }

                throw; // Re-throw to maintain original stack trace
            }
        }





        // Separate method for local storage
        private async Task<string> SaveToLocalStorage(Stream fileStream, string fileName)
        {
            // Ensure the upload directory exists
            string uploadDirectory = Path.Combine(_configuration["UploadSettings:UploadDirectory"], DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(uploadDirectory);

            // Generate a unique filename to prevent overwriting
            string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            string filePath = Path.Combine(uploadDirectory, uniqueFileName);

            using (var fileWriteStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fileStream.Position = 0;
                await fileStream.CopyToAsync(fileWriteStream);
            }

            return filePath;
        }
        // MIME Type detection


        // Sanitize filename method (handle Arabic and special characters)


        // Helper method for filename sanitization
        private string GenerateFilePath(IFormFile file)
        {
            // Create a structured directory for uploads
            string baseUploadDirectory = Path.Combine(
                _environment.WebRootPath,
                "wwwroot/Files",
                DateTime.Now.Year.ToString(),
                DateTime.Now.Month.ToString("D2")
            );

            // Ensure the directory exists
            Directory.CreateDirectory(baseUploadDirectory);

            // Generate a unique filename
            string uniqueFileName = $"{Guid.NewGuid():N}_{SanitizeFileName(file.FileName)}";

            // Combine the directory path with the unique filename
            string fullPath = Path.Combine(baseUploadDirectory, uniqueFileName);

            return fullPath;
        }
        [HttpGet("download-from-drive/{googleDriveFileId}")]
        public async Task<IActionResult> DownloadFromDrive(string googleDriveFileId)
        {
            try
            {
                var fileStream = await _googleDriveService.DownloadFileAsync(googleDriveFileId);

                // Find file metadata from local database
                var fileEntity = await _context.Files
                    .FirstOrDefaultAsync(f => f.GoogleDriveFileId == googleDriveFileId);

                string fileName = fileEntity?.FileName ?? "downloaded-file";
                string contentType = GetMimeType(fileName);

                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading from Google Drive");
                return StatusCode(500, "An error occurred while downloading from Google Drive");
            }
        }

        [HttpGet("view-link/{fileId}")]
        public async Task<IActionResult> GetFileViewLink(string fileId)
        {
            try
            {
                var webViewLink = await _googleDriveService.GetFileWebViewLink(fileId);

                // Additional validation
                if (string.IsNullOrEmpty(webViewLink))
                {
                    return NotFound(new
                    {
                        message = "No view link could be generated for this file",
                        fileId = fileId
                    });
                }

                return Ok(new
                {
                    webViewLink,
                    fileId
                });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, $"File not found: {fileId}");
                return NotFound(new
                {
                    message = ex.Message,
                    fileId
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, $"Unauthorized access for file {fileId}");
                return Unauthorized(new
                {
                    message = "Authentication or permission error",
                    details = ex.Message,
                    fileId
                });
            }
            catch (GoogleApiException ex)
            {
                // Catch-all for Google API specific exceptions
                _logger.LogError(ex, $"Google API error for file {fileId}");

                return StatusCode((int)ex.HttpStatusCode, new
                {
                    message = "Google Drive API error",
                    details = ex.Message,
                    httpStatusCode = ex.HttpStatusCode,
                    fileId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error getting web view link for file {fileId}");
                return StatusCode(500, new
                {
                    message = "Failed to get file view link",
                    details = ex.Message,
                    fileId
                });
      
            }
        }
        [HttpPut("update-in-drive/{googleDriveFileId}")]
        public async Task<IActionResult> UpdateInDrive(string googleDriveFileId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                using var fileStream = file.OpenReadStream();
                string mimeType = GetMimeType(file.FileName);

                string updatedFileId = await _googleDriveService.UpdateFileAsync(
                    googleDriveFileId,
                    fileStream,
                    mimeType
                );

                return Ok(new
                {
                    message = "File updated in Google Drive",
                    googleDriveFileId = updatedFileId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file in Google Drive");
                return StatusCode(500, "An error occurred while updating file in Google Drive");
            }
        }



        [HttpDelete("delete-file/{fileId}")]
        public async Task<IActionResult> DeleteFileFromGoogleDrive(string fileId)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(fileId))
                {
                    return BadRequest(new
                    {
                        message = "Invalid file ID",
                        status = "error"
                    });
                }

                // Optional: Check user permissions
                var currentUserId = GetCurrentUserId();
                var fileEntity = await _context.Files
                    .FirstOrDefaultAsync(f => f.GoogleDriveFileId == fileId);

                // Permission check
                if (fileEntity == null)
                {
                    return NotFound(new
                    {
                        message = "File not found in database",
                        status = "error"
                    });
                }

                // Verify user has permission to delete
                if (fileEntity.UploadedBy != currentUserId)
                {
                    return Forbid(); // Or return Unauthorized
                }

                // Delete from Google Drive
                await _googleDriveService.DeleteFileAsync(fileId);

                // Remove from local database
                _context.Files.Remove(fileEntity);
                await _context.SaveChangesAsync();

                // Log the deletion
                _logger.LogInformation($"File {fileId} deleted by user {currentUserId}");

                return Ok(new
                {
                    message = "File successfully deleted",
                    fileId = fileId,
                    status = "success"
                });
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Attempted to delete non-existent file: {fileId}");
                return NotFound(new
                {
                    message = "File not found in Google Drive",
                    fileId = fileId,
                    status = "error"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, $"Unauthorized deletion attempt for file {fileId}");
                return Unauthorized(new
                {
                    message = "You are not authorized to delete this file",
                    fileId = fileId,
                    status = "error"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {fileId}");
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred during file deletion",
                    details = ex.Message,
                    fileId = fileId,
                    status = "error"
                });
            }
        }

        [HttpDelete("bulk-delete")]
        public async Task<IActionResult> BulkDeleteFiles([FromBody] List<string> fileIds)
        {
            if (fileIds == null || !fileIds.Any())
            {
                return BadRequest(new
                {
                    message = "No file IDs provided",
                    status = "error"
                });
            }

            var currentUserId = GetCurrentUserId();
            var deletionResults = new List<object>();

            foreach (var fileId in fileIds)
            {
                try
                {
                    var fileEntity = await _context.Files
                        .FirstOrDefaultAsync(f => f.GoogleDriveFileId == fileId);

                    if (fileEntity == null || fileEntity.UploadedBy != currentUserId)
                    {
                        deletionResults.Add(new
                        {
                            fileId,
                            status = "failed",
                            reason = "Not found or unauthorized"
                        });
                        continue;
                    }

                    await _googleDriveService.DeleteFileAsync(fileId);
                    _context.Files.Remove(fileEntity);

                    deletionResults.Add(new
                    {
                        fileId,
                        status = "success"
                    });
                }
                catch (Exception ex)
                {
                    deletionResults.Add(new
                    {
                        fileId,
                        status = "failed",
                        reason = ex.Message
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Bulk delete processed",
                results = deletionResults,
                status = "success"
            });
        }

    }
}
