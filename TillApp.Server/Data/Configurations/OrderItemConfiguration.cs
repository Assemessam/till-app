using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TillApp.Server.Data.Entities;
using TillApp.Shared.Common;

namespace TillApp.Server.Data.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint(
                "CK_OrderItems_UnitPrice",
                $"[UnitPrice] >= {SqlMoney.MinPositiveSqlLiteral} AND [UnitPrice] <= {SqlMoney.MaxSqlLiteral}");
            table.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0");
        });

        builder.HasKey(item => item.OrderItemId);

        builder.Property(item => item.OrderItemId)
            .HasColumnName("OrderItemID")
            .UseIdentityColumn();

        builder.Property(item => item.OrderId)
            .HasColumnName("OrderID")
            .IsRequired();

        builder.Property(item => item.ProductId)
            .HasColumnName("ProductID");

        builder.Property(item => item.ProductName)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasColumnType("money")
            .IsRequired();

        builder.Property(item => item.Quantity)
            .IsRequired();

        builder.HasIndex(item => item.ProductId)
            .HasDatabaseName("IX_OrderItems_ProductID");

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
