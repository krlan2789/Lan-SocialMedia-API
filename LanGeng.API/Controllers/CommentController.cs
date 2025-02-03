using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Interfaces;
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
        private readonly ITokenService _tokenService;

        public CommentController(ILogger<ReactionController> logger, ITokenService tokenService, SocialMediaDatabaseContext context)
        {
            _logger = logger;
            dbContext = context;
            _tokenService = tokenService;
        }

        [Authorize]
        [HttpPost()]
        public async Task<IResult> Create(CreatePostCommentDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    UserPost? userPost = await dbContext.UserPosts.Where(up => up.Slug == dto.Slug).AsNoTracking().FirstOrDefaultAsync();
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

        [Authorize]
        [HttpDelete("{Id}")]
        public async Task<IResult> Delete(int Id)
        {
            try
            {
                var DeletedAt = DateTime.Now;
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var comment = await dbContext.PostComments
                        .Where(e => e.Id == Id).AsTracking().FirstOrDefaultAsync()
                        ?? throw new Exception("Deletion Failed");
                    dbContext.Entry(comment).CurrentValues.SetValues(new { DeletedAt });
                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new ResponseData<object>("Comment Deleted Successfully"));
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
