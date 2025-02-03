using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
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
    public class ReactionController : ControllerBase
    {
        private readonly ILogger<ReactionController> _logger;
        private readonly SocialMediaDatabaseContext dbContext;
        private readonly ITokenService _tokenService;

        public ReactionController(ILogger<ReactionController> logger, ITokenService tokenService, SocialMediaDatabaseContext context)
        {
            _logger = logger;
            dbContext = context;
            _tokenService = tokenService;
        }

        [Authorize]
        [HttpPost("post/{Slug}/{Reaction}")]
        public async Task<IResult> ReactPost(string Slug, ReactionTypeEnum Reaction)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    UserPost? userPost = await dbContext.UserPosts.Where(e => e.Slug == Slug).AsNoTracking().FirstOrDefaultAsync();
                    if (userPost == null) return Results.NotFound();
                    PostReaction? postReaction = await dbContext.PostReactions.Where(e => e.UserId == currentUser.Id && e.PostId == userPost.Id).FirstOrDefaultAsync();
                    if (postReaction == null)
                    {
                        postReaction = new() { PostId = userPost.Id, UserId = currentUser.Id, Type = Reaction };
                        dbContext.PostReactions.Add(postReaction);
                    }
                    else
                    {
                        dbContext.Entry(postReaction).CurrentValues.SetValues(new PostReaction
                        {
                            PostId = postReaction.Id,
                            UserId = currentUser.Id,
                            Type = Reaction,
                            UpdatedAt = DateTime.Now,
                        });
                    }
                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new ResponseData<object>("Successfully Reacted"));
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
        [HttpDelete("post/{Id}/")]
        public async Task<IResult> DeletePost(int Id)
        {
            try
            {
                var DeletedAt = DateTime.Now;
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    // var reaction = await dbContext.PostReactions
                    //     .Where(e => e.Id == Id).AsTracking().FirstOrDefaultAsync()
                    //     ?? throw new Exception("Deletion Failed");
                    // dbContext.Entry(reaction).CurrentValues.SetValues(new { DeletedAt });
                    // await dbContext.SaveChangesAsync();
                    await dbContext.PostReactions.Where(e => e.Id == Id).AsTracking().ExecuteDeleteAsync();
                    return Results.Ok(new ResponseData<object>("Reaction Deleted Successfully"));
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
        [HttpPost("comment/{Id}/{Reaction}")]
        public async Task<IResult> ReactComment(int Id, ReactionTypeEnum Reaction)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    PostComment? postComment = await dbContext.PostComments.Where(e => e.Id == Id).AsNoTracking().FirstOrDefaultAsync();
                    if (postComment == null) return Results.NotFound();
                    CommentReaction? commentReaction = await dbContext.CommentReactions.Where(e => e.UserId == currentUser.Id && e.CommentId == postComment.Id).FirstOrDefaultAsync();
                    if (commentReaction == null)
                    {
                        commentReaction = new() { CommentId = postComment.Id, UserId = currentUser.Id, Type = Reaction };
                        dbContext.CommentReactions.Add(commentReaction);
                    }
                    else
                    {
                        dbContext.Entry(commentReaction).CurrentValues.SetValues(new PostReaction
                        {
                            PostId = commentReaction.Id,
                            UserId = currentUser.Id,
                            Type = Reaction,
                            UpdatedAt = DateTime.Now,
                        });
                    }
                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new ResponseData<object>("Successfully Reacted"));
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
        [HttpDelete("comment/{Id}/")]
        public async Task<IResult> DeleteComment(int Id)
        {
            try
            {
                var DeletedAt = DateTime.Now;
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    await dbContext.CommentReactions.Where(e => e.Id == Id).AsTracking().ExecuteDeleteAsync();
                    return Results.Ok(new ResponseData<object>("Reaction Deleted Successfully"));
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
