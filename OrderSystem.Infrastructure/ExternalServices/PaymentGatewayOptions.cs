namespace OrderSystem.Infrastructure.ExternalServices;

public class PaymentGatewayOptions
{
    public string BaseUrl { get; set; } = default!;
    public string? Mode { get; set; }
}