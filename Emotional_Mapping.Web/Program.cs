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
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("api", (sp, c) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["ApiBaseUrl"] ?? "http://localhost:5000";
    c.BaseAddress = new Uri(baseUrl);
});
var app = builder.Build();

var supportedCultures = new[] { new CultureInfo("bg-BG") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("bg-BG"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();