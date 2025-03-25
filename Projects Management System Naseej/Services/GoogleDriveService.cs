using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Upload;
using System.IO;
using Projects_Management_System_Naseej.DTOs.GoogleUserDto;

namespace Projects_Management_System_Naseej.Services
{
    public class GoogleDriveService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly DriveService _driveService;

        public GoogleDriveService(
            IConfiguration configuration,
            ILogger<GoogleDriveService> logger,
            IWebHostEnvironment environment,
            DriveService driveService)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _driveService = driveService;
        }
        private async Task<DriveService> GetDriveServiceAsync()
        {
            try
            {
                string[] Scopes = {
                    DriveService.Scope.Drive,
                    DriveService.Scope.DriveFile
                };
                string ApplicationName = "Projects Management System";

                // Multiple potential paths for credentials
                string[] possibleCredentialPaths = {
                    Path.Combine(_environment.ContentRootPath, "credentials.json"),
                    Path.Combine(_environment.WebRootPath, "credentials.json"),
                    "credentials.json"
                };

                FileStream credentialsStream = null;

                // Find and open credentials file
                string foundCredentialsPath = null;
                foreach (var path in possibleCredentialPaths)
                {
                    _logger.LogInformation($"Attempting to load credentials from: {path}");

                    if (File.Exists(path))
                    {
                        try
                        {
                            credentialsStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                            foundCredentialsPath = path;
                            _logger.LogInformation($"Successfully found credentials at: {path}");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Could not open credentials file at {path}: {ex.Message}");
                        }
                    }
                }

                if (credentialsStream == null)
                {
                    string searchedPaths = string.Join(", ", possibleCredentialPaths);
                    _logger.LogError($"Could not find credentials file. Searched paths: {searchedPaths}");
                    throw new FileNotFoundException("Could not find Google Drive credentials file. Please check your configuration.");
                }

                using (credentialsStream)
                {
                    // Ensure token storage directory exists
                    string tokenPath = Path.Combine(_environment.ContentRootPath, "GoogleDriveTokens");
                    _logger.LogInformation($"Token storage path: {tokenPath}");
                    Directory.CreateDirectory(tokenPath);
                    // Load client secrets
                    var clientSecrets = GoogleClientSecrets.Load(credentialsStream);

                    // Validate client secrets
                    if (clientSecrets?.Secrets == null)
                    {
                        throw new InvalidOperationException("Invalid client secrets. Unable to create credentials.");
                    }

                    // Authorize
                    var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        clientSecrets.Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(tokenPath, true));

                    // Validate credential
                    if (credential == null)
                    {
                        throw new InvalidOperationException("Failed to obtain OAuth credential.");
                    }

                    // Create Drive service
                    return new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName,
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comprehensive error in creating Google Drive service");
                throw;
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string? mimeType = null)
        {
            try
            {
                // Validate inputs
                if (fileStream == null)
                    throw new ArgumentNullException(nameof(fileStream), "File stream cannot be null");

                if (string.IsNullOrEmpty(fileName))
                    throw new ArgumentNullException(nameof(fileName), "Filename cannot be empty");

                // Ensure stream is at the beginning
                fileStream.Position = 0;

                // Determine MIME type if not provided
                if (string.IsNullOrEmpty(mimeType))
                {
                    mimeType = GetMimeType(fileName);
                }

                // Get Drive service
                var driveService = await GetDriveServiceAsync();

                // Prepare file metadata
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName,
                    MimeType = mimeType
                };

                // Create request
                var request = driveService.Files.Create(fileMetadata, fileStream, mimeType);
                request.Fields = "id";

                // Upload file
                var uploadResult = await request.UploadAsync();

                // Check upload status
                if (uploadResult.Status == UploadStatus.Failed)
                {
                    throw new Exception($"Upload failed: {uploadResult.Exception?.Message ?? "Unknown error"}");
                }

                // Return file ID
                return request.ResponseBody?.Id ??
                       throw new InvalidOperationException("File upload succeeded, but no file ID was returned");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file {fileName} to Google Drive");
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string fileId)
        {
            try
            {
                var driveService = await GetDriveServiceAsync();

                var request = driveService.Files.Get(fileId);
                var stream = new MemoryStream();
                await request.DownloadAsync(stream);
                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file {fileId} from Google Drive");
                throw;
            }
        }

        public async Task<string> GetFileWebViewLink(string fileId)
        {
            try
            {
                var driveService = await GetDriveServiceAsync();
                var file = await driveService.Files.Get(fileId).ExecuteAsync();
                return file.WebViewLink;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting web view link for file {fileId}");
                throw;
            }
        }

        public async Task<string> UpdateFileAsync(string fileId, Stream fileStream, string mimeType)
        {
            try
            {
                var driveService = await GetDriveServiceAsync();

                var fileMetadata = new Google.Apis.Drive.v3.Data.File();

                var request = driveService.Files.Update(fileMetadata, fileId, fileStream, mimeType);
                request.Fields = "id";

                var uploadResult = await request.UploadAsync();

                if (uploadResult.Status == UploadStatus.Failed)
                {
                    throw new Exception($"Update failed: {uploadResult.Exception?.Message ?? "Unknown error"}");
                }

                return request.ResponseBody?.Id ??
                       throw new InvalidOperationException("File update succeeded, but no file ID was returned");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating file {fileId} in Google Drive");
                throw;
            }
        }
        public async Task<List<Google.Apis.Drive.v3.Data.File>> ListFilesAsync(GoogleDriveListRequest request)
        {
            try
            {
                var listRequest = _driveService.Files.List();

                // Configure query parameters
                listRequest.PageSize = request.PageSize;
                listRequest.Fields = "nextPageToken, files(id, name, mimeType, createdTime, size, webViewLink, owners)";

                // Build dynamic query
                var queryParts = new List<string>();

                // Add search query if provided
                if (!string.IsNullOrWhiteSpace(request.SearchQuery))
                {
                    queryParts.Add($"name contains '{request.SearchQuery}'");
                }

                // Exclude specific folders or file types if needed
                queryParts.Add("mimeType != 'application/vnd.google-apps.folder'");

                // Combine query parts
                if (queryParts.Any())
                {
                    listRequest.Q = string.Join(" and ", queryParts);
                }

                // Pagination
                listRequest.PageToken = request.PageNumber > 1
                    ? CalculatePageToken(request.PageNumber, request.PageSize)
                    : null;

                // Execute the request
                var result = await listRequest.ExecuteAsync();

                return result.Files.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing Google Drive files");
                throw;
            }
        }
        public async Task<int> GetTotalFileCountAsync(string searchQuery = null)
        {
            try
            {
                var listRequest = _driveService.Files.List();

                // Build query parts
                var queryParts = new List<string>
            {
                "mimeType != 'application/vnd.google-apps.folder'"
            };

                // Add search query if provided
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    queryParts.Add($"name contains '{searchQuery}'");
                }

                // Set query
                listRequest.Q = string.Join(" and ", queryParts);
                listRequest.Fields = "files(id)";
                listRequest.PageSize = 1000; // Adjust based on your needs

                var result = await listRequest.ExecuteAsync();
                return result.Files.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting Google Drive files");
                throw;
            }
        }
        private string CalculatePageToken(int pageNumber, int pageSize)
        {
            // Implement logic to generate page token based on page number and size
            // This might involve keeping track of previous page tokens or using a cursor-based approach
            return null; // Placeholder
        }
        public async Task<List<Google.Apis.Drive.v3.Data.File>> FilterFilesAsync(GoogleDriveFilterRequest filters)
{
    var query = new List<string>();

    // Build dynamic query based on provided filters
    if (!string.IsNullOrEmpty(filters.MimeType))
    {
        query.Add($"mimeType = '{filters.MimeType}'");
    }

    if (filters.StartDate.HasValue)
    {
        query.Add($"createdTime >= '{filters.StartDate.Value:yyyy-MM-dd}'");
    }

    if (filters.EndDate.HasValue)
    {
        query.Add($"createdTime <= '{filters.EndDate.Value:yyyy-MM-dd}'");
    }

    if (filters.MinSize.HasValue)
    {
        query.Add($"size >= {filters.MinSize.Value}");
    }

    if (filters.MaxSize.HasValue)
    {
        query.Add($"size <= {filters.MaxSize.Value}");
    }

    var listRequest = _driveService.Files.List();
    listRequest.Q = string.Join(" and ", query);
    listRequest.Fields = "files(id, name, mimeType, createdTime, size, webViewLink)";

    var result = await listRequest.ExecuteAsync();
    return result.Files.ToList();
}
      
        
        public async Task<Google.Apis.Drive.v3.Data.File> GetFileMetadataAsync(string fileId)
        {
            try
            {
                var driveService = await GetDriveServiceAsync();
                return await driveService.Files.Get(fileId).ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving metadata for file {fileId}");
                throw;
            }
        }

        public async Task DeleteFileAsync(string fileId)
        {
            try
            {
                var driveService = await GetDriveServiceAsync();
                await driveService.Files.Delete(fileId).ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {fileId}");
                throw;
            }
        }
        public string GetMimeType(string fileName)
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
    }
}