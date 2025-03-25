using Microsoft.Graph.Models;
using Projects_Management_System_Naseej.DTOs.FileDTOs;
using Projects_Management_System_Naseej.DTOs.GoogleUserDto;

namespace Projects_Management_System_Naseej.Repositories
{
    public interface IFileRepository
    {
        Task<IEnumerable<FileDTO>> GetFilesAsync();
        Task<FileDTO> GetFileByIdAsync(int fileId);
        Task<FileDTO> UploadFileAsync(IFormFile file, int uploadedBy, int categoryId, CreateFileDTO createFileDTO);
        Task<FileDTO> UpdateFileAsync(int fileId, IFormFile file, int updatedBy, UpdateFileDTO updateFileDTO);
        Task DeleteFileAsync(int fileId);
        Task<IEnumerable<FileDTO>> GetFilesByCategoryAsync(int categoryId);
        Task<IEnumerable<FileDTO>> SearchFilesAsync(string keyword, int? categoryId = null);
        Task<IEnumerable<FileDTO>> GetUserFilesAsync(int userId);
        Task<byte[]> DownloadFileAsync(int fileId);
        Task SetFilePermissionsAsync(int fileId, Dictionary<int, bool> permissions);
        Task<Dictionary<int, bool>> GetFilePermissionsAsync(int fileId);
        Task<FileConversionDTO> ConvertFileAsync(int fileId, string targetExtension);
        Task<List<Models.File>> GetFilesPaginatedAsync(GoogleDriveListRequest request);
        Task<int> GetTotalFileCountAsync(GoogleDriveListRequest request);
        Task SyncGoogleDriveFiles(int currentUserId);
    }
}
