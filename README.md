# Document Q&A API

A REST API that lets you upload documents and ask natural language questions about their content, powered by **Azure OpenAI**, built with **.NET 10 Clean Architecture**, **Entity Framework Core**, and **Azure Blob Storage**.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com)
[![Azure OpenAI](https://img.shields.io/badge/Azure-OpenAI-0078D4)](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
[![xUnit](https://img.shields.io/badge/tests-xUnit%20%2B%20Moq-green)](https://xunit.net)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![CI](https://github.com/rambmario/dotnet-azure-ai-docs/actions/workflows/ci.yml/badge.svg)](https://github.com/rambmario/dotnet-azure-ai-docs/actions/workflows/ci.yml)

---

## What it does

1. **Upload** a `.txt` or `.pdf` document → binary stored in Azure Blob Storage; extracted text persisted in SQL Server.
2. **Ask** a natural language question → Azure OpenAI generates an answer grounded in the document content (long documents are chunked and ranked by relevance).
3. **Browse history** → every question and answer is logged per document and fully queryable.

---

## Authentication

This API is protected with a custom **API key** scheme (not an Azure credential):

| Item | Detail |
|------|--------|
| Header | `X-Api-Key` |
| Config | `ApiKeyAuth:ApiKey` in User Secrets / env / `appsettings` |
| Development | Header **optional** — open Scalar and call endpoints immediately |
| Staging / Production | Header **required** — missing/invalid key → `401 Unauthorized` |
| Health | `GET /health` stays public |

Implementation: `ApiKeyAuthenticationHandler` + `[Authorize]` on `DocumentsController`. Integration tests assert that a missing key returns `401` outside Development.

---

## Architecture

Built following **Clean Architecture** principles — dependencies always point inward. The Application layer owns use cases; the API layer stays thin and delegates to `IDocumentService`.

```
Client / Scalar UI
        │  HTTP + X-Api-Key
        ▼
┌──────────────────────────────────────────┐
│              API Layer                   │
│     Auth · Middleware · Controllers      │
│   POST /upload · POST /{id}/ask          │
│         GET /{id}/history                │
│              GET /health                 │
└──────────────────┬───────────────────────┘
                   │  IDocumentService
                   ▼
┌──────────────────────────────────────────┐
│           Application Layer              │
│  DocumentService (use cases)             │
│  Options · DTOs · Exceptions             │
│  IAiService · IDocumentRepository        │
│  IStorageService                         │
└──────────┬───────────────────────────────┘
           │                  │
           ▼                  ▼
┌──────────────────┐  ┌───────────────────────────────────┐
│   Domain Layer   │  │       Infrastructure Layer         │
│  Document        │  │  AiService (Azure OpenAI SDK)      │
│  ConsultationLog │  │  StorageService (Blob + PdfPig)    │
│                  │  │  DocumentRepository (EF Core)      │
│                  │  │  AppDbContext + Migrations          │
└──────────────────┘  └──────┬────────────┬────────────────┘
                             │            │            │
                             ▼            ▼            ▼
                        Azure OpenAI   SQL Server  Azure Blob
                        (gpt-4o-mini)  (EF Core)   Storage
```

---

## Tech stack

| Layer          | Technology                                      |
|----------------|-------------------------------------------------|
| Runtime        | .NET 10                                         |
| API framework  | ASP.NET Core Web API                            |
| Auth           | API key (`X-Api-Key`)                           |
| AI             | Azure OpenAI Service — gpt-4o-mini              |
| File storage   | Azure Blob Storage                              |
| Text extract   | UTF-8 plain text + PdfPig (PDF)                 |
| Database       | SQL Server + Entity Framework Core 10           |
| Migrations     | EF Core Migrations (code-first)                 |
| Testing        | xUnit + Moq + WebApplicationFactory             |
| API docs       | OpenAPI + Scalar UI (Development only)          |
| Containers     | Docker + docker-compose                         |
| CI             | GitHub Actions (build, test, coverage, CodeQL)  |

---

## Project structure

```
AzureAiDocs.slnx
Nuget.Config
├── AzureAiDocs.Api/
│   ├── Auth/
│   ├── Controllers/
│   ├── Middleware/
│   ├── OpenApi/
│   ├── Program.cs
│   └── appsettings*.json
├── AzureAiDocs.Application/
│   ├── DTOs/
│   ├── Exceptions/
│   ├── Interfaces/
│   ├── Options/
│   ├── Services/
│   └── DependencyInjection.cs
├── AzureAiDocs.Domain/
│   └── Entities/
├── AzureAiDocs.Infrastructure/
│   ├── Data/
│   ├── Migrations/
│   ├── Repositories/
│   ├── Services/
│   └── DependencyInjection.cs
├── AzureAiDocs.Tests/
├── Dockerfile
└── docker-compose.yml
```

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (local instance, LocalDB, or Azure SQL)
- Azure OpenAI resource with a `gpt-4o-mini` deployment
- Azure Storage account with a container named `documents` (created automatically if missing)

### Configuration

Prefer [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local development — never commit real credentials.

```bash
dotnet user-secrets init --project AzureAiDocs.Api
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-RESOURCE.openai.azure.com/" --project AzureAiDocs.Api
dotnet user-secrets set "AzureOpenAI:ApiKey" "YOUR-API-KEY" --project AzureAiDocs.Api
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini" --project AzureAiDocs.Api
dotnet user-secrets set "AzureStorage:ConnectionString" "YOUR-BLOB-CONNECTION-STRING" --project AzureAiDocs.Api
dotnet user-secrets set "AzureStorage:ContainerName" "documents" --project AzureAiDocs.Api
dotnet user-secrets set "ApiKeyAuth:ApiKey" "your-local-api-key" --project AzureAiDocs.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=AzureAiDocs;Trusted_Connection=True;TrustServerCertificate=True;" --project AzureAiDocs.Api
```

Alternatively, copy `AzureAiDocs.Api/appsettings.template.json` to `appsettings.Development.json` (gitignored) and fill in the values.

Supported upload types: **`.txt`** and **`.pdf`** (default max size 5 MB, configurable via `DocumentUpload`).

### Run

```bash
# Apply EF Core migrations
dotnet ef database update --project AzureAiDocs.Infrastructure --startup-project AzureAiDocs.Api

# Start the API
dotnet run --project AzureAiDocs.Api
```

Open [http://localhost:5157/scalar](http://localhost:5157/scalar) and try **Upload → Ask → History**. No API key needed in Development.

### Docker

```bash
export AZURE_OPENAI_ENDPOINT="https://YOUR-RESOURCE.openai.azure.com/"
export AZURE_OPENAI_API_KEY="YOUR-API-KEY"
export AZURE_STORAGE_CONNECTION_STRING="YOUR-BLOB-CONNECTION-STRING"

docker compose up --build
```

API: `http://localhost:8080` · Health: `GET /health`

After the containers are up, apply migrations against the compose SQL instance (or add an init job):

```bash
dotnet ef database update --project AzureAiDocs.Infrastructure --startup-project AzureAiDocs.Api
```

---

## API endpoints

See [Authentication](#authentication) for when `X-Api-Key` is required.

### Upload a document

```http
POST /api/documents/upload
Content-Type: multipart/form-data
```

| Field | Type | Description |
|-------|------|-------------|
| `file` | `IFormFile` | `.txt` or `.pdf` document |

**Response `200 OK`:**
```json
{
  "documentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "report.pdf",
  "uploadedAt": "2026-04-27T10:00:00Z"
}
```

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

**Response `404 Not Found`:** document ID does not exist.

### Get Q&A history

```http
GET /api/documents/{id}/history
```

**Response `404 Not Found`:** document ID does not exist.

### Health

```http
GET /health
```

---

## How it works

**Upload flow:**
```
IFormFile
  → DocumentService       validates type/size
  → IStorageService       uploads binary to Blob, extracts .txt/.pdf text
  → IDocumentRepository   persists Document entity via EF Core
  → returns UploadDocumentResponse
```

**Ask flow:**
```
DocumentId + Question
  → DocumentService       loads document (404 if missing)
  → IAiService            ranks chunks if needed, calls Azure OpenAI
  → IDocumentRepository   persists ConsultationLog
  → returns AskDocumentResponse
```

---

## Running tests

```bash
dotnet test AzureAiDocs.slnx
```

Unit tests cover the controller, `DocumentService` use cases, and content-selection logic. Integration tests use `WebApplicationFactory` for auth and health checks. External Azure dependencies are mocked in unit tests.

| Area | Scenarios covered |
|------|-------------------|
| Controller | Upload/Ask/History delegation, null file → 400 |
| DocumentService | Valid upload, extension/size validation, ask + log, history 404 |
| AiService | Short-doc passthrough, long-doc relevance selection |
| Integration | Missing API key → 401, `/health` reachable |

---

## Author

**Mario Ramb** · [linkedin.com/in/marioramb](https://linkedin.com/in/marioramb) · [github.com/rambmario](https://github.com/rambmario)

---

## License

MIT
