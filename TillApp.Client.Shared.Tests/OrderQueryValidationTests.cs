using System.ComponentModel.DataAnnotations;
using TillApp.Shared.Orders;

namespace TillApp.Client.Shared.Tests;

public sealed class OrderQueryValidationTests
{
    [Fact]
    public void InvalidDateRange_ReturnsValidationErrorsForBothBounds()
    {
        var query = new OrderQuery
        {
            From = new DateOnly(2026, 9, 30),
            To = new DateOnly(2026, 9, 1)
        };

        var errors = Validate(query);

        var error = Assert.Single(errors);
        Assert.Contains(nameof(OrderQuery.From), error.MemberNames);
        Assert.Contains(nameof(OrderQuery.To), error.MemberNames);
    }

    [Fact]
    public void ConflictingStatusAndLegacyPaymentFilter_ReturnsValidationError()
    {
        var query = new OrderQuery { Status = OrderStatus.Paid, IsPaid = false };

        var errors = Validate(query);

        Assert.Single(errors);
    }

    [Fact]
    public void CompatibleLegacyPaymentFilterAndStatus_RemainsValid()
    {
        var query = new OrderQuery { Status = OrderStatus.Paid, IsPaid = true };

        Assert.Empty(Validate(query));
    }

    private static List<ValidationResult> Validate(OrderQuery query)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(query, new ValidationContext(query), results, validateAllProperties: true);
        return results;
    }
}
