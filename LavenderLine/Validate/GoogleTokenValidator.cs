using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LavenderLine.Validate
{
    public class GoogleTokenValidator
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static JsonWebKeySet _jwks = new JsonWebKeySet();
        private static DateTime _lastFetch = DateTime.MinValue;

        public async Task<ClaimsPrincipal> ValidateToken(string idToken, string clientId)
        {
            if (DateTime.UtcNow - _lastFetch > TimeSpan.FromHours(1))
            {
                var keysResponse = await _httpClient.GetStringAsync(
                    "https://www.googleapis.com/oauth2/v3/certs");
                _jwks = new JsonWebKeySet(keysResponse);
                _lastFetch = DateTime.UtcNow;
            }

            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidIssuers = new[] { "https://accounts.google.com", "accounts.google.com" },
                ValidAudience = clientId,
                IssuerSigningKeys = _jwks.Keys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            return handler.ValidateToken(idToken, validationParams, out _);
        }
    }
}
