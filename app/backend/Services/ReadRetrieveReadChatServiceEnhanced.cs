// Copyright (c) Microsoft. All rights reserved.
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Models;
using MinimalApi.Services.Prompts;

namespace MinimalApi.Services;

internal sealed class ReadRetrieveReadChatServiceEnhanced
{
    private readonly SearchClientFacade _searchClientFacade;
    private readonly ILogger<ReadRetrieveReadChatServiceEnhanced> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClientFacade _openAIClientFacade;

    public ReadRetrieveReadChatServiceEnhanced(OpenAIClientFacade openAIClientFacade,
                                               SearchClientFacade searchClientFacade,
                                               ILogger<ReadRetrieveReadChatServiceEnhanced> logger,
                                               IConfiguration configuration)
    {
        _searchClientFacade = searchClientFacade;
        _openAIClientFacade = openAIClientFacade;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApproachResponse> ReplyAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var history = request.History;
        var chatOptions = request.OptionFlags;
        var overrides = request.Overrides;

        var kernel = _openAIClientFacade.GetKernel(chatOptions.IsChatGpt4Enabled());
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();
        var documentLookupPlugIn = kernel.Plugins.GetFunction(DefaultSettings.DocumentRetrievalPluginName, DefaultSettings.DocumentRetrievalPluginQueryFunctionName);
        var context = new KernelArguments().AddUserParameters(history);

        // 1. INTENT - Create chat history starting with system message
        var intentChatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(PromptService.GetPromptByName(PromptService.SearchSystemPrompt))
            .AddChatHistory(history);

        // 1. INTENT - Execute
        var intentChatContext = new KernelArguments();
        intentChatContext[ContextVariableOptions.Question] = context[ContextVariableOptions.Question];
        var intentUserMessage = await PromptService.RenderPromptAsync(kernel,PromptService.GetPromptByName(PromptService.SearchUserPrompt), intentChatContext);
        intentChatHistory.AddUserMessage(intentUserMessage);

        var searchAnswer = await chatGpt.GetChatMessageContentAsync(intentChatHistory, DefaultSettings.AISearchRequestSettings, kernel);
        context["searchQuery"] = searchAnswer.Content;

        // 2. SEARCH SOURCES - Generate search query and execute sources search
        await kernel.InvokeAsync(documentLookupPlugIn, context);
        if (context[ContextVariableOptions.Knowledge] == "NO_SOURCES")
        {
            return new ApproachResponse(
                DataPoints: new SupportingContentRecord[] { },
                Answer: "No sources exist for your filter criteria",
                Thoughts: string.Empty,
                CitationBaseUrl: _configuration.ToCitationBaseUrl(),
                MessageId: request.ChatTurnId,
                ChatId: request.ChatId);
        }


        // 3. CHAT - Implementation
        var chatContext = new KernelArguments();
        chatContext[ContextVariableOptions.Knowledge] = context[ContextVariableOptions.Knowledge];
        chatContext[ContextVariableOptions.Question] = context[ContextVariableOptions.Question];

        // CHAT - Create chat history starting with system message
        var systemMessagePrompt = PromptService.GetPromptByName(PromptService.ChatSystemPrompt);
        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(systemMessagePrompt).AddChatHistory(history);

        // CHAT - latest message and source content
        var userMessage = await PromptService.RenderPromptAsync(kernel, PromptService.GetPromptByName(PromptService.ChatUserPrompt), chatContext);
        chatHistory.AddUserMessage(userMessage);

        // CHAT - Execute AOAI Chat API
        try
        {
            var result = await chatGpt.GetChatCompletionsWithUsageAsync(chatHistory);
            var json = (string)context[ContextVariableOptions.KnowledgeJSON];
            var dataSources = JsonSerializer.Deserialize<KnowledgeSource[]>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            var diagnostics = new Diagnostics(new CompletionsDiagnostics(result.Usage.CompletionTokens, result.Usage.PromptTokens, result.Usage.TotalTokens, result.DurationMilliseconds));

            _logger.LogInformation($"CHAT_DIAGNOSTICS: CompletionTokens={diagnostics.AnswerDiagnostics.CompletionTokens}, PromptTokens={diagnostics.AnswerDiagnostics.PromptTokens}, TotalTokens={diagnostics.AnswerDiagnostics.TotalTokens}, DurationMilliseconds={diagnostics.AnswerDiagnostics.AnswerDurationMilliseconds}");

            return new ApproachResponse(
                DataPoints: dataSources.Select(x => new SupportingContentRecord(x.filepath, x.content)).ToArray(),
                Answer: result.Answer.Replace("\n", "<br>"),
                Thoughts: $"Searched for:<br>{context["intent"]}<br><br>System:<br>{systemMessagePrompt.Replace("\n", "<br>")}<br><br>{userMessage.Replace("\n", "<br>")}<br><br>{result.Answer.Replace("\n", "<br>")}",
                CitationBaseUrl: _configuration.ToCitationBaseUrl(),
                MessageId: request.ChatTurnId,
                ChatId: request.ChatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating chat response: {ex.Message}");
            throw;
        }
    }
}
