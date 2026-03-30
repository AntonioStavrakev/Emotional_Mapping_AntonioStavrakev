using Emotional_Mapping.Infrastructure.OpenAiServices;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Api.Controllers;

public class TestController : ControllerBase
{
    private readonly OpenAiService _service;

    public TestController(OpenAiService service)
    {
        _service = service;
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        _service.Test();
        return Ok();
    }
}