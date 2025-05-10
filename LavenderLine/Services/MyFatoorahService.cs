using LavenderLine.DTO.FatoorahResponses;
using LavenderLine.Exceptions;
using LavenderLine.Settings;
using Microsoft.Extensions.Options;

namespace LavenderLine.Services
{
    public interface IPaymentGatewayService
    {
        Task<MyFatoorahPaymentResponse> CreatePaymentAsync(decimal amount, string orderId);
        Task<MyFatoorahPaymentStatus> GetPaymentStatusAsync(string paymentId);
        Task<MyFatoorahRefundResponse> RefundPaymentAsync(string paymentId, decimal amount);
        Task<bool> VerifyPaymentAsync(string paymentId);

    }

    public class MyFatoorahService : IPaymentGatewayService
    {
        private readonly MyFatoorahSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MyFatoorahService> _logger;
        public MyFatoorahService(IOptions<MyFatoorahSettings> settings, HttpClient httpClient, ILogger<MyFatoorahService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
            _logger = logger;
        }

        public async Task<MyFatoorahPaymentResponse> CreatePaymentAsync(decimal amount, string orderId)
        {
            var request = new
            {
                InvoiceAmount = amount,
                CurrencyIso = "QAR",
                Language = "en",
                CallbackUrl = _settings.SuccessUrl,
                ErrorUrl = _settings.ErrorUrl,
                UserDefinedField = orderId
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v2/ExecutePayment", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MyFatoorahPaymentResponse>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Payment failed");
                throw new PaymentGatewayException("Payment processing failed");
            }

        }

        public async Task<MyFatoorahPaymentStatus> GetPaymentStatusAsync(string paymentId)
        {
            var response = await _httpClient.GetAsync($"/v2/GetPaymentStatus?Key={paymentId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MyFatoorahPaymentStatus>();
        }

        public async Task<MyFatoorahRefundResponse> RefundPaymentAsync(string paymentId, decimal amount)
        {
            var request = new
            {
                Key = paymentId,
                Amount = amount,
                Currency = "QAR"
            };

            var response = await _httpClient.PostAsJsonAsync("/v2/MakeRefund", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MyFatoorahRefundResponse>();
        }

        public async Task<bool> VerifyPaymentAsync(string paymentId)
        {
            var response = await _httpClient.GetAsync($"/v2/GetPaymentStatus?Key={paymentId}");
            response.EnsureSuccessStatusCode();
            var status = await response.Content.ReadFromJsonAsync<MyFatoorahPaymentStatus>();
            return status.IsSuccess;
        }
    }
}
