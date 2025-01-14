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
                        .Where(e => e.Slug == Slug && e.Members != null && e.Members.Any(u => u.MemberId == currentUser.Id))
                        .FirstOrDefaultAsync();
                    return group == null ? Results.NotFound() : Results.Ok(new ResponseData<GroupDto>("Success", group.ToDto()));
                }
                else
                {
                    Group? group = await dbContext.Groups
                        .IncludeAll()
                        .Where(e => e.Slug == Slug && e.PrivacyType != PrivacyTypeEnum.Private)
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
                        var groups = await dbContext.Groups.Where(e => e.Slug == group.Slug).AsTracking().ToListAsync();
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
                var DeletedAt = DateTime.Now;
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var group = await dbContext.Groups
                        .Where(e => e.Slug == Slug && e.CreatorId == currentUser.Id)
                        .AsTracking().FirstOrDefaultAsync()
                        ?? throw new Exception("Deletion Failed");
                    dbContext.Entry(group).CurrentValues.SetValues(new { DeletedAt });
                    var posts = await dbContext.UserPosts
                        .Where(e => e.GroupId != null && e.GroupId == group.Id)
                        .AsTracking().ToListAsync() ?? [];
                    foreach (var post in posts)
                    {
                        dbContext.Entry(post).CurrentValues.SetValues(new { DeletedAt });
                    }
                    await dbContext.SaveChangesAsync();
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
        [HttpPost("join/{Slug}")]
        public async Task<IResult> RequestJoin(string Slug)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var group = await dbContext.Groups.Where(e => e.Slug == Slug && e.PrivacyType != PrivacyTypeEnum.Private).FirstOrDefaultAsync() ?? throw new Exception("Can't join to this group");
                    var member = await dbContext.GroupMembers.Where(e => e.GroupId == group.Id && e.MemberId == currentUser.Id).FirstOrDefaultAsync();
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
        [HttpPatch("join/{Slug}")]
        public async Task<IResult> UpdateMemberStatus(string Slug, [FromBody] UpdateGroupMemberStatusDto dto)
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    var group = await dbContext.Groups
                        .IncludeAll().Where(e => e.Slug == Slug)
                        .FirstOrDefaultAsync() ?? throw new Exception("Group not found");
                    if (group.CreatorId != currentUser.Id &&
                        dto.Status != GroupMemberStatusEnum.Left &&
                        dto.Status != GroupMemberStatusEnum.Request)
                        throw new Exception("You are not an admin");
                    var member = await dbContext.GroupMembers
                        .Where(e => e.MemberId == dto.MemberId && e.Status == GroupMemberStatusEnum.Request)
                        .FirstOrDefaultAsync() ?? throw new Exception("Member request not available");
                    dbContext.Entry(member).CurrentValues.SetValues(new
                    {
                        Status = dto.Status,
                        UpdatedAt = DateTime.Now,
                        JoinedAt = dto.Status == GroupMemberStatusEnum.Approved ? DateTime.Now : member.JoinedAt,
                    });
                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new ResponseData<object>("Updated Successfully"));
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
