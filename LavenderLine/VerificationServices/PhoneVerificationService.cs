using LavenderLine.Settings;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LavenderLine.VerificationServices
{
    public class PhoneVerificationService : IPhoneVerificationService
    {

        private static readonly Random R = new Random();
        private static readonly Dictionary<string, string> verificationCodes = new Dictionary<string, string>();

        private readonly PhoneSettings _settings;

        public PhoneVerificationService(IOptions<PhoneSettings> option)
        {
            _settings = option.Value;
            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
        }

        public async Task<string> SendVerificationCodeAsync(string phoneNumber)
        {
            var code = R.Next(100000, 999999).ToString();
            verificationCodes[phoneNumber] = code;


            await SendSms(phoneNumber, code);

            return code;
        }

        public bool VerifyCode(string phoneNumber, string verificationCode)
        {
            if (verificationCodes.TryGetValue(phoneNumber, out var code) && code == verificationCode)
            {
                verificationCodes.Remove(phoneNumber); // Clear the code after verification
                return true;
            }

            return false;
        }

        private async Task SendSms(string phoneNumber, string code)
        {
            var message = await MessageResource.CreateAsync(
                 body: $"Your verification code is: {code}",
                 from: new PhoneNumber(_settings.PhoneNumber),
                 to: new PhoneNumber(phoneNumber)
                 );
        }

    }
}
