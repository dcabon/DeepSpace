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
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<ArticleSection> ArticleSections => Set<ArticleSection>();
    
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
    }
}
