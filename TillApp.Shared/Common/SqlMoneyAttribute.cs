using System.ComponentModel.DataAnnotations;

namespace TillApp.Shared.Common;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SqlMoneyAttribute : ValidationAttribute
{
    public SqlMoneyAttribute()
        : base("The {0} field must be greater than zero, no more than four decimal places, and within the SQL Server money range.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value is decimal amount
            && amount >= SqlMoney.MinPositiveValue
            && amount <= SqlMoney.MaxValue
            && decimal.Round(amount, 4) == amount;
    }
}
