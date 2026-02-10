using Microsoft.EntityFrameworkCore;
using AppSec_Assignment_2.Models;

namespace AppSec_Assignment_2.Data;

/// <summary>
/// Application database context using Entity Framework Core
/// All database access uses LINQ to prevent SQL injection
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
 : base(options)
    {
    }

    public DbSet<Member> Members { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Member configuration
 modelBuilder.Entity<Member>(entity =>
        {
 entity.HasKey(e => e.Id);
  entity.HasIndex(e => e.Email).IsUnique();
entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
       entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Gender).IsRequired().HasMaxLength(10);
    entity.Property(e => e.Nric).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PasswordHash).IsRequired();
     });

   // AuditLog configuration
 modelBuilder.Entity<AuditLog>(entity =>
        {
 entity.HasKey(e => e.Id);
   entity.HasOne(e => e.Member)
          .WithMany()
  .HasForeignKey(e => e.MemberId)
        .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
