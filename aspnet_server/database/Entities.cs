using System.Text.Json.Serialization;

namespace entities;

public record User
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;

    [JsonIgnore]
    public string PasswordHash { get; init; } = string.Empty;

    // TODO initialize with default (free) plan
    public SubscriptionPlan SubscriptionPlan { get; init; }

    public int RemainingConversionMins { get; init; }
    public int CurrentConcurrentConversions { get; init; }
    public DateTimeOffset ResetDate { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public record SubscriptionPlan
{
    public int SubscriptionPlanId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int MaxFileSizeMegabytes { get; init; }
    public int MaxConversionMins { get; init; }
    public int MaxConcurrentConversions { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}