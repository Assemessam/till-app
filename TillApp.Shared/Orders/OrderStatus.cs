using System.Text.Json.Serialization;

namespace TillApp.Shared.Orders;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending,
    Paid,
    Cancelled
}
