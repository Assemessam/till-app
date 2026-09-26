using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TillApp.Server.Data.Entities;
using TillApp.Shared.Common;

namespace TillApp.Server.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint(
                "CK_Products_UnitPrice",
                $"[UnitPrice] >= {SqlMoney.MinPositiveSqlLiteral} AND [UnitPrice] <= {SqlMoney.MaxSqlLiteral}");
        });

        builder.HasKey(product => product.ProductId);

        builder.Property(product => product.ProductId)
            .HasColumnName("ProductID")
            .UseIdentityColumn();

        builder.Property(product => product.CategoryId)
            .HasColumnName("CategoryID")
            .IsRequired();

        builder.Property(product => product.Name)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(product => product.UnitPrice)
            .HasColumnType("money")
            .IsRequired();

        builder.Property(product => product.IsActive)
            .HasColumnType("bit")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(product => new { product.CategoryId, product.Name })
            .IsUnique()
            .HasDatabaseName("UX_Products_CategoryID_Name");

        builder.HasIndex(product => new { product.CategoryId, product.IsActive })
            .HasDatabaseName("IX_Products_CategoryID_IsActive");

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
