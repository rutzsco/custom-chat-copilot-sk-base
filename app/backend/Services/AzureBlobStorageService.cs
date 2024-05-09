// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services;

public sealed class AzureBlobStorageService(BlobServiceClient blobServiceClient, IConfiguration configuration)
{
    internal async Task<UploadDocumentsResponse> UploadFilesAsync(UserInformation userInfo, IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        try
        {
            var azureStorageContainer = configuration[AppConfigurationSetting.AzureStorageUserUploadContainer];
            var container = blobServiceClient.GetBlobContainerClient(azureStorageContainer);
            if (!await container.ExistsAsync())
            {
                // Create the container
                await container.CreateAsync();
                Console.WriteLine("Container created.");
            }

            List<UploadDocumentFileSummary> uploadedFiles = [];
            foreach (var file in files)
            {
                var fileName = file.FileName;

                await using var stream = file.OpenReadStream();
                var blobName = BlobNameFromFilePage(fileName);
                var blobClient = container.GetBlobClient(blobName);
                //if (await blobClient.ExistsAsync(cancellationToken))
                //{
                //    continue;
                //}

                var url = blobClient.Uri.AbsoluteUri;
                await using var fileStream = file.OpenReadStream();
                await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
                {
                    ContentType = "image"
                }, cancellationToken: cancellationToken);
                uploadedFiles.Add(new UploadDocumentFileSummary(blobName, file.Length));           
            }

            if (uploadedFiles.Count is 0)
            {
                return UploadDocumentsResponse.FromError("No files were uploaded. Either the files already exist or the files are not PDFs or images.");
            }

            return new UploadDocumentsResponse([.. uploadedFiles]);
        }
        catch (Exception ex)
        {
            return UploadDocumentsResponse.FromError(ex.ToString());
        }
    }

    private static string BlobNameFromFilePage(string filename, int page = 0) =>
        Path.GetExtension(filename).ToLower() is ".pdf"
            ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
            : Path.GetFileName(filename);
}
