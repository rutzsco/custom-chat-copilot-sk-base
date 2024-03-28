using System.Text.RegularExpressions;
using Azure.Core;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.VisualBasic;
using MinimalApi.Services.Search;
using TiktokenSharp;

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

        public static ApproachResponse BuildResoponse(this KernelArguments context, ChatRequest request, IConfiguration configuration, string modelDeploymentName, long workflowDurationMilliseconds)
        {
            var result = (SKResult)context["ChatResult"];
            var knowledgeSourceSummary = (KnowledgeSourceSummary)context[ContextVariableOptions.KnowledgeSummary];
            var dataSources = knowledgeSourceSummary.Sources.Select(x => new SupportingContentRecord(x.GetFilepath(), x.GetContent())).ToArray();

            var chatDiagnostics = new CompletionsDiagnostics(result.Usage.CompletionTokens, result.Usage.PromptTokens, result.Usage.TotalTokens, result.DurationMilliseconds);
            var diagnostics = new Diagnostics(chatDiagnostics, modelDeploymentName, workflowDurationMilliseconds);
            var systemMessagePrompt = (string)context["SystemMessagePrompt"];
            var userMessage = (string)context["UserMessage"];

            return new ApproachResponse(
                DataPoints: dataSources,
                Answer: result.Answer.Replace("\n", "<br>"),
                Thoughts: $"Searched for:<br>{context["intent"]}<br><br>System:<br>{systemMessagePrompt.Replace("\n", "<br>")}<br><br>{userMessage.Replace("\n", "<br>")}<br><br>{result.Answer.Replace("\n", "<br>")}",
                CitationBaseUrl: configuration.ToCitationBaseUrl(),
                MessageId: request.ChatTurnId,
                ChatId: request.ChatId,
                Diagnostics: diagnostics);
        }


        public static ApproachResponse BuildStreamingResoponse(this KernelArguments context, ChatRequest request, int requestTokenCount, string answer, IConfiguration configuration, string modelDeploymentName, long workflowDurationMilliseconds)
        {
            var knowledgeSourceSummary = (KnowledgeSourceSummary)context[ContextVariableOptions.KnowledgeSummary];
            var dataSources = knowledgeSourceSummary.Sources.Select(x => new SupportingContentRecord(x.GetFilepath(), x.GetContent())).ToArray();

            var completionTokens = GetTokenCount(answer);
            var totalTokens = completionTokens + requestTokenCount;
            var chatDiagnostics = new CompletionsDiagnostics(completionTokens, requestTokenCount, totalTokens, 0);
            var diagnostics = new Diagnostics(chatDiagnostics, modelDeploymentName, workflowDurationMilliseconds);
            var systemMessagePrompt = (string)context["SystemMessagePrompt"];
            var userMessage = (string)context["UserMessage"];

            return new ApproachResponse(
                DataPoints: dataSources,
                Answer: NormalizeResponseText(answer),
                Thoughts: $"Searched for:<br>{context["intent"]}<br><br>System:<br>{systemMessagePrompt.Replace("\n", "<br>")}<br><br>{userMessage.Replace("\n", "<br>")}<br><br>{answer.Replace("\n", "<br>")}",
                CitationBaseUrl: configuration.ToCitationBaseUrl(),
                MessageId: request.ChatTurnId,
                ChatId: request.ChatId,
                Diagnostics: diagnostics);
        }

        private static string NormalizeResponseText(string text)
        {
            text = text.StartsWith("null,") ? text[5..] : text;
            text = text.Replace("\r", "\n")
                .Replace("\\n\\r", "\n")
                .Replace("\\n", "\n");

            text = Regex.Unescape(text);
            return text;
        }
        public static int GetTokenCount(this ChatHistory chatHistory)
        {
            string requestContent = string.Join("", chatHistory.Select(x => x.Content));
            var tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");
            return tikToken.Encode(requestContent).Count;
        }

        public static int GetTokenCount(string text)
        {
            var tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");
            return tikToken.Encode(text).Count;
        }
    }
}

