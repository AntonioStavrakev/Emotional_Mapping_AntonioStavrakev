using Emotional_Mapping.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Web.Controllers;

public class MapController : Controller
{
    // GET /Map — обща карта (достъпна за всички)
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    // GET /Map/Generate — форма за генериране
    [AllowAnonymous]
    public IActionResult Generate()
    {
        return View();
    }

    // GET /Map/Result?mapId=...
    [AllowAnonymous]
    public IActionResult Result()
    {
        return View();
    }

    // GET /Map/MyMaps — само за регистрирани
    [Authorize]
    public IActionResult MyMaps()
    {
        return View();
    }

    // GET /Map/AddPlace — добавяне на емоционална точка
    [Authorize]
    public IActionResult AddPlace()
    {
        return View();
    }

    // GET /Map/SuggestPlace — предложение за ново място в каталога
    [Authorize]
    public IActionResult SuggestPlace()
    {
        return View();
    }
}
