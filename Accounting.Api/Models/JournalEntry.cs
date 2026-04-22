namespace Accounting.Api.Models;

public enum JournalStatus : byte
{
    Draft = 0,
    Posted = 1,
    Deleted = 2
}

public class JournalEntry
{
    public long Id { get; set; }

    public string? Number { get; set; }         // JV-YYYY-####
    public DateOnly EntryDate { get; set; }

    public string? Reference { get; set; }
    public string? Description { get; set; }

    public JournalStatus Status { get; set; } = JournalStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public List<JournalLine> Lines { get; set; } = new();
}