using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
});

builder.Services.AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", options =>
    {
        options.Cookie.Name = "__Host-DeepSpace.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.AddAuthenticationSchemes("AdminCookie");
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-DeepSpace.AntiForgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    DataSeeder.Seed(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-site";
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'; object-src 'none'; " +
        "img-src 'self' data: https:; style-src 'self' 'unsafe-inline' https://cdn.quilljs.com https://fonts.googleapis.com; " +
        "font-src 'self' data: https://fonts.gstatic.com; script-src 'self' 'unsafe-inline' https://cdn.quilljs.com; " +
        "connect-src 'self' https://api.wheretheiss.at https://api.open-notify.org https:; " +
        "upgrade-insecure-requests; block-all-mixed-content";

    await next();
});
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/robots.txt", (HttpContext context) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}".TrimEnd('/');
    var body = $"""
User-agent: *
Allow: /
Disallow: /admin/
Sitemap: {baseUrl}/sitemap.xml
""";

    return Results.Text(body, "text/plain; charset=utf-8", Encoding.UTF8);
});

app.MapGet("/sitemap.xml", async (HttpContext context, AppDbContext dbContext) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}".TrimEnd('/');
    var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
    var urlset = new XElement(ns + "urlset");

    static string ToDate(DateTime value) => value.ToUniversalTime().ToString("yyyy-MM-dd");

    void AddUrl(string path, DateTime? lastModifiedUtc, string priority, string changeFrequency)
    {
        var url = new XElement(ns + "url",
            new XElement(ns + "loc", $"{baseUrl}{path}"),
            new XElement(ns + "priority", priority),
            new XElement(ns + "changefreq", changeFrequency));

        if (lastModifiedUtc.HasValue)
        {
            url.Add(new XElement(ns + "lastmod", ToDate(lastModifiedUtc.Value)));
        }

        urlset.Add(url);
    }

    AddUrl("/", DateTime.UtcNow, "1.0", "daily");
    AddUrl("/quizy", DateTime.UtcNow, "0.8", "daily");
    AddUrl("/szukaj", DateTime.UtcNow, "0.5", "weekly");

    var articles = await dbContext.Articles
        .AsNoTracking()
        .Where(a => a.IsPublished && a.IsActive)
        .Select(a => new { a.Slug, LastModified = a.UpdatedAt ?? a.PublishedAt })
        .ToListAsync();
    foreach (var article in articles)
    {
        AddUrl($"/artykul/{article.Slug}", article.LastModified, "0.9", "weekly");
    }

    var categories = await dbContext.Categories
        .AsNoTracking()
        .Select(c => c.Slug)
        .ToListAsync();
    foreach (var categorySlug in categories)
    {
        AddUrl($"/kategoria/{categorySlug}", DateTime.UtcNow, "0.7", "weekly");
    }

    var tags = await dbContext.Tags
        .AsNoTracking()
        .Select(t => t.Slug)
        .ToListAsync();
    foreach (var tagSlug in tags)
    {
        AddUrl($"/tag/{tagSlug}", DateTime.UtcNow, "0.6", "weekly");
    }

    var quizzes = await dbContext.Quizzes
        .AsNoTracking()
        .Where(q => q.IsPublished && q.IsActive)
        .Select(q => new { q.Slug, q.CreatedAt })
        .ToListAsync();
    foreach (var quiz in quizzes)
    {
        AddUrl($"/quiz/{quiz.Slug}", quiz.CreatedAt, "0.8", "weekly");
    }

    var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);
    return Results.Text(document.ToString(), "application/xml; charset=utf-8", Encoding.UTF8);
});

app.MapRazorPages();

app.Run();
