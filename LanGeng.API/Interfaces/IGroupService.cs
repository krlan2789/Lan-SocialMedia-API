using LanGeng.API.Entities;
using LanGeng.API.Enums;

namespace LanGeng.API.Interfaces;

public interface IGroupService
{
    public Task<(List<Group>, long, short, byte)?> GetGroupsAsync(string? Keyword = null, string? Username = null, short Page = 1, byte? Limit = null);
    public Task<Group?> GetGroupBySlugUnsafeAsync(string Slug);
    public Task<Group?> GetGroupBySlugWithAllPrivacyAsync(string Slug, string Username);
    public Task<Group?> GetGroupBySlugWithNonPrivateAsync(string Slug);
    public Task<Group?> GetGroupBySlugWithPublicOnlyAsync(string Slug);
    public Task<string?> CreateGroupAsync(Group Group);
    public Task<string?> UpdateGroupAsync(string Slug, Group Group);
    public Task<string?> DeleteGroupAsync(string Slug, User Creator, DateTime DeletedAt);
    public Task<string?> JoinGroupAsync(string Slug, User User);
    public Task<string?> UpdateMemberStatusAsync(string Slug, int CreatorId, int MemberId, GroupMemberStatusEnum Status);
}
