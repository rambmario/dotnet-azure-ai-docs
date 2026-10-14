using AzureAiDocs.Application.Interfaces;
using AzureAiDocs.Application.Options;
using AzureAiDocs.Infrastructure.Data;
using AzureAiDocs.Infrastructure.Repositories;
using AzureAiDocs.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureAiDocs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AzureOpenAIOptions>()
            .Bind(configuration.GetSection(AzureOpenAIOptions.SectionName));

        services.AddOptions<AzureStorageOptions>()
            .Bind(configuration.GetSection(AzureStorageOptions.SectionName));

        services.AddOptions<DocumentUploadOptions>()
            .Bind(configuration.GetSection(DocumentUploadOptions.SectionName));

        services.AddOptions<ApiKeyAuthOptions>()
            .Bind(configuration.GetSection(ApiKeyAuthOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "ApiKeyAuth:ApiKey must be configured.")
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IStorageService, StorageService>();
        services.AddScoped<IAiService, AiService>();

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database");

        return services;
    }
}
