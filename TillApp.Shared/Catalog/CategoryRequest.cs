using System.ComponentModel.DataAnnotations;

namespace TillApp.Shared.Catalog;

public sealed class CategoryRequest
{
    private string _name = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name
    {
        get => _name;
        set => _name = value?.Trim() ?? string.Empty;
    }
}
