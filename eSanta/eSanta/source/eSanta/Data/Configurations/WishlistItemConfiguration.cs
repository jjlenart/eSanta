using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eSanta.Data.Configurations;

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name)
            .IsRequired()
            .HasMaxLength(WishlistItem.NameMaxLength);

        builder.Property(item => item.Description)
            .IsRequired()
            .HasMaxLength(WishlistItem.DescriptionMaxLength);

        builder.Property(item => item.Link)
            .HasMaxLength(WishlistItem.LinkMaxLength);

        builder.HasOne(item => item.Owner)
            .WithMany()
            .HasForeignKey(item => item.OwnerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => item.OwnerId);

        // Concurrency token: two people reserving the same gift at once cannot both succeed.
        builder.Property(item => item.ReservedById)
            .IsConcurrencyToken();

        builder.HasOne(item => item.ReservedBy)
            .WithMany()
            .HasForeignKey(item => item.ReservedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(item => item.ReservedById);
    }
}
