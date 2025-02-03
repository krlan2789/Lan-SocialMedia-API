using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Helper;
using LanGeng.API.Interfaces;
using LanGeng.API.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class GroupController : ControllerBase
    {
        private readonly ILogger<GroupController> _logger;
        private readonly IGroupService _groupService;
        private readonly ITokenService _tokenService;

        public GroupController(ILogger<GroupController> logger, ITokenService tokenService, IGroupService groupService)
        {
            _logger = logger;
            _tokenService = tokenService;
            _groupService = groupService;
        }

        [AllowAnonymous]
        [HttpGet("{Slug}")]
        [EndpointSummary("Get a group by Slug")]
        [EndpointDescription("Get a group by Slug, unauthorized user only return a public group.")]
        [ProducesResponseType<ResponseData<GroupDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status404NotFound)]
        public async Task<IResult> GetBySlug(string Slug)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                Group? group = null;
                if (currentUser != null)
                {
                    group = await _groupService.GetGroupBySlugWithAllPrivacyAsync(Slug, currentUser.Username);
                    return group == null ?
                        Results.NotFound(new ResponseError<object>("No group found")) :
                        Results.Ok(new ResponseData<GroupDto>("Success", group.ToDto()));
                }
                else
                {
                    group = await _groupService.GetGroupBySlugWithPublicOnlyAsync(Slug);
                    return group == null ?
                        Results.NotFound(new ResponseError<object>("No group found")) :
                        Results.Ok(new ResponseData<GroupDto>("Success", group.ToDto()));
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpPost()]
        [EndpointSummary("Create New Group")]
        [EndpointDescription("Create new Group.")]
        [ProducesResponseType<ResponseData<GroupDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status401Unauthorized)]
        public async Task<IResult> Create([FromBody] CreateGroupDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var error = await _groupService.CreateGroupAsync(dto.ToEntity(currentUser.Id));
                    return string.IsNullOrEmpty(error) ?
                        Results.Ok(new ResponseData<GroupDto>("Group Created Successfully")) :
                        throw new Exception(error);
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
        [EndpointSummary("Delete a Group")]
        [EndpointDescription("Delete a Group.")]
        [ProducesResponseType<ResponseData<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status401Unauthorized)]
        public async Task<IResult> Delete(string Slug)
        {
            try
            {
                var DeletedAt = DateTime.Now;
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var error = await _groupService.DeleteGroupAsync(Slug, currentUser, DeletedAt);
                    return string.IsNullOrEmpty(error) ?
                        Results.Ok(new ResponseData<object>("Delete Successful")) :
                        throw new Exception(error);
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
        [HttpPost("join/{Slug}")]
        [EndpointSummary("Join Group")]
        [EndpointDescription("request to join a group.")]
        [ProducesResponseType<ResponseData<GroupDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status401Unauthorized)]
        public async Task<IResult> RequestJoin(string Slug)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var error = await _groupService.JoinGroupAsync(Slug, currentUser);
                    return Results.Ok(new ResponseData<object>("Request Successful"));
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
        [HttpPatch("join/{Slug}")]
        [EndpointSummary("Update Member Status")]
        [EndpointDescription("Update member status.")]
        [ProducesResponseType<ResponseData<GroupDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status401Unauthorized)]
        public async Task<IResult> UpdateMemberStatus(string Slug, [FromBody] UpdateGroupMemberStatusDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var error = await _groupService.UpdateMemberStatusAsync(Slug, currentUser.Id, dto.MemberId, dto.Status);
                    return Results.Ok(new ResponseData<object>("Update Successful"));
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
