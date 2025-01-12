using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class CommentController : ControllerBase
    {
        private readonly ILogger<ReactionController> _logger;
        private readonly SocialMediaDatabaseContext dbContext;
        private readonly TokenService _tokenService;

        public CommentController(ILogger<ReactionController> logger, TokenService tokenService, SocialMediaDatabaseContext context)
        {
            _logger = logger;
            dbContext = context;
            _tokenService = tokenService;
        }

        [Authorize]
        [HttpPost("post/{Slug}")]
        public async Task<IResult> CreatePostComment(string Slug, CreatePostCommentDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    UserPost? userPost = await dbContext.UserPosts.Where(up => up.Slug == Slug).AsNoTracking().FirstOrDefaultAsync();
                    if (userPost == null) return Results.NotFound();
                    PostComment comment = new() { PostId = userPost.Id, UserId = currentUser.Id, Content = dto.Content };
                    if (dto.ReplyId != null) comment.ReplyId = dto.ReplyId;
                    dbContext.PostComments.Add(comment);
                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new ResponseData<UserPostDto>("Successfully Reacted"));
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
    }
}
