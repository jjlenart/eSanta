using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eSanta.Data.Configurations;

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("Families");

        builder.HasKey(family => family.Id);

        builder.Property(family => family.Name)
            .IsRequired()
            .HasMaxLength(Family.NameMaxLength);

        builder.Property(family => family.NormalizedName)
            .IsRequired()
            .HasMaxLength(Family.NameMaxLength);

        builder.HasIndex(family => family.NormalizedName)
            .IsUnique();

        builder.HasMany(family => family.Members)
            .WithOne(user => user.Family)
            .HasForeignKey(user => user.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
