using System.Net;
using DeepSpace.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Tests;

public class AdminAuditTests : IClassFixture<DeepSpaceFactory>
{
    private readonly DeepSpaceFactory _factory;

    public AdminAuditTests(DeepSpaceFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Category_lifecycle_is_recorded_with_who_what_and_where()
    {
        var client = await _factory.CreateAdminClientAsync();
        var token = await DeepSpaceFactory.GetAntiforgeryTokenAsync(client, "/admin/categories");

        var create = await client.PostAsync("/admin/categories?handler=Create",
            SecurityTests.Form(token, ("name", "Egzoplanety"), ("description", "Opis A")));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var categoryId = await _factory.WithDbAsync(db =>
            db.Categories.Where(c => c.Name == "Egzoplanety").Select(c => c.Id).SingleAsync());

        var edit = await client.PostAsync("/admin/categories?handler=Edit",
            SecurityTests.Form(token, ("id", categoryId.ToString()), ("name", "Egzoplanety 2"), ("description", "Opis A")));
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

        var delete = await client.PostAsync("/admin/categories?handler=Delete",
            SecurityTests.Form(token, ("id", categoryId.ToString())));
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

        var events = await EventsForAsync(nameof(Category), categoryId.ToString());
        Assert.Equal(
            new[] { AdminAuditActions.Created, AdminAuditActions.Updated, AdminAuditActions.Deleted },
            events.Select(e => e.Action));
        Assert.All(events, e => Assert.Equal(DeepSpaceFactory.AdminLogin, e.AdminLogin));

        var created = events[0];
        Assert.Equal("POST /admin/categories?handler=Create", created.RequestPath);
        Assert.Contains(created.Changes, c => c is { PropertyName: "Name", OldValue: null, NewValue: "Egzoplanety" });
        Assert.Contains(created.Changes, c => c is { PropertyName: "Slug", NewValue: "egzoplanety" });

        var updated = events[1];
        Assert.Contains(updated.Changes, c => c is { PropertyName: "Name", OldValue: "Egzoplanety", NewValue: "Egzoplanety 2" });
        Assert.DoesNotContain(updated.Changes, c => c.PropertyName == "Description");

        var deleted = events[2];
        Assert.Contains(deleted.Changes, c => c is { PropertyName: "Name", OldValue: "Egzoplanety 2", NewValue: null });
    }

    [Fact]
    public async Task Article_edit_records_changed_fields_and_new_tags()
    {
        var client = await _factory.CreateAdminClientAsync();
        var token = await DeepSpaceFactory.GetAntiforgeryTokenAsync(client, "/admin/articles");
        var categoryId = await _factory.WithDbAsync(db => db.Categories.Select(c => c.Id).FirstAsync());

        var create = await client.PostAsync("/admin/articles?handler=Create", SecurityTests.Form(token,
            ("title", "Audytowany artykul"), ("summary", "Streszczenie"), ("content", "<p>Wersja 1</p>"),
            ("categoryId", categoryId.ToString()), ("tags", "audyt-tag"), ("isPublished", "false")));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var articleId = await _factory.WithDbAsync(db =>
            db.Articles.Where(a => a.Title == "Audytowany artykul").Select(a => a.Id).SingleAsync());

        var edit = await client.PostAsync("/admin/articles?handler=Edit", SecurityTests.Form(token,
            ("id", articleId.ToString()), ("title", "Audytowany artykul"), ("summary", "Streszczenie"),
            ("content", "<p>Wersja 2</p>"), ("categoryId", categoryId.ToString()), ("tags", "audyt-tag"),
            ("isPublished", "true")));
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

        var events = await EventsForAsync(nameof(Article), articleId.ToString());
        Assert.Equal(new[] { AdminAuditActions.Created, AdminAuditActions.Updated }, events.Select(e => e.Action));
        var updated = events[1];
        Assert.Contains(updated.Changes, c => c is { PropertyName: "Content", OldValue: "<p>Wersja 1</p>", NewValue: "<p>Wersja 2</p>" });
        Assert.Contains(updated.Changes, c => c is { PropertyName: "IsPublished", OldValue: "False", NewValue: "True" });
        Assert.DoesNotContain(updated.Changes, c => c.PropertyName == "Title");

        var tagEvents = await _factory.WithDbAsync(db => db.AdminAuditEvents
            .Where(e => e.EntityType == nameof(Tag) && e.Action == AdminAuditActions.Created &&
                        e.Changes.Any(c => c.PropertyName == "Name" && c.NewValue == "audyt-tag"))
            .CountAsync());
        Assert.Equal(1, tagEvents);
    }

    [Fact]
    public async Task Logins_are_recorded_without_storing_passwords()
    {
        var client = _factory.CreateHttpsClient();
        await DeepSpaceFactory.LoginAsync(client, "intruz", "Sekretne-Haslo-Intruza");
        await DeepSpaceFactory.LoginAsync(client, DeepSpaceFactory.AdminLogin, DeepSpaceFactory.AdminPassword);

        var sessionEvents = await _factory.WithDbAsync(db => db.AdminAuditEvents
            .Include(e => e.Changes)
            .Where(e => e.EntityType == "Session")
            .ToListAsync());

        Assert.Contains(sessionEvents, e => e is { Action: AdminAuditActions.LoginFailed, AdminLogin: "intruz" });
        Assert.Contains(sessionEvents, e => e is { Action: AdminAuditActions.LoginSucceeded, AdminLogin: DeepSpaceFactory.AdminLogin });

        var storedText = await _factory.WithDbAsync(async db =>
            string.Join("\n", await db.AdminAuditEvents.Select(e => e.AdminLogin + e.RequestPath).ToListAsync()) +
            string.Join("\n", await db.AdminAuditChanges.Select(c => c.OldValue + c.NewValue).ToListAsync()));
        Assert.DoesNotContain("Sekretne-Haslo-Intruza", storedText);
        Assert.DoesNotContain(DeepSpaceFactory.AdminPassword, storedText);
    }

    [Fact]
    public async Task Audit_page_lists_recorded_changes_html_encoded()
    {
        var client = await _factory.CreateAdminClientAsync();
        var token = await DeepSpaceFactory.GetAntiforgeryTokenAsync(client, "/admin/tags");
        await client.PostAsync("/admin/tags?handler=Create", SecurityTests.Form(token, ("name", "<b>pulsar</b>")));

        var html = await client.GetStringAsync("/admin/audit");

        Assert.Contains("&lt;b&gt;pulsar&lt;/b&gt;", html);
        Assert.DoesNotContain("<b>pulsar</b>", html);
    }

    private Task<List<AdminAuditEvent>> EventsForAsync(string entityType, string entityId) =>
        _factory.WithDbAsync(db => db.AdminAuditEvents
            .Include(e => e.Changes)
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderBy(e => e.Id)
            .ToListAsync());
}

public class AuditScopeTests
{
    [Fact]
    public async Task Seeding_and_other_non_admin_writes_are_not_audited()
    {
        using var factory = new DeepSpaceFactory();
        factory.CreateHttpsClient();

        Assert.True(await factory.WithDbAsync(db => db.Categories.AnyAsync()));
        Assert.False(await factory.WithDbAsync(db => db.AdminAuditEvents.AnyAsync()));
    }
}
