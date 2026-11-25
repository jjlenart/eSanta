using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eSanta.Data.Configurations;

public class AttendeeConfiguration : IEntityTypeConfiguration<Attendee>
{
    public void Configure(EntityTypeBuilder<Attendee> builder)
    {
        builder.ToTable("Attendees");

        builder.HasKey(attendee => attendee.Id);

        builder.Property(attendee => attendee.FullName)
            .IsRequired()
            .HasMaxLength(Attendee.FullNameMaxLength);

        builder.HasIndex(attendee => attendee.FullName);

        builder.HasOne(attendee => attendee.User)
            .WithMany()
            .HasForeignKey(attendee => attendee.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(attendee => attendee.UserId)
            .IsUnique();

        builder.HasOne(attendee => attendee.Family)
            .WithMany()
            .HasForeignKey(attendee => attendee.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(attendee => attendee.FamilyId);
    }
}
