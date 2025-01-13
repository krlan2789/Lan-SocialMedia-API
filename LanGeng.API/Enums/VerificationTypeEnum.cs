namespace LanGeng.API.Enums;

public enum VerificationTypeEnum : byte
{
    Register = 1,
    UsernameChanges = 10,
    PasswordChanges = 20,
    PasswordReset = 21,
    AccountDeactivation = 40,
    AccountDeletion = 41,
}
