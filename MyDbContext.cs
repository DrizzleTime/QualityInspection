using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace QualityInspection;

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Batch> Batches { get; set; }
    public DbSet<Hospital> Hospitals { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ScoreLevel> ScoreLevels { get; set; }
    public DbSet<Region> Regions { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Score> Scores { get; set; }
    public DbSet<BatchCategory> BatchCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置多对多关系
        modelBuilder.Entity<BatchCategory>()
            .HasKey(bc => new { bc.BatchId, bc.CategoryId }); // 复合主键

        modelBuilder.Entity<BatchCategory>()
            .HasOne(bc => bc.Batch)
            .WithMany(b => b.BatchCategories)
            .HasForeignKey(bc => bc.BatchId);

        modelBuilder.Entity<BatchCategory>()
            .HasOne(bc => bc.Category)
            .WithMany(c => c.BatchCategories)
            .HasForeignKey(bc => bc.CategoryId);
    }
}

public class User
{
    public int Id { get; set; }
    [MaxLength(255)] [Required] public string Username { get; set; } = null!;
    [MaxLength(255)] [Required] public string Password { get; set; } = null!;
    [JsonIgnore] public Role Role { get; set; } = null!;
    public int RoleId { get; set; }
    [MaxLength(100)] public string? Email { get; set; }
    [MaxLength(20)] public string? Telephone { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}

public class Role
{
    public int Id { get; set; }
    [MaxLength(255)] public string Name { get; set; } = null!;
    [JsonIgnore] public ICollection<User> Users { get; set; } = new List<User>();
}

public class Batch
{
    [MaxLength(32)] public int Id { get; set; }

    [MaxLength(100)] [Required] public string Name { get; set; } = null!;

    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    public DateTime? EndTime { get; set; }

    public int Status { get; set; }

    [MaxLength(1000)] public string? SummarizeProblem { get; set; }

    [MaxLength(1000)] public string? SummarizeHighlight { get; set; }

    [MaxLength(1000)] public string? SummarizeNeedImprove { get; set; }

    [MaxLength(1000)] public string? Note { get; set; }

    public int? SummarizePersonId { get; set; }
    public User? SummarizePerson { get; set; }
    public int? InspectorId { get; set; }
    public User? Inspector { get; set; }
    public int HospitalId { get; set; }
    public Hospital Hospital { get; set; } = null!;
    public ICollection<BatchCategory> BatchCategories { get; set; } = new List<BatchCategory>();
    public bool DeleteFlag { get; set; }
}

public class BatchCategory
{
    public int BatchId { get; set; }
    public Batch Batch { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

public class Hospital
{
    public int Id { get; set; }
    [MaxLength(255)] [Required] public string Name { get; set; } = null!;
    [MaxLength(255)] [Required] public string? Address { get; set; }
    [JsonIgnore] public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public bool DeleteFlag { get; set; }
}

public class Category
{
    public int Id { get; set; }
    [MaxLength(255)] [Required] public string Name { get; set; } = null!;
    [MaxLength(1000)] public string? Description { get; set; }
    public ICollection<Region> Regions { get; set; } = new List<Region>();
    public ICollection<BatchCategory> BatchCategories { get; set; } = new List<BatchCategory>();
    public bool DeleteFlag { get; set; }
}

public class ScoreLevel
{
    public int Id { get; set; }
    [MaxLength(255)] [Required] public string Name { get; set; } = null!;
    public int Score { get; set; }
    public int UpperBound { get; set; }
    public int LowerBound { get; set; }
    public bool DeleteFlag { get; set; }
    public ICollection<Item> Items { get; set; } = new List<Item>();
}

public class Region
{
    public int Id { get; set; }
    [MaxLength(255)] [Required] public string Name { get; set; } = null!;
    [MaxLength(1000)] public string? Description { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public bool DeleteFlag { get; set; }
}

public class Item
{
    public int Id { get; set; }
    [MaxLength(255)] [Required] public string Name { get; set; } = null!;
    [MaxLength(1000)] public string? Description { get; set; }
    public int Score { get; set; }
    public int RegionId { get; set; }
    public Region Region { get; set; } = null!;
    public ICollection<ScoreLevel> ScoreLevels { get; set; } = new List<ScoreLevel>();

    public bool DeleteFlag { get; set; }
}

public class Score
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public Batch Batch { get; set; } = null!;
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int ScoreValue { get; set; }
    [MaxLength(500)] public string? Comment { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public bool DeleteFlag { get; set; }
}