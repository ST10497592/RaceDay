using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

// =======================================================
// START: APPLICATION SETUP
// Purpose: Configures the RaceDay MVC web application.
// This application has NO database access. Every piece of data
// comes from the RaceDay API through RaceDayApiClient.
// =======================================================

var builder = WebApplication.CreateBuilder(args);

// START: MVC AND ANTI-FORGERY
builder.Services.AddControllersWithViews(options =>
{
    // Every POST must carry an anti-forgery token.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Only attributes written on the view models decide what is required.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
// END: MVC AND ANTI-FORGERY

// START: SESSION
// The JWT and the basic user details are kept in the server-side session.
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddScoped<UserSession>();
// END: SESSION

// START: API CLIENT
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7239/";

if (!apiBaseUrl.EndsWith("/"))
    apiBaseUrl += "/";

builder.Services.AddTransient<BearerTokenHandler>();

builder.Services
    .AddHttpClient<RaceDayApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();
// END: API CLIENT

var app = builder.Build();

// START: MIDDLEWARE PIPELINE
// Friendly error pages are used in every environment.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

if (builder.Configuration.GetValue("UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// END: MIDDLEWARE PIPELINE

app.Run();

// =======================================================
// END: APPLICATION SETUP
// =======================================================
