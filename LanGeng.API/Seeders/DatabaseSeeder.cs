using LanGeng.API.Data;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Seeders;

public static class DatabaseSeeder
{
    public static async Task Seed(IServiceProvider serviceProvider)
    {
        using var context = new SocialMediaDatabaseContext(serviceProvider.GetRequiredService<DbContextOptions<SocialMediaDatabaseContext>>());
        if (context.Database.GetPendingMigrations().Count() > 0)
        {
            context.Database.EnsureDeleted();
            await context.Database.MigrateAsync();
        }
        await UserSeeder.Seed(context);
        await UserPostSeeder.Seed(context);
    }
}
