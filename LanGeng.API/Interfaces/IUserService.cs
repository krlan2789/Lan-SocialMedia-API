using LanGeng.API.Entities;
using LanGeng.API.Enums;

namespace LanGeng.API.Interfaces;

public interface IUserService
{
    #region User
    public Task<List<User>?> GetAllUsersAsync();
    public Task<User?> GetUserByIdAsync(int userId);
    public Task<User?> GetUserByUsernameAsync(string username);
    public Task<bool> HasEmailAsync(string email);
    public Task<User?> CreateUserAsync(User user, string token);
    public Task<string?> UpdateUserAsync(User user);
    public Task<string?> DeleteUserAsync(int userId, DateTime DeletedAt);
    #endregion User

    #region Profile
    public Task<UserProfile?> GetProfileAsync(string username);
    public Task<string?> CreateProfileAsync(UserProfile profile);
    public Task<string?> UpdateProfileAsync(string username, UserProfile profile);
    #endregion Profile

    #region User Status
    public Task<UserStatus?> GetUserStatusByIdAsync(int userId);
    public Task<string?> CreateUserStatusAsync(UserStatus userStatus);
    public Task<string?> UpdateUserStatusAsync(int userId, AccountStatusEnum status);
    #endregion User Status

    #region User Token
    public Task<string?> CreateUserTokenAsync(int userId, string token);
    public Task<string?> DeleteUserTokenAsync(int userId);
    public Task<UserToken?> GetUserTokenByTokenAsync(string token);
    #endregion User Token

    #region Verification with Code
    public Task<UserVerification?> GetVerificationCodeAsync(int userId, string code, VerificationTypeEnum? type);
    public Task<string?> UpdateVerificationCodeAsync(UserVerification verificationToken, DateTime verifiedAt);
    #endregion Verification with Code

    #region Verification with Token
    public Task<UserVerificationToken?> GetVerificationTokenByTokenAsync(int userId, string token, VerificationTypeEnum? type);
    public Task<string?> CreateVerificationTokenAsync(int userId, string token, VerificationTypeEnum type);
    public Task<string?> UpdateVerificationTokenAsync(UserVerificationToken userVerification, DateTime verifiedAt);
    #endregion Verification with Token
}
