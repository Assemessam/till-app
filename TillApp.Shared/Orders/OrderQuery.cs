using System.ComponentModel.DataAnnotations;

namespace TillApp.Shared.Orders;

public sealed class OrderQuery : IValidatableObject
{
    private string? _search;

    public OrderStatus? Status { get; set; }

    public bool? IsPaid { get; set; }

    [StringLength(100)]
    public string? Search
    {
        get => _search;
        set => _search = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From > To)
        {
            yield return new ValidationResult(
                "The From date must be on or before the To date.",
                [nameof(From), nameof(To)]);
        }

        if (Status.HasValue && IsPaid.HasValue)
        {
            var legacyStatus = IsPaid.Value ? OrderStatus.Paid : OrderStatus.Pending;
            if (Status.Value != legacyStatus)
            {
                yield return new ValidationResult(
                    "The status and isPaid filters cannot specify different order states.",
                    [nameof(Status), nameof(IsPaid)]);
            }
        }
    }
}
