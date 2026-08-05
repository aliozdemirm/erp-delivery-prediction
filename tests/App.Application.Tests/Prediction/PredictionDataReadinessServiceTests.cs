using App.Application.Contracts.Configuration;
using App.Application.Contracts.Erp;
using App.Application.Prediction;
using App.Application.Prediction.Resolvers;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction;

public sealed class PredictionDataReadinessServiceTests
{
    [Fact]
    public void Evaluate_WithResolvedInputs_ReturnsExactBuilderAndResolverOutputs()
    {
        var context = CreateContext();
        var procurement = new FallbackResult<DateTimeOffset>(DateTimeOffset.UtcNow.AddDays(1), false, null);
        var shipping = new FallbackResult<TimeSpan?>(TimeSpan.FromMinutes(45), false, null);
        var capacity = new FallbackResult<bool>(true, true, "Capacity is assumed unlimited for MVP");
        var fixture = CreateFixture(context, procurement, shipping, capacity);

        var result = fixture.Service.Evaluate(
            CreateSnapshot(), null, 45, DateTimeOffset.UtcNow, CreateOptions());

        var ready = Assert.IsType<Ready>(result);
        Assert.Same(context, ready.Context);
        Assert.Same(procurement, ready.Procurement);
        Assert.Same(shipping, ready.Shipping);
        Assert.Same(capacity, ready.Capacity);
        Assert.Equal(1, fixture.Builder.InvocationCount);
        Assert.Equal(1, fixture.Procurement.InvocationCount);
        Assert.Equal(1, fixture.Shipping.InvocationCount);
        Assert.Equal(1, fixture.Capacity.InvocationCount);
        Assert.Equal(["builder", "procurement", "shipping", "capacity"], fixture.Calls);
    }

    [Fact]
    public void Evaluate_WithConfiguredFallbackOutputs_PassesThemThroughUnchanged()
    {
        var procurement = new FallbackResult<DateTimeOffset>(DateTimeOffset.UtcNow.AddDays(2), true, "procurement fallback");
        var shipping = new FallbackResult<TimeSpan?>(TimeSpan.FromHours(3), true, "shipping fallback");
        var capacity = new FallbackResult<bool>(true, true, "unlimited capacity");
        var fixture = CreateFixture(CreateContext(), procurement, shipping, capacity);

        var ready = Assert.IsType<Ready>(fixture.Service.Evaluate(
            CreateSnapshot(), null, null, DateTimeOffset.UtcNow, CreateOptions()));

        Assert.Same(procurement, ready.Procurement);
        Assert.Same(shipping, ready.Shipping);
        Assert.Same(capacity, ready.Capacity);
        Assert.Equal((procurement.Value, procurement.IsFallbackApplied, procurement.Reason),
            (ready.Procurement.Value, ready.Procurement.IsFallbackApplied, ready.Procurement.Reason));
        Assert.Equal((shipping.Value, shipping.IsFallbackApplied, shipping.Reason),
            (ready.Shipping.Value, ready.Shipping.IsFallbackApplied, ready.Shipping.Reason));
        Assert.Equal((capacity.Value, capacity.IsFallbackApplied, capacity.Reason),
            (ready.Capacity.Value, ready.Capacity.IsFallbackApplied, ready.Capacity.Reason));
    }

    [Fact]
    public void Evaluate_WithBuilderInsufficiency_ReturnsPredictionContextInsufficientAndSkipsResolvers()
    {
        var fixture = CreateFixture(null);
        fixture.Builder.Status = DataSufficiency.InsufficientData;

        var result = fixture.Service.Evaluate(
            CreateSnapshot(), null, null, DateTimeOffset.UtcNow, CreateOptions());

        var insufficient = Assert.IsType<InsufficientData>(result);
        Assert.Equal(ReadinessFailureSource.PredictionContext, insufficient.Source);
        Assert.NotNull(insufficient.Reason);
        Assert.Equal(1, fixture.Builder.InvocationCount);
        Assert.Equal(0, fixture.Procurement.InvocationCount);
        Assert.Equal(0, fixture.Shipping.InvocationCount);
        Assert.Equal(0, fixture.Capacity.InvocationCount);
        Assert.Equal(["builder"], fixture.Calls);
    }

