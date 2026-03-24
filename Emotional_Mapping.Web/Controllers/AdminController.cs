using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    // GET /Admin
    public IActionResult Index()
    {
        return View();
    }
}