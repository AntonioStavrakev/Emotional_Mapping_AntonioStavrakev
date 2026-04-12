using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Emotional_Mapping.Web.Controllers;

public class LanguageController : Controller
{
    [HttpGet]
    public IActionResult Set(string culture, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(culture))
            culture = "bg-BG";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}
