namespace OrderSystem.Infrastructure.Messaging;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = default!;
    public string OrdersConfirmedTopic { get; set; } = "orders-confirmed";
    public string PaymentSucceededTopic { get; set; } = "payment-succeeded";
    public string PaymentFailedTopic { get; set; } = "payment-failed";
    public string InventoryReserveRequestedTopic { get; set; } = "inventory-reserve-requested";
    public string InventoryReservedTopic { get; set; } = "inventory-reserved";

    public string InventoryReservationFailedTopic { get; set; } = "inventory-reservation-failed";
    public string InventoryReleaseRequestedTopic { get; set; } = "inventory-release-requested";
    public string PaymentRequestedTopic { get; set; } = "payment-requested";
    public string InventoryReleasedTopic { get; set; } = "inventory-released";

}