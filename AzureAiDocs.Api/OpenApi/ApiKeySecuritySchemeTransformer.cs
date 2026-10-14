using AzureAiDocs.Application.Options;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AzureAiDocs.Api.OpenApi;

/// <summary>
/// Documents API-key authentication in the OpenAPI spec so Scalar shows an Auth control.
/// </summary>
internal sealed class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = ApiKeyAuthOptions.HeaderName,
            In = ParameterLocation.Header,
            Description =
                "Application API key (not an Azure key). " +
                "Configured via ApiKeyAuth:ApiKey. " +
                "Optional in Development; required in Staging/Production."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("ApiKey")] = []
        });

        return Task.CompletedTask;
    }
}
