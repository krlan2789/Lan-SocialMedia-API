using LanGeng.API.Data;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Helper;
using LanGeng.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Services;

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private readonly SocialMediaDatabaseContext dbContext;

    public UserService(ILogger<UserService> logger, SocialMediaDatabaseContext context)
    {
        _logger = logger;
        dbContext = context;
        dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    #region User
    public async Task<List<User>?> GetAllUsersAsync()
    {
        try
        {
            return await dbContext.Users.ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get all users");
            return null;
        }
    }

    public async Task<User?> GetUserByIdAsync(int UserId)
    {
        try
        {
            return await dbContext.Users.Where(e => e.Id == UserId).AsTracking().FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get user by username");
            return null;
        }
    }

    public async Task<User?> GetUserByUsernameAsync(string Username)
    {
        try
        {
            return await dbContext.Users.Where(e => e.Username == Username).FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get user by username");
            return null;
        }
    }

    public async Task<bool> HasEmailAsync(string Email)
    {
        try
        {
            return await dbContext.Users.Where(e => e.Email == Email).AsTracking().AnyAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to check email");
            return false;
        }
    }

    public async Task<User?> CreateUserAsync(User User, string Token)
    {
        try
        {
            // Save User verification
            User.VerificationTokens = [new UserVerificationToken
            {
                UserId = User.Id,
                VerificationType = VerificationTypeEnum.Register,
                Email = User.Email,
                Token = SlugHelper.Create(User.Username)
            }];
            // Save User Account Status
            User.AccountStatus = new UserStatus
            {
                UserId = User.Id,
                AccountStatus = AccountStatusEnum.Unverified
            };
            // Generate Token
            User.UserTokens = [new UserToken
            {
                Token = Token,
                UserId = User.Id,
                ExpiresDate = DateTime.Now.Add(TimeSpan.FromDays(30))
            }];
            dbContext.Users.Add(User);
            var result = await dbContext.SaveChangesAsync();
            return result > 0 ? User : throw new Exception("Failed to create user");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create user");
            return null;
        }
    }

    public Task<string?> UpdateUserAsync(User User)
    {
        throw new NotImplementedException();
    }

    public async Task<string?> DeleteUserAsync(int UserId, DateTime DeletedAt)
    {
        try
        {
            var user = await GetUserByIdAsync(UserId) ?? throw new Exception("User not found");
            dbContext.Entry(user).CurrentValues.SetValues(new { DeletedAt });
            await dbContext.SaveChangesAsync();
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete user");
            return e.Message;
        }
    }
    #endregion User

    #region Profile
    public Task<UserProfile?> GetProfileAsync(string Username)
    {
        throw new NotImplementedException();
    }

    public async Task<string?> CreateProfileAsync(UserProfile Profile)
    {
        try
        {
            dbContext.UserProfiles.Add(Profile);
            await dbContext.SaveChangesAsync();
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create profile");
            return e.Message;
        }
    }

    public async Task<string?> UpdateProfileAsync(string Username, UserProfile Profile)
    {
        try
        {
            var user = await GetUserByUsernameAsync(Username) ?? throw new Exception("User not found");
            if (user.Profile == null) throw new Exception("User profile not found");
            dbContext.Entry(user.Profile).CurrentValues.SetValues(Profile);
            var results = await dbContext.SaveChangesAsync();
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update profile");
            return e.Message;
        }
    }
    #endregion Profile

    #region User Status
    public async Task<UserStatus?> GetUserStatusByIdAsync(int UserId)
    {
        try
        {
            return await dbContext.UserStatuses.Where(e => e.UserId == UserId).AsTracking().FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get user status");
            return null;
        }
    }

    public async Task<string?> CreateUserStatusAsync(UserStatus UserStatus)
    {
        try
        {
            var exist = await GetUserStatusByIdAsync(UserStatus.UserId);
            if (exist != null) throw new Exception("User status already exist");
            dbContext.UserStatuses.Add(new UserStatus
            {
                UserId = UserStatus.UserId,
                AccountStatus = AccountStatusEnum.Unverified
            });
            var result = await dbContext.SaveChangesAsync();
            return result > 0 ? null : throw new Exception("Failed to create Account Status");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create Account Status");
            return e.Message;
        }
    }

    public async Task<string?> UpdateUserStatusAsync(int UserId, AccountStatusEnum Status)
    {
        try
        {
            var currentStatus = GetUserStatusByIdAsync(UserId) ?? throw new Exception("User status not found");
            dbContext.Entry(currentStatus).CurrentValues.SetValues(new { AccountStatus = Status, UpdatedAt = DateTime.Now });
            await dbContext.SaveChangesAsync();
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update user status");
            return e.Message;
        }
    }
    #endregion User Status

    #region User Token
    public async Task<UserToken?> GetUserTokenByTokenAsync(string Token)
    {
        try
        {
            return await dbContext.UserTokens
                .Include(e => e.User!)
                .Where(e => e.Token == Token)
                .OrderByDescending(e => e.CreatedAt)
                .AsTracking().FirstAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get token");
            return null;
        }
    }

    public async Task<string?> CreateUserTokenAsync(int UserId, string Token)
    {
        try
        {
            var resultDelete = await DeleteUserTokenAsync(UserId);
            if (resultDelete != null) throw new Exception(resultDelete);
            dbContext.UserTokens.Add(new UserToken
            {
                Token = Token,
                UserId = UserId,
                ExpiresDate = DateTime.Now.Add(TimeSpan.FromDays(30))
            });
            var resultCreate = await dbContext.SaveChangesAsync();
            return resultCreate > 0 ? null : throw new Exception("Failed to create token");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create token");
            return e.Message;
        }
    }

    public async Task<string?> DeleteUserTokenAsync(int UserId)
    {
        try
        {
            var result = await dbContext.UserTokens.Where(e => e.UserId == UserId).AsTracking().ExecuteDeleteAsync();
            return result > 0 ? null : throw new Exception("Failed to delete token");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete token");
            return e.Message;
        }
    }
    #endregion User Token

    #region Verification with Code
    public async Task<UserVerification?> GetVerificationCodeAsync(int UserId, string Code, VerificationTypeEnum? VerifyType)
    {
        try
        {
            return await dbContext.UserVerifications
                .Include(e => e.User!)
                .Where(e => e.UserId == UserId && e.Code == Code && e.VerificationType == VerifyType)
                .OrderByDescending(e => e.CreatedAt)
                .AsTracking().FirstAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get verification code");
            return null;
        }
    }

    public async Task<string?> UpdateVerificationCodeAsync(UserVerification VerificationToken, DateTime VerifiedAt)
    {
        try
        {
            dbContext.Entry(VerificationToken).CurrentValues.SetValues(new { VerifiedAt = VerifiedAt });
            var result = await dbContext.SaveChangesAsync();
            return result > 0 ? null : throw new Exception("Failed to update verification code");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update verification code");
            return e.Message;
        }
    }
    #endregion Verification with Code

    #region Verification with Token
    public async Task<UserVerificationToken?> GetVerificationTokenByTokenAsync(int UserId, string Token, VerificationTypeEnum? VerifyType)
    {
        try
        {
            return await dbContext.UserVerificationTokens
                .Include(e => e.User!)
                .Where(e => e.UserId == UserId && e.Token == Token && e.VerificationType == VerifyType)
                .OrderByDescending(e => e.CreatedAt)
                .AsTracking().FirstAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get verification token");
            return null;
        }
    }

    public async Task<string?> CreateVerificationTokenAsync(int UserId, string Token, VerificationTypeEnum VerifyType)
    {
        try
        {
            var currentUser = await GetUserByIdAsync(UserId) ?? throw new Exception("User not found");
            var userVerifyToken = new UserVerificationToken
            {
                UserId = UserId,
                Email = currentUser.Email,
                Token = SlugHelper.Create(currentUser.Username),
                VerificationType = VerifyType,
            };
            dbContext.UserVerificationTokens.Add(userVerifyToken);
            var results = await dbContext.SaveChangesAsync();
            return results > 0 ? null : throw new Exception("Failed to creating verification link");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create verification token");
            return e.Message;
        }
    }

    public async Task<string?> UpdateVerificationTokenAsync(UserVerificationToken VerificationToken, DateTime VerifiedAt)
    {
        try
        {
            dbContext.Entry(VerificationToken).CurrentValues.SetValues(new { VerifiedAt = VerifiedAt });
            var result = await dbContext.SaveChangesAsync();
            return result > 0 ? null : throw new Exception("Failed to update verification token");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update verification token");
            return e.Message;
        }
    }
    #endregion Verification with Token
}
