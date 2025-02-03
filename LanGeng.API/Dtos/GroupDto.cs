using System;
using LanGeng.API.Entities;

namespace LanGeng.API.Dtos;

public record class GroupDto
(
    string Name,
    string Slug,
    byte PrivacyType,
    string? ProfileImage,
    string? Description,
    ResponseUserDto Creator,
    int MemberCount,
    string CreateAt
);