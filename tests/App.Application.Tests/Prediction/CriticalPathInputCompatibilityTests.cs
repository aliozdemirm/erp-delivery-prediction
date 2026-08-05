using App.Application.Contracts.Configuration;
using App.Application.Prediction;
using App.Application.Prediction.Resolvers;
using App.Domain.Abstractions;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction;

public class CriticalPathInputCompatibilityTests
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RuleEngineOutput_IsConsumedDirectlyByCriticalPathCalculator_WithoutChangingValues()
    {
        var predecessors = new[] { "OP-10" };
        var routingOperations = new[]
        {
            new RoutingOperation("OP-10", 10, "WC-1", 30, Array.Empty<string>()),
            new RoutingOperation("OP-20", 20, "WC-2", 45, predecessors)
        };
        var context = CreateContext(routingOperations);

        var engineResult = CreateEngine().Run(context);
        var operationsBeforeCpm = engineResult.Context.Operations.ToArray();

        var outcome = new CriticalPathCalculator().Calculate(engineResult.Context);

        Assert.True(engineResult.Success);
        Assert.Same(context, engineResult.Context);
        Assert.Equal(2, engineResult.Context.Operations.Count);
        Assert.Collection(
            engineResult.Context.Operations,
            operation =>
            {
                Assert.Equal("OP-10", operation.OperationReference);
                Assert.Equal(30, operation.DurationMinutes);
            },
            operation =>
            {
                Assert.Equal("OP-20", operation.OperationReference);
                Assert.Equal(45, operation.DurationMinutes);
            });
        Assert.Same(predecessors, engineResult.Context.RoutingSnapshot.Operations[1].PredecessorOperations);

        Assert.Equal(CriticalPathStatus.Success, outcome.Status);
        Assert.Equal(75, outcome.Result!.TotalWorkingMinutes);
        Assert.Equal(["OP-10", "OP-20"], outcome.Result.CriticalOperationRefs);
        Assert.Equal(operationsBeforeCpm, engineResult.Context.Operations);
    }

    [Fact]
    public void RuleEngineOutput_ParallelGraph_PreservesExistingCriticalPathBehavior()
    {
        var context = CreateContext(
            new RoutingOperation("OP-SHORT", 10, "WC-1", 20, Array.Empty<string>()),
            new RoutingOperation("OP-LONG", 20, "WC-2", 50, Array.Empty<string>()),
            new RoutingOperation("OP-END", 30, "WC-3", 10, ["OP-SHORT", "OP-LONG"]));

        var engineResult = CreateEngine().Run(context);
        var outcome = new CriticalPathCalculator().Calculate(engineResult.Context);

        Assert.Equal(CriticalPathStatus.Success, outcome.Status);
        Assert.Equal(60, outcome.Result!.TotalWorkingMinutes);
        Assert.Equal(["OP-LONG", "OP-END"], outcome.Result.CriticalOperationRefs);
        Assert.Equal(3, outcome.Result.Schedule.Count);
    }

    [Fact]
    public void RuleEngineOutput_EmptyRouting_PreservesExistingCalculatorBehavior()
    {
        var engineResult = CreateEngine().Run(CreateContext());

        var outcome = new CriticalPathCalculator().Calculate(engineResult.Context);

        Assert.Equal(CriticalPathStatus.Success, outcome.Status);
        Assert.Equal(0, outcome.Result!.TotalWorkingMinutes);
        Assert.Empty(outcome.Result.CriticalOperationRefs);
        Assert.Empty(outcome.Result.Schedule);
    }

    [Fact]
    public void RuleEngineOutput_InvalidPredecessor_PreservesExistingCalculatorBehavior()
    {
        var context = CreateContext(
            new RoutingOperation("OP-10", 10, "WC-1", 30, Array.Empty<string>()),
            new RoutingOperation("OP-20", 20, "WC-2", 45, ["OP-MISSING"]));
        var engineResult = CreateEngine().Run(context);

        var outcome = new CriticalPathCalculator().Calculate(engineResult.Context);

        Assert.Equal(CriticalPathStatus.MissingPredecessorReference, outcome.Status);
        Assert.Null(outcome.Result);
        Assert.Contains("OP-20", outcome.FailureReason);
        Assert.Contains("OP-MISSING", outcome.FailureReason);
    }

    [Fact]
    public void RuleEngineOutput_Cycle_PreservesExistingCalculatorBehavior()
    {
        var context = CreateContext(
            new RoutingOperation("OP-10", 10, "WC-1", 30, ["OP-20"]),
            new RoutingOperation("OP-20", 20, "WC-2", 45, ["OP-10"]));
        var engineResult = CreateEngine().Run(context);

        var outcome = new CriticalPathCalculator().Calculate(engineResult.Context);

        Assert.Equal(CriticalPathStatus.CycleDetected, outcome.Status);
        Assert.Null(outcome.Result);
        Assert.NotNull(outcome.FailureReason);
    }

    private static RuleBasedPredictionEngine CreateEngine() =>
        new(
            new ProcurementResolver(),
            new CapacityResolver(),
            new FixedClock(),
            new MvpAssumptionsOptions
            {
                Procurement = new ProcurementAssumptionsOptions { FallbackDurationMinutes = 1 }
            });

    private static PredictionContext CreateContext(params RoutingOperation[] routingOperations) =>
        new(
            new OrderInput("ORD-1", "PROD-1", 1, FixedUtcNow),
            new MaterialSnapshot(
                Array.Empty<MaterialProduct>(),
                Array.Empty<MaterialBomItem>(),
                Array.Empty<MaterialStock>(),
                Array.Empty<MaterialPurchaseOrder>()),
            new RoutingSnapshot(routingOperations),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            new ShippingSnapshot());

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => FixedUtcNow;
    }
}
