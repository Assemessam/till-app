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
                "CK_OrderItems_Price",
                $"[Price] >= {SqlMoney.MinPositiveSqlLiteral} AND [Price] <= {SqlMoney.MaxSqlLiteral}");
        });

        builder.HasKey(item => item.OrderItemId);

        builder.Property(item => item.OrderItemId)
            .HasColumnName("OrderItemID")
            .UseIdentityColumn();

        builder.Property(item => item.OrderId)
            .HasColumnName("OrderID")
            .IsRequired();

        builder.Property(item => item.ItemName)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(item => item.Price)
            .HasColumnType("money")
            .IsRequired();
    }
}
