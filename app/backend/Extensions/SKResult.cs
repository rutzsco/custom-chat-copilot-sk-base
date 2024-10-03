using Azure.AI.Inference;

namespace MinimalApi.Extensions
{
    public record SKResult(string Answer, CompletionsUsage? Usage, long DurationMilliseconds);
}
