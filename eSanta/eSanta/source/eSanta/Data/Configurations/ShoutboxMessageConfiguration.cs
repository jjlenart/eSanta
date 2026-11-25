using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eSanta.Data.Configurations;

public class ShoutboxMessageConfiguration : IEntityTypeConfiguration<ShoutboxMessage>
{
    public void Configure(EntityTypeBuilder<ShoutboxMessage> builder)
    {
        builder.ToTable("ShoutboxMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Content)
            .IsRequired()
            .HasMaxLength(ShoutboxMessage.ContentMaxLength);

        builder.HasOne(message => message.Family)
            .WithMany()
            .HasForeignKey(message => message.FamilyId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(message => message.Author)
            .WithMany()
            .HasForeignKey(message => message.AuthorId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Messages are paged per family by descending Id.
        builder.HasIndex(message => new { message.FamilyId, message.Id });

        builder.HasIndex(message => message.AuthorId);
    }
}
