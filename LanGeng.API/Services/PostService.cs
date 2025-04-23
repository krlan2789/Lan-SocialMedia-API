using LanGeng.API.Data;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Helper;
using LanGeng.API.Interfaces;
using LanGeng.API.Mapping;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Services;

public class PostService : IPostService
{
    private readonly ILogger<PostService> _logger;
    private readonly SocialMediaDatabaseContext dbContext;
    private readonly string MEDIA_ROOT = "post/media";

    public PostService(ILogger<PostService> logger, SocialMediaDatabaseContext context)
    {
        _logger = logger;
        dbContext = context;
        dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<(List<UserPost>, long, short, byte)?> GetPostsAsync(string? Keyword = null, string? Author = null, string? Group = null, string[]? Tags = null, short Page = 1, byte? Limit = null)
    {
        try
        {
            Keyword = Keyword?.Trim().ToLower();
            Author = Author?.Trim().ToLower();
            Group = Group?.Trim().ToLower();

            byte defaultLimit = 16;
            var query = dbContext.UserPosts.IncludeAll();

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                query = query.Where(e => e.Content != null && e.Content.ToLower().Contains(Keyword));
            }
            if (!string.IsNullOrWhiteSpace(Author))
            {
                query = query.Where(e => e.Author != null && (e.Author.Username != null && e.Author.Username.ToLower().Contains(Author) || e.Author.Fullname != null && e.Author.Fullname.ToLower().Contains(Author)));
            }
            if (!string.IsNullOrWhiteSpace(Group))
            {
                query = query.Where(e => e.Group != null && (e.Group.Slug != null && e.Group.Slug.ToLower().Contains(Group) || e.Group.Name != null && e.Group.Name.ToLower().Contains(Group)));
            }
            if (Tags != null && Tags.Length > 0)
            {
                query = query.Where(e => e.Hashtags.Any(t => Tags.Contains(t.Tag)));
            }

            long totalPosts = await query.CountAsync();
            byte limit = Limit ?? defaultLimit;
            var start = ((Page > 0 ? Page : 1) - 1) * limit;
            var posts = await query
                .OrderByDescending(p => p.UpdatedAt)
                .Skip(start)
                .Take(limit)
                .AsSplitQuery()
                .ToListAsync();
            // _logger.LogInformation($"Get posts: {posts.Count}, Total: {totalPosts}, Page: {Page}, Limit: {limit}, Start: {start}, TotalRecords: {totalRecords}, Keyword: {Keyword}, Author: {Author}, Group: {Group}, Tags: {Tags?.Length}.");
            return (posts, totalPosts, Page, limit);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get post");
            return null;
        }
    }

    public async Task<UserPost?> GetPostByIdAsync(int postId)
    {
        try
        {
            return await dbContext.UserPosts
                .IncludeAll()
                .Where(e => e.Id == postId)
                .FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get post");
            return null;
        }
    }

    public async Task<UserPost?> GetPostBySlugAsync(string slug)
    {
        try
        {
            return await dbContext.UserPosts
                .IncludeAll()
                .Where(e => e.Slug == slug)
                .FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get post");
            return null;
        }
    }

    public async Task<List<UserPost>?> GetPostsByGroupIdAsync(int groupId, int? memberId = null)
    {
        try
        {
            return await dbContext.UserPosts
                .IncludeAll()
                .Where(e => e.GroupId != null && e.GroupId == groupId && (memberId == null || e.AuthorId == memberId))
                .AsTracking()
                .AsSplitQuery()
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get post");
            return null;
        }
    }

    public async Task<List<UserPost>?> GetPostsByAuthorIdAsync(int authorId)
    {
        try
        {
            return await dbContext.UserPosts
                .IncludeAll()
                .Where(e => e.AuthorId == authorId)
                .AsTracking()
                .AsSplitQuery()
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get post");
            return null;
        }
    }

