using LanGeng.API.Dtos;
using LanGeng.API.Entities;

namespace LanGeng.API.Mapping;

public static class PostReactionMapping
{
    public static PostReactionDto ToDto(this PostReaction reaction)
    {
        return new PostReactionDto
        (
            reaction.Id,
            reaction.Post!.Slug,
            "" + reaction.User?.Username,
            "" + reaction.User?.Fullname,
            (byte)reaction.Type
        );
    }
}
