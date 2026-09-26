using System.ComponentModel.DataAnnotations;
using TillApp.Shared.Common;

namespace TillApp.Shared.Catalog;

public sealed class ProductRequest
{
    private string _name = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name
    {
        get => _name;
        set => _name = value?.Trim() ?? string.Empty;
    }

    [SqlMoney]
    public decimal UnitPrice { get; set; }
}
