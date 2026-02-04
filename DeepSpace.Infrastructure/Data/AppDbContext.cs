using DeepSpace.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    
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
    }
}
