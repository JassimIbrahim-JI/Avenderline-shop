namespace LavenderLine.Extensions
{
    public static class HttpRequestExtensions
    {

        private const string NotificationCountKey = "NotificationCount";

        public static void IncrementNotificationCount(this ISession session)
        {
            int currentCount = session.GetInt32(NotificationCountKey) ?? 0;
            session.SetInt32(NotificationCountKey, currentCount + 1);

        }

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
