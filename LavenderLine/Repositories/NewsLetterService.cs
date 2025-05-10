using LavenderLine.Enums.NewsLetter;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LavenderLine.Repositories
{
    public interface INewsLetterService
    {
        Task<SubscriptionResult> SubscribeAsync(string email);
        Task<UnsubscribeResult> UnsubscribeAsync(string email,string token);
   
    }

    public class NewsLetterService : INewsLetterService
    {
        private readonly EcommerceContext _context;

        public NewsLetterService(EcommerceContext context)
        {
            _context = context;
        }

        public async Task<SubscriptionResult> SubscribeAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            var existing = await _context.Newsletters
                .FirstOrDefaultAsync(n => n.Email == normalizedEmail);

            if (existing != null)
            {
                if (existing.IsActive)
                    return SubscriptionResult.AlreadyExists;

                return await ReactivateSubscription(existing);
            }

            var subscription = new NewsletterSubscription
            {
                Email = normalizedEmail,
                SubscribedOn = QatarDateTime.Now
            };

            _context.Newsletters.Add(subscription);
            await _context.SaveChangesAsync();

            return SubscriptionResult.Success;
        }

        public async Task<UnsubscribeResult> UnsubscribeAsync(string email, string token)
        {
            var normalizedEmail = email.Trim().ToLower();

            var subscription = await _context.Newsletters
                .FirstOrDefaultAsync(n =>
                    n.Email == normalizedEmail &&
                    n.UnsubscribeToken == token);

            if (subscription == null)
                return UnsubscribeResult.NotFound;

            if (!subscription.IsTokenValid)
                return UnsubscribeResult.TokenExpired;

            subscription.IsActive = false;
            subscription.UnsubscribedOn = QatarDateTime.Now;
            await _context.SaveChangesAsync();

            return UnsubscribeResult.Success;
        }

        private async Task<SubscriptionResult> ReactivateSubscription(NewsletterSubscription subscription)
        {
            if (subscription.IsTokenValid)
            {
                subscription.IsActive = true;
                await _context.SaveChangesAsync();
                return SubscriptionResult.Reactivated;
            }

            // Generate new token if expired
            subscription.UnsubscribeToken = NewsletterSubscription.GenerateNewToken();
            subscription.TokenExpiration = QatarDateTime.Now.Plus(Duration.FromDays(90));
            subscription.IsActive = true;
            await _context.SaveChangesAsync();

            return SubscriptionResult.ReactivatedWithNewToken;
        }
    }

}