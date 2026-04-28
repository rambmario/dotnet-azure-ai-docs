using Azure;
using Azure.AI.OpenAI;
using AzureAiDocs.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace AzureAiDocs.Infrastructure.Services;

public class AiService : IAiService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;

    public AiService(IConfiguration config)
    {
        _client = new AzureOpenAIClient(
            new Uri(config["AzureOpenAI:Endpoint"]!),
            new AzureKeyCredential(config["AzureOpenAI:ApiKey"]!));
        _deploymentName = config["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";
    }

    public async Task<string> AskAsync(string documentContent, string question)
    {
        var chatClient = _client.GetChatClient(_deploymentName);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are a helpful assistant. Answer questions based only " +
                "on the document content provided. If the answer is not in " +
                "the document, say so clearly."),
            new UserChatMessage(
                $"Document content:\n\n{documentContent}\n\nQuestion: {question}")
        };

        var response = await chatClient.CompleteChatAsync(messages);
        return response.Value.Content[0].Text;
    }
}