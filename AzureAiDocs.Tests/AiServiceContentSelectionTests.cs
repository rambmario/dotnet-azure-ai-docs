using AzureAiDocs.Infrastructure.Services;

namespace AzureAiDocs.Tests.Services;

public class AiServiceContentSelectionTests
{
    [Fact]
    public void SelectRelevantContent_ShortDocument_ReturnsFullText()
    {
        var content = "Short document about Azure OpenAI.";
        var result = AiService.SelectRelevantContent(content, "What is this about?", 12_000);
        Assert.Equal(content, result);
    }

    [Fact]
    public void SelectRelevantContent_LongDocument_PrefersChunksMatchingQuestion()
    {
        var filler = new string('x', 2_500);
        var relevant = "The capital of France is Paris and it is famous for the Eiffel Tower.";
        var content = $"{filler}\n{relevant}\n{filler}\n{filler}";

        var result = AiService.SelectRelevantContent(content, "capital of France Paris", 3_000);

        Assert.Contains("Paris", result, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Length <= 3_000);
    }
}
