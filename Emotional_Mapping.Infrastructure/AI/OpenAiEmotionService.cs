using System.Net.Http.Json;
using System.Text.Json;
using Emotional_Mapping.Application.AI;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Emotional_Mapping.Infrastructure.AI;

public class OpenAiEmotionService : IAiEmotionService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly OpenAiOptions _opt;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OpenAiEmotionService(IHttpClientFactory httpFactory, IOptions<OpenAiOptions> opt)
    {
        _httpFactory = httpFactory;
        _opt = opt.Value;
    }

    public async Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisInput input, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("openai");

        var schema = BuildSchema(); // strict json_schema за стабилен парсинг  [oai_citation:3‡OpenAI Platform](https://platform.openai.com/docs/guides/structured-outputs?utm_source=chatgpt.com)
        var prompt = BuildBulgarianPrompt(input);

        var body = new ResponsesRequest
        {
            Model = _opt.Model,
            Input = prompt,
            Temperature = 0.2,
            ResponseFormat = new ResponseFormat
            {
                Type = "json_schema",
                JsonSchema = schema
            }
        };

        var resp = await http.PostAsJsonAsync("responses", body, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();

        var raw = await resp.Content.ReadFromJsonAsync<ResponsesResponse>(JsonOpts, ct)
                  ?? throw new InvalidOperationException("OpenAI response is empty.");

        // Responses API връща текст/структуриран output; ние взимаме parsed JSON от output_text  [oai_citation:4‡OpenAI Platform](https://platform.openai.com/docs/api-reference/responses?utm_source=chatgpt.com)
        var json = raw.OutputText;
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("OpenAI returned empty output_text.");

        var parsed = JsonSerializer.Deserialize<AiJsonResult>(json, JsonOpts)
                     ?? throw new InvalidOperationException("Cannot parse AI JSON.");

        return new AiAnalysisResult
        {
            FinalEmotion = ParseEmotion(parsed.Emotion),
            Confidence = Math.Clamp(parsed.Confidence, 0, 1),
            Summary = parsed.Summary,
            AiModel = _opt.Model,
            TokensUsed = 0,
            Recommendations = parsed.Recommendations
                .Where(r => r.PlaceId != Guid.Empty)
                .Select(r => new AiRecommendedPlace
                {
                    PlaceId = r.PlaceId,
                    Score = Math.Clamp(r.Score, 0, 1),
                    Reason = r.Reason ?? "Подходящо според AI анализа."
                })
                .ToList()
        };
    }

    private static string BuildBulgarianPrompt(AiAnalysisInput input)
    {
        // Държим промпта на български, защото UI ти е на български.
        // Важно: подаваме “кандидат-места” + емоционални сигнали.
        var places = input.Places.Take(120).Select(p =>
            $"- {p.PlaceId} | {p.Name} | {p.Type} | ({p.Lat},{p.Lng}) | {p.Description}"
        );

        var signals = input.EmotionalSignals.Take(200).Select(s =>
            $"- {s.Emotion} intensity={s.Intensity} at ({s.Lat},{s.Lng})"
        );

        return $@"
ТИ СИ AI МОДУЛ ЗА 'ЕМОЦИОНАЛНА КАРТОГРАФИЯ' ЗА ГРАД {input.CityName}.
ЗАДАЧА: На база текстово настроение, подсказки за емоция/тип място, налични места и емоционални точки, върни:
- избрана емоция (enum име на английски, напр. Calm/Joy/...)
- confidence 0..1
- кратко резюме на български
- топ 15-25 препоръчани места (PlaceId от списъка), score 0..1 и reason на български.

НАСТРОЕНИЕ (текст от потребителя): {input.QueryText}
ПОДСКАЗКА ЕМОЦИЯ: {input.EmotionHint?.ToString() ?? "няма"}
ПОДСКАЗКА ТИП МЯСТО: {input.PlaceTypeHint?.ToString() ?? "няма"}

СПИСЪК МЕСТА (кандидати):
{string.Join("\n", places)}

ЕМОЦИОНАЛНИ ТОЧКИ (исторически сигнали):
{string.Join("\n", signals)}

ВАЖНО:
- PlaceId трябва да е точно от списъка.
- Не измисляй нови места.
- Изходът да е САМО JSON по зададената схема.
".Trim();
    }

    private static JsonSchemaWrapper BuildSchema()
    {
        // Минимална JSON schema, без сложни oneOf/anyOf за да е стабилно.  [oai_citation:5‡OpenAI Platform](https://platform.openai.com/docs/guides/structured-outputs?utm_source=chatgpt.com)
        return new JsonSchemaWrapper
        {
            Name = "emotional_map_result",
            Strict = true,
            Schema = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "emotion", "confidence", "summary", "recommendations" },
                properties = new
                {
                    emotion = new { type = "string" },
                    confidence = new { type = "number" },
                    summary = new { type = "string" },
                    recommendations = new
                    {
                        type = "array",
                        minItems = 10,
                        maxItems = 30,
                        items = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "placeId", "score", "reason" },
                            properties = new
                            {
                                placeId = new { type = "string" },
                                score = new { type = "number" },
                                reason = new { type = "string" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static EmotionType ParseEmotion(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return EmotionType.Inspiration;
        return Enum.TryParse<EmotionType>(s, ignoreCase: true, out var e) ? e : EmotionType.Inspiration;
    }

    // ====== Models for OpenAI Responses API ======

    private sealed class ResponsesRequest
    {
        public string Model { get; set; } = "";
        public object Input { get; set; } = "";
        public double Temperature { get; set; } = 0.2;

        public ResponseFormat ResponseFormat { get; set; } = new();
    }

    private sealed class ResponseFormat
    {
        public string Type { get; set; } = "json_schema";
        public JsonSchemaWrapper JsonSchema { get; set; } = new();
    }

    private sealed class JsonSchemaWrapper
    {
        public string Name { get; set; } = "schema";
        public bool Strict { get; set; } = true;
        public object Schema { get; set; } = new();
    }

    private sealed class ResponsesResponse
    {
        // Responses API връща output_text агрегирано (SDK/документацията показват outputText).
        // В REST е "output_text" в някои клиенти; тук го мапваме safe чрез JsonPropertyName.
        public string OutputText
        {
            get
            {
                // fallback: ако липсва, пробвай да извадиш от output[...].content[...].text
                if (!string.IsNullOrWhiteSpace(_outputText)) return _outputText;

                if (Output is null) return "";
                foreach (var o in Output)
                {
                    if (o?.Content is null) continue;
                    foreach (var c in o.Content)
                    {
                        if (!string.IsNullOrWhiteSpace(c?.Text)) return c.Text!;
                    }
                }
                return "";
            }
        }

        public string? _outputText { get; set; } // ако някой клиент го върне директно
        public List<OutputItem>? Output { get; set; }

        public sealed class OutputItem
        {
            public List<ContentItem>? Content { get; set; }
        }

        public sealed class ContentItem
        {
            public string? Type { get; set; }
            public string? Text { get; set; }
        }
    }

    // ====== Our structured JSON ======
    private sealed class AiJsonResult
    {
        public string Emotion { get; set; } = "Inspiration";
        public double Confidence { get; set; } = 0.5;
        public string Summary { get; set; } = "";
        public List<AiJsonRec> Recommendations { get; set; } = new();
    }

    private sealed class AiJsonRec
    {
        public Guid PlaceId
        {
            get
            {
                return Guid.TryParse(PlaceIdRaw, out var g) ? g : Guid.Empty;
            }
            set => PlaceIdRaw = value.ToString();
        }

        public string PlaceIdRaw { get; set; } = "";
        public double Score { get; set; } = 0.5;
        public string? Reason { get; set; }
        
    }
}