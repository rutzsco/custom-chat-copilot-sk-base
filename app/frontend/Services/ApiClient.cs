// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http.Headers;

namespace ClientApp.Services;

public sealed class ApiClient(HttpClient httpClient)
{
    public async Task<bool> ShowLogoutButtonAsync()
    {
        var response = await httpClient.GetAsync("api/enableLogout");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<bool>();
    }
    public async Task<UserInformation> GetUserAsync()
    {
        var response = await httpClient.GetAsync("api/user");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserInformation>();
    }

    public async Task<List<DocumentSummary>> GetUserDocumentsAsync()
    {
        var response = await httpClient.GetAsync("api/user/documents");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<DocumentSummary>>();
    }

    public async Task<UploadDocumentsResponse> UploadDocumentsAsync(IReadOnlyList<IBrowserFile> files, long maxAllowedSize, string cookie)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            foreach (var file in files)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize));
#pragma warning restore CA2000 // Dispose objects before losing scope
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                content.Add(fileContent, file.Name, file.Name);
            }


            var tokenResponse = await httpClient.GetAsync("api/token/csrf");
            tokenResponse.EnsureSuccessStatusCode();
            var token = await tokenResponse.Content.ReadAsStringAsync();
            token = token.Trim('"');

            // set token
            content.Headers.Add("X-CSRF-TOKEN-FORM", token);
            content.Headers.Add("X-CSRF-TOKEN-HEADER", token);

            var response = await httpClient.PostAsync("api/documents", content);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<UploadDocumentsResponse>();

            return result
                ?? UploadDocumentsResponse.FromError(
                    "Unable to upload files, unknown error.");
        }
        catch (Exception ex)
        {
            return UploadDocumentsResponse.FromError(ex.ToString());
        }
    }

    public async IAsyncEnumerable<DocumentSummary> GetDocumentsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/user/documents", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var options = SerializerOptions.Default;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            await foreach (var document in JsonSerializer.DeserializeAsyncEnumerable<DocumentSummary>(stream, options, cancellationToken))
            {
                if (document is null)
                {
                    continue;
                }

                yield return document;
            }
        }
    }

    public async IAsyncEnumerable<ChatHistoryResponse> GetFeedbackAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/feedback", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var options = SerializerOptions.Default;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            await foreach (var document in JsonSerializer.DeserializeAsyncEnumerable<ChatHistoryResponse>(stream, options, cancellationToken))
            {
                if (document is null)
                {
                    continue;
                }

                yield return document;
            }
        }
    }
    public async IAsyncEnumerable<ChatHistoryResponse> GetHistoryAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/chat/history", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var options = SerializerOptions.Default;
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await foreach (var document in JsonSerializer.DeserializeAsyncEnumerable<ChatHistoryResponse>(stream, options, cancellationToken))
            {
                if (document is null)
                {
                    continue;
                }

                yield return document;
            }
        }
    }

    public async IAsyncEnumerable<ChatSessionModel> GetHistoryV2Async([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/chat/history-v2", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var options = SerializerOptions.Default;
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await foreach (var session in JsonSerializer.DeserializeAsyncEnumerable<ChatSessionModel>(stream, options, cancellationToken))
            {
                if (session is null)
                {
                    continue;
                }

                yield return session;
            }
        }
    }

    public async IAsyncEnumerable<ChatHistoryResponse> GetChatHistorySessionAsync([EnumeratorCancellation] CancellationToken cancellationToken, string chatId)
    {
        var response = await httpClient.GetAsync($"api/chat/history/{chatId}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var options = SerializerOptions.Default;
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await foreach (var document in JsonSerializer.DeserializeAsyncEnumerable<ChatHistoryResponse>(stream, options, cancellationToken))
            {
                if (document is null)
                {
                    continue;
                }

                yield return document;
            }
        }
    }

    public async Task ChatRatingAsync(ChatRatingRequest request)
    {
        await PostBasicAsync(request, "api/chat/rating");
    }


    private async Task<AnswerResult<TRequest>> PostRequestAsync<TRequest>(TRequest request, string apiRoute) where TRequest : ApproachRequest
    {
        var result = new AnswerResult<TRequest>(
            IsSuccessful: false,
            Response: null,
            Approach: request.Approach,
            Request: request);

        var json = JsonSerializer.Serialize(request, SerializerOptions.Default);
        using var body = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(apiRoute, body);
        if (response.IsSuccessStatusCode)
        {
            var answer = await response.Content.ReadFromJsonAsync<ApproachResponse>();
            return result with
            {
                IsSuccessful = answer is not null,
                Response = answer
            };
        }
        else
        {
            var answer = new ApproachResponse($"HTTP {(int)response.StatusCode} : {response.ReasonPhrase ?? "☹️ Unknown error..."}","Unable to retrieve valid response from the server.", null);

            return result with
            {
                IsSuccessful = false,
                Response = answer
            };
        }
    }

    private async Task PostBasicAsync<TRequest>(TRequest request, string apiRoute) where TRequest : ApproachRequest
    {
        var json = JsonSerializer.Serialize(request,SerializerOptions.Default);
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(apiRoute, body);
    }
}
