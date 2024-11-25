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
    public DbSet<HospitalInspection> HospitalInspections { get; set; }
    public DbSet<InspectionRecord> InspectionRecords { get; set; }
}

public class User
{
    public int Id { get; set; }
    [MaxLength(255)] public required string Username { get; set; }
    [MaxLength(255)] public required string Password { get; set; }
    [JsonIgnore] public Role Role { get; set; } = null!;
    public int RoleId { get; set; }
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

    public int Status { get; set; } = 1;

    [MaxLength(1000)] public string? SummarizeProblem { get; set; }

    [MaxLength(1000)] public string? SummarizeHighlight { get; set; }

    [MaxLength(1000)] public string? SummarizeNeedImprove { get; set; }

    [MaxLength(1000)] public string? Note { get; set; }

    public int SummarizePersonId { get; set; }
    public User SummarizePerson { get; set; } = null!;
    public ICollection<HospitalInspection> HospitalInspections { get; set; } = new List<HospitalInspection>();

    public bool DeleteFlag { get; set; }
}

public class HospitalInspection
{
    public int Id { get; set; }
    public int HospitalId { get; set; }
    public Hospital Hospital { get; set; } = null!;
    public int BatchId { get; set; }
    public Batch Batch { get; set; } = null!;
    public ICollection<InspectionRecord> InspectionRecords { get; set; } = new List<InspectionRecord>();
    public bool DeleteFlag { get; set; }
}

public class InspectionRecord
{
    public int Id { get; set; }
    public int HospitalInspectionId { get; set; }
    public HospitalInspection HospitalInspection { get; set; }
    public int ItemId { get; set; }
    public Item Item { get; set; }
    public int Score { get; set; }
}

public class Hospital
{
    public int Id { get; set; }
    [MaxLength(255)] [Required] public string Name { get; set; } = null!;
    [MaxLength(255)] [Required] public string? Address { get; set; }
    public ICollection<HospitalInspection> HospitalInspections { get; set; } = new List<HospitalInspection>();
    public bool DeleteFlag { get; set; }
}

public class Category
{
    public int Id { get; set; }
    [MaxLength(255)] [Required] public string Name { get; set; } = null!;
    [MaxLength(1000)] public string? Description { get; set; }
    public ICollection<Region> Regions { get; set; } = new List<Region>();
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
    public ScoreLevel? ScoreLevel { get; set; }
    public int? ScoreLevelId;
    public bool DeleteFlag { get; set; }
}