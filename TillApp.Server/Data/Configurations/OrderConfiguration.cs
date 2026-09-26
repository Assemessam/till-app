using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TillApp.Server.Data.Entities;
using TillApp.Shared.Common;
using TillApp.Shared.Orders;

namespace TillApp.Server.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_Amount", $"[Amount] >= 0 AND [Amount] <= {SqlMoney.MaxSqlLiteral}");
            table.HasCheckConstraint("CK_Orders_Status", "[Status] IN (0, 1, 2)");
        });

        builder.HasKey(order => order.OrderId);

        builder.Property(order => order.OrderId)
            .HasColumnName("OrderID")
            .UseIdentityColumn();

        builder.Property(order => order.OrderName)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(order => order.Amount)
            .HasColumnType("money")
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .HasDefaultValue(OrderStatus.Pending)
            .IsRequired();

        builder.Property(order => order.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(order => order.PaidAt)
            .HasColumnType("datetime2");

        builder.Property(order => order.CancelledAt)
            .HasColumnType("datetime2");

        builder.HasIndex(order => new { order.Status, order.CreatedAt })
            .HasDatabaseName("IX_Orders_Status_CreatedAt");

        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
