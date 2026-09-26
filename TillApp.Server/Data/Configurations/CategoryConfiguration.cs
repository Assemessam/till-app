using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TillApp.Server.Data.Entities;

namespace TillApp.Server.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.CategoryId);

        builder.Property(category => category.CategoryId)
            .HasColumnName("CategoryID")
            .UseIdentityColumn();

        builder.Property(category => category.Name)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique()
            .HasDatabaseName("UX_Categories_Name");
    }
}
