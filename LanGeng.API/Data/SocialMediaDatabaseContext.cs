using LanGeng.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LanGeng.API.Data;

public class SocialMediaDatabaseContext(DbContextOptions<SocialMediaDatabaseContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserVerification> UserVerifications => Set<UserVerification>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<UserStatus> UserStatuses => Set<UserStatus>();
    public DbSet<UserSessionLog> UserSessionLogs => Set<UserSessionLog>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<UserPost> UserPosts => Set<UserPost>();
    public DbSet<Hashtag> Hashtags => Set<Hashtag>();
    public DbSet<PostHashtag> PostHashtags => Set<PostHashtag>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<CommentReaction> CommentReactions => Set<CommentReaction>();
    public DbSet<PostReaction> PostReactions => Set<PostReaction>();
    public DbSet<UserEvent> UserEvents => Set<UserEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users Table
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasOne(e => e.Profile).WithOne(r => r.User).HasForeignKey<UserProfile>(r => r.UserId);
            entity.HasOne(e => e.AccountStatus).WithOne(r => r.User).HasForeignKey<UserStatus>(r => r.UserId);
            entity.HasMany(e => e.UserTokens).WithOne(r => r.User).HasForeignKey(r => r.UserId);
            // entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            // entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
        });

        // UserProfiles Table
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.HasOne(e => e.User).WithOne(r => r.Profile).HasForeignKey<UserProfile>(r => r.UserId);
        });

        // UserStatus Table
        modelBuilder.Entity<UserStatus>(entity =>
        {
            entity.Property(e => e.AccountStatus).HasConversion<byte>();
            entity.HasOne(e => e.User).WithOne(r => r.AccountStatus).HasForeignKey<UserStatus>(r => r.UserId);
        });

        // UserTokens Table
        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.User).WithMany(r => r.UserTokens).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // UserVerifications Table
        modelBuilder.Entity<UserVerification>(entity =>
        {
            entity.Property(e => e.VerificationType).HasConversion<byte>();
            entity.HasOne(e => e.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Groups Table
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.PrivacyType).HasConversion<byte>();
            entity.HasOne(e => e.Creator).WithMany().HasForeignKey(r => r.CreatorId).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.Members).WithOne(r => r.Group).HasForeignKey(r => r.GroupId).OnDelete(DeleteBehavior.Cascade);
        });

        // GroupMembers Table
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.HasOne(e => e.Group).WithMany(r => r.Members).HasForeignKey(r => r.MemberId);
            entity.HasOne(e => e.Member).WithMany().HasForeignKey(r => r.MemberId);//.OnDelete(DeleteBehavior.Restrict);
        });

        // UserPosts Table
        modelBuilder.Entity<UserPost>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(e => e.Author).WithMany().HasForeignKey(r => r.AuthorId);
            entity.HasOne(e => e.Group).WithMany().HasForeignKey(r => r.GroupId);
            entity.HasMany(e => e.Comments).WithOne(r => r.Post).HasForeignKey(r => r.PostId);
            entity.HasMany(e => e.Reactions).WithOne(r => r.Post).HasForeignKey(r => r.PostId);
            entity.HasMany(e => e.PostHashtags).WithOne(e => e.Post).HasForeignKey(r => r.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Hashtags).WithMany(e => e.Posts).UsingEntity<PostHashtag>(
            // l => l.HasOne<Hashtag>().WithMany().HasForeignKey(e => e.HashtagId),
            // r => r.HasOne<UserPost>().WithMany().HasForeignKey(e => e.PostId)
            );
        });

        // Hashtags Table
        modelBuilder.Entity<Hashtag>(entity =>
        {
            entity.HasIndex(e => e.Tag).IsUnique();
            entity.HasMany(e => e.PostHashtags).WithOne(e => e.Hashtag).HasForeignKey(r => r.HashtagId).OnDelete(DeleteBehavior.Cascade);
            // entity.HasMany(e => e.Posts).WithMany(e => e.Hashtags).UsingEntity<PostHashtag>(
            //     l => l.HasOne<UserPost>().WithMany().HasForeignKey(e => e.PostId),
            //     r => r.HasOne<Hashtag>().WithMany().HasForeignKey(e => e.HashtagId)
            // );
        });

        // PostHashtags Table
        modelBuilder.Entity<PostHashtag>(entity =>
        {
            entity.HasKey(e => new { e.PostId, e.HashtagId });
            entity.HasOne(e => e.Hashtag).WithMany(r => r.PostHashtags).HasForeignKey(r => r.HashtagId);//.OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Post).WithMany(r => r.PostHashtags).HasForeignKey(r => r.PostId);//.OnDelete(DeleteBehavior.Restrict);
        });

        // PostComments Table
        modelBuilder.Entity<PostComment>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Post).WithMany(r => r.Comments).HasForeignKey(r => r.PostId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Reply).WithMany().HasForeignKey(r => r.ReplyId).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.Reactions).WithOne(r => r.Comment).HasForeignKey(r => r.CommentId);
        });

        // PostReactions Table
        modelBuilder.Entity<PostReaction>(entity =>
        {
            entity.Property(e => e.Type).HasConversion<byte>();
            entity.HasOne(e => e.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Post).WithMany(r => r.Reactions).HasForeignKey(r => r.PostId).OnDelete(DeleteBehavior.NoAction);
        });

        // CommentReactions Table
        modelBuilder.Entity<CommentReaction>(entity =>
        {
            entity.Property(e => e.Type).HasConversion<byte>();
            entity.HasOne(e => e.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Comment).WithMany(r => r.Reactions).HasForeignKey(r => r.CommentId).OnDelete(DeleteBehavior.NoAction);
        });

        // UserEvents Table
        modelBuilder.Entity<UserEvent>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(e => e.Creator).WithMany().HasForeignKey(r => r.CreatorId);
            entity.HasOne(e => e.Group).WithMany().HasForeignKey(r => r.GroupId);
            entity.HasOne(e => e.Post).WithOne().HasForeignKey<UserEvent>(r => r.PostId);
        });

        // UserSessionLogs Table
        modelBuilder.Entity<UserSessionLog>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.NoAction);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
}
