using System.ComponentModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.SemanticKernel.ChatCompletion;
using MinimalApi.Extensions;
using MinimalApi.Services.Profile.Prompts;
using Newtonsoft.Json.Linq;

namespace MinimalApi.Services.Skills;

public class WeatherSkill
{
    [KernelFunction("GetForcast"), Description("Get a weather forcast.")]
    public async Task RetrieveWeatherForcastAsync([Description("chat History")] ChatTurn[] chatTurns,
                                                  string WeatherLatLong,
                                                  KernelArguments arguments,
                                                  Kernel kernel)
    {
        var parts = WeatherLatLong.Split(',');
        string url = $"https://api.weather.gov/points/{parts[0].Trim()},{parts[1].Trim()}";

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "app");
        HttpResponseMessage response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        // Parse the response body
        string responseBody = await response.Content.ReadAsStringAsync();
        JObject json = JObject.Parse(responseBody);

        // Extract the forecast URL from the JSON response
        string forecastUrl = json["properties"]["forecast"].ToString();

        HttpResponseMessage forecastResponse = await httpClient.GetAsync(forecastUrl);
        forecastResponse.EnsureSuccessStatusCode();
        string forecastResponseBody = await forecastResponse.Content.ReadAsStringAsync();
        arguments[ContextVariableOptions.Knowledge] = forecastResponseBody;
    }

    [KernelFunction("GetLocationLatLong"), Description("Determine the location latitude and longitude based on user request")]
    public async Task DetermineLatLongAsync([Description("chat History")] ChatTurn[] chatTurns,
                                            string WeatherLocation,
                                            KernelArguments arguments,
                                            Kernel kernel)
    {
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(PromptService.GetPromptByName("WeatherLatLongSystemPrompt")).AddChatHistory(chatTurns);
        chatHistory.AddUserMessage(WeatherLocation);

        var searchAnswer = await chatGpt.GetChatMessageContentAsync(chatHistory, DefaultSettings.AISearchRequestSettings, kernel);
        arguments[ContextVariableOptions.WeatherLatLong] = searchAnswer.Content;
    }

    [KernelFunction("GetLocation"), Description("Determine the location latitude and longitude based on user request")]
    public async Task DetermineLocationAsync([Description("chat History")] ChatTurn[] chatTurns,
                                        KernelArguments arguments,
                                        Kernel kernel)
    {
        var chatGpt = kernel.Services.GetService<IChatCompletionService>();

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory(PromptService.GetPromptByName("WeatherLocationSystemPrompt")).AddChatHistory(chatTurns);
        var userMessage = await PromptService.RenderPromptAsync(kernel, PromptService.GetPromptByName(PromptService.WeatherUserPrompt), arguments);
        chatHistory.AddUserMessage(userMessage);

        var searchAnswer = await chatGpt.GetChatMessageContentAsync(chatHistory, DefaultSettings.AISearchRequestSettings, kernel);
        arguments[ContextVariableOptions.WeatherLocation] = searchAnswer.Content;
    }
}
