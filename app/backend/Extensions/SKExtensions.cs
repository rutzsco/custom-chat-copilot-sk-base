﻿using Azure.Core;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MinimalApi.Extensions
{
    public static class SKExtensions
    {
        public static async Task<SKResult> GetChatCompletionsWithUsageAsync(this IChatCompletionService chatGpt, ChatHistory chatHistory)
        {
            var sw = Stopwatch.StartNew();
            var result = await chatGpt.GetChatMessageContentAsync(chatHistory, DefaultSettings.AIChatRequestSettings);
            sw.Stop();

            var answer = result.Content;
            var usage = result.Metadata?["Usage"] as CompletionsUsage;
            return new SKResult(answer, usage, sw.ElapsedMilliseconds);
        }

        public static KernelArguments AddUserParameters(this KernelArguments arguments, ChatTurn[] history)
        {
            if (history.LastOrDefault()?.User is { } userQuestion)
            {
                arguments[ContextVariableOptions.Question] = $"{userQuestion}";
            }
            else
            {
                throw new InvalidOperationException("User question is null");
            }

            arguments["chatTurns"] = history;
            return arguments;
        }

        public static ChatHistory AddChatHistory(this ChatHistory chatHistory, ChatTurn[] history)
        {
            foreach (var chatTurn in history.SkipLast(1))
            {
                chatHistory.AddUserMessage(chatTurn.User);
                if (chatTurn.Bot != null)
                {
                    chatHistory.AddAssistantMessage(chatTurn.Bot);
                }
            }

            return chatHistory;
        }

        public static bool IsChatGpt4Enabled(this Dictionary<string, bool> options)
        {
            var value = options.GetValueOrDefault("GPT4ENABLED", false);
            return value;
        }

        public static ApproachResponse BuildResoponse(this KernelArguments context, ChatRequest request, IConfiguration configuration)
        {
            var result = (SKResult)context["ChatResult"];
            var json = (string)context[ContextVariableOptions.KnowledgeJSON];
            var dataSources = JsonSerializer.Deserialize<KnowledgeSource[]>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            var diagnostics = new Diagnostics(new CompletionsDiagnostics(result.Usage.CompletionTokens, result.Usage.PromptTokens, result.Usage.TotalTokens, result.DurationMilliseconds));
            var systemMessagePrompt = (string)context["SystemMessagePrompt"];
            var userMessage = (string)context["UserMessage"];

            return new ApproachResponse(
                DataPoints: dataSources.Select(x => new SupportingContentRecord(x.filepath, x.content)).ToArray(),
                Answer: result.Answer.Replace("\n", "<br>"),
                Thoughts: $"Searched for:<br>{context["intent"]}<br><br>System:<br>{systemMessagePrompt.Replace("\n", "<br>")}<br><br>{userMessage.Replace("\n", "<br>")}<br><br>{result.Answer.Replace("\n", "<br>")}",
                CitationBaseUrl: configuration.ToCitationBaseUrl(),
                MessageId: request.ChatTurnId,
                ChatId: request.ChatId,
                Diagnostics: diagnostics);
        }
    }
}
