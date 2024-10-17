// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Services.Documents;

namespace MinimalApi.Services.ChatHistory;
public interface IDocumentService
{
    Task<UploadDocumentsResponse> CreateDocumentUploadAsync(UserInformation userInfo, IFormFileCollection files, Dictionary<string, string>? fileMetadata, CancellationToken cancellationToken);
    Task<List<DocumentUpload>> GetDocumentUploadsAsync(UserInformation user);
    Task<DocumentIndexResponse> MergeDocumentsIntoIndexAsync(UploadDocumentsResponse documentList); // DocumentIndexRequest indexRequest);
}
