namespace Accounting.Api.Models;

public enum AccountType : byte
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Income = 4,
    Expense = 5
}

public class Account
{
    public long Id { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public AccountType Type { get; set; }

    public long? ParentId { get; set; }

    public bool IsPostable { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}