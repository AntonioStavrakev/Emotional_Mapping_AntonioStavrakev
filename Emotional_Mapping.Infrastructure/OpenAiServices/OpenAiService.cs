using Emotional_Mapping.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace Emotional_Mapping.Infrastructure.OpenAiServices;

public class OpenAiService
{
    private readonly string _apiKey;

    public OpenAiService(IOptions<OpenAiOptions> options)
    {
        _apiKey = options.Value.ApiKey;
    }

    public void Test()
    {
        Console.WriteLine(_apiKey);
    }
}