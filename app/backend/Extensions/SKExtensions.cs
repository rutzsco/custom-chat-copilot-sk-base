using System.Text.RegularExpressions;
using Azure.Core;
using ClientApp.Components;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.VisualBasic;
using MinimalApi.Services.Profile;
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

        public static KernelArguments AddUserParameters(this KernelArguments arguments, ChatTurn[] history, ProfileDefinition profile, UserInformation user, IEnumerable<string>? selectedDocuments = null)
        {
            arguments[ContextVariableOptions.Profile] = profile;
            arguments[ContextVariableOptions.UserId] = user.UserId;
            arguments[ContextVariableOptions.SessionId] = user.SessionId;

            if (selectedDocuments != null)
            {
                arguments[ContextVariableOptions.SelectedDocuments] = selectedDocuments;
            }

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

        public static bool IsChatGpt4Enabled(this Dictionary<string, string> options)
        {
            var value = options.GetValueOrDefault("GPT4ENABLED", "false");
            return value.ToLower() == "true";
        }

        public static bool IsChatProfile(this Dictionary<string, string> options)
        {
            var profile = options.GetChatProfile();
            var selected = ProfileDefinition.All.FirstOrDefault(x => x.Name == profile.Name);
            return selected?.Approach.ToUpper() == "CHAT";
        }
        public static bool IsEndpointAssistantProfile(this Dictionary<string, string> options)
        {
            var profile = options.GetChatProfile();
            var selected = ProfileDefinition.All.FirstOrDefault(x => x.Name == profile.Name);
            return selected?.Approach.ToUpper() == "ENDPOINTASSISTANT";
        }

        public static ProfileDefinition GetChatProfile(this Dictionary<string, string> options)
        {
            var defaultProfile = ProfileDefinition.All.First();
            var value = options.GetValueOrDefault("PROFILE", defaultProfile.Name);
            return ProfileDefinition.All.FirstOrDefault(x => x.Name == value) ?? defaultProfile;
        }

        public static string? GetImageContent(this Dictionary<string, string> options)
        {
            if (options.TryGetValue("IMAGECONTENT", out var value))
                return value;

            return null;
        }
        public static bool ImageContentExists(this Dictionary<string, string> options)
        {
            return options.ContainsKey("IMAGECONTENT");
        }
        public static ApproachResponse BuildResoponse(this KernelArguments context, ProfileDefinition profile, ChatRequest request, IConfiguration configuration, string modelDeploymentName, long workflowDurationMilliseconds)
        {
            var result = context[ContextVariableOptions.ChatResult] as SKResult;
            var knowledgeSourceSummary = context[ContextVariableOptions.KnowledgeSummary] as KnowledgeSourceSummary;
            var systemMessagePrompt = context[ContextVariableOptions.SystemMessagePrompt] as string;
            var userMessage = context[ContextVariableOptions.UserMessage] as string;

            ArgumentNullException.ThrowIfNull(result, "ChatResult is null");
            ArgumentNullException.ThrowIfNull(knowledgeSourceSummary, "KnowledgeSummary is null");
            ArgumentNullException.ThrowIfNull(systemMessagePrompt, "SystemMessagePrompt is null");
            ArgumentNullException.ThrowIfNull(userMessage, "UserMessage is null");

            var dataSources = knowledgeSourceSummary?.Sources.Select(x => new SupportingContentRecord(x.GetFilepath(profile.RAGSettings.CitationUseSourcePage), x.GetContent())).ToArray();

            var chatDiagnostics = new CompletionsDiagnostics(result.Usage.CompletionTokens, result.Usage.PromptTokens, result.Usage.TotalTokens, result.DurationMilliseconds);
            var diagnostics = new Diagnostics(chatDiagnostics, modelDeploymentName, workflowDurationMilliseconds);


            var thoughts = GetThoughtsRAG(context, result.Answer);
            var contextData = new ResponseContext(profile.Name, dataSources, thoughts.ToArray(), request.ChatTurnId, request.ChatId, diagnostics);

            return new ApproachResponse(
                Answer: result.Answer.Replace("\n", "<br>"),
                CitationBaseUrl: profile.Id,
                contextData);
        }


        public static ApproachResponse BuildStreamingResoponse(this KernelArguments context, ProfileDefinition profile, ChatRequest request, int requestTokenCount, string answer, IConfiguration configuration, string modelDeploymentName, long workflowDurationMilliseconds, List<KeyValuePair<string, string>> requestSettings = null)
        {
            var dataSources = new SupportingContentRecord[] { };
            if (context.ContainsName(ContextVariableOptions.Knowledge) && context[ContextVariableOptions.Knowledge] != "NO_SOURCES")
            {
                var knowledgeSourceSummary = context[ContextVariableOptions.KnowledgeSummary] as KnowledgeSourceSummary;
                ArgumentNullException.ThrowIfNull(knowledgeSourceSummary, "knowledgeSourceSummary is null");

                dataSources = knowledgeSourceSummary.Sources.Select(x => new SupportingContentRecord(x.GetFilepath(profile.RAGSettings.CitationUseSourcePage), x.GetContent())).ToArray();
            }

            var completionTokens = GetTokenCount(answer);
            var totalTokens = completionTokens + requestTokenCount;
            var chatDiagnostics = new CompletionsDiagnostics(completionTokens, requestTokenCount, totalTokens, 0);
            var diagnostics = new Diagnostics(chatDiagnostics, modelDeploymentName, workflowDurationMilliseconds);

            var thoughts = GetThoughtsRAGV2(context, answer, requestSettings);
            var contextData = new ResponseContext(profile.Name, dataSources, thoughts.ToArray(), request.ChatTurnId, request.ChatId, diagnostics);

            return new ApproachResponse(
                Answer: NormalizeResponseText(answer),
                CitationBaseUrl: profile.Id,
                contextData);
        }

        public static ApproachResponse BuildChatSimpleResoponse(this KernelArguments context, ProfileDefinition profile, ChatRequest request, int requestTokenCount, string answer, IConfiguration configuration, string modelDeploymentName, long workflowDurationMilliseconds)
        {
            var completionTokens = GetTokenCount(answer);
            var totalTokens = completionTokens + requestTokenCount;
            var chatDiagnostics = new CompletionsDiagnostics(completionTokens, requestTokenCount, totalTokens, 0);
            var diagnostics = new Diagnostics(chatDiagnostics, modelDeploymentName, workflowDurationMilliseconds);

            var thoughts = GetThoughts(context, answer);
            var contextData = new ResponseContext(profile.Name, null, thoughts.ToArray(), request.ChatTurnId, request.ChatId, diagnostics);

            return new ApproachResponse(
                Answer: NormalizeResponseText(answer),
                CitationBaseUrl: string.Empty,
                contextData);
        }

        private static string NormalizeResponseText(string text)
        {
            text = text.StartsWith("null,") ? text[5..] : text;
            text = text.Replace("\r", "\n")
                .Replace("\\n\\r", "\n")
                .Replace("\\n", "\n");

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

        private static IEnumerable<ThoughtRecord> GetThoughtsRAG(KernelArguments context, string answer)
        {
            var intent = context["intent"] as string;
            var systemMessagePrompt = context["SystemMessagePrompt"] as string;
            var userMessage = context["UserMessage"] as string;

            var thoughts = new List<ThoughtRecord>
                {
                    new("Generated search query", intent),
                    new("Prompt", $"System:\n\n{systemMessagePrompt}\n\nUser:\n\n{userMessage}"),
                    new("Answer", answer)
                };

            return thoughts;
        }

        private static IEnumerable<ThoughtRecord> GetThoughtsRAGV2(KernelArguments context, string answer, List<KeyValuePair<string, string>> requestSettings)
        {
            if (requestSettings == null)
                return new List<ThoughtRecord>();

            var searchRequestDiagnostics = context[ContextVariableOptions.SearchDiagnostics] as List<KeyValuePair<string, string>>;
            var thoughts = new List<ThoughtRecord>
                {
                    new("Generated search query", BuildPromptContext(searchRequestDiagnostics)),
                    new("Prompt", BuildPromptContext(requestSettings)),
                    new("Answer", answer)
                };

            return thoughts;
        }

        private static IEnumerable<ThoughtRecord> GetThoughts(KernelArguments context, string answer)
        {
            var userMessage = context["UserMessage"] as string;
            var thoughts = new List<ThoughtRecord>
                {
                    new("Prompt", userMessage),
                    new("Answer", answer)
                };

            return thoughts;
        }

        private static string BuildPromptContext(List<KeyValuePair<string, string>> requestSettings)
        {
            var sb = new StringBuilder();
            foreach (var setting in requestSettings.Where(x => x.Key.StartsWith("PROMPTMESSAGE:")))
            {

                sb.AppendLine($"{setting.Key.Replace("PROMPTMESSAGE:", "")}:\n\n{setting.Value}\n");
            }

            sb.AppendLine("Settings: \n");
            foreach (var setting in requestSettings.Where(x => x.Key.StartsWith("PROMPTKEY:")))
            {
                sb.AppendLine($"{setting.Key.Replace("PROMPTKEY:", "")}: {setting.Value}");
            }
            return sb.ToString();
        }

        public static List<KeyValuePair<string, string>> GenerateRequestProperties(this Microsoft.SemanticKernel.ChatCompletion.ChatHistory chatHistory, PromptExecutionSettings settings)
        {
            var results = new List<KeyValuePair<string, string>>();
            foreach (var item in chatHistory)
            {
                if (item is ChatMessageContent chatMessageContent)
                {
                    var content = chatMessageContent.Content;
                    var role = chatMessageContent.Role;
                    results.Add(new KeyValuePair<string, string>($"PROMPTMESSAGE:{role}", content));
                }
            }

            foreach (var item in settings.ExtensionData ?? new Dictionary<string, object>())
            {
                results.Add(new KeyValuePair<string, string>($"PROMPTKEY:{item.Key}", item.Value.ToString()));
            }

            return results;
        }
    }
}

