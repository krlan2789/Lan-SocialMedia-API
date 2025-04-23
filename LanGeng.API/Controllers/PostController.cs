using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Interfaces;
using LanGeng.API.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanGeng.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class PostController : ControllerBase
    {
        private readonly ILogger<PostController> _logger;
        private readonly IPostService _postService;
        private readonly ITokenService _tokenService;
        private readonly IWebHostEnvironment _environment;

        public PostController(ILogger<PostController> logger, ITokenService tokenService, IPostService postService, IWebHostEnvironment environment)
        {
            _logger = logger;
            _tokenService = tokenService;
            _environment = environment;
            _postService = postService;
        }

        [HttpGet("{Slug}")]
        [EndpointSummary("Get post by Slug")]
        [EndpointDescription("Get post by Slug.")]
        [ProducesResponseType<ResponseData<UserPostFullDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status404NotFound)]
        public async Task<IResult> GetBySlug(string Slug)
        {
            try
            {
                UserPost? post = await _postService.GetPostBySlugAsync(Slug);
                return post == null ?
                    Results.NotFound(new ResponseError<object>("No posts found")) :
                    Results.Ok(new ResponseData<UserPostFullDto>("Success", post.ToFullDto()));
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [HttpGet()]
        [EndpointSummary("Get Posts")]
        [EndpointDescription("Get posts with filters and pagination.")]
        [ProducesResponseType<ResponseData<ResponsePostsDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<FilterPostDto>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status404NotFound)]
        public async Task<IResult> GetPerPage([FromQuery] FilterPostDto filters)
        {
            try
            {
                var allTags = ("" + filters.Tags).ToLower();
                var tags = allTags.Contains(',') ? allTags.Replace("#", "").Split(",") : null;
                var result = await _postService.GetPostsAsync(
                    filters.Keyword, filters.Author, filters.Group, tags, filters.Page, filters.Limit
                );
                if (result == null) throw new Exception("No posts found");
                var (posts, totalPosts, page, limit) = result.Value;
                var postsDto = posts.Select(e => e.ToDto()).ToList();
                return postsDto != null && postsDto.Count > 0 ?
                    Results.Ok(new ResponseData<ResponsePostsDto>(
                        "Success",
                        new ResponsePostsDto(page, limit, totalPosts, postsDto)
                    )) :
                    Results.NotFound(new ResponseError<object>("No posts found"));
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<FilterPostDto>(e.Message, filters));
            }
        }

        [Authorize]
        [HttpPost()]
        [EndpointSummary("Create New Post")]
        [EndpointDescription("Create new post.")]
        [ProducesResponseType<ResponseData<UserPostFullDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<CreateUserPostDto>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IResult> Create([FromBody] CreateUserPostDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    UserPost post = dto.ToEntity(currentUser.Id);
                    // Create post
                    var error = await _postService.CreatePostAsync(post);
                    if (!string.IsNullOrEmpty(error)) throw new Exception(error);
                    // Save media from post
                    error = await _postService.CreateMediaPostAsync(post.Slug, _environment.WebRootPath, dto.Media);
                    return string.IsNullOrEmpty(error) ?
                        Results.Ok(new ResponseData<object>("Post Created Successfully")) :
                        Results.BadRequest(new ResponseData<CreateUserPostDto>("Failed to create post", dto));
                }
                else
                {
                    return Results.Unauthorized();
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<CreateUserPostDto>(e.Message, dto));
            }
        }

        [Authorize]
        [HttpPost("{Slug}")]
        [EndpointSummary("Update a Post")]
        [EndpointDescription("Update a post.")]
        [ProducesResponseType<ResponseData<UserPostFullDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<UpdateUserPostDto>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IResult> Update(string Slug, [FromBody] UpdateUserPostDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var post = dto.ToEntity(Slug, currentUser.Id);
                    var error = await _postService.UpdatePostAsync(Slug, post);
                    if (!string.IsNullOrEmpty(error)) throw new Exception(error);
                    error = await _postService.UpdateMediaPostAsync(Slug, _environment.WebRootPath, dto.NewMedia, dto.DeletedMediaIds);
                    return string.IsNullOrEmpty(error) ?
                        Results.Ok(new ResponseData<object>("Post Updated Successfully")) :
                        Results.BadRequest(new ResponseData<UpdateUserPostDto>("Failed to update post", dto));
                }
                else
                {
                    return Results.Unauthorized();
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<UpdateUserPostDto>(e.Message, dto));
            }
        }

        [Authorize]
        [HttpDelete("{Slug}")]
        [EndpointSummary("Delete a Post")]
        [EndpointDescription("Delete a post by slug and/or author.")]
        [ProducesResponseType<ResponseData<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IResult> Delete(string Slug)
        {
            try
            {
                var DeletedAt = DateTime.Now;
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var error = await _postService.DeletePostBySlugAsync(Slug, DeletedAt, currentUser.Id);
                    if (!string.IsNullOrEmpty(error)) throw new Exception(error);
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
    }
}
