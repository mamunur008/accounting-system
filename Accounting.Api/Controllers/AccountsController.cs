using Accounting.Api.Infrastructure;
using Accounting.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AccountsController(AppDbContext db) => _db = db;

    public record CreateAccountRequest(
        string Code,
        string Name,
        AccountType Type,
        long? ParentId,
        bool IsPostable = true,
        bool IsActive = true
    );

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var list = await _db.Accounts.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                type = x.Type,
                x.ParentId,
                x.IsPostable,
                x.IsActive
            })
            .ToListAsync();

        static string Key(long? id) => id?.ToString() ?? "root";

        var byParent = list
            .GroupBy(x => Key(x.ParentId))
            .ToDictionary(g => g.Key, g => g.ToList());

        object Build(long? parentId)
        {
            if (!byParent.TryGetValue(Key(parentId), out var children))
                return Array.Empty<object>();

            return children.Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.type,
                c.ParentId,
                c.IsPostable,
                c.IsActive,
                children = Build(c.Id)
            }).ToList();
        }

        return Ok(Build(null));
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var list = await _db.Accounts.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                type = x.Type,
                x.ParentId,
                x.IsPostable,
                x.IsActive
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest("Code is required.");
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");

        var code = req.Code.Trim();
        var name = req.Name.Trim();

        var exists = await _db.Accounts.AnyAsync(x => x.Code == code);
        if (exists) return BadRequest("Account code already exists.");

        if (req.ParentId.HasValue)
        {
            var parent = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.ParentId.Value);
            if (parent == null) return BadRequest("Parent account not found.");

            if (parent.Type != req.Type)
                return BadRequest("Child account type must match parent account type.");

            if (parent.IsPostable)
                return BadRequest("Parent account must not be postable. Set parent as non-postable.");
        }

        var acc = new Account
        {
            Code = code,
            Name = name,
            Type = req.Type,
            ParentId = req.ParentId,
            IsPostable = req.IsPostable,
            IsActive = req.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(acc);
        await _db.SaveChangesAsync();

        return Ok(new { id = acc.Id });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] CreateAccountRequest req)
    {
        var acc = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == id);
        if (acc == null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest("Code is required.");
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");

        var code = req.Code.Trim();
        var name = req.Name.Trim();

        var exists = await _db.Accounts.AnyAsync(x => x.Code == code && x.Id != id);
        if (exists) return BadRequest("Account code already exists.");

        if (req.ParentId == id) return BadRequest("Parent cannot be self.");

        if (req.ParentId.HasValue)
        {
            var parent = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.ParentId.Value);
            if (parent == null) return BadRequest("Parent account not found.");

            if (parent.Type != req.Type)
                return BadRequest("Child account type must match parent account type.");

            if (parent.IsPostable)
                return BadRequest("Parent account must not be postable.");
        }

        acc.Code = code;
        acc.Name = name;
        acc.Type = req.Type;
        acc.ParentId = req.ParentId;
        acc.IsPostable = req.IsPostable;
        acc.IsActive = req.IsActive;
        acc.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { id = acc.Id });
    }
}