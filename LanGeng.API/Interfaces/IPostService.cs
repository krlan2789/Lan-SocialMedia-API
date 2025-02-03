using LanGeng.API.Entities;

namespace LanGeng.API.Interfaces;

public interface IPostService
{
    public Task<(List<UserPost>, long, short, byte)?> GetPostsAsync(string? Keyword = null, string? Author = null, string? Group = null, string[]? Tags = null, short Page = 1, byte? Limit = null);
    public Task<UserPost?> GetPostByIdAsync(int postId);
    public Task<UserPost?> GetPostBySlugAsync(string slug);
    public Task<List<UserPost>?> GetPostsByAuthorIdAsync(int userId);
    public Task<List<UserPost>?> GetPostsByGroupIdAsync(int groupId, int? memberId);
    public Task<string?> CreatePostAsync(UserPost post);
    public Task<string?> CreateMediaPostAsync(string slug, string rootDir, List<IFormFile>? Media);
    public Task<string?> UpdatePostAsync(string slug, UserPost post);
    public Task<string?> UpdateMediaPostAsync(string slug, string rootDir, List<IFormFile>? newMedia, List<int>? deletedMediaIds);
    public Task<string?> DeletePostAsync(int postId, DateTime DeletedAt, int? userId = null);
    public Task<string?> DeletePostBySlugAsync(string slug, DateTime DeletedAt, int? userId = null);
}
