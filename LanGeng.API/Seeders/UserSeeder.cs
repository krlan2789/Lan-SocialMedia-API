using System.Text.RegularExpressions;
using LanGeng.API.Data;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Helper;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Seeders;

public static class UserSeeder
{
    public static async Task Seed(SocialMediaDatabaseContext context)
    {
        if (!context.Users.Any())
        {
            // Create Users
            var emails = new HashSet<string>();
            var usernames = new HashSet<string>();
            while (emails.Count < 128)
            {
                var email = Faker.Internet.Email();
                var username = Regex.Replace("" + email.Split('@')[0], @"[^a-zA-Z0-9_.]", "");
                var success = usernames.Add(username);
                if (success) emails.Add(email);
            }
            var users = Enumerable.Range(0, 128).Select(x =>
            {
                return new User
                {
                    Fullname = Faker.Name.FullName(),
                    Email = emails.ElementAt(x),
                    Username = usernames.ElementAt(x),
                    PasswordHash = "12345678".Hash()
                };
            }).ToList();
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            // Add Account Status and Profiles
            var accountStatuses = new List<UserStatus>();
            var profiles = new List<UserProfile>();
            users = await context.Users.ToListAsync();
            users.ForEach(user =>
            {
                accountStatuses.Add(new UserStatus
                {
                    UserId = user.Id,
                    AccountStatus = user.Id >= 96 ? AccountStatusEnum.Unverified : AccountStatusEnum.Verified
                });
                if (user.Id <= 96)
                {
                    profiles.Add(new UserProfile
                    {
                        UserId = user.Id,
                        Bio = Faker.Lorem.Sentence(),
                        ProfileImage = "",
                        PhoneNumber = Faker.Phone.Number(),
                    });
                }
            });
            context.UserStatuses.AddRange(accountStatuses);
            await context.SaveChangesAsync();
            context.UserProfiles.AddRange(profiles);
            await context.SaveChangesAsync();
        }
    }
}
