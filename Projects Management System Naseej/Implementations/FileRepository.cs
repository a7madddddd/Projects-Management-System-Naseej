using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Projects_Management_System_Naseej.DTOs.FileDTOs;
using Projects_Management_System_Naseej.DTOs.UserDTOs;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;
using File = Projects_Management_System_Naseej.Models.File;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Projects_Management_System_Naseej.DTOs.GoogleUserDto;
using Projects_Management_System_Naseej.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Projects_Management_System_Naseej.Implementations
{
    public class FileRepository : IFileRepository
    {
        private readonly MyDbContext _context;
        private readonly IEnumerable<IFileTypeHandler> _fileTypeHandlers;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly GoogleDriveService _googleDriveService;
        public FileRepository(MyDbContext context, IEnumerable<IFileTypeHandler> fileTypeHandlers, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IWebHostEnvironment environment
    , GoogleDriveService googleDriveService)
        {
            _context = context;
            _fileTypeHandlers = fileTypeHandlers;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _environment = environment;
            _googleDriveService = googleDriveService;
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



        public async Task<IEnumerable<FileDTO>> GetFilesAsync()
        {
            return await _context.Files
                .Select(f => new FileDTO
                {
                    FileId = f.FileId,
                    FileName = f.FileName,
                    FileExtension = f.FileExtension,
                    FilePath = f.FilePath,
                    FileSize = f.FileSize,
                    CategoryId = f.CategoryId,
                    UploadedBy = f.UploadedBy,
                    UploadDate = f.UploadDate ?? DateTime.MinValue,
                    LastModifiedBy = f.LastModifiedBy,
                    LastModifiedDate = f.LastModifiedDate,
                    IsActive = f.IsActive ?? false,
                    IsPublic = f.IsPublic ?? false,
                })
                .ToListAsync();
        }

        public async Task<FileDTO> GetFileByIdAsync(int fileId)
        {
            var file = await _context.Files.FindAsync(fileId);
            if (file == null) return null;

            return new FileDTO
            {
                FileId = file.FileId,
                FileName = file.FileName,
                FileExtension = file.FileExtension,
                FilePath = file.FilePath,
                FileSize = file.FileSize,
                CategoryId = file.CategoryId,
                UploadedBy = file.UploadedBy,
                UploadDate = file.UploadDate ?? DateTime.MinValue,
                LastModifiedBy = file.LastModifiedBy,
                LastModifiedDate = file.LastModifiedDate,
                IsActive = file.IsActive ?? false,
                IsPublic = file.IsPublic ?? false
            };
        }


        public async Task<FileDTO> UploadFileAsync(IFormFile file, int uploadedBy, int categoryId, CreateFileDTO createFileDTO)
        {
            try
            {
                // Validate file
                ValidateFile(file);

                // Get upload directory from configuration
                var uploadDirectory = _configuration["UploadSettings:UploadDirectory"];
                var uploadsPath = Path.Combine(_environment.WebRootPath, uploadDirectory);

                // Ensure directory exists
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename to prevent overwriting
                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create file entity
                var newFile = new File
                {
                    FileName = createFileDTO.FileName, // Use provided file name
                    FileExtension = Path.GetExtension(file.FileName),
                    FilePath = filePath, // Full path to the saved file
                    FileSize = file.Length,
                    CategoryId = categoryId,
                    UploadedBy = uploadedBy,
                    IsPublic = createFileDTO.IsPublic,
                    UploadDate = DateTime.UtcNow,
                    IsActive = true
                };

                // Save to database
                _context.Files.Add(newFile);
                await _context.SaveChangesAsync();

                // Return DTO
                return new FileDTO
                {
                    FileId = newFile.FileId,
                    FileName = newFile.FileName,
                    FileExtension = newFile.FileExtension,
                    FilePath = newFile.FilePath,
                    FileSize = newFile.FileSize,
                    CategoryId = newFile.CategoryId,
                    UploadedBy = newFile.UploadedBy,
                    UploadDate = newFile.UploadDate.GetValueOrDefault(),
                    IsActive = newFile.IsActive ?? false,
                    IsPublic = newFile.IsPublic ?? false,
                };
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new Exception($"An error occurred while uploading the file: {ex.Message}", ex);
            }
        }


        public async Task<FileDTO> UpdateFileAsync(int fileId, IFormFile file, int updatedBy, UpdateFileDTO updateFileDTO)
        {
            try
            {

                var existingFile = await _context.Files.FindAsync(fileId);
                if (existingFile == null)
                {
                    return null;
                }

                // Prepare upload paths
                var uploadDirectory = _configuration["UploadSettings:UploadDirectory"];
                var uploadsPath = Path.Combine(_environment.WebRootPath, uploadDirectory);

                // Ensure directory exists
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Handle file update
                if (file != null && file.Length > 0)
                {
                    // Validate file
                    await ValidateFile(file);

                    // Generate unique filename
                    var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var newFilePath = Path.Combine(uploadsPath, uniqueFileName);

                    // Delete existing file if it exists
                    if (!string.IsNullOrEmpty(existingFile.FilePath) && System.IO.File.Exists(existingFile.FilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(existingFile.FilePath);
                        }
                        catch (IOException ex)
                        {
                            throw new Exception($"Failed to delete existing file: {ex.Message}", ex);
                        }
                    }

                    // Save new file
                    try
                    {
                        using (var stream = new FileStream(newFilePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }
                    catch (IOException ex)
                    {
                        throw new Exception($"Failed to save new file: {ex.Message}", ex);
                    }

                    // Update file properties
                    existingFile.FileName = updateFileDTO.FileName ?? Path.GetFileNameWithoutExtension(file.FileName);
                    existingFile.FileExtension = Path.GetExtension(file.FileName);
                    existingFile.FilePath = newFilePath;
                    existingFile.FileSize = file.Length;
                }
                else
                {
                    // Update only file name if no new file is provided
                    if (!string.IsNullOrEmpty(updateFileDTO.FileName))
                    {
                        existingFile.FileName = updateFileDTO.FileName;
                    }
                }

                // Update other properties
                if (updateFileDTO.CategoryId.HasValue)
                {
                    existingFile.CategoryId = updateFileDTO.CategoryId.Value;
                }

                if (updateFileDTO.IsPublic.HasValue)
                {
                    existingFile.IsPublic = updateFileDTO.IsPublic.Value;
                }

                existingFile.LastModifiedBy = updatedBy;
                existingFile.LastModifiedDate = DateTime.UtcNow;

                // Save changes
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    throw new Exception($"Database error while updating file: {ex.Message}", ex);
                }

                // Return updated file DTO
                return new FileDTO
                {
                    FileId = existingFile.FileId,
                    FileName = existingFile.FileName,
                    FileExtension = existingFile.FileExtension,
                    FilePath = existingFile.FilePath,
                    FileSize = existingFile.FileSize,
                    CategoryId = existingFile.CategoryId,
                    UploadedBy = existingFile.UploadedBy,
                    UploadDate = existingFile.UploadDate ?? DateTime.MinValue,
                    LastModifiedBy = existingFile.LastModifiedBy,
                    LastModifiedDate = existingFile.LastModifiedDate,
                    IsActive = existingFile.IsActive ?? false,
                    IsPublic = existingFile.IsPublic ?? false
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while updating the file: {ex.Message}", ex);
            }
        }








        public async Task DeleteFileAsync(int fileId)
        {
            var file = await _context.Files.FindAsync(fileId);
            if (file != null)
            {
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<FileDTO>> GetFilesByCategoryAsync(int categoryId)
        {
            return await _context.Files
                .Where(f => f.CategoryId == categoryId)
                .Select(f => new FileDTO
                {
                    FileId = f.FileId,
                    FileName = f.FileName,
                    FileExtension = f.FileExtension,
                    FilePath = f.FilePath,
                    FileSize = f.FileSize,
                    CategoryId = f.CategoryId,
                    UploadedBy = f.UploadedBy,
                    UploadDate = f.UploadDate ?? DateTime.MinValue,
                    LastModifiedBy = f.LastModifiedBy,
                    LastModifiedDate = f.LastModifiedDate,
                    IsActive = f.IsActive ?? false,
                    IsPublic = f.IsPublic ?? false
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<FileDTO>> SearchFilesAsync(string keyword, int? categoryId = null)
        {
            var query = _context.Files.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(f => f.FileName.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(f => f.CategoryId == categoryId.Value);
            }

            return await query
                .Select(f => new FileDTO
                {
                    FileId = f.FileId,
                    FileName = f.FileName,
                    FileExtension = f.FileExtension,
                    FilePath = f.FilePath,
                    FileSize = f.FileSize,
                    CategoryId = f.CategoryId,
                    UploadedBy = f.UploadedBy,
                    UploadDate = f.UploadDate ?? DateTime.MinValue,
                    LastModifiedBy = f.LastModifiedBy,
                    LastModifiedDate = f.LastModifiedDate,
                    IsActive = f.IsActive ?? false,
                    IsPublic = f.IsPublic ?? false
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<FileDTO>> GetUserFilesAsync(int userId)
        {
            return await _context.Files
                .Where(f => f.UploadedBy == userId)
                .Select(f => new FileDTO
                {
                    FileId = f.FileId,
                    FileName = f.FileName,
                    FileExtension = f.FileExtension,
                    FilePath = f.FilePath,
                    FileSize = f.FileSize,
                    CategoryId = f.CategoryId,
                    UploadedBy = f.UploadedBy,
                    UploadDate = f.UploadDate ?? DateTime.MinValue,
                    LastModifiedBy = f.LastModifiedBy,
                    LastModifiedDate = f.LastModifiedDate,
                    IsActive = f.IsActive ?? false,
                    IsPublic = f.IsPublic ?? false
                })
                .ToListAsync();
        }


        public async Task<byte[]> DownloadFileAsync(int fileId)
        {
            var file = await _context.Files.FindAsync(fileId);
            if (file == null) return null;

            using (var fileStream = System.IO.File.OpenRead(file.FilePath))
            {
                using (var memoryStream = new MemoryStream())
                {
                    await fileStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }


        public async Task SetFilePermissionsAsync(int fileId, Dictionary<int, bool> permissions)
        {
            var existingPermissions = await _context.FilePermissions.Where(fp => fp.FileId == fileId).ToListAsync();

            foreach (var permission in existingPermissions)
            {
                _context.FilePermissions.Remove(permission);
            }

            foreach (var kvp in permissions)
            {
                _context.FilePermissions.Add(new FilePermission
                {
                    FileId = fileId,
                    RoleId = kvp.Key,
                    CanView = kvp.Value,
                    CanEdit = kvp.Value,
                    CanUpload = kvp.Value,
                    CanDownload = kvp.Value,
                    CanDelete = kvp.Value
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<int, bool>> GetFilePermissionsAsync(int fileId)
        {
            var permissions = await _context.FilePermissions
                .Where(fp => fp.FileId == fileId)
                .ToDictionaryAsync(fp => fp.RoleId, fp => fp.CanView ?? false);

            return permissions;
        }


        public async Task<FileConversionDTO> ConvertFileAsync(int fileId, string targetExtension)
        {
            try
            {
                // Find the file
                var file = await _context.Files.FindAsync(fileId);
                if (file == null)
                    throw new FileNotFoundException("File not found.");

                // Validate target extension
                var allowedExtensions = new[] { ".pdf", ".xlsx", ".xls", ".docx", ".txt" };
                if (!allowedExtensions.Contains(targetExtension.ToLower()))
                    throw new ArgumentException("Invalid target extension.");

                // Open file stream
                using (var fileStream = System.IO.File.OpenRead(file.FilePath))
                {
                    // Find appropriate handler
                    var handler = _fileTypeHandlers.FirstOrDefault(h => h.CanHandleAsync(fileStream, file.FileName).Result);

                    if (handler == null)
                        throw new NotSupportedException("No handler found for the file type.");

                    // Convert file
                    var convertedBytes = await handler.ConvertToAsync(fileStream, targetExtension);

                    // Prepare converted file path
                    var convertedFilesPath = Path.Combine(_environment.WebRootPath, "ConvertedFiles");
                    Directory.CreateDirectory(convertedFilesPath);

                    // Generate unique filename
                    var convertedFileName = $"{Guid.NewGuid()}{targetExtension}";
                    var convertedFilePath = Path.Combine(convertedFilesPath, convertedFileName);

                    // Save converted file
                    await System.IO.File.WriteAllBytesAsync(convertedFilePath, convertedBytes);

                    // Create audit log
                    var auditLog = new AuditLog
                    {
                        UserId = file.UploadedBy,
                        ActionType = "Convert",
                        FileId = fileId,
                        ActionDate = DateTime.UtcNow,
                        Details = $"Converted from {file.FileExtension} to {targetExtension}"
                    };
                    _context.AuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();

                    // Return conversion details
                    return new FileConversionDTO
                    {
                        FileId = fileId,
                        OriginalExtension = file.FileExtension,
                        TargetExtension = targetExtension,
                        ConvertedFilePath = convertedFilePath,
                        FileName = convertedFileName
                    };
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<PaginatedResult<FileDTO>> GetFilesAsync(int pageNumber = 1, int pageSize = 5)
        {
            var totalCount = await _context.Files.CountAsync();
            var files = await _context.Files
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FileDTO
                {
                    FileId = f.FileId,
                    FileName = f.FileName,
                    FileExtension = f.FileExtension,
                    FilePath = f.FilePath,
                    FileSize = f.FileSize,
                    CategoryId = f.CategoryId,
                    UploadedBy = f.UploadedBy,
                    UploadDate = f.UploadDate.GetValueOrDefault(),
                    LastModifiedBy = f.LastModifiedBy,
                    LastModifiedDate = f.LastModifiedDate,
                    IsActive = f.IsActive.GetValueOrDefault(),
                    IsPublic = f.IsPublic.GetValueOrDefault()
                })
                .ToListAsync();

            return new PaginatedResult<FileDTO>(files, totalCount, pageNumber, pageSize);
        }


        public async Task<List<Models.File>> GetFilesPaginatedAsync(GoogleDriveListRequest request)
        {
            try
            {
                // Base query with multiple conditions
                var query = _context.Files
                    .Where(f =>
                        !string.IsNullOrEmpty(f.GoogleDriveFileId) &&
                        f.IsActive == true) // Add additional conditions as needed
                    .AsQueryable();

                // Apply search if not empty (case-insensitive)
                if (!string.IsNullOrWhiteSpace(request.SearchQuery))
                {
                    query = query.Where(f =>
                        EF.Functions.Like(f.FileName, $"%{request.SearchQuery}%") ||
                        EF.Functions.Like(f.FileExtension, $"%{request.SearchQuery}%")
                    );
                }

                // Apply pagination
                return await query
                    .OrderByDescending(f => f.UploadDate)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine( "Error in GetFilesPaginatedAsync");
                throw;
            }
        }
        public async Task SyncGoogleDriveFiles(int currentUserId)
        {
            try
            {
                // Fetch files from Google Drive
                var googleDriveFiles = await _googleDriveService.ListFilesAsync(new GoogleDriveListRequest());

                foreach (var driveFile in googleDriveFiles)
                {
                    // Check if file already exists in local database
                    var existingFile = await _context.Files
                        .FirstOrDefaultAsync(f => f.GoogleDriveFileId == driveFile.Id);

                    if (existingFile == null)
                    {
                        // Create new file entry
                        var newFile = new Models.File
                        {
                            FileName = driveFile.Name,
                            FileExtension = Path.GetExtension(driveFile.Name),
                            GoogleDriveFileId = driveFile.Id,
                            UploadDate = driveFile.CreatedTime,
                            FileSize = driveFile.Size ?? 0,
                            IsPublic = true, // Adjust based on your requirements
                            UploadedBy = currentUserId // Use the passed user ID
                        };

                        _context.Files.Add(newFile);
                    }
                    else
                    {
                        // Update existing file
                        existingFile.FileName = driveFile.Name;
                        existingFile.FileExtension = Path.GetExtension(driveFile.Name);
                        existingFile.UploadDate = driveFile.CreatedTime;
                        existingFile.FileSize = driveFile.Size ?? 0;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Improved error handling
                throw; // Re-throw to allow caller to handle
            }
        }



        public async Task<int> GetTotalFileCountAsync(GoogleDriveListRequest request)
        {
            try
            {
                var query = _context.Files.AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchQuery))
                {
                    query = query.Where(f =>
                        f.FileName.Contains(request.SearchQuery) ||
                        f.FileExtension.Contains(request.SearchQuery)
                    );
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        private string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return "Unknown";
            }

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            return ipAddress ?? "Unknown";
        }
    }
}
