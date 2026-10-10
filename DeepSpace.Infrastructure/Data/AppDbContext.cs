using System.Globalization;
using DeepSpace.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DeepSpace.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly IAuditActorProvider? _auditActorProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, IAuditActorProvider? auditActorProvider = null)
        : base(options)
    {
        _auditActorProvider = auditActorProvider;
    }
    
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<ArticleSection> ArticleSections => Set<ArticleSection>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<AdminAuditEvent> AdminAuditEvents => Set<AdminAuditEvent>();
    public DbSet<AdminAuditChange> AdminAuditChanges => Set<AdminAuditChange>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Article
        modelBuilder.Entity<Article>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.Slug).IsUnique();
            e.Property(a => a.Title).HasMaxLength(200).IsRequired();
            e.Property(a => a.Slug).HasMaxLength(200).IsRequired();
            e.Property(a => a.Summary).HasMaxLength(500);
        });
        
        // Category
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(100).IsRequired();
        });
        
        // Tag
        modelBuilder.Entity<Tag>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Name).HasMaxLength(50).IsRequired();
            e.Property(t => t.Slug).HasMaxLength(50).IsRequired();
        });
        
        // ArticleTag (many-to-many)
        modelBuilder.Entity<ArticleTag>(e =>
        {
            e.HasKey(at => new { at.ArticleId, at.TagId });
            
            e.HasOne(at => at.Article)
                .WithMany(a => a.ArticleTags)
                .HasForeignKey(at => at.ArticleId);
            
            e.HasOne(at => at.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(at => at.TagId);
        });
        
        // Quiz
        modelBuilder.Entity<Quiz>(e =>
        {
            e.HasKey(q => q.Id);
            e.HasIndex(q => q.Slug).IsUnique();
            e.Property(q => q.Title).HasMaxLength(200).IsRequired();
            e.Property(q => q.Slug).HasMaxLength(200).IsRequired();
        });
        
        // QuizQuestion
        modelBuilder.Entity<QuizQuestion>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.QuestionText).HasMaxLength(500).IsRequired();
            
            e.HasOne(q => q.Quiz)
                .WithMany(quiz => quiz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // QuizAnswer
        modelBuilder.Entity<QuizAnswer>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.AnswerText).HasMaxLength(300).IsRequired();
            
            e.HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // ArticleSection
        modelBuilder.Entity<ArticleSection>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Title).HasMaxLength(200).IsRequired();
            e.Property(s => s.Slug).HasMaxLength(200).IsRequired();
            
            e.HasOne(s => s.Article)
                .WithMany(a => a.Sections)
                .HasForeignKey(s => s.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SiteSettings
        modelBuilder.Entity<SiteSettings>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.SiteTitle).HasMaxLength(120).IsRequired();
            e.Property(s => s.SeoDescription).HasMaxLength(320).IsRequired();
            e.Property(s => s.SeoKeywords).HasMaxLength(320).IsRequired();
            e.Property(s => s.FaviconPath).HasMaxLength(260);
            e.Property(s => s.HomePageH1).HasMaxLength(120).IsRequired();
            e.Property(s => s.HomePageSubtitle).HasMaxLength(220).IsRequired();
        });

        // AdminAuditEvent
        modelBuilder.Entity<AdminAuditEvent>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.OccurredAt);
            e.HasIndex(a => new { a.EntityType, a.EntityId });
            e.Property(a => a.AdminLogin).HasMaxLength(100).IsRequired();
            e.Property(a => a.Action).HasMaxLength(30).IsRequired();
            e.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityId).HasMaxLength(100);
            e.Property(a => a.RequestPath).HasMaxLength(300);
            e.Property(a => a.IpAddress).HasMaxLength(64);
        });

        // AdminAuditChange
        modelBuilder.Entity<AdminAuditChange>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.PropertyName).HasMaxLength(100).IsRequired();

            e.HasOne(c => c.Event)
                .WithMany(a => a.Changes)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var actor = _auditActorProvider?.Current;
        var pending = actor == null ? [] : CapturePendingAudits();
        if (actor == null || pending.Count == 0)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        using var transaction = Database.CurrentTransaction == null ? Database.BeginTransaction() : null;
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        AdminAuditEvents.AddRange(pending.Select(p => p.ToEvent(actor)));
        base.SaveChanges(acceptAllChangesOnSuccess);
        transaction?.Commit();
        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var actor = _auditActorProvider?.Current;
        var pending = actor == null ? [] : CapturePendingAudits();
        if (actor == null || pending.Count == 0)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        await using var transaction = Database.CurrentTransaction == null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        AdminAuditEvents.AddRange(pending.Select(p => p.ToEvent(actor)));
        await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return result;
    }

    // Zbierane przed zapisem (stare wartości i klucze usuwanych encji), zamieniane na zdarzenia po zapisie,
    // gdy nowe encje mają już nadane Id.
    private List<PendingAudit> CapturePendingAudits()
    {
        var pending = new List<PendingAudit>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AdminAuditEvent or AdminAuditChange)
            {
                continue;
            }

            string action;
            switch (entry.State)
            {
                case EntityState.Added: action = AdminAuditActions.Created; break;
                case EntityState.Modified: action = AdminAuditActions.Updated; break;
                case EntityState.Deleted: action = AdminAuditActions.Deleted; break;
                default: continue;
            }

            var changes = new List<AdminAuditChange>();
            foreach (var property in entry.Properties)
            {
                var name = property.Metadata.Name;
                var oldValue = FormatAuditValue(property.OriginalValue);
                var newValue = FormatAuditValue(property.CurrentValue);

                if (entry.State == EntityState.Added)
                {
                    if (!property.Metadata.IsPrimaryKey() && newValue != null)
                    {
                        changes.Add(new AdminAuditChange { PropertyName = name, NewValue = newValue });
                    }
                }
                else if (entry.State == EntityState.Deleted)
                {
                    if (!property.Metadata.IsPrimaryKey() && oldValue != null)
                    {
                        changes.Add(new AdminAuditChange { PropertyName = name, OldValue = oldValue });
                    }
                }
                else if (property.IsModified && oldValue != newValue)
                {
                    changes.Add(new AdminAuditChange { PropertyName = name, OldValue = oldValue, NewValue = newValue });
                }
            }

            if (entry.State == EntityState.Modified && changes.Count == 0)
            {
                continue;
            }

            // Usunięta encja po zapisie jest odłączona, więc jej klucz zapamiętujemy od razu.
            var entityId = entry.State == EntityState.Added ? null : FormatEntityId(entry);
            pending.Add(new PendingAudit(entry, action, entityId, changes));
        }

        return pending;
    }

    private static string? FormatAuditValue(object? value) => value switch
    {
        null => null,
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)
    };

    private static string FormatEntityId(EntityEntry entry) => string.Join(",", entry.Properties
        .Where(p => p.Metadata.IsPrimaryKey())
        .Select(p => FormatAuditValue(p.CurrentValue)));

    private sealed class PendingAudit(EntityEntry entry, string action, string? entityId, List<AdminAuditChange> changes)
    {
        public AdminAuditEvent ToEvent(AuditActor actor) => new()
        {
            OccurredAt = DateTime.UtcNow,
            AdminLogin = actor.Login,
            Action = action,
            EntityType = entry.Metadata.ClrType.Name,
            EntityId = entityId ?? FormatEntityId(entry),
            RequestPath = actor.RequestPath,
            IpAddress = actor.IpAddress,
            Changes = changes
        };
    }
}
