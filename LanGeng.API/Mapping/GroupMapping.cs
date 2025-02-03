using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Helper;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Mapping;

public static class GroupMapping
{
    public static Group ToEntity(this CreateGroupDto dto, int creatorId)
    {
        return new Group
        {
            Name = dto.Name,
            Slug = SlugHelper.Create(dto.Name),
            CreatorId = creatorId,
            PrivacyType = dto.PrivacyType ?? PrivacyTypeEnum.Public,
            Description = dto.Description,
            ProfileImage = dto.ProfileImage
        };
    }

    public static GroupDto ToDto(this Group group)
    {
        return new GroupDto
        (
            group.Name,
            group.Slug,
            (byte)group.PrivacyType,
            group.ProfileImage,
            group.Description,
            group.Creator!.ToResponseDto(),
            group.Members?.Count ?? 0,
            group.CreatedAt.ToString()
        );
    }

    public static IQueryable<Group> IncludeAll(this DbSet<Group> group)
    {
        return group.Include(g => g.Creator).Include(g => g.Members);
    }
}
