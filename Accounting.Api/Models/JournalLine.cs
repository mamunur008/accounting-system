namespace Accounting.Api.Models;

public class JournalLine
{
    public long Id { get; set; }
    public long EntryId { get; set; }
    public long AccountId { get; set; }
    public string? Memo { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    public JournalEntry? Entry { get; set; }
    public Account? Account { get; set; }
}