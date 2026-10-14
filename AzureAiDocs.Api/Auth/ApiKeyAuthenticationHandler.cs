using System.Security.Claims;
using System.Text.Encodings.Web;
using AzureAiDocs.Application.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AzureAiDocs.Api.Auth;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";

    private readonly ApiKeyAuthOptions _apiKeyOptions;
    private readonly IHostEnvironment _environment;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiKeyAuthOptions> apiKeyOptions,
        IHostEnvironment environment)
        : base(options, logger, encoder)
    {
        _apiKeyOptions = apiKeyOptions.Value;
        _environment = environment;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Development convenience: allow local Scalar/curl demos without a header.
        // Staging/Production still require a valid X-Api-Key (see README → Authentication).
        if (_environment.IsDevelopment() &&
            !Request.Headers.ContainsKey(ApiKeyAuthOptions.HeaderName))
        {
            return Task.FromResult(Authenticated("DevUser"));
        }

        if (!Request.Headers.TryGetValue(ApiKeyAuthOptions.HeaderName, out var providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing {ApiKeyAuthOptions.HeaderName} header."));
        }

        if (string.IsNullOrWhiteSpace(_apiKeyOptions.ApiKey) ||
            !string.Equals(providedKey.ToString(), _apiKeyOptions.ApiKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        return Task.FromResult(Authenticated("ApiKeyUser"));
    }

    private static AuthenticateResult Authenticated(string name)
    {
        var claims = new[] { new Claim(ClaimTypes.Name, name) };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
