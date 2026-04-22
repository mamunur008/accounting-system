using Accounting.Api.Infrastructure;
using Accounting.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/periods")]
public class AccountingPeriodsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AccountingPeriodsController(AppDbContext db) => _db = db;

    public record UpsertPeriodRequest(int Year, int Month);
    public record TogglePeriodRequest(bool IsClosed);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? year)
    {
        var q = _db.AccountingPeriods.AsNoTracking().AsQueryable();

        if (year.HasValue)
            q = q.Where(x => x.Year == year.Value);

        var list = await q
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Select(x => new
            {
                x.Id,
                x.Year,
                x.Month,
                x.IsClosed,
                x.ClosedAt,
                x.ClosedBy
            })
            .ToListAsync();

        return Ok(list);
    }

    // Create if missing (or return existing)
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertPeriodRequest req)
    {
        if (req.Year < 2000 || req.Year > 2100) return BadRequest("Invalid year.");
        if (req.Month < 1 || req.Month > 12) return BadRequest("Invalid month.");

        var existing = await _db.AccountingPeriods
            .FirstOrDefaultAsync(x => x.Year == req.Year && x.Month == req.Month);

        if (existing != null)
        {
            return Ok(new
            {
                existing.Id,
                existing.Year,
                existing.Month,
                existing.IsClosed,
                existing.ClosedAt,
                existing.ClosedBy
            });
        }

        var p = new AccountingPeriod
        {
            Year = req.Year,
            Month = req.Month,
            IsClosed = false
        };

        _db.AccountingPeriods.Add(p);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            p.Id,
            p.Year,
            p.Month,
            p.IsClosed,
            p.ClosedAt,
            p.ClosedBy
        });
    }

    // Close / Open a period
    [HttpPost("{id:long}/toggle")]
    public async Task<IActionResult> Toggle(long id, [FromBody] TogglePeriodRequest req)
    {
        var p = await _db.AccountingPeriods.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        p.IsClosed = req.IsClosed;

        if (req.IsClosed)
        {
            p.ClosedAt = DateTime.UtcNow;
            p.ClosedBy = User?.Identity?.Name ?? "system";
        }
        else
        {
            p.ClosedAt = null;
            p.ClosedBy = null;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            p.Id,
            p.Year,
            p.Month,
            p.IsClosed,
            p.ClosedAt,
            p.ClosedBy
        });
    }
}