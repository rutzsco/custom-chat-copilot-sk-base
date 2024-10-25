// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Services.ChatHistory;

namespace MinimalApi.Services.Documents;

public class DocumentServiceSub : IDocumentService
{
    public Task<UploadDocumentsResponse> CreateDocumentUploadAsync(UserInformation userInfo, IFormFileCollection files, string selectedProfile, Dictionary<string, string>? fileMetadata, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<DocumentUpload>> GetDocumentUploadsAsync(UserInformation user, string profileId = null)
    {
        return Task.FromResult(new List<DocumentUpload>());
    }
    public Task<DocumentIndexResponse> MergeDocumentsIntoIndexAsync(UploadDocumentsResponse documentList) // DocumentIndexRequest indexRequest)
    {
        throw new NotImplementedException();
    }
}
