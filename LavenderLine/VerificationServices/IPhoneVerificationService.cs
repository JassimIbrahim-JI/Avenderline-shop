namespace LavenderLine.VerificationServices
{
    public interface IPhoneVerificationService
    {
        Task<string> SendVerificationCodeAsync(string phoneNumber);
        bool VerifyCode(string phoneNumber, string verificationCode);
    }
}
