using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReactionController : ControllerBase
    {
        private readonly ILogger<ReactionController> _logger;
        private readonly SocialMediaDatabaseContext dbContext;
        private readonly TokenService _tokenService;

        public ReactionController(ILogger<ReactionController> logger, TokenService tokenService, SocialMediaDatabaseContext context)
        {
            _logger = logger;
            dbContext = context;
            _tokenService = tokenService;
        }

        [Authorize]
        [HttpPost("post/{Slug}/{Reaction}", Name = nameof(ReactPost))]
        public async Task<IResult> ReactPost(string Slug, ReactionTypeEnum Reaction)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    UserPost? userPost = await dbContext.UserPosts.Where(up => up.Slug == Slug).FirstOrDefaultAsync();
                    if (userPost == null) return Results.NotFound();
                    PostReaction? postReaction = await dbContext.PostReactions.Where(pr => pr.UserId == currentUser.Id && pr.PostId == userPost.Id).FirstOrDefaultAsync();
                    if (postReaction == null)
                    {
                        postReaction = new() { PostId = userPost.Id, UserId = currentUser.Id, Type = Reaction };
                        dbContext.PostReactions.Add(postReaction);
                    }
                    else
                    {
                        dbContext.Entry(postReaction).CurrentValues.SetValues(new PostReaction { PostId = postReaction.Id, UserId = currentUser.Id, Type = Reaction });
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
        [HttpPost("comment/{Id}/{Reaction}", Name = nameof(ReactComment))]
        public async Task<IResult> ReactComment(int Id, ReactionTypeEnum Reaction)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    PostComment? postComment = await dbContext.PostComments.Where(up => up.Id == Id).FirstOrDefaultAsync();
                    if (postComment == null) return Results.NotFound();
                    CommentReaction? commentReaction = await dbContext.CommentReactions.Where(pr => pr.UserId == currentUser.Id && pr.CommentId == postComment.Id).FirstOrDefaultAsync();
                    if (commentReaction == null)
                    {
                        commentReaction = new() { CommentId = postComment.Id, UserId = currentUser.Id, Type = Reaction };
                        dbContext.CommentReactions.Add(commentReaction);
                    }
                    else
                    {
                        dbContext.Entry(commentReaction).CurrentValues.SetValues(new PostReaction { PostId = commentReaction.Id, UserId = currentUser.Id, Type = Reaction });
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
    }
}
