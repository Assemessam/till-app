using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TillApp.Server.Data.Entities;
using TillApp.Shared.Common;

namespace TillApp.Server.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_Amount", $"[Amount] >= 0 AND [Amount] <= {SqlMoney.MaxSqlLiteral}");
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

        builder.Property(order => order.IsPaid)
            .HasColumnType("bit")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(order => order.IsPaid)
            .HasDatabaseName("IX_Orders_IsPaid");

        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
