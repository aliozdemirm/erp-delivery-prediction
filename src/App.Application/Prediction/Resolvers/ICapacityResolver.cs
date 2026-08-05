namespace App.Application.Prediction.Resolvers;

public interface ICapacityResolver
{
    FallbackResult<bool> ResolveCapacityConstraint();
}
