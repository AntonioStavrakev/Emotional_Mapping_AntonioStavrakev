using Emotional_Mapping.Web.Models;
using Emotional_Mapping.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Web.Controllers;

public class ContactController : Controller
{
    private readonly IContactEmailService _emailService;

    public ContactController(IContactEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ContactViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _emailService.SendAsync(
                model.Name,
                model.Email,
                model.Subject ?? "Ново съобщение от сайта",
                model.Message);

            TempData["ContactSuccess"] = "Съобщението беше изпратено успешно.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ViewBag.Error = "Възникна проблем при изпращането. Провери email настройките.";
            return View(model);
        }
    }
}