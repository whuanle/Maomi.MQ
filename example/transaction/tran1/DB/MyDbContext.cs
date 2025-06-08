using Microsoft.EntityFrameworkCore;

namespace tran1.DB;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEneity>().ToTable("test");
        modelBuilder.Entity<TestEneity>().HasKey(e => e.Id);
        modelBuilder.Entity<TestEneity>().Property(e => e.Name).IsRequired();
        modelBuilder.Entity<TestEneity>().Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }

    public DbSet<TestEneity> Test { get; set; }
}

public class TestEneity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
