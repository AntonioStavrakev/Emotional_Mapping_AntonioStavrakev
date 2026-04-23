using System;
using System.Globalization;
using AspNet.Security.OAuth.Apple;
using Emotional_Mapping.Web.Middleware;
using Emotional_Mapping.Web.Localization;
using Emotional_Mapping.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SiteText>();
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("api", (sp, c) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["ApiBaseUrl"] ?? "http://127.0.0.1:5052";
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
    })
    .AddCookie("External");

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle("Google", options =>
        {
            options.SignInScheme = "External";
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}



builder.Services.AddScoped<IContactEmailService, SmtpContactEmailService>();
builder.Services.AddScoped<IUserOnboardingService, UserOnboardingService>();

builder.Services.AddAuthorization();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

var supportedCultures = new[] { new CultureInfo("bg-BG"), new CultureInfo("en-US") };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("bg-BG"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};
localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());

app.UseRequestLocalization(localizationOptions);

app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Forward /api/* requests to the API backend
app.UseApiProxy();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Apple login is temporarily disabled until an active Apple Developer account is available.
// var appleClientId = builder.Configuration["Authentication:Apple:ClientId"];
// var appleKeyId = builder.Configuration["Authentication:Apple:KeyId"];
// var appleTeamId = builder.Configuration["Authentication:Apple:TeamId"];
// var applePrivateKeyPath = builder.Configuration["Authentication:Apple:PrivateKey"];
// if (!string.IsNullOrWhiteSpace(appleClientId) &&
//     !string.IsNullOrWhiteSpace(appleKeyId) &&
//     !string.IsNullOrWhiteSpace(appleTeamId) &&
//     !string.IsNullOrWhiteSpace(applePrivateKeyPath) &&
//     File.Exists(applePrivateKeyPath))
// {
//     builder.Services.AddAuthentication()
//         .AddApple("Apple", options =>
//         {
//             options.SignInScheme = "External";
//             options.ClientId = appleClientId;
//             options.KeyId = appleKeyId;
//             options.TeamId = appleTeamId;
//             options.UsePrivateKey(_ => builder.Environment.ContentRootFileProvider.GetFileInfo(applePrivateKeyPath));
//         });
// }