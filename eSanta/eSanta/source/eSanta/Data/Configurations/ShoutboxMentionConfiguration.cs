using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eSanta.Data.Configurations;

public class ShoutboxMentionConfiguration : IEntityTypeConfiguration<ShoutboxMention>
{
    public void Configure(EntityTypeBuilder<ShoutboxMention> builder)
    {
        builder.ToTable("ShoutboxMentions");

        builder.HasKey(mention => new { mention.MessageId, mention.UserId });

        builder.HasOne(mention => mention.Message)
            .WithMany(message => message.Mentions)
            .HasForeignKey(mention => mention.MessageId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mention => mention.User)
            .WithMany()
            .HasForeignKey(mention => mention.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mention => mention.UserId);
    }
}
