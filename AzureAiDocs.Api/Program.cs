using AzureAiDocs.Api.Auth;
using AzureAiDocs.Api.Middleware;
using AzureAiDocs.Api.OpenApi;
using AzureAiDocs.Application;
using AzureAiDocs.Application.Options;
using AzureAiDocs.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new()
        {
            Title = "Document Q&A API",
            Version = "v1",
            Description =
                "Upload .txt/.pdf documents to Azure Blob Storage, persist extracted text in SQL Server, " +
                "and ask grounded questions via Azure OpenAI. " +
                "Protected by API-key authentication (X-Api-Key). " +
                "In Development the header is optional for local demos; required in other environments."
        };
        return Task.CompletedTask;
    });
    options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>();
});

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName,
        _ => { });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Document Q&A API");
    });

    var configuredKey = app.Configuration[$"{ApiKeyAuthOptions.SectionName}:ApiKey"]
        ?? "(not set)";
    app.Logger.LogInformation(
        "Auth: Development mode — {Header} is optional. Non-Dev environments require it (ApiKeyAuth:ApiKey = {KeyHint}).",
        ApiKeyAuthOptions.HeaderName,
        configuredKey.Length <= 4 ? configuredKey : $"{configuredKey[..2]}***");
}

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
