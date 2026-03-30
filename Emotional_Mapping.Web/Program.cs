using System.Globalization;
using Emotional_Mapping.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("api", (sp, c) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["ApiBaseUrl"] ?? "http://localhost:5052";
    c.BaseAddress = new Uri(baseUrl);
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
    // .AddCookie("External")
    // .AddGoogle("Google", options =>
    // {
    //     options.SignInScheme = "External";
    //     options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    //     options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    // });
    // .AddApple("Apple", options =>
    // {
    //     options.SignInScheme = "External";
    //     options.ClientId = builder.Configuration["Authentication:Apple:ClientId"] ?? "";
    //     options.KeyId = builder.Configuration["Authentication:Apple:KeyId"] ?? "";
    //     options.TeamId = builder.Configuration["Authentication:Apple:TeamId"] ?? "";
    //     // options.UsePrivateKey(keyId => builder.Configuration["Authentication:Apple:PrivateKey"] ?? "");
    // });

builder.Services.AddScoped<IContactEmailService, SmtpContactEmailService>();

builder.Services.AddAuthorization();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

var supportedCultures = new[] { new CultureInfo("bg-BG") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("bg-BG"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();