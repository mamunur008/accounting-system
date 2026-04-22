using Accounting.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Accounts
        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("accounts", "dbo");
            e.HasKey(x => x.Id);

            e.Property(x => x.Code).HasMaxLength(30).IsRequired();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();

            e.HasIndex(x => x.Code).IsUnique();

            e.Property(x => x.Type).HasConversion<byte>();

            e.HasMany<Account>()
                .WithOne()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // JournalEntry
        modelBuilder.Entity<JournalEntry>(e =>
        {
            e.ToTable("journal_entries", "dbo");
            e.HasKey(x => x.Id);

            e.Property(x => x.Number).HasMaxLength(30);
            e.Property(x => x.Reference).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(250);

            // IMPORTANT: convert enum -> byte but default must be ENUM (not byte)
            e.Property(x => x.Status)
                .HasConversion<byte>()
                .HasDefaultValue(JournalStatus.Draft);

            e.Property(x => x.PostedBy).HasMaxLength(100);
            e.Property(x => x.DeletedBy).HasMaxLength(100);

            e.HasMany(x => x.Lines)
                .WithOne(l => l.Entry)
                .HasForeignKey(l => l.EntryId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.Number);
        });

        // JournalLine
        modelBuilder.Entity<JournalLine>(e =>
        {
            e.ToTable("journal_lines", "dbo");
            e.HasKey(x => x.Id);

            e.Property(x => x.Debit).HasColumnType("decimal(18,2)");
            e.Property(x => x.Credit).HasColumnType("decimal(18,2)");
            e.Property(x => x.Memo).HasMaxLength(250);

            e.HasOne(l => l.Account)
                .WithMany()
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AccountingPeriod
        modelBuilder.Entity<AccountingPeriod>(e =>
        {
            e.ToTable("accounting_periods", "dbo");
            e.HasKey(x => x.Id);

            e.Property(x => x.ClosedBy).HasMaxLength(100);

            e.HasIndex(x => new { x.Year, x.Month }).IsUnique();
        });
    }
}