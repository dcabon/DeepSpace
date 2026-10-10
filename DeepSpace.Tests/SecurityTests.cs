using System.Net;
using DeepSpace.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Tests;

public class SecurityTests : IClassFixture<DeepSpaceFactory>
{
    private readonly DeepSpaceFactory _factory;

    public SecurityTests(DeepSpaceFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/articles")]
    [InlineData("/admin/articles?handler=Article&id=1")]
    [InlineData("/admin/articles/create")]
    [InlineData("/admin/articles/edit/1")]
    [InlineData("/admin/articles/delete/1")]
    [InlineData("/admin/categories")]
    [InlineData("/admin/categories?handler=Edit&id=1")]
    [InlineData("/admin/tags")]
    [InlineData("/admin/tags?handler=Edit&id=1")]
    [InlineData("/Admin/Quizzes")]
    [InlineData("/admin/quizzes/edit/1")]
    [InlineData("/admin/settings")]
    [InlineData("/admin/audit")]
    public async Task Admin_pages_redirect_anonymous_users_to_login(string url)
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/admin/login", response.Headers.Location!.PathAndQuery, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Anonymous_post_with_valid_antiforgery_token_cannot_change_data()
    {
        var client = _factory.CreateHttpsClient();
        var token = await DeepSpaceFactory.GetAntiforgeryTokenAsync(client, "/admin/login");

        var response = await client.PostAsync("/admin/categories?handler=Create", Form(token, ("name", "Anon Kategoria")));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(await _factory.WithDbAsync(db => db.Categories.AnyAsync(c => c.Name == "Anon Kategoria")));
    }

    [Fact]
    public async Task Admin_post_without_antiforgery_token_is_rejected()
    {
        var client = await _factory.CreateAdminClientAsync();

        var response = await client.PostAsync("/admin/categories?handler=Create", Form(null, ("name", "CSRF Kategoria")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await _factory.WithDbAsync(db => db.Categories.AnyAsync(c => c.Name == "CSRF Kategoria")));
    }

    [Theory]
    [InlineData(DeepSpaceFactory.AdminLogin, "zle-haslo")]
    [InlineData("ktos-inny", DeepSpaceFactory.AdminPassword)]
    [InlineData("", "")]
    [InlineData("' OR '1'='1", "' OR '1'='1")]
    public async Task Login_with_wrong_credentials_does_not_sign_in(string login, string password)
    {
        var client = _factory.CreateHttpsClient();

        var response = await DeepSpaceFactory.LoginAsync(client, login, password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(response.Headers, h => h.Key == "Set-Cookie" && h.Value.Any(v => v.Contains("DeepSpace.Admin")));
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/admin")).StatusCode);
    }

    [Fact]
    public async Task Login_cookie_is_host_prefixed_secure_httponly_and_samesite_strict()
    {
        var client = _factory.CreateHttpsClient();

        var response = await DeepSpaceFactory.LoginAsync(client, DeepSpaceFactory.AdminLogin, DeepSpaceFactory.AdminPassword);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), v => v.StartsWith("__Host-DeepSpace.Admin="));
        Assert.Contains("; secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; path=/", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_requires_post_so_a_link_cannot_sign_the_admin_out()
    {
        var client = await _factory.CreateAdminClientAsync();

        await client.GetAsync("/admin/logout");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/admin")).StatusCode);

        var token = await DeepSpaceFactory.GetAntiforgeryTokenAsync(client, "/admin");
        var response = await client.PostAsync("/admin/logout", Form(token));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/admin")).StatusCode);
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("strict-origin-when-cross-origin", Header(response, "Referrer-Policy"));
        Assert.Contains("max-age=31536000", Header(response, "Strict-Transport-Security"));
        var csp = Header(response, "Content-Security-Policy");
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("form-action 'self'", csp);
        Assert.Contains("object-src 'none'", csp);
    }

    [Fact]
    public async Task Search_encodes_reflected_query()
    {
        var client = _factory.CreateHttpsClient();
        const string payload = "<script>alert(1)</script>";

        var html = await client.GetStringAsync("/szukaj?q=" + Uri.EscapeDataString(payload));

        Assert.DoesNotContain(payload, html);
    }

    [Fact]
    public async Task Search_treats_sql_metacharacters_as_text()
    {
        var client = _factory.CreateHttpsClient();
        var articleCount = await _factory.WithDbAsync(db => db.Articles.CountAsync());

        var response = await client.GetAsync("/szukaj?q=" + Uri.EscapeDataString("'; DROP TABLE Articles; --"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(articleCount, await _factory.WithDbAsync(db => db.Articles.CountAsync()));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Unpublished_or_removed_articles_are_not_public(bool isPublished, bool isActive)
    {
        var slug = $"ukryty-{isPublished}-{isActive}".ToLowerInvariant();
        await _factory.WithDbAsync(async db =>
        {
            db.Articles.Add(new Article
            {
                Title = "Ukryty",
                Slug = slug,
                Content = "<p>tajne</p>",
                CategoryId = await db.Categories.Select(c => c.Id).FirstAsync(),
                PublishedAt = DateTime.UtcNow,
                IsPublished = isPublished,
                IsActive = isActive
            });
            return await db.SaveChangesAsync();
        });
        var client = _factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/artykul/{slug}")).StatusCode);
        Assert.DoesNotContain(slug, await client.GetStringAsync("/sitemap.xml"));
        Assert.DoesNotContain(slug, await client.GetStringAsync("/szukaj?q=Ukryty"));
    }

    [Fact]
    public async Task Robots_txt_hides_admin_from_crawlers()
    {
        var client = _factory.CreateHttpsClient();

        Assert.Contains("Disallow: /admin/", await client.GetStringAsync("/robots.txt"));
    }

    [Fact]
    public async Task Admin_bar_shows_title_and_login_only_when_signed_in()
    {
        var anonymous = _factory.CreateHttpsClient();
        Assert.DoesNotContain("class=\"admin-bar\"", await anonymous.GetStringAsync("/"));

        var admin = await _factory.CreateAdminClientAsync();
        foreach (var url in new[] { "/admin", "/" })
        {
            var html = await admin.GetStringAsync(url);
            var bar = html[html.IndexOf("class=\"admin-bar\"", StringComparison.Ordinal)..html.IndexOf("<nav", StringComparison.Ordinal)];
            Assert.Contains("Panel Administracyjny", bar);
            Assert.Contains(DeepSpaceFactory.AdminLogin, bar);
        }
    }

    internal static FormUrlEncodedContent Form(string? token, params (string Key, string Value)[] fields)
    {
        var values = fields.ToDictionary(f => f.Key, f => f.Value);
        if (token != null)
        {
            values["__RequestVerificationToken"] = token;
        }
        return new FormUrlEncodedContent(values);
    }

    private static string Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : string.Empty;
}

public class LoginHardeningTests
{
    [Fact]
    public async Task Missing_admin_configuration_does_not_allow_empty_login()
    {
        using var factory = new UnconfiguredAdminFactory();
        var client = factory.CreateHttpsClient();

        var response = await DeepSpaceFactory.LoginAsync(client, "", "");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/admin")).StatusCode);
    }

    [Fact]
    public async Task Repeated_login_attempts_are_rate_limited()
    {
        using var factory = new DeepSpaceFactory();
        var client = factory.CreateHttpsClient();
        var token = await DeepSpaceFactory.GetAntiforgeryTokenAsync(client, "/admin/login");
        Task<HttpResponseMessage> Attempt() => client.PostAsync("/admin/login",
            SecurityTests.Form(token, ("username", DeepSpaceFactory.AdminLogin), ("password", "zgaduje")));

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await Attempt()).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Attempt()).StatusCode);

        // Po przekroczeniu limitu nawet poprawne hasło nie przechodzi do końca okna.
        var correct = await client.PostAsync("/admin/login", SecurityTests.Form(token,
            ("username", DeepSpaceFactory.AdminLogin), ("password", DeepSpaceFactory.AdminPassword)));
        Assert.Equal(HttpStatusCode.TooManyRequests, correct.StatusCode);
    }
}
