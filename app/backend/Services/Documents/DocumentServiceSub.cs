// Copyright (c) Microsoft. All rights reserved.

// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Services.ChatHistory;

namespace MinimalApi.Services.Documents;

public class DocumentServiceSub : IDocumentService
{
    public Task<UploadDocumentsResponse> CreateDocumentUploadAsync(UserInformation userInfo, IFormFileCollection files, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<DocumentUpload>> GetDocumentUploadsAsync(UserInformation user)
    {
        return Task.FromResult(new List<DocumentUpload>());
    }
}
