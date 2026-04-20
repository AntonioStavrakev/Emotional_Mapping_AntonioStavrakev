using System;
using System.Threading.Tasks;
using Emotional_Mapping.Web.Models;
using Emotional_Mapping.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Emotional_Mapping.Web.Controllers;

public class ContactController : Controller
{
    private readonly IContactEmailService _emailService;
    private readonly ILogger<ContactController> _logger;

    public ContactController(IContactEmailService emailService, ILogger<ContactController> logger)
    {
        _emailService = emailService;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contact form email delivery failed for {Email}.", model.Email);
            ViewBag.Error = "Възникна проблем при изпращането. Провери email настройките.";
            return View(model);
        }
    }
}
