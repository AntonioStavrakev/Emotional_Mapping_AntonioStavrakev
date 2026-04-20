using Emotional_Mapping.Application.AI;

namespace Emotional_Mapping.Application.Interfaces;

public interface IAiEmotionService
{
    Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisInput input, CancellationToken ct);
    Task<string?> TranslateTextAsync(string? text, string targetLanguage, CancellationToken ct);
}
