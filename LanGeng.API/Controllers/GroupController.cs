using System.ComponentModel;
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
    public class GroupController : ControllerBase
    {
        private readonly ILogger<GroupController> _logger;
        private readonly SocialMediaDatabaseContext dbContext;
        private readonly TokenService _tokenService;

        public GroupController(ILogger<GroupController> logger, TokenService tokenService, SocialMediaDatabaseContext context)
        {
            _logger = logger;
            dbContext = context;
            _tokenService = tokenService;
            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        [AllowAnonymous]
        [HttpGet("{Slug}")]
        public async Task<IResult> GetBySlug(string Slug)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    Group? group = await dbContext.Groups
                        .IncludeAll()
                        .Where(g => g.Slug == Slug && g.Members != null && g.Members.Any(u => u.MemberId == currentUser.Id))
                        .FirstOrDefaultAsync();
                    return group == null ? Results.NotFound() : Results.Ok(new ResponseData<GroupDto>("Success", group.ToDto()));
                }
                else
                {
                    Group? group = await dbContext.Groups
                        .IncludeAll()
                        .Where(g => g.Slug == Slug && g.PrivacyType != PrivacyTypeEnum.Private)
                        .FirstOrDefaultAsync();
                    return group == null ? Results.NotFound() : Results.Ok(new ResponseData<GroupDto>("Success", group.ToDto()));
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpPost()]
        public async Task<IResult> Create(CreateGroupDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    Group? group = null;
                    int tryCount = 0;
                    while (tryCount < 16)
                    {
                        group = dto.ToEntity(currentUser.Id);
                        var groups = await dbContext.Groups.Where(g => g.Slug == group.Slug).AsTracking().ToListAsync();
                        tryCount++;
                        if (groups == null || groups.Count <= 0) break;
                        else group = null;
                    }
                    if (group != null) dbContext.Groups.Add(group);
                    else throw new Exception("Failed to create group, try again later.");
                    await dbContext.SaveChangesAsync();
                    GroupMember member = new()
                    {
                        Slug = SlugHelper.Create($"{group.Name}-{currentUser.Username}"),
                        Status = GroupMemberStatusEnum.Approved,
                        GroupId = group.Id,
                        MemberId = currentUser.Id,
                        JoinedAt = DateTime.Now,
                    };
                    dbContext.GroupMembers.Add(member);
                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new ResponseData<UserPostDto>("Group Created Successfully"));
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
                    await dbContext.Groups.Where(g => g.Slug == Slug).AsTracking().ExecuteDeleteAsync();
                    return Results.Ok(new ResponseData<UserPostDto>("Deleted Successfully"));
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
        [HttpPost("g/{Slug}")]
        public async Task<IResult> RequestJoin(string Slug)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var group = await dbContext.Groups.Where(g => g.Slug == Slug && g.PrivacyType != PrivacyTypeEnum.Private).FirstOrDefaultAsync() ?? throw new Exception("Can't join to this group");
                    var member = await dbContext.GroupMembers.Where(gm => gm.GroupId == group.Id && gm.MemberId == currentUser.Id).FirstOrDefaultAsync();
                    if (member != null) throw new Exception("You have already requested or joined to this group");
                    member = new()
                    {
                        Slug = SlugHelper.Create($"{group.Name}-{currentUser.Username}"),
                        Status = GroupMemberStatusEnum.Request,
                        MemberId = currentUser.Id,
                        GroupId = group.Id,
                    };
                    dbContext.GroupMembers.Add(member);
                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new ResponseData<UserPostDto>("Requested Successfully"));
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
        [HttpPatch("g/{Slug}/{Status}")]
        public async Task<IResult> UpdateMemberStatus(string Slug, GroupMemberStatusEnum Status)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var member = await dbContext.GroupMembers.Where(gm => gm.Slug == Slug && gm.Status == GroupMemberStatusEnum.Request).FirstOrDefaultAsync() ?? throw new Exception("Member request not available");
                    dbContext.Entry(member).CurrentValues.SetValues(new { Status = Status, UpdatedAt = DateTime.Now, JoinedAt = Status == GroupMemberStatusEnum.Approved ? (DateTime?)DateTime.Now : null });
                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new ResponseData<UserPostDto>("Updated Successfully"));
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
