using Accounting.Api.Infrastructure;
using Accounting.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    // Trial Balance: Posted journals only
    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var fromDate = from ?? new DateOnly(1900, 1, 1);
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);

        // sums per account
        var sums = await (
            from jl in _db.JournalLines
            join je in _db.JournalEntries on jl.EntryId equals je.Id
            where je.Status == JournalStatus.Posted
               && je.EntryDate >= fromDate
               && je.EntryDate <= toDate
            group jl by jl.AccountId into g
            select new
            {
                AccountId = g.Key,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            }
        ).ToListAsync();

        var accounts = await _db.Accounts.AsNoTracking()
            .OrderBy(a => a.Code)
            .Select(a => new
            {
                a.Id,
                a.Code,
                a.Name,
                Type = a.Type,
                a.IsActive,
                a.IsPostable
            })
            .ToListAsync();

        var sumMap = sums.ToDictionary(x => x.AccountId, x => x);

        var rows = accounts.Select(a =>
        {
            var s = sumMap.TryGetValue(a.Id, out var v) ? v : null;
            return new
            {
                a.Id,
                a.Code,
                a.Name,
                type = a.Type,
                debit = s?.Debit ?? 0m,
                credit = s?.Credit ?? 0m
            };
        }).ToList();

        var totalDebit = rows.Sum(r => r.debit);
        var totalCredit = rows.Sum(r => r.credit);

        return Ok(new
        {
            from = fromDate,
            to = toDate,
            totals = new { debit = totalDebit, credit = totalCredit },
            rows
        });
    }

    // Ledger with opening + running
    [HttpGet("ledger/{accountId:long}")]
    public async Task<IActionResult> Ledger(
        long accountId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var fromDate = from ?? new DateOnly(1900, 1, 1);
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);

        var account = await _db.Accounts.AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => new { a.Id, a.Code, a.Name, type = a.Type })
            .FirstOrDefaultAsync();

        if (account == null) return NotFound("Account not found.");

        var opening = await (
            from jl in _db.JournalLines
            join je in _db.JournalEntries on jl.EntryId equals je.Id
            where jl.AccountId == accountId
               && je.Status == JournalStatus.Posted
               && je.EntryDate < fromDate
            select jl.Debit - jl.Credit
        ).SumAsync();

        var lines = await (
            from jl in _db.JournalLines
            join je in _db.JournalEntries on jl.EntryId equals je.Id
            where jl.AccountId == accountId
               && je.Status == JournalStatus.Posted
               && je.EntryDate >= fromDate
               && je.EntryDate <= toDate
            orderby je.EntryDate, je.Id, jl.Id
            select new
            {
                je.EntryDate,
                je.Number,
                je.Reference,
                jl.Debit,
                jl.Credit,
                jl.Memo
            }
        ).ToListAsync();

        decimal running = opening;

        var outLines = lines.Select(l =>
        {
            running += l.Debit - l.Credit;
            return new
            {
                l.EntryDate,
                l.Number,
                l.Reference,
                l.Debit,
                l.Credit,
                balance = running,
                l.Memo
            };
        }).ToList();

        return Ok(new
        {
            account,
            from = fromDate,
            to = toDate,
            openingBalance = opening,
            lines = outLines
        });
    }

    // (Optional later) balance sheet / P&L endpoints can go here
}