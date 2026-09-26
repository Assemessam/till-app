namespace TillApp.Server.Data.Entities;

public sealed class Product
{
    public int ProductId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public Category Category { get; set; } = null!;
}