    [Fact]
    public void Evaluate_WithNullContext_ReturnsPredictionContextInsufficientAndSkipsResolvers()
    {
        var fixture = CreateFixture(null);

        var result = fixture.Service.Evaluate(
            CreateSnapshot(), null, null, DateTimeOffset.UtcNow, CreateOptions());

        Assert.Equal(ReadinessFailureSource.PredictionContext, Assert.IsType<InsufficientData>(result).Source);
        Assert.Equal(0, fixture.Procurement.InvocationCount);
        Assert.Equal(0, fixture.Shipping.InvocationCount);
        Assert.Equal(0, fixture.Capacity.InvocationCount);
    }

    [Fact]
    public void Evaluate_WithUnresolvedShipping_PreservesReasonAndSkipsCapacity()
    {
        var shipping = new FallbackResult<TimeSpan?>(null, true, "resolver reason");
        var fixture = CreateFixture(CreateContext(), shipping: shipping);

        var result = fixture.Service.Evaluate(
            CreateSnapshot(), null, null, DateTimeOffset.UtcNow, CreateOptions());

        var insufficient = Assert.IsType<InsufficientData>(result);
        Assert.Equal(ReadinessFailureSource.Shipping, insufficient.Source);
        Assert.Same(shipping.Reason, insufficient.Reason);
        Assert.Equal(1, fixture.Procurement.InvocationCount);
        Assert.Equal(1, fixture.Shipping.InvocationCount);
        Assert.Equal(0, fixture.Capacity.InvocationCount);
        Assert.Equal(["builder", "procurement", "shipping"], fixture.Calls);
        Assert.DoesNotContain("capacity", fixture.Calls);
    }

