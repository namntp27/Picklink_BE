using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Picklink.Domain.Common;
using Picklink.Domain.Entities;
using Picklink.Infrastructure.Identity;

namespace Picklink.Infrastructure.Data;

public sealed class PicklinkDbContext(DbContextOptions<PicklinkDbContext> options)
    : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<CourtImage> CourtImages => Set<CourtImage>();
    public DbSet<CourtFeature> CourtFeatures => Set<CourtFeature>();
    public DbSet<CourtSchedule> CourtSchedules => Set<CourtSchedule>();
    public DbSet<CourtBlockedSlot> CourtBlockedSlots => Set<CourtBlockedSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingParticipant> BookingParticipants => Set<BookingParticipant>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<MatchRequest> MatchRequests => Set<MatchRequest>();
    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();
    public DbSet<MatchInvite> MatchInvites => Set<MatchInvite>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostMedia> PostMedia => Set<PostMedia>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<PostReaction> PostReactions => Set<PostReaction>();
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMember> ClubMembers => Set<ClubMember>();
    public DbSet<ClubPost> ClubPosts => Set<ClubPost>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentityTables(builder);
        ConfigureIdentityRelations(builder);
        ConfigureDomainTables(builder);
        ConfigureIndexes(builder);
    }

    private static void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.City).HasMaxLength(120);
            entity.Property(x => x.District).HasMaxLength(120);
            entity.Property(x => x.Ward).HasMaxLength(120);
        });

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(x => x.Description).HasMaxLength(250);
        });

        builder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId);
        });
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
    }

    private static void ConfigureIdentityRelations(ModelBuilder builder)
    {
        builder.Entity<Player>()
            .HasOne<User>()
            .WithOne(x => x.Player)
            .HasForeignKey<Player>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Owner>()
            .HasOne<User>()
            .WithOne(x => x.Owner)
            .HasForeignKey<Owner>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Admin>()
            .HasOne<User>()
            .WithOne(x => x.Admin)
            .HasForeignKey<Admin>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RefreshToken>()
            .HasOne<User>()
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ExternalLogin>()
            .HasOne<User>()
            .WithMany(x => x.ExternalLogins)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureDomainTables(ModelBuilder builder)
    {
        builder.Entity<Sport>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        });

        builder.Entity<Venue>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(300).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();
            entity.Property(x => x.District).HasMaxLength(120);
            entity.Property(x => x.Ward).HasMaxLength(120);
            entity.Property(x => x.Latitude).HasPrecision(10, 7);
            entity.Property(x => x.Longitude).HasPrecision(10, 7);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Court>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.PricePerHour).HasPrecision(18, 2);
            entity.HasOne(x => x.Venue).WithMany(x => x.Courts).HasForeignKey(x => x.VenueId);
            entity.HasOne(x => x.Sport).WithMany().HasForeignKey(x => x.SportId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CourtImage>().Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.Entity<CourtFeature>().Property(x => x.Name).HasMaxLength(120).IsRequired();

        builder.Entity<Booking>(entity =>
        {
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Court).WithMany().HasForeignKey(x => x.CourtId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentTransaction>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Provider).HasMaxLength(80);
            entity.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Review>(entity =>
        {
            entity.Property(x => x.Comment).HasMaxLength(1000);
            entity.HasOne(x => x.Booking).WithOne().HasForeignKey<Review>(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Court).WithMany().HasForeignKey(x => x.CourtId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MatchRequest>(entity =>
        {
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Sport).WithMany().HasForeignKey(x => x.SportId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Post>(entity =>
        {
            entity.Property(x => x.Content).HasMaxLength(5000).IsRequired();
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PostReaction>()
            .HasIndex(x => new { x.PostId, x.UserId })
            .IsUnique();

        builder.Entity<SavedPost>()
            .HasIndex(x => new { x.PostId, x.UserId })
            .IsUnique();

        builder.Entity<Club>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Tournament>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Fee).HasPrecision(18, 2);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.OrganizerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Sport).WithMany().HasForeignKey(x => x.SportId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ConversationMember>()
            .HasIndex(x => new { x.ConversationId, x.UserId })
            .IsUnique();
    }

    private static void ConfigureIndexes(ModelBuilder builder)
    {
        builder.Entity<Player>().HasIndex(x => x.UserId).IsUnique();
        builder.Entity<Owner>().HasIndex(x => x.UserId).IsUnique();
        builder.Entity<Admin>().HasIndex(x => x.UserId).IsUnique();
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
            entity.Property(x => x.DeviceInfo).HasMaxLength(500);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.RevokedByIp).HasMaxLength(64);
            entity.Property(x => x.RevokeReason).HasMaxLength(200);
        });
        builder.Entity<ExternalLogin>().HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
        builder.Entity<Sport>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Court>().HasIndex(x => new { x.VenueId, x.Name });
        builder.Entity<Booking>().HasIndex(x => new { x.CourtId, x.StartTime, x.EndTime });
        builder.Entity<Post>().HasIndex(x => x.CreatedAt);
    }

    private void ApplyAuditFields()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
