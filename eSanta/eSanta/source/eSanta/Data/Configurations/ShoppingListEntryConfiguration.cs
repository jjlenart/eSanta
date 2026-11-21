using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eSanta.Data.Configurations;

public class ShoppingListEntryConfiguration : IEntityTypeConfiguration<ShoppingListEntry>
{
    public void Configure(EntityTypeBuilder<ShoppingListEntry> builder)
    {
        builder.ToTable("ShoppingListEntries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.RecipientName)
            .IsRequired()
            .HasMaxLength(ShoppingListEntry.RecipientNameMaxLength);

        builder.Property(entry => entry.GiftName)
            .HasMaxLength(ShoppingListEntry.GiftNameMaxLength);

        builder.Property(entry => entry.Note)
            .HasMaxLength(ShoppingListEntry.NoteMaxLength);

        builder.HasOne(entry => entry.Owner)
            .WithMany()
            .HasForeignKey(entry => entry.OwnerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // When a recipient disappears, the entry is kept as an "outside the list" entry with its name snapshot.
        builder.HasOne(entry => entry.RecipientUser)
            .WithMany()
            .HasForeignKey(entry => entry.RecipientUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(entry => entry.Attendee)
            .WithMany()
            .HasForeignKey(entry => entry.AttendeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(entry => new { entry.OwnerId, entry.RecipientUserId })
            .IsUnique();

        builder.HasIndex(entry => new { entry.OwnerId, entry.AttendeeId })
            .IsUnique();

        builder.HasIndex(entry => entry.RecipientUserId);

        builder.HasIndex(entry => entry.AttendeeId);
    }
}
