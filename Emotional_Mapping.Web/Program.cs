// var builder = WebApplication.CreateBuilder(args);
//
// // Add services to the container.
// builder.Services.AddControllersWithViews();
//
// var app = builder.Build();
//
// // Configure the HTTP request pipeline.
// if (!app.Environment.IsDevelopment())
// {
//     app.UseExceptionHandler("/Home/Error");
//     // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//     app.UseHsts();
// }
//
// app.UseHttpsRedirection();
// app.UseRouting();
//
// app.UseAuthorization();
//
// app.MapStaticAssets();
//
// app.MapControllerRoute(
//         name: "default",
//         pattern: "{controller=Home}/{action=Index}/{id?}")
//     .WithStaticAssets();
//
//
// app.Run();

using System.Globalization;
using Emotional_Mapping.Web.Services;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// ── MVC Views ──────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── HTTP Client към API проекта ────────────────────────
builder.Services.AddHttpClient("api", (sp, c) =>
{
    var config  = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["ApiBaseUrl"] ?? "http://localhost:5000";
    c.BaseAddress = new Uri(baseUrl);
});

// ── Cookie Authentication (за Web проекта) ─────────────
// Web проектът не управлява Identity директно —
// той само чете cookie-то, издадено от API проекта.
// Ако Web и API са на един хост, това е достатъчно.
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath    = "/Account/Login";
        options.LogoutPath   = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan   = TimeSpan.FromDays(7);
    });
builder.Services.AddScoped<IContactEmailService, SmtpContactEmailService>();

builder.Services.AddAuthorization();

// ── Session (по желание, за flash messages) ────────────
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ── Pipeline ───────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Локализация
var supportedCultures = new[] { new CultureInfo("bg-BG") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("bg-BG"),
    SupportedCultures     = supportedCultures,
    SupportedUICultures   = supportedCultures
});

app.UseStaticFiles();
app.UseRouting();

app.UseSession();          // преди Authentication
app.UseAuthentication();   // задължително за User.Identity
app.UseAuthorization();    // задължително за [Authorize]

app.MapControllerRoute(
    name:    "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();