    public async Task<string?> CreatePostAsync(UserPost post)
    {
        try
        {
            if (post != null) dbContext.UserPosts.Add(post);
            else throw new Exception("Failed to create post, try again later.");
            await dbContext.SaveChangesAsync();
            // Save hashtag from post
            string[] tags = ("" + post.Content).ExtractHashtags();
            if (tags.Length > 0)
            {
                foreach (string tag in tags)
                {
                    Hashtag? hashtag = await dbContext.Hashtags.Where(e => e.Tag == tag).AsTracking().FirstOrDefaultAsync();
                    if (hashtag == null)
                    {
                        hashtag = new Hashtag { Tag = tag.Replace("#", "") };
                        await dbContext.Hashtags.AddAsync(hashtag);
                        await dbContext.SaveChangesAsync();
                    }
                    PostHashtag? postTag = new() { HashtagId = hashtag.Id, PostId = post.Id };
                    await dbContext.PostHashtags.AddAsync(postTag);
                    await dbContext.SaveChangesAsync();
                }
            }
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create post");
            return e.Message;
        }
    }

    public async Task<string?> CreateMediaPostAsync(string slug, string rootDir, List<IFormFile>? Media)
    {
        try
        {
            var post = await GetPostBySlugAsync(slug);
            if (post == null) throw new Exception("Invalid post");
            // Save media from post
            if (Media != null && Media.Count > 0)
            {
                var postMedia = new List<PostMedia>();
                foreach (var formFile in Media)
                {
                    if (formFile.Length > 0)
                    {
                        var fileExtension = ("" + formFile.FileName).ToLower().Split('.')[^1];
                        var mediaType = fileExtension switch
                        {
                            "jpg" or "png" or "jpeg" => MediaTypeEnum.Image,
                            "mp3" or "wav" or "ogg" => MediaTypeEnum.Audio,
                            "mp4" or "m4a" or "mkv" => MediaTypeEnum.Video,
                            _ => throw new Exception("Not allowed file"),
                        };
                        var mediaDir = GetPath(post.AuthorId, post);
                        var filePath = Path.Combine(rootDir, mediaDir, formFile.FileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await formFile.CopyToAsync(stream);
                        }
                        postMedia.Add(new PostMedia
                        {
                            Path = $"{mediaDir}/{formFile.FileName}",
                            PostId = post.Id,
                            MediaType = mediaType,
                        });
                    }
                }
                await dbContext.PostMedia.AddRangeAsync(postMedia);
                await dbContext.SaveChangesAsync();
            }
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get post");
            return e.Message;
        }
    }

    public async Task<string?> UpdatePostAsync(string slug, UserPost newPost)
    {
        try
        {
            UserPost? oldPost = await dbContext.UserPosts
                .IncludeAll()
                .Where(e => e.Slug == slug)
                .AsTracking().FirstOrDefaultAsync()
                ?? throw new Exception("Invalid Post");
            dbContext.UserPosts.Update(oldPost);
            await dbContext.SaveChangesAsync();
            // Update hashtag from post
            var oldTags = ("" + oldPost.Content).ExtractHashtags();
            var newTags = ("" + newPost.Content).ExtractHashtags();
            var commonTags = oldTags.Intersect(newTags).ToArray();
            if (commonTags.Length != newTags.Length || commonTags.Length != oldTags.Length)
            {
                // Save new post hashtags
                newTags = [.. newTags.Except(commonTags)];
                var postTags = new List<PostHashtag>();
                foreach (string tag in newTags)
                {
                    Hashtag? hashtag = await dbContext.Hashtags.Where(e => e.Tag == tag).AsTracking().FirstOrDefaultAsync();
                    if (hashtag == null)
                    {
                        hashtag = new Hashtag { Tag = tag.Replace("#", "") };
                        await dbContext.Hashtags.AddAsync(hashtag);
                        await dbContext.SaveChangesAsync();
                    }
                    postTags.Add(new() { HashtagId = hashtag.Id, PostId = newPost.Id });
                }
                await dbContext.PostHashtags.AddRangeAsync(postTags);
                await dbContext.SaveChangesAsync();
                // Delete old post hashtags
                oldTags = [.. oldTags.Except(commonTags)];
                foreach (string tag in oldTags)
                {
                    Hashtag? hashtag = await dbContext.Hashtags.Where(e => e.Tag == tag).AsTracking().FirstOrDefaultAsync();
                    if (hashtag == null) continue;
                    await dbContext.PostHashtags.Where(e => e.HashtagId == hashtag.Id && e.PostId == newPost.Id).AsTracking().ExecuteDeleteAsync();
                }
            }
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get post");
            return e.Message;
        }
    }

