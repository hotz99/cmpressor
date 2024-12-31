namespace services;

public interface ISubscriptionService
{
    Result<bool> ValidateSubscriptionLimits(string subscriptionPlan, int fileSizeMB, int conversionMinutes, int concurrentConversions);
}

class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;

    public SubscriptionService(ISubscriptionPlanRepository subscriptionPlanRepository)
    {
        _subscriptionPlanRepository = subscriptionPlanRepository;
    }

    public Result<bool> ValidateSubscriptionLimits(string subscriptionPlan, int fileSizeBytes, int conversionMinutes, int concurrentConversions)
    {
        var plan = _subscriptionPlanRepository.GetByName(subscriptionPlan).Result;

        // should not happen
        if (plan is null)
        {
            return Result<bool>.Failure("Invalid subscription plan");
        }

        if (fileSizeBytes > plan.MaxFileSizeMegabytes * 1024 * 1024)
        {
            return Result<bool>.Failure("File size exceeds plan limit");
        }

        if (conversionMinutes > plan.MaxConversionMins)
        {
            return Result<bool>.Failure("Conversion time exceeds plan limit");
        }

        if (concurrentConversions > plan.MaxConcurrentConversions)
        {
            return Result<bool>.Failure("Concurrent conversions exceeds plan limit");
        }

        return Result<bool>.Success(true);
    }
}
