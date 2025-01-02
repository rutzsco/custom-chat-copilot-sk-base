// Copyright (c) Microsoft. All rights reserved.
using MinimalApi.Services.Profile;

namespace MinimalApi.Services;

internal sealed class ReadRetrieveReadChatService
{
    private readonly ILogger<ReadRetrieveReadChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClientFacade _openAIClientFacade;

    public ReadRetrieveReadChatService(OpenAIClientFacade openAIClientFacade,
                                       ILogger<ReadRetrieveReadChatService> logger,
                                       IConfiguration configuration)
    {
        _openAIClientFacade = openAIClientFacade;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApproachResponse> ReplyAsync(UserInformation user, ProfileDefinition profile, ChatRequest request, CancellationToken cancellationToken = default)
    {   
        try
        {
            var sw = Stopwatch.StartNew();
 
            var kernel = _openAIClientFacade.GetKernel(request.OptionFlags.IsChatGpt4Enabled());

            var generateSearchQueryFunction = kernel.Plugins.GetFunction(profile.RAGSettings.GenerateSearchQueryPluginName, profile.RAGSettings.GenerateSearchQueryPluginQueryFunctionName);
            var documentLookupFunction = kernel.Plugins.GetFunction(profile.RAGSettings.DocumentRetrievalPluginName, profile.RAGSettings.DocumentRetrievalPluginQueryFunctionName);
            var chatFunction = kernel.Plugins.GetFunction(DefaultSettings.ChatPluginName, DefaultSettings.ChatPluginFunctionName);

            var context = new KernelArguments().AddUserParameters(request, profile, user);

            await kernel.InvokeAsync(generateSearchQueryFunction, context);
            await kernel.InvokeAsync(documentLookupFunction, context);
            await kernel.InvokeAsync(chatFunction, context);


            sw.Stop();

            var result = context.BuildResponse(profile, request, _configuration, _openAIClientFacade.GetKernelDeploymentName(request.OptionFlags.IsChatGpt4Enabled()), sw.ElapsedMilliseconds);

            var diagnostics = result.Context.Diagnostics;
            _logger.LogInformation($"CHAT_DIAGNOSTICS: CompletionTokens={diagnostics.AnswerDiagnostics.CompletionTokens}, PromptTokens={diagnostics.AnswerDiagnostics.PromptTokens}, TotalTokens={diagnostics.AnswerDiagnostics.TotalTokens}, DurationMilliseconds={diagnostics.AnswerDiagnostics.AnswerDurationMilliseconds}");

            return result;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating chat response: {ex.Message}");
            throw;
        }
    }
}
