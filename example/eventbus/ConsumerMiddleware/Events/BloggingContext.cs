using Microsoft.EntityFrameworkCore;
using Polly;
using System.ComponentModel.DataAnnotations;

namespace ConsumerMiddleware.Events;

public class BloggingContext : DbContext
{
    public DbSet<Post> Posts { get; set; }

    public string DbPath { get; }

    public BloggingContext()
    {
        DbPath = "blogging.db";
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {   
        options.UseSqlite($"Data Source={DbPath}");
    }
}

public class Post
{
    [Key]
    public int PostId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
}