namespace Accounting.Api.Models;

public class AccountingPeriod
{
    public long Id { get; set; }

    public int Year { get; set; }
    public int Month { get; set; } // 1..12

    public bool IsClosed { get; set; }

    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
}