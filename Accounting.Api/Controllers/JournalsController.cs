using Accounting.Api.Infrastructure;
using Accounting.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JournalsController : ControllerBase
{
    private readonly AppDbContext _db;
    public JournalsController(AppDbContext db) => _db = db;

    public record JournalLineRequest(long AccountId, decimal Debit, decimal Credit, string? Memo);
    public record CreateJournalRequest(DateOnly EntryDate, string? Reference, string? Description, List<JournalLineRequest> Lines);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJournalRequest req)
    {
        if (req.Lines is null || req.Lines.Count < 2)
            return BadRequest("Journal must have at least 2 lines.");

        decimal totalDebit = 0, totalCredit = 0;

        foreach (var l in req.Lines)
        {
            if (l.AccountId <= 0) return BadRequest("Each line must have an AccountId.");
            if (l.Debit < 0 || l.Credit < 0) return BadRequest("Debit/Credit cannot be negative.");
            if (l.Debit == 0 && l.Credit == 0) return BadRequest("Each line must have either Debit or Credit.");
            if (l.Debit > 0 && l.Credit > 0) return BadRequest("A line cannot have both Debit and Credit.");

            totalDebit += l.Debit;
            totalCredit += l.Credit;
        }

        if (totalDebit != totalCredit)
            return BadRequest($"Debit ({totalDebit}) must equal Credit ({totalCredit}).");

        // Validate accounts exist and are postable + active
        var accountIds = req.Lines.Select(x => x.AccountId).Distinct().ToList();
        var okCount = await _db.Accounts.CountAsync(a => accountIds.Contains(a.Id) && a.IsActive && a.IsPostable);
        if (okCount != accountIds.Count)
            return BadRequest("One or more accounts are invalid/inactive/not postable.");

        var entry = new JournalEntry
        {
            EntryDate = req.EntryDate,
            Reference = string.IsNullOrWhiteSpace(req.Reference) ? null : req.Reference.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            Status = JournalStatus.Draft,
            Lines = req.Lines.Select(l => new JournalLine
            {
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                Memo = string.IsNullOrWhiteSpace(l.Memo) ? null : l.Memo.Trim()
            }).ToList()
        };

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        return Ok(new { id = entry.Id });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var entry = await _db.JournalEntries.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.EntryDate,
                e.Reference,
                e.Description,
                status = e.Status,
                e.PostedAt,
                e.PostedBy,
                e.DeletedAt,
                e.DeletedBy,
                lines = e.Lines.Select(l => new { l.Id, l.AccountId, l.Debit, l.Credit, l.Memo })
            })
            .FirstOrDefaultAsync();

        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var list = await _db.JournalEntries.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Select(x => new {
                x.Id,
                x.EntryDate,
                x.Reference,
                x.Description,
                status = x.Status,
                x.PostedAt,
                x.PostedBy,
                x.DeletedAt,
                x.DeletedBy
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost("{id:long}/post")]
    public async Task<IActionResult> Post(long id)
    {
        var entry = await _db.JournalEntries
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entry == null) return NotFound();
        if (entry.Status != JournalStatus.Draft) return BadRequest("Only Draft journals can be posted.");

        var totalDebit = entry.Lines.Sum(x => x.Debit);
        var totalCredit = entry.Lines.Sum(x => x.Credit);
        if (totalDebit != totalCredit) return BadRequest("Journal is not balanced.");

        // period lock check
        var y = entry.EntryDate.Year;
        var m = entry.EntryDate.Month;
        var period = await _db.AccountingPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Year == y && p.Month == m);
        if (period?.IsClosed == true) return BadRequest("This accounting period is closed. Cannot post.");

        entry.Status = JournalStatus.Posted;
        entry.PostedAt = DateTime.UtcNow;
        entry.PostedBy = User?.Identity?.Name ?? "system";

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id:long}/delete")]
    public async Task<IActionResult> SoftDelete(long id)
    {
        var entry = await _db.JournalEntries.FirstOrDefaultAsync(x => x.Id == id);
        if (entry == null) return NotFound();

        if (entry.Status == JournalStatus.Posted)
            return BadRequest("Posted journals cannot be deleted.");

        entry.Status = JournalStatus.Deleted;
        entry.DeletedAt = DateTime.UtcNow;
        entry.DeletedBy = User?.Identity?.Name ?? "system";

        await _db.SaveChangesAsync();
        return Ok();
    }

    // ✅ NEW: Reverse a posted journal (creates a new Draft journal with swapped debits/credits)
    [HttpPost("{id:long}/reverse")]
    public async Task<IActionResult> Reverse(long id)
    {
        var src = await _db.JournalEntries
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (src == null) return NotFound();
        if (src.Status != JournalStatus.Posted) return BadRequest("Only Posted journals can be reversed.");

        // Create new draft reversal entry (same date by default; you can change in UI before posting)
        var rev = new JournalEntry
        {
            EntryDate = src.EntryDate,
            Reference = $"REV of #{src.Id}",
            Description = string.IsNullOrWhiteSpace(src.Description) ? "Reversal" : $"Reversal: {src.Description}",
            Status = JournalStatus.Draft,
            Lines = src.Lines.Select(l => new JournalLine
            {
                AccountId = l.AccountId,
                Debit = l.Credit,   // swap
                Credit = l.Debit,   // swap
                Memo = string.IsNullOrWhiteSpace(l.Memo) ? null : $"REV: {l.Memo}"
            }).ToList()
        };

        _db.JournalEntries.Add(rev);
        await _db.SaveChangesAsync();

        return Ok(new { id = rev.Id });
    }
}