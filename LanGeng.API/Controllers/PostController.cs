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

        [HttpGet("{Slug}")]
        public async Task<IResult> GetBySlug(string Slug)
        {
            try
            {
                UserPost? post = await dbContext.UserPosts
                    .IncludeAll()
                    .Where(up => up.Slug == Slug)
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
        public async Task<IResult> Create(CreateUserPostDto dto)
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
        [HttpDelete("{Slug}")]
        public async Task<IResult> Delete(string Slug)
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
                    .Where(up =>
                        (string.IsNullOrEmpty(filters.Tags) || up.Hashtags.Any(t => tags.Contains(t.Tag))) &&
                        (string.IsNullOrEmpty(filters.Author) || EF.Functions.Like(up.Author!.Username.ToLower(), author) || EF.Functions.Like(up.Author!.Fullname.ToLower(), author)) &&
                        (string.IsNullOrEmpty(filters.Group) || (up.Group != null && (EF.Functions.Like(up.Group.Slug.ToLower(), group) || EF.Functions.Like(up.Group.Name.ToLower(), group)))) &&
                        (
                            string.IsNullOrEmpty(filters.Keyword) || EF.Functions.Like(("" + up.Content).ToLower(), keyword) ||
                            up.Author == null || EF.Functions.Like(up.Author.Username.ToLower(), keyword) || EF.Functions.Like(up.Author.Fullname.ToLower(), keyword) ||
                            up.Group == null || EF.Functions.Like(up.Group.Slug.ToLower(), keyword) || EF.Functions.Like(up.Group.Name.ToLower(), keyword)
                        )
                    );
                var totalPosts = await query.CountAsync();
                var limit = filters.Limit ?? defaultLimit;
                var start = ((filters.Page > 0 ? filters.Page : 1) - 1) * limit;
                var posts = await query
                    .Skip(start)
                    .Take(limit)
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
    }
}
