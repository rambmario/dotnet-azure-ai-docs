using AzureAiDocs.Application.Interfaces;
using AzureAiDocs.Infrastructure.Data;
using AzureAiDocs.Infrastructure.Repositories;
using AzureAiDocs.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IAiService, AiService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();