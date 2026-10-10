namespace DeepSpace.Core.Entities;

public class AdminAuditEvent
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string AdminLogin { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? RequestPath { get; set; }
    public string? IpAddress { get; set; }

    public ICollection<AdminAuditChange> Changes { get; set; } = new List<AdminAuditChange>();
}

public class AdminAuditChange
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public AdminAuditEvent Event { get; set; } = null!;
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public static class AdminAuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string LoginSucceeded = "LoginSucceeded";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
}
