using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Helper;
using LanGeng.API.Mapping;
using LanGeng.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class PostController : ControllerBase
    {
        private readonly ILogger<PostController> _logger;
        private readonly SocialMediaDatabaseContext dbContext;
        private readonly TokenService _tokenService;
        private readonly IWebHostEnvironment _environment;
        private readonly string MEDIA_ROOT = "post/media";

        public PostController(ILogger<PostController> logger, TokenService tokenService, SocialMediaDatabaseContext context, IWebHostEnvironment environment)
        {
            _logger = logger;
            _tokenService = tokenService;
            _environment = environment;
            dbContext = context;
            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        [HttpGet("{Slug}")]
        public async Task<IResult> GetBySlug(string Slug)
        {
            try
            {
                UserPost? post = await dbContext.UserPosts
                    .IncludeAll()
                    .Where(e => e.Slug == Slug)
                    .FirstOrDefaultAsync();
                return post == null ? Results.NotFound() : Results.Ok(new ResponseData<UserPostFullDto>("Success", post.ToFullDto()));
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpPost()]
        public async Task<IResult> Create([FromBody] CreateUserPostDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    UserPost? post = null;
                    int tryCount = 0;
                    while (tryCount < 16)
                    {
                        post = dto.ToEntity(currentUser.Id);
                        var posts = await dbContext.UserPosts.Where(e => e.Slug == post.Slug).AsTracking().ToListAsync();
                        tryCount++;
                        if (posts == null || posts.Count <= 0) break;
                        else post = null;
                    }
                    if (post != null) dbContext.UserPosts.Add(post);
                    else throw new Exception("Failed to create post, try again later.");
                    await dbContext.SaveChangesAsync();
                    // Save media from post
                    if (dto.Media != null && dto.Media.Count > 0)
                    {
                        var postMedia = new List<PostMedia>();
                        foreach (var formFile in dto.Media)
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
                                var mediaDir = GetPath(currentUser, post);
                                var filePath = Path.Combine(_environment.WebRootPath, mediaDir, formFile.FileName);
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
                    // Save hashtag from post
                    string[] tags = ("" + dto.Content).ExtractHashtags();
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
                    return Results.Ok(new ResponseData<object>("Post Created Successfully"));
                }
                else
                {
                    return Results.Unauthorized();
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpPost("{Slug}")]
        public async Task<IResult> Update(string Slug, [FromBody] UpdateUserPostDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    UserPost? post = await dbContext.UserPosts
                        .IncludeAll()
                        .Where(e => e.Slug == Slug)
                        .AsTracking().FirstOrDefaultAsync()
                        ?? throw new Exception("Invalid Post");
                    dbContext.UserPosts.Update(post);
                    await dbContext.SaveChangesAsync();
                    // Deleted old media from post
                    if (dto.DeletedMediaIds != null && dto.DeletedMediaIds.Count > 0)
                    {
                        var deletedMedia = await dbContext.PostMedia.Where(e => dto.DeletedMediaIds.Contains(e.Id)).ToListAsync();
                        foreach (var media in deletedMedia)
                        {
                            var filePath = Path.Combine(_environment.WebRootPath, media.Path);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                            await dbContext.PostMedia.Where(e => e.Id == media.Id).ExecuteDeleteAsync();
                        }
                    }
                    // Add new media from post
                    if (dto.NewMedia != null && dto.NewMedia.Count > 0)
                    {
                        var postMedia = new List<PostMedia>();
                        foreach (var formFile in dto.NewMedia)
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
                                var mediaDir = GetPath(currentUser, post);
                                var filePath = Path.Combine(_environment.WebRootPath, mediaDir, formFile.FileName);
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
                    // Update hashtag from post
                    var oldTags = ("" + post.Content).ExtractHashtags();
                    var newTags = ("" + dto.Content).ExtractHashtags();
                    var commonTags = oldTags.Intersect(newTags).ToArray();
                    if (commonTags.Length != newTags.Length || commonTags.Length != oldTags.Length)
                    {
                        // Save new post hashtags
                        newTags = newTags.Except(commonTags).ToArray();
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
                            postTags.Add(new() { HashtagId = hashtag.Id, PostId = post.Id });
                        }
                        await dbContext.PostHashtags.AddRangeAsync(postTags);
                        await dbContext.SaveChangesAsync();
                        // Delete old post hashtags
                        oldTags = oldTags.Except(commonTags).ToArray();
                        foreach (string tag in oldTags)
                        {
                            Hashtag? hashtag = await dbContext.Hashtags.Where(e => e.Tag == tag).AsTracking().FirstOrDefaultAsync();
                            if (hashtag == null) continue;
                            await dbContext.PostHashtags.Where(e => e.HashtagId == hashtag.Id && e.PostId == post.Id).ExecuteDeleteAsync();
                        }
                    }
                    return Results.Ok(new ResponseData<object>("Post Updated Successfully"));
                }
                else
                {
                    return Results.Unauthorized();
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpDelete("{Slug}")]
        public async Task<IResult> Delete(string Slug)
        {
            try
            {
                var DeletedAt = DateTime.Now;
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var post = await dbContext.UserPosts
                        .Include(e => e.Group)
                        .Where(e => e.Slug == Slug).AsTracking().FirstOrDefaultAsync()
                        ?? throw new Exception("Deletion Failed");
                    if (currentUser.Id != post.AuthorId)
                    {
                        if (post.GroupId == null)
                        {
                            throw new Exception("Not allowed to delete this post");
                        }
                        else if (currentUser.Id != post.Group!.CreatorId)
                        {
                            throw new Exception("Not allowed to delete this post");
                        }
                    }
                    dbContext.Entry(post).CurrentValues.SetValues(new { DeletedAt });
                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new ResponseData<object>("Post Deleted Successfully"));
                }
                else
                {
                    return Results.Unauthorized();
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [HttpGet()]
        public async Task<IResult> GetPerPage([FromQuery] FilterPostDto filters)
        {
            try
            {
                var tags = ("" + filters.Tags).ToLower().Replace("#", "").Split(",");
                var defaultLimit = 16;
                string keyword = $"%{filters.Keyword}%".ToLower();
                string author = $"%{filters.Author}%".ToLower();
                string group = $"%{filters.Group}%".ToLower();
                var query = dbContext.UserPosts
                    .IncludeAll()
                    .Where(e =>
                        (string.IsNullOrEmpty(filters.Tags) || e.Hashtags.Any(t => tags.Contains(t.Tag))) &&
                        (string.IsNullOrEmpty(filters.Author) || EF.Functions.Like(e.Author!.Username.ToLower(), author) || EF.Functions.Like(e.Author!.Fullname.ToLower(), author)) &&
                        (string.IsNullOrEmpty(filters.Group) || (e.Group != null && (EF.Functions.Like(e.Group.Slug.ToLower(), group) || EF.Functions.Like(e.Group.Name.ToLower(), group)))) &&
                        (
                            string.IsNullOrEmpty(filters.Keyword) || EF.Functions.Like(("" + e.Content).ToLower(), keyword) ||
                            e.Author == null || EF.Functions.Like(e.Author.Username.ToLower(), keyword) || EF.Functions.Like(e.Author.Fullname.ToLower(), keyword) ||
                            e.Group == null || EF.Functions.Like(e.Group.Slug.ToLower(), keyword) || EF.Functions.Like(e.Group.Name.ToLower(), keyword)
                        )
                    );
                var totalPosts = await query.CountAsync();
                var limit = filters.Limit ?? defaultLimit;
                var start = ((filters.Page > 0 ? filters.Page : 1) - 1) * limit;
                var posts = await query
                    .Skip(start)
                    .Take(limit)
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToListAsync();
                var postsDto = posts.Select(e => e.ToDto()).ToList();
                return Results.Ok(new ResponseData<ResponsePostsDto>(
                    "Success",
                    new ResponsePostsDto(
                        filters.Page > 0 ? filters.Page : 1,
                        limit,
                        totalPosts,
                        postsDto
                    )
                ));
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        private string GetPath(User user, UserPost post)
        {
            return Path.Combine(MEDIA_ROOT, "" + user.Id, post.Slug);
        }
    }
}
