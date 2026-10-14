using AzureAiDocs.Application.Interfaces;
using AzureAiDocs.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AzureAiDocs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        return services;
    }
}
