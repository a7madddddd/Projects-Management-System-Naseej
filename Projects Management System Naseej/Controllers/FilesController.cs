using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Projects_Management_System_Naseej.DTOs.FileDTOs;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileRepository _fileRepository;

        public FilesController(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
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
                // Log the exception
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
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving the file.");
            }
        }

        [HttpPost]
        [Authorize(Policy = "Uploader")]
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

                var userIdClaim = User.FindFirst("UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized("User ID not found in the token.");
                }

                if (!int.TryParse(userIdClaim.Value, out int uploadedBy))
                {
                    return BadRequest("Invalid user ID in the token.");
                }

                var uploadedFile = await _fileRepository.UploadFileAsync(file, uploadedBy, createFileDTO.CategoryId, createFileDTO);
                return CreatedAtAction(nameof(GetFileById), new { fileId = uploadedFile.FileId }, uploadedFile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while uploading the file.");
            }
        }

        [HttpPut("{fileId}")]
        [Authorize(Policy = "Editor")]
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



        [HttpDelete("{fileId}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            try
            {
                await _fileRepository.DeleteFileAsync(fileId);
                return NoContent();
            }
            catch (Exception ex)
            {
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

        [HttpGet("{fileId}/download")]
        [Authorize(Policy = "Viewer")]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            try
            {
                var fileBytes = await _fileRepository.DownloadFileAsync(fileId);
                if (fileBytes == null)
                {
                    return NotFound($"File with ID {fileId} not found.");
                }

                var file = await _fileRepository.GetFileByIdAsync(fileId);
                return File(fileBytes, "application/octet-stream", file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while downloading the file.");
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
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while converting the file.");
            }
        }
    }
}
