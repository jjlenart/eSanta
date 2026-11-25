using eSanta.Data.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace eSanta.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Attendee> Attendees => Set<Attendee>();

        public DbSet<Family> Families => Set<Family>();

        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

        public DbSet<ShoppingListEntry> ShoppingListEntries => Set<ShoppingListEntry>();

        public DbSet<ShoutboxMessage> ShoutboxMessages => Set<ShoutboxMessage>();

        public DbSet<ShoutboxMention> ShoutboxMentions => Set<ShoutboxMention>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new AttendeeConfiguration());
            builder.ApplyConfiguration(new FamilyConfiguration());
            builder.ApplyConfiguration(new ApplicationUserConfiguration());
            builder.ApplyConfiguration(new WishlistItemConfiguration());
            builder.ApplyConfiguration(new ShoppingListEntryConfiguration());
            builder.ApplyConfiguration(new ShoutboxMessageConfiguration());
            builder.ApplyConfiguration(new ShoutboxMentionConfiguration());
        }
    }
}