    public async Task<string?> UpdateMediaPostAsync(string slug, string rootDir, List<IFormFile>? newMedia, List<int>? deletedMediaIds)
    {
        try
        {
            // Add new media from post
            var error = await CreateMediaPostAsync(slug, rootDir, newMedia);
            if (!string.IsNullOrEmpty(error)) throw new Exception(error);
            // Deleted old media from post
            if (deletedMediaIds != null && deletedMediaIds.Count > 0)
            {
                var deletedMedia = await dbContext.PostMedia.Where(e => deletedMediaIds.Contains(e.Id)).AsTracking().ToListAsync();
                foreach (var media in deletedMedia)
                {
                    var filePath = Path.Combine(rootDir, media.Path);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    await dbContext.PostMedia.Where(e => e.Id == media.Id).AsTracking().ExecuteDeleteAsync();
                }
            }

            // if (newMedia != null && newMedia.Count > 0)
            // {
            //     var postMedia = new List<PostMedia>();
            //     foreach (var formFile in newMedia)
            //     {
            //         if (formFile.Length > 0)
            //         {
            //             var fileExtension = ("" + formFile.FileName).ToLower().Split('.')[^1];
            //             var mediaType = fileExtension switch
            //             {
            //                 "jpg" or "png" or "jpeg" => MediaTypeEnum.Image,
            //                 "mp3" or "wav" or "ogg" => MediaTypeEnum.Audio,
            //                 "mp4" or "m4a" or "mkv" => MediaTypeEnum.Video,
            //                 _ => throw new Exception("Not allowed file"),
            //             };
            //             var mediaDir = GetPath(post.AuthorId, post);
            //             var filePath = Path.Combine(rootDir, mediaDir, formFile.FileName);
            //             using (var stream = new FileStream(filePath, FileMode.Create))
            //             {
            //                 await formFile.CopyToAsync(stream);
            //             }
            //             postMedia.Add(new PostMedia
            //             {
            //                 Path = $"{mediaDir}/{formFile.FileName}",
            //                 PostId = post.Id,
            //                 MediaType = mediaType,
            //             });
            //         }
            //     }
            //     await dbContext.PostMedia.AddRangeAsync(postMedia);
            //     await dbContext.SaveChangesAsync();
            // }

            return null;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    public async Task<string?> DeletePostAsync(int postId, DateTime DeletedAt, int? userId = null)
    {
        try
        {
            var post = await dbContext.UserPosts
                .Include(e => e.Group)
                .Where(e => e.Id == postId).AsTracking().FirstOrDefaultAsync()
                ?? throw new Exception("Deletion Failed");
            if (userId != post.AuthorId)
            {
                if (post.GroupId == null)
                {
                    throw new Exception("Not allowed to delete this post");
                }
                else if (userId != post.Group!.CreatorId)
                {
                    throw new Exception("Not allowed to delete this post");
                }
            }
            dbContext.Entry(post).CurrentValues.SetValues(new { DeletedAt });
            var result = await dbContext.SaveChangesAsync();
            return result > 0 ? null : throw new Exception("Failed to delete post");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete post");
            return e.Message;
        }
    }

    public async Task<string?> DeletePostBySlugAsync(string slug, DateTime DeletedAt, int? userId = null)
    {
        try
        {
            var post = await dbContext.UserPosts
                .Include(e => e.Group)
                .Where(e => e.Slug == slug).AsTracking().FirstOrDefaultAsync()
                ?? throw new Exception("Deletion Failed");
            if (userId != post.AuthorId)
            {
                if (post.GroupId == null)
                {
                    throw new Exception("Not allowed to delete this post");
                }
                else if (userId != post.Group!.CreatorId)
                {
                    throw new Exception("Not allowed to delete this post");
                }
            }
            dbContext.Entry(post).CurrentValues.SetValues(new { DeletedAt });
            var result = await dbContext.SaveChangesAsync();
            return result > 0 ? null : throw new Exception("Failed to delete post");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete post");
            return e.Message;
        }
    }

    private string GetPath(int userId, UserPost post)
    {
        return Path.Combine(MEDIA_ROOT, "" + userId, post.Slug);
    }
}
