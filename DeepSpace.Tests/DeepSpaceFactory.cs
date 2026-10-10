using System.Net;
using System.Text.RegularExpressions;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSpace.Tests;

/// <summary>
/// Uruchamia aplikację w trybie Production na tymczasowej bazie SQLite, żeby testy nie dotykały deepspace.db.
/// Limit logowań to 10 prób na 5 minut na fabrykę, więc każda klasa testów dostaje własną instancję.
/// </summary>
public class DeepSpaceFactory : WebApplicationFactory<Program>
{
    public const string AdminLogin = "test-admin";
    public const string AdminPassword = "Test-Password-123!";

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"deepspace-tests-{Guid.NewGuid():N}.db");

    protected virtual string ConfiguredLogin => AdminLogin;
    protected virtual string ConfiguredPassword => AdminPassword;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
        builder.UseSetting("Admin:Username", ConfiguredLogin);
        builder.UseSetting("Admin:Password", ConfiguredPassword);
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        // Ciasteczka mają flagę Secure i prefiks __Host-, więc klient musi rozmawiać po HTTPS.
        BaseAddress = new Uri("https://deepspace.test"),
        AllowAutoRedirect = false
    });

    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateHttpsClient();
        var response = await LoginAsync(client, AdminLogin, AdminPassword);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    public static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string? login, string? password)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/admin/login");
        return await client.PostAsync("/admin/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = login ?? string.Empty,
            ["password"] = password ?? string.Empty,
            ["__RequestVerificationToken"] = token
        }));
    }

    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, $"Brak tokenu antiforgery na {url}");
        return match.Groups[1].Value;
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
        {
            File.Delete(file);
        }
    }
}

public class UnconfiguredAdminFactory : DeepSpaceFactory
{
    protected override string ConfiguredLogin => string.Empty;
    protected override string ConfiguredPassword => string.Empty;
}
