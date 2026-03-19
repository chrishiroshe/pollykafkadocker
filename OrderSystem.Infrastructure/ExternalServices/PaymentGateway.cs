using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace OrderSystem.Infrastructure.ExternalServices
{
    public class PaymentGateway
    {
        private readonly HttpClient _httpClient;
        private readonly PaymentGatewayOptions _options;

        public PaymentGateway(HttpClient httpClient, IOptions<PaymentGatewayOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        //public async Task ChargeAsync(Guid orderId, CancellationToken ct)
        //{
        //    var response = await _httpClient.PostAsync(
        //        $"payments/{orderId}",
        //        null,
        //        ct);

        //   // await _httpClient.PostAsync($"payments/{orderId}?fail=true", null, ct);

        //    response.EnsureSuccessStatusCode();
        //}

        public async Task ChargeAsync(Guid orderId, CancellationToken ct, string? mode = null)
        {
            var url = $"payments/{orderId}";

            if (!string.IsNullOrWhiteSpace(_options.Mode))
            {
                url += $"?mode={_options.Mode}";
            }

            var response = await _httpClient.PostAsync(url, null, ct);
            response.EnsureSuccessStatusCode();
        }
    }
}
