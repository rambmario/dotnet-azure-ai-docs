# Document Q&A API

A REST API that lets you upload documents and ask natural language questions about their content, powered by **Azure OpenAI**, built with **.NET 8 Clean Architecture**, **Entity Framework Core**, and **Azure Blob Storage**.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com)
[![Azure OpenAI](https://img.shields.io/badge/Azure-OpenAI-0078D4)](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
[![xUnit](https://img.shields.io/badge/tests-xUnit%20%2B%20Moq-green)](https://xunit.net)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![CI](https://github.com/rambmario/dotnet-azure-ai-docs/actions/workflows/ci.yml/badge.svg)](https://github.com/rambmario/dotnet-azure-ai-docs/actions/workflows/ci.yml)

---

## What it does

1. **Upload** a document → file is stored in Azure Blob Storage; extracted text is persisted in SQL Server.
2. **Ask** a natural language question → Azure OpenAI generates an answer grounded in the document content.
3. **Browse history** → every question and answer is logged per document and fully queryable.

---

## Architecture

Built following **Clean Architecture** principles — dependencies always point inward, and the inner layers (Domain, Application) have zero knowledge of Azure, SQL Server, or any external framework.

```
Client / Swagger UI
        │  HTTP
        ▼
┌──────────────────────────────────────────┐
│              API Layer                   │
│          DocumentsController             │
│   POST /upload · POST /{id}/ask          │
│         GET /{id}/history                │
└──────────────────┬───────────────────────┘
                   │  depends on interfaces
                   ▼
┌──────────────────────────────────────────┐
│           Application Layer              │
│  IAiService · IDocumentRepository        │
│  IStorageService · Request/Response DTOs │
└──────────┬───────────────────────────────┘
           │                  │
           ▼                  ▼
┌──────────────────┐  ┌───────────────────────────────────┐
│   Domain Layer   │  │       Infrastructure Layer         │
│  Document        │  │  AiService (Azure OpenAI SDK)      │
│  ConsultationLog │  │  StorageService (Azure Blob SDK)   │
└──────────────────┘  │  DocumentRepository (EF Core)      │
                      │  AppDbContext + Migrations          │
                      └──────┬────────────┬────────────────┘
                             │            │            │
                             ▼            ▼            ▼
                        Azure OpenAI   SQL Server  Azure Blob
                        (gpt-4o-mini)  (EF Core)   Storage
```

---

## Tech stack

| Layer          | Technology                                      |
|----------------|-------------------------------------------------|
| Runtime        | .NET 8                                          |
| API framework  | ASP.NET Core Web API                            |
| AI             | Azure OpenAI Service — gpt-4o-mini              |
| File storage   | Azure Blob Storage                              |
| Database       | SQL Server + Entity Framework Core 8            |
| Migrations     | EF Core Migrations (code-first)                 |
| Testing        | xUnit + Moq — 11 unit tests                     |
| API docs       | Swagger / OpenAPI                               |

---

## Project structure

```
AzureAiDocs.slnx
Nuget.Config
├── AzureAiDocs.Api/
│   ├── Controllers/
│   │   └── DocumentsController.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── AzureAiDocs.Api.csproj
├── AzureAiDocs.Application/
│   ├── Interfaces/
│   │   ├── IAiService.cs
│   │   ├── IDocumentRepository.cs
│   │   ├── IStorageService.cs
│   │   ├── AskDocumentRequest.cs
│   │   ├── AskDocumentResponse.cs
│   │   └── UploadDocumentResponse.cs
│   └── AzureAiDocs.Application.csproj
├── AzureAiDocs.Domain/
│   ├── Entities/
│   │   ├── Document.cs
│   │   └── ConsultationLog.cs
│   └── AzureAiDocs.Domain.csproj
├── AzureAiDocs.Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Migrations/
│   │   ├── 20260424110828_InitialCreate.cs
│   │   ├── 20260424110828_InitialCreate.Designer.cs
│   │   └── AppDbContextModelSnapshot.cs
│   ├── Repositories/
│   │   └── DocumentRepository.cs
│   ├── Services/
│   │   └── StorageService.cs
│   ├── AiService.cs
│   └── AzureAiDocs.Infrastructure.csproj
└── AzureAiDocs.Tests/
    ├── DocumentsControllerTests.cs
    └── AzureAiDocs.Tests.csproj
```

---

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local instance or Azure SQL)
- Azure OpenAI resource with a `gpt-4o-mini` deployment
- Azure Storage account with a container named `documents`

### Configuration

Add the following to `appsettings.json` (or use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local development — never commit real credentials):

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
    "ApiKey": "YOUR-API-KEY",
    "DeploymentName": "gpt-4o-mini"
  },
  "AzureStorage": {
    "ConnectionString": "YOUR-BLOB-CONNECTION-STRING",
    "ContainerName": "documents"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AzureAiDocs;Trusted_Connection=True;"
  }
}
```

### Run

```bash
# Apply EF Core migrations
dotnet ef database update --project AzureAiDocs.Infrastructure --startup-project AzureAiDocs.Api

# Start the API
dotnet run --project AzureAiDocs.Api
```

Swagger UI will be available at the URL shown in the console (configured in `launchSettings.json`).

---

## API endpoints

### Upload a document

```http
POST /api/documents/upload
Content-Type: multipart/form-data
```

| Field | Type | Description |
|-------|------|-------------|
| `file` | `IFormFile` | The document to upload |

**Response `200 OK`:**
```json
{
  "documentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "report.pdf",
  "uploadedAt": "2026-04-27T10:00:00Z"
}
```

---

### Ask a question

```http
POST /api/documents/{id}/ask
Content-Type: application/json
```

```json
{
  "question": "What are the main conclusions of this document?"
}
```

**Response `200 OK`:**
```json
{
  "question": "What are the main conclusions of this document?",
  "answer": "The main conclusions are...",
  "askedAt": "2026-04-27T10:05:00Z"
}
```

**Response `404 Not Found`:** document ID does not exist.

---

### Get Q&A history

```http
GET /api/documents/{id}/history
```

**Response `200 OK`:**
```json
[
  {
    "question": "What is this document about?",
    "answer": "This document covers...",
    "askedAt": "2026-04-27T10:05:00Z"
  }
]
```

---

## How it works

**Upload flow:**
```
IFormFile
  → IStorageService      uploads binary to Azure Blob, returns (blobUrl, textContent)
  → IDocumentRepository  persists Document entity (FileName, BlobUrl, Content) via EF Core
  → returns UploadDocumentResponse
```

**Ask flow:**
```
DocumentId + Question
  → IDocumentRepository  fetches Document.Content from SQL Server
  → IAiService           sends (content, question) to Azure OpenAI → returns answer
  → IDocumentRepository  persists ConsultationLog (DocumentId, Question, Answer)
  → returns AskDocumentResponse
```

The Application layer orchestrates the flow by depending exclusively on interfaces — `AiService`, `StorageService`, and `DocumentRepository` are injected at runtime by the DI container, keeping business logic fully decoupled from infrastructure concerns.

---

## Running tests

```bash
dotnet test AzureAiDocs.Tests
```

11 unit tests covering all three endpoints. All external dependencies (`IAiService`, `IDocumentRepository`, `IStorageService`) are mocked with Moq — no Azure account or database required to run the test suite.

| Endpoint  | Scenarios covered |
|-----------|-------------------|
| `Upload`  | Valid file → 200, null file → 400, empty file → 400, storage and repository calls verified |
| `Ask`     | Document found → 200, document not found → 404, log persisted correctly, document content passed to AI |
| `History` | All logs returned, empty list, correct DTO mapping |

---

## Author

**Mario Ramb** · [linkedin.com/in/marioramb](https://linkedin.com/in/marioramb) · [github.com/rambmario](https://github.com/rambmario)

---

## License

MIT
