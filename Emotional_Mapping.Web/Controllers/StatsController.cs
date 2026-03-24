using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Web.Controllers;

public class StatsController : Controller
{
    // GET /Stats
    public IActionResult Index()
    {
        return View();
    }
}