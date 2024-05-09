// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.SemanticKernel.Services;
using MinimalApi;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOutputCache();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddCrossOriginResourceSharing();
builder.Services.AddAzureServices(builder.Configuration);
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN-HEADER"; options.FormFieldName = "X-CSRF-TOKEN-FORM"; });
AppConfiguration.Load(builder.Configuration);


static string? GetEnvVar(string key) => Environment.GetEnvironmentVariable(key);
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    //static string? GetEnvVar(string key) => Environment.GetEnvironmentVariable(key);

    // set application telemetry
    if (GetEnvVar("APPLICATIONINSIGHTS_CONNECTION_STRING") is string appInsightsConnectionString && !string.IsNullOrEmpty(appInsightsConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry((option) =>
        {
            option.ConnectionString = appInsightsConnectionString;
        });
    }

    if (AppConfiguration.EnableDataProtectionBlobKeyStorage)
    {
        var serviceProvider = builder.Services.BuildServiceProvider();
        var blobServiceClient = serviceProvider.GetRequiredService<BlobServiceClient>();
        var container = blobServiceClient.GetBlobContainerClient(AppConfiguration.DataProtectionKeyContainer);
        if (!await container.ExistsAsync())
        {
            await container.CreateAsync();
            Console.WriteLine("Container created.");
        }
        builder.Services.AddDataProtection().PersistKeysToAzureBlobStorage(AppConfiguration.AzureStorageAccountConnectionString, AppConfiguration.DataProtectionKeyContainer, "keys.xml");
    } 
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseOutputCache();
app.UseRouting();
app.UseStaticFiles();
app.UseCors();
app.UseBlazorFrameworkFiles();
app.UseAntiforgery();
app.MapRazorPages();
app.MapControllers();
app.Use(next => context =>
{
    var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
    var tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Cookies.Append("XSRF-TOKEN", tokens?.RequestToken ?? string.Empty, new CookieOptions() { HttpOnly = false });
    return next(context);
});
app.MapFallbackToFile("index.html");

app.MapApi();

app.Run();
