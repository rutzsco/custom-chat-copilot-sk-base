// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;

namespace MinimalApi.Services;

public sealed class AzureBlobStorageService(BlobServiceClient blobServiceClient, IConfiguration configuration)
{
    internal async Task<string> UploadFileAsync(Stream content, string contentType)
    {
        var azureStorageContainer = configuration[AppConfigurationSetting.AzureStorageUserUploadContainer];
        var container = blobServiceClient.GetBlobContainerClient(azureStorageContainer);
        if (!await container.ExistsAsync())
        {
            // Create the container
            await container.CreateAsync();
            Console.WriteLine("Container created.");
        }

        var blobClient = container.GetBlobClient(Guid.NewGuid().ToString());
        await blobClient.UploadAsync(content, new BlobHttpHeaders{ContentType = contentType });
        return blobClient.Uri.AbsoluteUri;
    }

    internal async Task<UploadDocumentsResponse> UploadFilesAsync(UserInformation userInfo, IEnumerable<IFormFile> files, CancellationToken cancellationToken, IDictionary<string,string> metadata)
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
                var blobName = BlobNameFromFilePage(fileName, DateTime.UtcNow.Ticks);
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
                }, metadata, cancellationToken: cancellationToken);
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

    private static string BlobNameFromFilePage(string filename, long page = 0)
    {
        return Path.GetExtension(filename).ToLower() is ".pdf"
            ? $"{Path.GetFileNameWithoutExtension(filename)}_{page}.pdf"
            : Path.GetFileName(filename);
    }
}