    [Theory]
    [InlineData(FailingDependency.Builder)]
    [InlineData(FailingDependency.Procurement)]
    [InlineData(FailingDependency.Shipping)]
    [InlineData(FailingDependency.Capacity)]
    public void Evaluate_WhenDependencyThrows_PropagatesSameException(FailingDependency dependency)
    {
        var fixture = CreateFixture(CreateContext());
        var expected = new TestException();
        fixture.SetException(dependency, expected);

        var actual = Assert.Throws<TestException>(() => fixture.Service.Evaluate(
            CreateSnapshot(), null, 30, DateTimeOffset.UtcNow, CreateOptions()));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void ReadinessResult_HasExactlyTwoConcreteOutcomes()
    {
        var outcomes = typeof(ReadinessResult).Assembly.GetTypes()
            .Where(type => type.BaseType == typeof(ReadinessResult) && !type.IsAbstract)
            .ToArray();

        Assert.Equal(2, outcomes.Length);
        Assert.Contains(typeof(Ready), outcomes);
        Assert.Contains(typeof(InsufficientData), outcomes);
    }

    [Fact]
    public void ReadinessResult_PublicContractDoesNotExposeIntegrationTypes()
    {
        var exposedTypes = new[] { typeof(ReadinessResult), typeof(Ready), typeof(InsufficientData) }
            .SelectMany(type => type.GetProperties().Select(property => property.PropertyType));

        Assert.DoesNotContain(exposedTypes, type =>
            type.Namespace?.StartsWith("App.Integration", StringComparison.Ordinal) == true);
    }

    private static Fixture CreateFixture(
        PredictionContext? context,
        FallbackResult<DateTimeOffset>? procurement = null,
        FallbackResult<TimeSpan?>? shipping = null,
        FallbackResult<bool>? capacity = null)
    {
        var calls = new List<string>();
        var builder = new BuilderFake { Context = context, Calls = calls };
        var procurementResolver = new ProcurementResolverFake
        {
            Result = procurement ?? new(DateTimeOffset.UtcNow, false, null),
            Calls = calls
        };
        var shippingResolver = new ShippingResolverFake
        {
            Result = shipping ?? new(TimeSpan.FromMinutes(30), false, null),
            Calls = calls
        };
        var capacityResolver = new CapacityResolverFake
        {
            Result = capacity ?? new(true, true, "unlimited"),
            Calls = calls
        };

        return new Fixture(
            new PredictionDataReadinessService(builder, procurementResolver, shippingResolver, capacityResolver),
            builder,
            procurementResolver,
            shippingResolver,
            capacityResolver,
            calls);
    }

    private static PredictionContext CreateContext() => new(
        new OrderInput("ORD-1", "PROD-1", 1, DateTimeOffset.UtcNow),
        new MaterialSnapshot([], [], [], []),
        new RoutingSnapshot([]),
        new CapacitySnapshot(),
        new CalendarSnapshot(),
        new ShippingSnapshot());

    private static ErpBatchSnapshot CreateSnapshot() => new(
        DateTimeOffset.UtcNow,
        new OrderReadDto("ORD-1", DateTimeOffset.UtcNow, null, null),
        [], [], [], [], [], []);

    private static MvpAssumptionsOptions CreateOptions() => new();

    public enum FailingDependency
    {
        Builder,
        Procurement,
        Shipping,
        Capacity
    }

    private sealed class TestException : Exception;

    private sealed class BuilderFake : IPredictionContextBuilder
    {
        public int InvocationCount { get; private set; }
        public DataSufficiency Status { get; set; } = DataSufficiency.Sufficient;
        public PredictionContext? Context { get; set; }
        public Exception? Exception { get; set; }
        public required List<string> Calls { get; init; }

        public (DataSufficiency Status, PredictionContext? Context) Build(ErpBatchSnapshot snapshot)
        {
            InvocationCount++;
            Calls.Add("builder");
            if (Exception is not null) throw Exception;
            return (Status, Context);
        }
    }

    private sealed class ProcurementResolverFake : IProcurementResolver
    {
        public int InvocationCount { get; private set; }
        public required FallbackResult<DateTimeOffset> Result { get; init; }
        public Exception? Exception { get; set; }
        public required List<string> Calls { get; init; }

        public FallbackResult<DateTimeOffset> ResolveAvailabilityDate(
            MaterialPurchaseOrder? openPo, DateTimeOffset currentTime, MvpAssumptionsOptions options)
        {
            InvocationCount++;
            Calls.Add("procurement");
            if (Exception is not null) throw Exception;
            return Result;
        }
    }

    private sealed class ShippingResolverFake : IShippingResolver
    {
        public int InvocationCount { get; private set; }
        public required FallbackResult<TimeSpan?> Result { get; init; }
        public Exception? Exception { get; set; }
        public required List<string> Calls { get; init; }

        public FallbackResult<TimeSpan?> ResolveShippingDuration(
            long? actualShippingDurationMinutes, MvpAssumptionsOptions options)
        {
            InvocationCount++;
            Calls.Add("shipping");
            if (Exception is not null) throw Exception;
            return Result;
        }
    }

    private sealed class CapacityResolverFake : ICapacityResolver
    {
        public int InvocationCount { get; private set; }
        public required FallbackResult<bool> Result { get; init; }
        public Exception? Exception { get; set; }
        public required List<string> Calls { get; init; }

        public FallbackResult<bool> ResolveCapacityConstraint()
        {
            InvocationCount++;
            Calls.Add("capacity");
            if (Exception is not null) throw Exception;
            return Result;
        }
    }

    private sealed record Fixture(
        PredictionDataReadinessService Service,
        BuilderFake Builder,
        ProcurementResolverFake Procurement,
        ShippingResolverFake Shipping,
        CapacityResolverFake Capacity,
        List<string> Calls)
    {
        public void SetException(FailingDependency dependency, Exception exception)
        {
            switch (dependency)
            {
                case FailingDependency.Builder: Builder.Exception = exception; break;
                case FailingDependency.Procurement: Procurement.Exception = exception; break;
                case FailingDependency.Shipping: Shipping.Exception = exception; break;
                case FailingDependency.Capacity: Capacity.Exception = exception; break;
                default: throw new ArgumentOutOfRangeException(nameof(dependency), dependency, null);
            }
        }
    }
}
