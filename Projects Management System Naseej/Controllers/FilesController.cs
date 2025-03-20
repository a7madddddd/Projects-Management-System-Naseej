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

        public FilesController(IFileRepository fileRepository, ILogger<FilesController> logger, IConfiguration configuration, IWebHostEnvironment environment, MyDbContext context
)
        {
            _fileRepository = fileRepository;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _context = context ?? throw new ArgumentNullException(nameof(context));

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


        [HttpPost("update")]
        public async Task<IActionResult> UpdateFile(IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded" });
                }

                // Get current user ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Validate file extension
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                string[] allowedExtensions = { ".xlsx", ".xls" };

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new
                    {
                        message = "Invalid file type",
                        allowedExtensions = allowedExtensions
                    });
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "Files");

                // Ensure directory exists
                Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Create file record in database
                var newFile = new Models.File
                {
                    FileName = Path.GetFileNameWithoutExtension(file.FileName),
                    FileExtension = fileExtension,
                    FilePath = filePath,
                    FileSize = file.Length,
                    UploadDate = DateTime.UtcNow,
                    UploadedBy = userId != null ? int.Parse(userId) : 0,
                    IsActive = true,
                    IsPublic = false
                };

                _context.Files.Add(newFile);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "File updated successfully",
                    fileId = newFile.FileId,
                    fileName = newFile.FileName + newFile.FileExtension
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file");
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the file",
                    details = ex.Message
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


        private async Task ValidateFile(IFormFile file)
        {
            if (file.Length == 0)
            {
                throw new ArgumentException("File is empty.");
            }

            if (file.Length > 10 * 1024 * 1024) // 10 MB
            {
                throw new ArgumentException("File size exceeds the maximum allowed size of 10 MB.");
            }

            var allowedExtensions = new[] { ".pdf", ".xlsx", ".xls", ".docx", ".txt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Allowed types are: PDF, Excel, Word, and Text.");
            }
        }


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
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

                // List of file extensions that should be viewed inline
                string[] inlineExtensions = new[] {
            ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
            ".txt", ".html", ".xls", ".xlsx"
        };

                // Determine if file should be viewed inline
                bool shouldViewInline = view && inlineExtensions.Contains(fileExtension);

                // Create file stream
                var fileStream = System.IO.File.OpenRead(filePath);

                // Set content disposition based on view parameter
                if (shouldViewInline)
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















        // Comprehensive content type method
        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                // Images
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",

                // Documents
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                // Excel files
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                // Text-based
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".csv" => "text/csv",

                // Default
                _ => "application/octet-stream"
            };
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





    }
}
