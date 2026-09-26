namespace TillApp.Shared.Catalog;

public sealed record ProductDto(
    int ProductId,
    int CategoryId,
    string CategoryName,
    string Name,
    decimal UnitPrice,
    bool IsActive);
