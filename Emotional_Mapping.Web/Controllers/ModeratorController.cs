using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Web.Controllers;

[Authorize(Roles = "Admin,Moderator")]
public class ModeratorController : Controller
{
    // GET /Moderator
    public IActionResult Index()
    {
        return View();
    }
}