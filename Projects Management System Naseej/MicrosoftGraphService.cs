using Microsoft.Graph;
using Microsoft.Identity.Client;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Projects_Management_System_Naseej.Services
{
    public class MicrosoftGraphService
    {
        private readonly GraphServiceClient _graphClient;

        public MicrosoftGraphService(IConfiguration configuration)
        {
            var clientId = configuration["AzureAd:ClientId"];
            var tenantId = configuration["AzureAd:TenantId"];
            var clientSecret = configuration["AzureAd:ClientSecret"];

            // Use ClientSecretCredential for app-only authentication
            var credential = new ClientSecretCredential(
                tenantId,
                clientId,
                clientSecret
            );

            // Create GraphServiceClient using the credential
            _graphClient = new GraphServiceClient(credential);
        }

        public async Task<Stream> DownloadFileAsync(string fileId)
        {
            try
            {
                // Fetch file content
                var fileStream = await _graphClient.Drives["me"]
                    .Items[fileId]
                    .Content
                    .GetAsync();

                return fileStream;
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new Exception($"Error downloading file: {ex.Message}", ex);
            }
        }

        public async Task<string> UpdateWordFileAsync(string fileId, Stream fileStream)
        {
            try
            {
                // Update file content
                var updatedFile = await _graphClient.Drives["me"]
                    .Items[fileId]
                    .Content
                    .PutAsync(fileStream);

                return updatedFile.Id;
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new Exception($"Error updating file: {ex.Message}", ex);
            }
        }

        public async Task<string> CreateWordFileAsync(string fileName, Stream fileContent)
        {
            try
            {
                // Create new file in OneDrive
                var driveItem = await _graphClient.Drives["me"]
                    .Root
                    .ItemWithPath(fileName)
                    .Content
                    .PutAsync(fileContent);

                return driveItem.Id;
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new Exception($"Error creating file: {ex.Message}", ex);
            }
        }
    }
}