using System.Security.Claims;

namespace LavenderLine.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal user, HttpContext context)
        {
            if (user.Identity?.IsAuthenticated == true)
            {
                return user.FindFirstValue(ClaimTypes.NameIdentifier);
            }


            context.Session.TryGetValue("__dummy", out _);

            var sessionUserId = context.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId))
            {
                sessionUserId = Guid.NewGuid().ToString();
                context.Session.SetString("UserId", sessionUserId);
            }
            return sessionUserId;
        }

    }
}
