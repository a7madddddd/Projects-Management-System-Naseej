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
            _logger = logger;
            configuration = _configuration;
            _environment = environment;
            _context = context;

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




        [HttpPut("{fileId}")]
        public async Task<ActionResult<FileDTO>> UpdateFile(int fileId, IFormFile file, [FromForm] UpdateFileDTO updateFileDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst("UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized("User ID not found in the token.");
                }

                if (!int.TryParse(userIdClaim.Value, out int updatedBy))
                {
                    return BadRequest("Invalid user ID in the token.");
                }

                var updatedFile = await _fileRepository.UpdateFileAsync(fileId, file, updatedBy, updateFileDTO);
                if (updatedFile == null)
                {
                    return NotFound($"File with ID {fileId} not found.");
                }
                return Ok(updatedFile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the file.");
            }
        }

        [HttpGet("serve/{fileName}")]
        public async Task<IActionResult> ServeFile(string fileName)
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
                var filePath = file.FilePath;

                // Normalize the file path (remove duplicate wwwroot)
                filePath = filePath.Replace("wwwroot/wwwroot", "wwwroot");

                // Check if file exists
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"File not found on the server. Path: {filePath}");
                }

                // Determine content type based on file extension
                var contentType = GetContentType(fileName);

                // Return file with inline content disposition to open in browser
                return PhysicalFile(filePath, contentType, fileName, true);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, $"Error serving file: {fileName}");
                return StatusCode(500, $"An error occurred while serving the file: {ex.Message}");
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
                var filePath = file.FilePath;

                // Normalize the file path (remove duplicate wwwroot)
                filePath = filePath.Replace("wwwroot/wwwroot", "wwwroot");

                // Check if file exists
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"File not found on the server. Path: {filePath}");
                }

                // Determine content type based on file extension
                var contentType = GetContentType(fileName);

                // Read file bytes
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                // Return file content
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, $"Error viewing file: {fileName}");
                return StatusCode(500, $"An error occurred while viewing the file: {ex.Message}");
            }
        }

        // Helper method to determine content type with inline disposition
        private IActionResult ServeFileWithInlineDisposition(string filePath, string contentType, string fileName)
        {
            // Create file stream
            var fileStream = System.IO.File.OpenRead(filePath);

            // Return file with inline content disposition
            return File(fileStream, contentType, fileName, true);
        }

        // Updated GetContentType method to handle inline disposition
        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}
