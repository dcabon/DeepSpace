namespace DeepSpace.Infrastructure.Data;

public record AuditActor(string Login, string? RequestPath, string? IpAddress);

public interface IAuditActorProvider
{
    /// <summary>Zalogowany admin wykonujący bieżące żądanie; null poza żądaniem admina (seeder, migracje).</summary>
    AuditActor? Current { get; }
}
