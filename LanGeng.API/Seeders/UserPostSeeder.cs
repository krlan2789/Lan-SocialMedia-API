using LanGeng.API.Data;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Helper;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Seeders;

public class UserPostSeeder
{
    public static async Task Seed(SocialMediaDatabaseContext context)
    {
        if (!context.UserPosts.Any())
        {
            // Create Posts
            var allPosts = new List<UserPost>();
            var usersCount = context.Users.Count();
            var userIds = new HashSet<int>();
            while (userIds.Count <= 64)
            {
                userIds.Add(Faker.RandomNumber.Next(1, usersCount > 96 ? 96 : usersCount));
            }
            var users = await context.Users.Where(user => userIds.Contains(user.Id)).ToListAsync();
            users.ForEach(user =>
            {
                var postCount = Faker.RandomNumber.Next(0, 40) % 5;
                var posts = Enumerable.Range(0, postCount).Select(x =>
                {
                    var content = Faker.Lorem.Sentence(Faker.RandomNumber.Next(1, 32));
                    return new UserPost
                    {
                        AuthorId = user.Id,
                        Content = content,
                        Slug = SlugHelper.Create(content),
                    };
                }).ToList();
                allPosts.AddRange(posts);
            });
            context.UserPosts.AddRange(allPosts);
            await context.SaveChangesAsync();

            // Create Comments
            var allComments = new List<PostComment>();
            var postsCount = context.UserPosts.Count();
            var postIds = new List<int>();
            var limitCount = postsCount >= 128 ? 96 : postsCount;
            while (postIds.Count < limitCount)
            {
                postIds.Add(Faker.RandomNumber.Next(1, postsCount));
            }
            var posts = await context.UserPosts.Where(post => postIds.Contains(post.Id)).ToListAsync();
            posts.ForEach(post =>
            {
                var commentCount = Faker.RandomNumber.Next(0, 80) % 8;
                var comments = Enumerable.Range(0, commentCount).Select(x =>
                {
                    var content = Faker.Lorem.Sentence(Faker.RandomNumber.Next(1, 4));
                    return new PostComment
                    {
                        Content = content,
                        PostId = post.Id,
                        UserId = Faker.RandomNumber.Next(1, usersCount),
                    };
                }).ToList();
                allComments.AddRange(comments);
            });
            context.PostComments.AddRange(allComments);
            await context.SaveChangesAsync();

            // Create Reactions
            var allReactions = new List<PostReaction>();
            var reactionIds = new List<int>();
            limitCount = postsCount >= 168 ? 128 : postsCount;
            postIds.Clear();
            while (postIds.Count < limitCount)
            {
                postIds.Add(Faker.RandomNumber.Next(1, postsCount));
            }
            posts = await context.UserPosts.Where(post => postIds.Contains(post.Id)).ToListAsync();
            posts.ForEach(post =>
            {
                var reactionCount = Faker.RandomNumber.Next(0, 80) % 32;
                var reaction = Enumerable.Range(0, reactionCount).Select(x =>
                {
                    return new PostReaction
                    {
                        PostId = post.Id,
                        UserId = Faker.RandomNumber.Next(1, usersCount),
                        Type = ReactionTypeEnum.Like,
                    };
                }).ToList();
                allReactions.AddRange(reaction);
            });
            context.PostReactions.AddRange(allReactions);
            await context.SaveChangesAsync();
        }
    }
}
