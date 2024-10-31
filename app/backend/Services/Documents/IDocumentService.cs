// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Services.Documents;

namespace MinimalApi.Services.ChatHistory;
public interface IDocumentService
{
    Task<UploadDocumentsResponse> CreateDocumentUploadAsync(UserInformation userInfo, IFormFileCollection files, string selectedProfile, Dictionary<string, string>? fileMetadata, CancellationToken cancellationToken);
    Task<List<DocumentUpload>> GetDocumentUploadsAsync(UserInformation user, string profileId);
}
