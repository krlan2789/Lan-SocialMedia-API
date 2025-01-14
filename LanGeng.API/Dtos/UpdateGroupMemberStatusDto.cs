using LanGeng.API.Enums;

namespace LanGeng.API.Dtos;

public record class UpdateGroupMemberStatusDto
(
    GroupMemberStatusEnum Status,
    int MemberId
);