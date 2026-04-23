using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Emotional_Mapping.Application.AI;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emotional_Mapping.Infrastructure.AI;

public class OpenAiEmotionService : IAiEmotionService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly OpenAiOptions _opt;
    private readonly ILogger<OpenAiEmotionService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public OpenAiEmotionService(
        IHttpClientFactory httpFactory,
        IOptions<OpenAiOptions> opt,
        ILogger<OpenAiEmotionService> logger)
    {
        _httpFactory = httpFactory;
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisInput input, CancellationToken ct)
    {
        try
        {
            var apiKey = !string.IsNullOrWhiteSpace(_opt.ApiKey)
                ? _opt.ApiKey
                : Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
                return BuildFallbackResult(input);

            var http = _httpFactory.CreateClient("openai");

            var schema = BuildSchema(input.Places.Count > 0); // strict json_schema за стабилен парсинг
            var prompt = BuildPrompt(input);

            var body = new ResponsesRequest
            {
                Model = _opt.Model,
                Input = prompt,
                Temperature = 0.2,
                Text = new ResponseTextOptions
                {
                    Format = schema
                }
            };

            var resp = await http.PostAsJsonAsync("responses", body, JsonOpts, ct);
            resp.EnsureSuccessStatusCode();

            var raw = await resp.Content.ReadFromJsonAsync<ResponsesResponse>(JsonOpts, ct)
                      ?? throw new InvalidOperationException("OpenAI response is empty.");

            // Responses API връща текст/структуриран output; ние взимаме parsed JSON от output_text
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
                    .Select(r => new AiRecommendedPlace
                    {
                        PlaceId = r.PlaceId,
                        Name = string.IsNullOrWhiteSpace(r.PlaceName) ? null : r.PlaceName.Trim(),
                        Type = ParsePlaceType(r.PlaceType),
                        Lat = r.Lat,
                        Lng = r.Lng,
                        Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim(),
                        Score = Math.Clamp(r.Score, 0, 1),
                        Reason = r.Reason ?? (IsEnglish(input) ? "Suitable according to the AI analysis." : "Подходящо според AI анализа.")
                    })
                    .Where(r => r.PlaceId != Guid.Empty || (!string.IsNullOrWhiteSpace(r.Name) && r.Lat.HasValue && r.Lng.HasValue))
                    .ToList()
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI analysis failed for city {CityName}. Falling back to local AI mode.", input.CityName);
            return BuildFallbackResult(input);
        }
    }

    public async Task<string?> TranslateTextAsync(string? text, string targetLanguage, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalizedTargetLanguage = string.Equals(targetLanguage, "en", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "bg";

        try
        {
            var apiKey = !string.IsNullOrWhiteSpace(_opt.ApiKey)
                ? _opt.ApiKey
                : Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
                return text;

            var http = _httpFactory.CreateClient("openai");
            var prompt = BuildTranslationPrompt(text, normalizedTargetLanguage);

            var body = new ResponsesRequest
            {
                Model = _opt.Model,
                Input = prompt,
                Temperature = 0.1
            };

            var resp = await http.PostAsJsonAsync("responses", body, JsonOpts, ct);
            resp.EnsureSuccessStatusCode();

            var raw = await resp.Content.ReadFromJsonAsync<ResponsesResponse>(JsonOpts, ct)
                      ?? throw new InvalidOperationException("OpenAI response is empty.");

            return string.IsNullOrWhiteSpace(raw.OutputText)
                ? text
                : raw.OutputText.Trim();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Text translation failed for target language {TargetLanguage}. Returning original text.", normalizedTargetLanguage);
            return text;
        }
    }

    private static string BuildPrompt(AiAnalysisInput input)
    {
        return IsEnglish(input) ? BuildEnglishPrompt(input) : BuildBulgarianPrompt(input);
    }

    private static string BuildTranslationPrompt(string text, string targetLanguage)
    {
        var targetLabel = targetLanguage == "en" ? "English" : "Bulgarian";
        return $@"
Translate the following user-facing UI text into {targetLabel}.

Rules:
- Preserve the meaning and tone.
- Keep it concise and natural.
- Return only the translated text.
- Do not add quotes, labels, or explanations.

Text:
{text}
        ".Trim();
    }

    private static bool IsEnglish(AiAnalysisInput input)
    {
        return string.Equals(input.Language, "en", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildBulgarianPrompt(AiAnalysisInput input)
    {
        // Държим промпта на български, защото UI ти е на български.
        var districtHint = string.IsNullOrWhiteSpace(input.DistrictName)
            ? "няма зададен район"
            : input.DistrictName.Trim();
        var thematicPriority = BuildThematicPriorityInstruction(input);
        var minRecommendations = Math.Clamp(Math.Min(5, input.Places.Count), 1, 5);
        var maxRecommendations = Math.Clamp(Math.Min(20, input.Places.Count), 1, 20);

        if (input.Places.Count == 0)
        {
            var generatedMin = 3;
            var generatedMax = 5;
            var targetType = input.PlaceTypeHint?.ToString() ?? "автоматично избран според емоцията";
            var emotionHint = input.EmotionHint?.ToString() ?? "няма";
            return $@"
ТИ СИ AI МОДУЛ ЗА 'ЕМОЦИОНАЛНА КАРТОГРАФИЯ' ЗА ГРАД {input.CityName}.
ЗАДАЧА: Няма предварително seed-нати места за този град, затова генерирай между {generatedMin} и {generatedMax} реалистични градски локации според емоцията и текста на потребителя.

НАСТРОЕНИЕ (текст от потребителя): {input.QueryText}
ПОДСКАЗКА ЕМОЦИЯ: {emotionHint}
ПОДСКАЗКА ТИП МЯСТО: {targetType}
РАЙОН: {districtHint}
ЦЕНТЪР НА ГРАДА: ({input.CityCenterLat.ToString(CultureInfo.InvariantCulture)},{input.CityCenterLng.ToString(CultureInfo.InvariantCulture)})

{thematicPriority}

ВЪРНИ:
- emotion: enum име на английски
- confidence: число 0..1
- summary: кратко резюме на български
- recommendations: между {generatedMin} и {generatedMax} нови локации с:
  - placeId = "" (празен string, защото мястото още не съществува в базата)
  - placeName = кратко име на локация
  - placeType = валиден enum от PlaceType (Park, Garden, Cafe, Restaurant, CulturalSite, Museum, Gallery, Theater, Viewpoint, Landmark, HistoricSite, Sport, Market, University, Beach, Street, Nightlife, Other)
  - lat/lng = координати в или около {input.CityName}, близо до центъра на града или избрания район
  - description = едно изречение на български
  - score = 0..1
  - reason = защо тази локация пасва на емоцията, на български

ВАЖНО:
- Връщай само реални, конкретни и разпознаваеми места: парк, улица, площад, музей, университет, заведение, градина, плаж, гледка, landmark.
- Не връщай общи имена като ""център"", ""главна улица"", ""парк"" без собствено име.
- Ако е зададен район, координатите и името трябва да са в този район или непосредствено до него.
- Ако няма район, избирай локации вътре в урбанизираната част на {input.CityName}, не в произволни периферни координати.
- Координатите да са валидни decimal числа и да съответстват логично на името на мястото.
- Изходът да е САМО JSON по зададената схема.
            ".Trim();
        }

        // Важно: ако имаме seed/user места, подаваме “кандидат-места” + емоционални сигнали.
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
- между {minRecommendations} и {maxRecommendations} препоръчани места (PlaceId от списъка), score 0..1 и reason на български.

НАСТРОЕНИЕ (текст от потребителя): {input.QueryText}
ПОДСКАЗКА ЕМОЦИЯ: {input.EmotionHint?.ToString() ?? "няма"}
ПОДСКАЗКА ТИП МЯСТО: {input.PlaceTypeHint?.ToString() ?? "няма"}
РАЙОН: {districtHint}

{thematicPriority}

СПИСЪК МЕСТА (кандидати):
{string.Join("\n", places)}

ЕМОЦИОНАЛНИ ТОЧКИ (исторически сигнали):
{string.Join("\n", signals)}

ВАЖНО:
- Ако е зададен район, приоритизирай места в този район; ако няма такива в кандидат-списъка, избери най-подходящите най-близки места от целия град.
- PlaceId трябва да е точно от списъка.
- Не измисляй нови места.
- Предпочитай по-конкретни и разпознаваеми места пред общи описания.
- Изходът да е САМО JSON по зададената схема.
    ".Trim();
    }

    private static string BuildEnglishPrompt(AiAnalysisInput input)
    {
        var districtHint = string.IsNullOrWhiteSpace(input.DistrictName)
            ? "no district selected"
            : input.DistrictName.Trim();
        var thematicPriority = BuildThematicPriorityInstruction(input);
        var minRecommendations = Math.Clamp(Math.Min(5, input.Places.Count), 1, 5);
        var maxRecommendations = Math.Clamp(Math.Min(20, input.Places.Count), 1, 20);

        if (input.Places.Count == 0)
        {
            var generatedMin = 3;
            var generatedMax = 5;
            var targetType = input.PlaceTypeHint?.ToString() ?? "auto-selected for the emotion";
            var emotionHint = input.EmotionHint?.ToString() ?? "none";
            return $@"
YOU ARE AN AI MODULE FOR 'EMOTIONAL MAPPING' FOR THE CITY OF {input.CityName}.
TASK: There are no pre-seeded places for this city, so generate between {generatedMin} and {generatedMax} realistic urban locations based on the user's emotion and text.

MOOD (user text): {input.QueryText}
EMOTION HINT: {emotionHint}
PLACE TYPE HINT: {targetType}
DISTRICT: {districtHint}
CITY CENTER: ({input.CityCenterLat.ToString(CultureInfo.InvariantCulture)},{input.CityCenterLng.ToString(CultureInfo.InvariantCulture)})

{thematicPriority}

RETURN:
- emotion: English enum name
- confidence: number 0..1
- summary: short summary in English
- recommendations: between {generatedMin} and {generatedMax} new locations with:
  - placeId = "" (empty string because the place does not exist in the database yet)
  - placeName = short location name
  - placeType = valid PlaceType enum (Park, Garden, Cafe, Restaurant, CulturalSite, Museum, Gallery, Theater, Viewpoint, Landmark, HistoricSite, Sport, Market, University, Beach, Street, Nightlife, Other)
  - lat/lng = coordinates in or around {input.CityName}, near the city center or selected district
  - description = one sentence in English
  - score = 0..1
  - reason = why this location matches the emotion, in English

IMPORTANT:
- Return only real, concrete, recognizable places: park, street, square, museum, university, cafe, garden, beach, viewpoint, landmark.
- Do not return generic names like ""center"", ""main street"", ""park"" without a proper name.
- If a district is provided, the coordinates and name must be inside that district or directly next to it.
- If there is no district, choose locations inside the urban area of {input.CityName}, not random peripheral coordinates.
- Coordinates must be valid decimals and logically match the place name.
- Output must be ONLY JSON following the given schema.
            ".Trim();
        }

        var places = input.Places.Take(120).Select(p =>
            $"- {p.PlaceId} | {p.Name} | {p.Type} | ({p.Lat},{p.Lng}) | {p.Description}"
        );

        var signals = input.EmotionalSignals.Take(200).Select(s =>
            $"- {s.Emotion} intensity={s.Intensity} at ({s.Lat},{s.Lng})"
        );

        return $@"
YOU ARE AN AI MODULE FOR 'EMOTIONAL MAPPING' FOR THE CITY OF {input.CityName}.
TASK: Based on the user's mood text, emotion/place hints, available places, and emotional points, return:
- selected emotion (English enum name, for example Calm/Joy/...)
- confidence 0..1
- short summary in English
- between {minRecommendations} and {maxRecommendations} recommended places (PlaceId from the list), score 0..1, and reason in English

MOOD (user text): {input.QueryText}
EMOTION HINT: {input.EmotionHint?.ToString() ?? "none"}
PLACE TYPE HINT: {input.PlaceTypeHint?.ToString() ?? "none"}
DISTRICT: {districtHint}

{thematicPriority}

CANDIDATE PLACES:
{string.Join("\n", places)}

EMOTIONAL POINTS (historical signals):
{string.Join("\n", signals)}

IMPORTANT:
- If a district is selected, prioritize places in that district; if there are none in the candidate list, choose the closest suitable places from the whole city.
- PlaceId must be taken exactly from the list.
- Do not invent new places.
- Prefer specific, recognizable places over generic descriptions.
- Output must be ONLY JSON following the given schema.
    ".Trim();
    }

    private static JsonSchemaWrapper BuildSchema(bool hasCandidatePlaces)
    {
        // Минимална JSON schema, без сложни oneOf/anyOf за да е стабилно.
        var minItems = hasCandidatePlaces ? 1 : 3;
        var maxItems = hasCandidatePlaces ? 20 : 5;
        object recommendationItemSchema = hasCandidatePlaces
            ? new
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
            : new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "placeId", "placeName", "placeType", "lat", "lng", "description", "score", "reason" },
                properties = new
                {
                    placeId = new { type = "string" },
                    placeName = new { type = "string" },
                    placeType = new { type = "string" },
                    lat = new { type = "number" },
                    lng = new { type = "number" },
                    description = new { type = "string" },
                    score = new { type = "number" },
                    reason = new { type = "string" }
                }
            };

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
                        minItems,
                        maxItems,
                        items = recommendationItemSchema
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

    private static AiAnalysisResult BuildFallbackResult(AiAnalysisInput input)
    {
        var text = (input.QueryText ?? "").ToLowerInvariant();
        var isEnglish = IsEnglish(input);

        var emotion = DetectEmotion(input.EmotionHint, text);

        if (input.Places.Count == 0)
        {
            return new AiAnalysisResult
            {
                FinalEmotion = emotion,
                Confidence = 0.55,
                Summary = isEnglish
                    ? $"A fallback AI analysis was used and new locations around {input.CityName} were generated for the mood \"{emotion}\"."
                    : $"Използван е резервен AI анализ и са генерирани нови локации около {input.CityName} за настроение „{emotion}“.",
                AiModel = "fallback-rule-based",
                Recommendations = BuildGeneratedFallbackRecommendations(input, emotion)
            };
        }

        var recommendations = input.Places
            .Select(p => new AiRecommendedPlace
            {
                PlaceId = p.PlaceId,
                Score = ScorePlace(p.Type, p.Name, p.Description, input.PlaceTypeHint, emotion, text),
                Reason = isEnglish
                    ? $"Suitable for \"{emotion}\" according to the local AI fallback mode."
                    : $"Подходящо за „{emotion}“ според локалния AI (резервен режим)."
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => input.Places.FindIndex(p => p.PlaceId == x.PlaceId))
            .Take(Math.Min(20, input.Places.Count))
            .ToList();

        return new AiAnalysisResult
        {
            FinalEmotion = emotion,
            Confidence = 0.55,
            Summary = isEnglish
                ? $"A fallback AI analysis was used for the mood \"{emotion}\"."
                : $"Използван е резервен AI анализ за настроение „{emotion}“.",
            AiModel = "fallback-rule-based",
            Recommendations = recommendations
        };
    }

    private static List<AiRecommendedPlace> BuildGeneratedFallbackRecommendations(AiAnalysisInput input, EmotionType emotion)
    {
        var seedType = input.PlaceTypeHint ?? GetDefaultPlaceTypeForEmotion(emotion);
        var centerLat = input.CityCenterLat;
        var centerLng = input.CityCenterLng;
        var isEnglish = IsEnglish(input);
        var nightlifeIntent = ContainsNightlifeIntent((input.QueryText ?? string.Empty).ToLowerInvariant());

        if (nightlifeIntent || seedType is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar)
        {
            return new List<AiRecommendedPlace>
            {
                new()
                {
                    Name = isEnglish ? $"{input.CityName} central nightlife zone" : $"{input.CityName} централна nightlife зона",
                    Type = PlaceType.Nightlife,
                    Lat = centerLat,
                    Lng = centerLng,
                    Description = isEnglish
                        ? $"A lively central area in {input.CityName}, suitable for music, dancing, and meeting people."
                        : $"Оживена централна зона в {input.CityName}, подходяща за музика, танци и срещи с хора.",
                    Score = 0.82,
                    Reason = isEnglish
                        ? $"Fallback nightlife recommendation near the center of {input.CityName}."
                        : $"Fallback nightlife препоръка близо до центъра на {input.CityName}."
                },
                new()
                {
                    Name = isEnglish ? $"{input.CityName} evening social area" : $"{input.CityName} вечерна социална зона",
                    Type = PlaceType.Bar,
                    Lat = centerLat + 0.0035,
                    Lng = centerLng + 0.0025,
                    Description = isEnglish
                        ? $"An evening social spot around the center with a more energetic atmosphere."
                        : $"Вечерна социална локация около центъра с по-енергична атмосфера.",
                    Score = 0.76,
                    Reason = isEnglish
                        ? "Fallback option aligned with nightlife and social intent."
                        : "Fallback опция, съобразена с nightlife и социалния intent."
                },
                new()
                {
                    Name = isEnglish ? $"{input.CityName} music and dance area" : $"{input.CityName} зона за музика и танци",
                    Type = PlaceType.Club,
                    Lat = centerLat - 0.0032,
                    Lng = centerLng - 0.0028,
                    Description = isEnglish
                        ? $"A fallback urban area matching a club-style mood in {input.CityName}."
                        : $"Fallback градска зона, която пасва на клубно настроение в {input.CityName}.",
                    Score = 0.74,
                    Reason = isEnglish
                        ? "Fallback recommendation for dance, music, and party-style mood."
                        : "Fallback препоръка за танци, музика и парти настроение."
                }
            };
        }

        return new List<AiRecommendedPlace>
        {
            new()
            {
                Name = isEnglish ? $"{input.CityName} center" : $"{input.CityName} център",
                Type = seedType,
                Lat = centerLat,
                Lng = centerLng,
                Description = isEnglish
                    ? $"A central area in {input.CityName}, suitable for the mood \"{emotion}\"."
                    : $"Централна зона в {input.CityName}, подходяща за настроение „{emotion}“.",
                Score = 0.75,
                Reason = isEnglish
                    ? $"Shows the center of {input.CityName} as a fallback location for \"{emotion}\"."
                    : $"Показва центъра на {input.CityName} като fallback локация за „{emotion}“."
            },
            new()
            {
                Name = isEnglish ? $"{input.CityName} city walk" : $"{input.CityName} градска разходка",
                Type = seedType == PlaceType.Other ? PlaceType.Street : seedType,
                Lat = centerLat + 0.006,
                Lng = centerLng + 0.006,
                Description = isEnglish
                    ? $"An urban walk near the center of {input.CityName}."
                    : $"Градска разходка близо до центъра на {input.CityName}.",
                Score = 0.7,
                Reason = isEnglish
                    ? "Added in fallback mode so there is a visible AI location on the map."
                    : "Добавена е в резервен режим, за да има видима AI локация на картата."
            },
            new()
            {
                Name = isEnglish ? $"{input.CityName} viewpoint" : $"{input.CityName} панорамна точка",
                Type = PlaceType.Viewpoint,
                Lat = centerLat - 0.006,
                Lng = centerLng - 0.006,
                Description = isEnglish
                    ? $"A viewpoint around {input.CityName}."
                    : $"Панорамна точка около {input.CityName}.",
                Score = 0.68,
                Reason = isEnglish
                    ? "Fallback recommendation around the geographic center of the city."
                    : "Fallback препоръка около координатния център на града."
            }
        };
    }

    private static PlaceType ParsePlaceType(string? value)
    {
        return Enum.TryParse<PlaceType>(value, ignoreCase: true, out var placeType)
            ? placeType
            : PlaceType.Other;
    }

    private static PlaceType GetDefaultPlaceTypeForEmotion(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Calm or EmotionType.Relaxed or EmotionType.Safe => PlaceType.Park,
            EmotionType.Joy or EmotionType.Happy or EmotionType.Excited => PlaceType.Street,
            EmotionType.Social => PlaceType.Nightlife,
            EmotionType.Romantic => PlaceType.Viewpoint,
            EmotionType.Nostalgia or EmotionType.Inspiration => PlaceType.HistoricSite,
            EmotionType.Energetic => PlaceType.Sport,
            _ => PlaceType.Other
        };
    }

    private static EmotionType DetectEmotion(EmotionType? hint, string text)
    {
        return hint ??
               (ContainsNightlifeIntent(text) ? EmotionType.Excited :
                (text.Contains("спокой") || text.Contains("тих") ? EmotionType.Calm :
                text.Contains("calm") || text.Contains("quiet") ? EmotionType.Calm :
                text.Contains("релакс") || text.Contains("почив") ? EmotionType.Relaxed :
                text.Contains("relax") || text.Contains("rest") ? EmotionType.Relaxed :
                text.Contains("радост") || text.Contains("щаст") || text.Contains("усмив") ? EmotionType.Joy :
                text.Contains("joy") || text.Contains("happy") || text.Contains("smile") ? EmotionType.Joy :
                text.Contains("тъг") || text.Contains("меланх") ? EmotionType.Sad :
                text.Contains("sad") || text.Contains("melanch") ? EmotionType.Sad :
                text.Contains("ностал") || text.Contains("спомен") ? EmotionType.Nostalgia :
                text.Contains("nostalg") || text.Contains("memory") ? EmotionType.Nostalgia :
                text.Contains("роман") || text.Contains("любов") ? EmotionType.Romantic :
                text.Contains("roman") || text.Contains("love") ? EmotionType.Romantic :
                text.Contains("социал") || text.Contains("хора") || text.Contains("приятел") ? EmotionType.Social :
                text.Contains("social") || text.Contains("people") || text.Contains("friend") ? EmotionType.Social :
                text.Contains("енерг") || text.Contains("динами") ? EmotionType.Energetic :
                text.Contains("energet") || text.Contains("dynamic") || text.Contains("active") ? EmotionType.Energetic :
                text.Contains("напреж") || text.Contains("стрес") ? EmotionType.Tension :
                text.Contains("tension") || text.Contains("stress") ? EmotionType.Tension :
                text.Contains("тревог") ? EmotionType.Anxiety :
                text.Contains("anx") || text.Contains("worry") ? EmotionType.Anxiety :
                text.Contains("сигур") ? EmotionType.Safe :
                text.Contains("safe") || text.Contains("secure") ? EmotionType.Safe :
                text.Contains("опас") ? EmotionType.Unsafe :
                text.Contains("danger") || text.Contains("unsafe") ? EmotionType.Unsafe :
                text.Contains("еuforia") || text.Contains("вълнув") ? EmotionType.Excited :
                text.Contains("excited") || text.Contains("euphor") ? EmotionType.Excited :
                text.Contains("lonely") || text.Contains("alone") ? EmotionType.Lonely :
                EmotionType.Inspiration));
    }

    private static double ScorePlace(
        PlaceType type,
        string name,
        string? description,
        PlaceType? hint,
        EmotionType emotion,
        string queryText)
    {
        var score = 0.5;

        if (hint is not null && type == hint) score += 0.3;

        var text = $"{name} {description}".ToLowerInvariant();

        if (emotion is EmotionType.Calm or EmotionType.Relaxed or EmotionType.Safe &&
            type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest or PlaceType.Lake)
            score += 0.35;

        if (emotion is EmotionType.Joy or EmotionType.Happy or EmotionType.Excited &&
            type is PlaceType.Street or PlaceType.Park or PlaceType.CulturalSite or PlaceType.Cafe or PlaceType.Bar or PlaceType.Club or PlaceType.Nightlife)
            score += 0.28;

        if (emotion == EmotionType.Social &&
            type is PlaceType.Cafe or PlaceType.Bar or PlaceType.Restaurant or PlaceType.Nightlife or PlaceType.Street)
            score += 0.35;

        if (emotion is EmotionType.Inspiration or EmotionType.Nostalgia &&
            type is PlaceType.Museum or PlaceType.Gallery or PlaceType.CulturalSite or PlaceType.HistoricSite)
            score += 0.36;

        if (emotion == EmotionType.Romantic &&
            type is PlaceType.Park or PlaceType.RiverSide or PlaceType.Lake or PlaceType.Restaurant or PlaceType.Viewpoint)
            score += 0.34;

        if (emotion is EmotionType.Tension or EmotionType.Anxiety or EmotionType.Sad or EmotionType.Lonely &&
            type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest or PlaceType.Health)
            score += 0.25;

        if (emotion == EmotionType.Energetic &&
            type is PlaceType.Sport or PlaceType.Street or PlaceType.Nightlife or PlaceType.Bar or PlaceType.Club or PlaceType.Park)
            score += 0.33;

        if ((queryText.Contains("тих") || queryText.Contains("quiet")) && (text.Contains("парк") || text.Contains("park"))) score += 0.08;
        if ((queryText.Contains("култур") || queryText.Contains("cultur")) && (text.Contains("галерия") || text.Contains("театър") || text.Contains("музей") || text.Contains("gallery") || text.Contains("theater") || text.Contains("museum"))) score += 0.08;
        if ((queryText.Contains("разход") || queryText.Contains("walk")) && (text.Contains("парк") || text.Contains("градина") || text.Contains("улица") || text.Contains("park") || text.Contains("garden") || text.Contains("street"))) score += 0.08;
        if ((queryText.Contains("вечер") || queryText.Contains("evening") || queryText.Contains("night")) && (text.Contains("нощ") || text.Contains("бар") || text.Contains("кафе") || text.Contains("night") || text.Contains("bar") || text.Contains("cafe"))) score += 0.08;
        if (ContainsNightlifeIntent(queryText))
        {
            if (type is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar) score += 0.45;
            if (type is PlaceType.Cafe or PlaceType.Restaurant or PlaceType.Street) score += 0.14;
            if (type is PlaceType.Park or PlaceType.Garden or PlaceType.Forest or PlaceType.Lake or PlaceType.Viewpoint) score -= 0.22;
            if (ContainsAny(text, "дискот", "клуб", "club", "бар", "bar", "music", "музик", "dance", "танц", "party", "парти", "нощ", "night")) score += 0.24;
        }

        if (ContainsAny(queryText, "хора", "people", "приятел", "friend", "social", "социал") &&
            type is PlaceType.Nightlife or PlaceType.Club or PlaceType.Bar or PlaceType.Cafe)
            score += 0.12;

        return Math.Clamp(score, 0.1, 1.0);
    }

    private static string BuildThematicPriorityInstruction(AiAnalysisInput input)
    {
        var text = (input.QueryText ?? string.Empty).ToLowerInvariant();

        if (ContainsNightlifeIntent(text))
        {
            return IsEnglish(input)
                ? "IMPORTANT THEMATIC PRIORITY: The user explicitly wants nightlife, dancing, music, party energy, or a club/bar atmosphere. Prioritize nightlife, club, bar, lively cafe, event, and social indoor places. Do not prefer parks, calm gardens, or quiet nature spots unless there are no plausible nightlife-style options in the city."
                : "ВАЖЕН ТЕМАТИЧЕН ПРИОРИТЕТ: Потребителят изрично търси нощен живот, танци, музика, парти атмосфера или клуб/бар. Приоритизирай nightlife, клубове, барове, оживени кафенета, събития и социални вътрешни места. Не предпочитай паркове, тихи градини или спокойни природни локации, освен ако в града няма правдоподобни nightlife опции.";
        }

        if (ContainsAny(text, "разход", "walk", "nature", "природ", "quiet", "тих", "спокой"))
        {
            return IsEnglish(input)
                ? "IMPORTANT THEMATIC PRIORITY: The user is looking for a calm outdoor or walking-style experience. Prefer parks, gardens, viewpoints, and relaxed open-air places over nightlife."
                : "ВАЖЕН ТЕМАТИЧЕН ПРИОРИТЕТ: Потребителят търси спокойна разходка или outdoor изживяване. Предпочитай паркове, градини, гледки и релаксиращи открити места пред nightlife.";
        }

        return IsEnglish(input) ? "IMPORTANT THEMATIC PRIORITY: Match the place recommendations closely to the user's text, not only to the emotion label." : "ВАЖЕН ТЕМАТИЧЕН ПРИОРИТЕТ: Напасни препоръките максимално по текста на потребителя, а не само по етикета на емоцията.";
    }

    private static bool ContainsNightlifeIntent(string text)
    {
        return ContainsAny(text, "дискот", "клуб", "club", "party", "парти", "танц", "dance", "dj", "музик", "music", "нощ", "nightlife", "бар", "bar");
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }

    // ====== Models for OpenAI Responses API ======

    private sealed class ResponsesRequest
    {
        public string Model { get; set; } = "";
        public object Input { get; set; } = "";
        public double Temperature { get; set; } = 0.2;

        public ResponseTextOptions? Text { get; set; }
    }

    private sealed class ResponseTextOptions
    {
        public JsonSchemaWrapper Format { get; set; } = new();
    }

    private sealed class JsonSchemaWrapper
    {
        public string Type { get; set; } = "json_schema";
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

        [JsonPropertyName("output_text")]
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
        [JsonIgnore]
        public Guid PlaceId
        {
            get
            {
                return Guid.TryParse(PlaceIdRaw, out var g) ? g : Guid.Empty;
            }
        }

        [JsonPropertyName("placeId")]
        public string? PlaceIdRaw { get; set; } = "";
        public string? PlaceName { get; set; }
        public string? PlaceType { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public string? Description { get; set; }
        public double Score { get; set; } = 0.5;
        public string? Reason { get; set; }
    }
}
