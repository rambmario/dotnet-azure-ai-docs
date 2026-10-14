FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Nuget.Config ./
COPY AzureAiDocs.slnx ./
COPY AzureAiDocs.Domain/AzureAiDocs.Domain.csproj AzureAiDocs.Domain/
COPY AzureAiDocs.Application/AzureAiDocs.Application.csproj AzureAiDocs.Application/
COPY AzureAiDocs.Infrastructure/AzureAiDocs.Infrastructure.csproj AzureAiDocs.Infrastructure/
COPY AzureAiDocs.Api/AzureAiDocs.Api.csproj AzureAiDocs.Api/

RUN dotnet restore AzureAiDocs.Api/AzureAiDocs.Api.csproj

COPY . .
RUN dotnet publish AzureAiDocs.Api/AzureAiDocs.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AzureAiDocs.Api.dll"]
