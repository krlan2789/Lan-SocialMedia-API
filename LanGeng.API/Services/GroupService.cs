using LanGeng.API.Data;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Helper;
using LanGeng.API.Interfaces;
using LanGeng.API.Mapping;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Services;

public class GroupService : IGroupService
{
    private readonly ILogger<GroupService> _logger;
    private readonly SocialMediaDatabaseContext dbContext;

    public GroupService(ILogger<GroupService> logger, SocialMediaDatabaseContext context)
    {
        _logger = logger;
        dbContext = context;
        dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<(List<Group>, long, short, byte)?> GetGroupsAsync(string? Keyword = null, string? Username = null, short Page = 1, byte? Limit = null)
    {
        try
        {
            Keyword = Keyword?.Trim();
            Username = Username?.Trim();

            var limit = Limit ?? 16;
            var query = dbContext.Groups.IncludeAll();

            if (!string.IsNullOrWhiteSpace(Username))
            {
                query = query.Where(e =>
                    (e.Creator != null && e.Creator.Username != null && e.Creator.Username.ToLower().Contains(Username)) ||
                    (e.Members != null && e.Members.Any(u => u.Member!.Username != null && u.Member.Username.ToLower().Contains(Username))));
            }
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                query = query.Where(e =>
                    (e.Name != null && e.Name.ToLower().Contains(Keyword)) ||
                    (e.Description != null && e.Description.ToLower().Contains(Keyword)));
            }

            long totalGroups = query.Count();
            var start = ((Page > 0 ? Page : 1) - 1) * limit;
            var groups = await query
                .OrderBy(e => e.PrivacyType)
                .Skip(start)
                .Take(limit)
                .AsSplitQuery()
                .ToListAsync();
            return groups != null ? (groups, totalGroups, Page, limit) : null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to find group");
            return null;
        }
    }

    public async Task<Group?> GetGroupBySlugUnsafeAsync(string Slug)
    {
        try
        {
            return await dbContext.Groups
                .IncludeAll()
                .Where(e => e.Slug == Slug)
                .FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to find group");
            return null;
        }
    }

    public async Task<Group?> GetGroupBySlugWithAllPrivacyAsync(string Slug, string Username)
    {
        try
        {
            return await dbContext.Groups
                .IncludeAll()
                .Where(e =>
                    e.Slug == Slug &&
                    (
                        (e.Members != null && e.Members.Any(u => u.Member!.Username == Username)) ||
                        (e.Creator != null && e.Creator.Username == Username)
                    )
                )
                .FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to find group");
            return null;
        }
    }

    public async Task<Group?> GetGroupBySlugWithNonPrivateAsync(string Slug)
    {
        try
        {
            return await dbContext.Groups
                .IncludeAll()
                .Where(e => e.Slug == Slug && e.PrivacyType != PrivacyTypeEnum.Private)
                .FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to find group");
            return null;
        }
    }

    public async Task<Group?> GetGroupBySlugWithPublicOnlyAsync(string Slug)
    {
        try
        {
            return await dbContext.Groups
                .IncludeAll()
                .Where(e => e.Slug == Slug && e.PrivacyType == PrivacyTypeEnum.Public)
                .FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to find group");
            return null;
        }
    }

    public async Task<string?> CreateGroupAsync(Group Group)
    {
        try
        {
            Group.Members ??= [];
            Group.Members.Add(new()
            {
                Slug = SlugHelper.Create($"{Group.Name}"),
                Status = GroupMemberStatusEnum.Approved,
                GroupId = Group.Id,
                MemberId = Group.CreatorId,
                JoinedAt = DateTime.Now,
            });
            if (Group != null) dbContext.Groups.Add(Group);
            else throw new Exception("Failed to create group.");
            await dbContext.SaveChangesAsync();
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create group");
            return e.Message;
        }
    }

    public Task<string?> UpdateGroupAsync(string Slug, Group Group)
    {
        throw new NotImplementedException();
    }

    public async Task<string?> DeleteGroupAsync(string Slug, User Creator, DateTime DeletedAt)
    {
        try
        {
            var group = await dbContext.Groups
                .Where(e => e.Slug == Slug && e.CreatorId == Creator.Id)
                .AsTracking().FirstOrDefaultAsync()
                ?? throw new Exception("You don't have permission to delete this group");
            dbContext.Entry(group).CurrentValues.SetValues(new { DeletedAt });
            var posts = await dbContext.UserPosts
                .Where(e => e.GroupId != null && e.GroupId == group.Id)
                .AsTracking().ToListAsync() ?? [];
            foreach (var post in posts)
            {
                dbContext.Entry(post).CurrentValues.SetValues(new { DeletedAt });
            }
            await dbContext.SaveChangesAsync();
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete group");
            return e.Message;
        }
    }

    public async Task<string?> JoinGroupAsync(string Slug, User User)
    {
        try
        {
            Group? group = await GetGroupBySlugWithNonPrivateAsync(Slug) ?? throw new Exception("Can't join to this group");
            GroupMember? member = await dbContext.GroupMembers
                .Where(e => e.GroupId == group.Id && e.MemberId == User.Id)
                .AsTracking()
                .FirstOrDefaultAsync();
            if (member != null) throw new Exception("You have already requested or joined to this group");
            member = new()
            {
                Slug = SlugHelper.Create($"{group.Name}-{User.Username}"),
                Status = GroupMemberStatusEnum.Request,
                GroupId = group.Id,
                MemberId = User.Id,
                JoinedAt = DateTime.Now,
            };
            dbContext.GroupMembers.Add(member);
            await dbContext.SaveChangesAsync();
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create group");
            return e.Message;
        }
    }

    public async Task<string?> UpdateMemberStatusAsync(string Slug, int CreatorId, int MemberId, GroupMemberStatusEnum Status)
    {
        try
        {
            var group = await dbContext.Groups
                .IncludeAll().Where(e => e.Slug == Slug).AsTracking()
                .FirstOrDefaultAsync() ?? throw new Exception("Group not found");
            if (group.CreatorId != CreatorId &&
                Status != GroupMemberStatusEnum.Left && Status != GroupMemberStatusEnum.Request)
                throw new Exception("You are not an admin");
            var member = await dbContext.GroupMembers
                .Where(e => e.MemberId == MemberId && e.Status == GroupMemberStatusEnum.Request)
                .AsTracking().FirstOrDefaultAsync() ?? throw new Exception("Member request not available");
            dbContext.Entry(member).CurrentValues.SetValues(new
            {
                Status = Status,
                UpdatedAt = DateTime.Now,
                JoinedAt = Status == GroupMemberStatusEnum.Approved ? DateTime.Now : member.JoinedAt,
            });
            await dbContext.SaveChangesAsync();
            return null;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
}
