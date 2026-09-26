namespace TillApp.Server.Services;

public enum ProductCatalogError
{
    NotFound,
    DuplicateName,
    CategoryHasProducts
}

public sealed class ProductCatalogException(ProductCatalogError error, string message) : Exception(message)
{
    public ProductCatalogError Error { get; } = error;
}
