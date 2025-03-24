using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Upload;
using System.IO;

namespace Projects_Management_System_Naseej.Services
{
    public class GoogleDriveService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly IWebHostEnvironment _environment;

        public GoogleDriveService(
            IConfiguration configuration,
            ILogger<GoogleDriveService> logger,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
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