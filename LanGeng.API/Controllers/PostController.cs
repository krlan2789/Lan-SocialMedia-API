using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
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

        public PostController(ILogger<PostController> logger, TokenService tokenService, SocialMediaDatabaseContext context)
        {
            _logger = logger;
            dbContext = context;
            _tokenService = tokenService;
            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        [Authorize]
        [HttpPost(Name = nameof(PostCreatePost))]
        public async Task<IResult> PostCreatePost(CreateUserPostDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    UserPost? post = null;
                    int tryCount = 0;
                    while (tryCount < 8)
                    {
                        post = dto.ToEntity(currentUser.Id);
                        var posts = await dbContext.UserPosts.Where(up => up.Slug == post.Slug).AsTracking().ToListAsync();
                        tryCount++;
                        if (posts == null || posts.Count <= 0) break;
                        else post = null;
                    }
                    if (post != null) dbContext.UserPosts.Add(post);
                    else throw new Exception("Failed to create post, try again later.");
                    await dbContext.SaveChangesAsync();
                    // Save hashtag in post
                    string[] tags = ("" + dto.Content).ExtractHashtags();
                    if (tags.Length > 0)
                    {
                        foreach (string tag in tags)
                        {
                            Hashtag? hashtag = await dbContext.Hashtags.Where(h => h.Tag == tag).AsTracking().FirstOrDefaultAsync();
                            if (hashtag == null)
                            {
                                hashtag = new Hashtag { Tag = tag.Replace("#", "") };
                                dbContext.Hashtags.Add(hashtag);
                                await dbContext.SaveChangesAsync();
                            }
                            PostHashtag? postTag = new() { HashtagId = hashtag.Id, PostId = post.Id };
                            dbContext.PostHashtags.Add(postTag);
                            await dbContext.SaveChangesAsync();
                        }
                    }
                    return Results.Ok(new ResponseData<UserPostDto>("Post Created Successfully"));
                }
                else
                {
                    return Results.Unauthorized();
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message, dto));
            }
        }

        [Authorize]
        [HttpDelete("{Slug}", Name = nameof(DeleteCreatePost))]
        public async Task<IResult> DeleteCreatePost(string Slug)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    await dbContext.UserPosts.Where(post => post.Slug == Slug).AsTracking().ExecuteDeleteAsync();
                    return Results.Ok(new ResponseData<UserPostDto>("Post deleted Successfully"));
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

        [HttpGet(Name = nameof(GetPosts))]
        public async Task<IResult> GetPosts([FromQuery] FilterPostDto filters)
        {
            try
            {
                var tags = ("" + filters.Tags).ToLower().Replace("#", "").Split(",");
                var defaultLimit = 16;
                string keyword = $"%{filters.Keyword}%";
                var query = dbContext.UserPosts
                    .Include(up => up.Author)
                    .Include(up => up.Group)
                    .Include(up => up.Reactions)
                    .Include(up => up.Comments)
                    .Include(up => up.Hashtags)
                    .Where(up =>
                        (string.IsNullOrEmpty(filters.Tags) || up.Hashtags.Select(t => tags.Contains(t.Tag)).Count() > 0) &&
                        (string.IsNullOrEmpty(filters.Author) || EF.Functions.Like(up.Author!.Username, filters.Author) || EF.Functions.Like(up.Author!.Fullname, filters.Author)) &&
                        (string.IsNullOrEmpty(filters.Group) || (up.Group != null && (EF.Functions.Like(up.Group.Slug, filters.Group) || EF.Functions.Like(up.Group.Slug, filters.Group)))) &&
                        (
                            string.IsNullOrEmpty(filters.Keyword) || EF.Functions.Like(up.Content, keyword) ||
                            up.Author == null || EF.Functions.Like(up.Author.Username, keyword) || EF.Functions.Like(up.Author.Fullname, keyword) ||
                            up.Group == null || EF.Functions.Like(up.Group.Slug, keyword) || EF.Functions.Like(up.Group.Name, keyword)
                        )
                    );
                var totalPosts = await query.CountAsync();
                var limit = filters.Limit ?? defaultLimit;
                var start = ((filters.Page > 0 ? filters.Page : 1) - 1) * limit;
                var posts = await query
                    .Take(start..(start + limit))
                    .OrderBy(p => p.UpdatedAt)
                    .ToListAsync();
                var postsDto = posts.Select(post => post.ToDto()).ToList();
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

        [HttpGet("{Slug}", Name = nameof(GetPostBySlug))]
        public async Task<IResult> GetPostBySlug(string Slug)
        {
            try
            {
                UserPost? post = await dbContext.UserPosts
                    .Include(up => up.Author)
                    .Include(up => up.Group)
                    .Include(up => up.Reactions)
                    .Include(up => up.Comments)
                    .Include(up => up.Hashtags)
                    .Where(up => up.Slug == Slug)
                    .FirstOrDefaultAsync();
                return post == null ? Results.NotFound() : Results.Ok(new ResponseData<UserPostFullDto>("Success", post.ToFullDto()));
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }
    }
}
