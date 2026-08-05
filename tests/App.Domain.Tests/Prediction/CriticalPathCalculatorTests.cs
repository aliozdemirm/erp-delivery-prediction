using App.Domain.Prediction;

namespace App.Domain.Tests.Prediction;

public class CriticalPathCalculatorTests
{
    private readonly CriticalPathCalculator _calculator = new();

    [Fact]
    public void SingleOperation_IsCriticalWithZeroSlack()
    {
        var context = CreateContext(
            new RoutingOperation("OP-10", 10, "WC-1", 60, Array.Empty<string>()));

        var outcome = _calculator.Calculate(context);

        Assert.Equal(CriticalPathStatus.Success, outcome.Status);
        Assert.Null(outcome.FailureReason);
        var result = outcome.Result!;
        Assert.Equal(60, result.TotalWorkingMinutes);
        Assert.Equal(["OP-10"], result.CriticalOperationRefs);

        var schedule = Assert.Single(result.Schedule);
        Assert.Equal("OP-10", schedule.OperationRef);
        Assert.Equal(0, schedule.EarliestStartMinutes);
        Assert.Equal(60, schedule.EarliestFinishMinutes);
        Assert.Equal(0, schedule.LatestStartMinutes);
        Assert.Equal(60, schedule.LatestFinishMinutes);
        Assert.Equal(0, schedule.SlackMinutes);
    }

    [Fact]
    public void LinearThreeOperations_AllCriticalAndTotalIsSumOfDurations()
    {
        var context = CreateContext(
            new RoutingOperation("OP-10", 10, "WC-1", 30, Array.Empty<string>()),
            new RoutingOperation("OP-20", 20, "WC-1", 45, ["OP-10"]),
            new RoutingOperation("OP-30", 30, "WC-1", 15, ["OP-20"]));

        var outcome = _calculator.Calculate(context);

        Assert.Equal(CriticalPathStatus.Success, outcome.Status);
        var result = outcome.Result!;
        Assert.Equal(90, result.TotalWorkingMinutes);
        Assert.Equal(["OP-10", "OP-20", "OP-30"], result.CriticalOperationRefs);

        AssertSchedule(result, "OP-10", earliestStart: 0, earliestFinish: 30, latestStart: 0, latestFinish: 30, slack: 0);
        AssertSchedule(result, "OP-20", earliestStart: 30, earliestFinish: 75, latestStart: 30, latestFinish: 75, slack: 0);
        AssertSchedule(result, "OP-30", earliestStart: 75, earliestFinish: 90, latestStart: 75, latestFinish: 90, slack: 0);
    }

    [Fact]
    public void ParallelBranches_LongBranchIsCritical_ShortBranchHasPositiveSlack()
    {
        // Short branch: OP-A1 (20). Long branch: OP-B1 (30) -> OP-B2 (20), total 50.
        // Both feed into OP-FINAL (10). Total duration must follow the long branch (60).
        var context = CreateContext(
            new RoutingOperation("OP-A1", 10, "WC-1", 20, Array.Empty<string>()),
            new RoutingOperation("OP-B1", 20, "WC-1", 30, Array.Empty<string>()),
            new RoutingOperation("OP-B2", 30, "WC-1", 20, ["OP-B1"]),
            new RoutingOperation("OP-FINAL", 40, "WC-1", 10, ["OP-A1", "OP-B2"]));

        var outcome = _calculator.Calculate(context);

        Assert.Equal(CriticalPathStatus.Success, outcome.Status);
        var result = outcome.Result!;
        Assert.Equal(60, result.TotalWorkingMinutes);
        Assert.Equal(["OP-B1", "OP-B2", "OP-FINAL"], result.CriticalOperationRefs);

        AssertSchedule(result, "OP-A1", earliestStart: 0, earliestFinish: 20, latestStart: 30, latestFinish: 50, slack: 30);
        AssertSchedule(result, "OP-B1", earliestStart: 0, earliestFinish: 30, latestStart: 0, latestFinish: 30, slack: 0);
        AssertSchedule(result, "OP-B2", earliestStart: 30, earliestFinish: 50, latestStart: 30, latestFinish: 50, slack: 0);
        AssertSchedule(result, "OP-FINAL", earliestStart: 50, earliestFinish: 60, latestStart: 50, latestFinish: 60, slack: 0);
    }

    [Fact]
    public void ConvergingParallelGraph_ForwardAndBackwardPassAreCorrect_CommonFinalOperationIsAggregatedCorrectly()
    {
        // Three independent single-operation branches (X, Y, Z) converge into OP-END.
        // OP-END's EarliestStart must be the maximum of the three branches' finishes.
        var context = CreateContext(
            new RoutingOperation("OP-X", 10, "WC-1", 10, Array.Empty<string>()),
            new RoutingOperation("OP-Y", 20, "WC-1", 25, Array.Empty<string>()),
            new RoutingOperation("OP-Z", 30, "WC-1", 15, Array.Empty<string>()),
            new RoutingOperation("OP-END", 40, "WC-1", 5, ["OP-X", "OP-Y", "OP-Z"]));

        var outcome = _calculator.Calculate(context);

        Assert.Equal(CriticalPathStatus.Success, outcome.Status);
        var result = outcome.Result!;
        Assert.Equal(30, result.TotalWorkingMinutes);
        Assert.Equal(["OP-Y", "OP-END"], result.CriticalOperationRefs);

        AssertSchedule(result, "OP-X", earliestStart: 0, earliestFinish: 10, latestStart: 15, latestFinish: 25, slack: 15);
        AssertSchedule(result, "OP-Y", earliestStart: 0, earliestFinish: 25, latestStart: 0, latestFinish: 25, slack: 0);
        AssertSchedule(result, "OP-Z", earliestStart: 0, earliestFinish: 15, latestStart: 10, latestFinish: 25, slack: 10);
        AssertSchedule(result, "OP-END", earliestStart: 25, earliestFinish: 30, latestStart: 25, latestFinish: 30, slack: 0);
    }

    [Fact]
    public void MissingPredecessorReference_ReturnsFailureOutcomeWithoutResult()
    {
        var context = CreateContext(
            new RoutingOperation("OP-10", 10, "WC-1", 30, Array.Empty<string>()),
            new RoutingOperation("OP-20", 20, "WC-1", 20, ["OP-10", "OP-UNKNOWN"]));

        var outcome = _calculator.Calculate(context);

        Assert.Equal(CriticalPathStatus.MissingPredecessorReference, outcome.Status);
        Assert.Null(outcome.Result);
        Assert.NotNull(outcome.FailureReason);
        Assert.Contains("OP-20", outcome.FailureReason);
        Assert.Contains("OP-UNKNOWN", outcome.FailureReason);
    }

    [Fact]
    public void Cycle_ReturnsFailureOutcomeWithoutResult_AndDoesNotThrow()
    {
        var context = CreateContext(
            new RoutingOperation("OP-10", 10, "WC-1", 30, ["OP-20"]),
            new RoutingOperation("OP-20", 20, "WC-1", 20, ["OP-10"]));

        var outcome = _calculator.Calculate(context);

        Assert.Equal(CriticalPathStatus.CycleDetected, outcome.Status);
        Assert.Null(outcome.Result);
        Assert.NotNull(outcome.FailureReason);
    }

    [Fact]
    public void SameInput_ProducesDeterministicScheduleAndCriticalPath()
    {
        var first = _calculator.Calculate(CreateContext(
            new RoutingOperation("OP-10", 10, "WC-1", 30, Array.Empty<string>()),
            new RoutingOperation("OP-20", 20, "WC-1", 45, ["OP-10"]),
            new RoutingOperation("OP-30", 30, "WC-1", 15, ["OP-20"])));

        var second = _calculator.Calculate(CreateContext(
            new RoutingOperation("OP-10", 10, "WC-1", 30, Array.Empty<string>()),
            new RoutingOperation("OP-20", 20, "WC-1", 45, ["OP-10"]),
            new RoutingOperation("OP-30", 30, "WC-1", 15, ["OP-20"])));

        Assert.Equal(CriticalPathStatus.Success, first.Status);
        Assert.Equal(CriticalPathStatus.Success, second.Status);
        Assert.Equal(first.Result!.TotalWorkingMinutes, second.Result!.TotalWorkingMinutes);
        Assert.Equal(first.Result.CriticalOperationRefs, second.Result.CriticalOperationRefs);
        Assert.Equal(first.Result.Schedule, second.Result.Schedule);
    }

    private static void AssertSchedule(
        CriticalPathResult result,
        string operationRef,
        long earliestStart,
        long earliestFinish,
        long latestStart,
        long latestFinish,
        long slack)
    {
        var schedule = Assert.Single(result.Schedule, entry => entry.OperationRef == operationRef);

        Assert.Equal(earliestStart, schedule.EarliestStartMinutes);
        Assert.Equal(earliestFinish, schedule.EarliestFinishMinutes);
        Assert.Equal(latestStart, schedule.LatestStartMinutes);
        Assert.Equal(latestFinish, schedule.LatestFinishMinutes);
        Assert.Equal(slack, schedule.SlackMinutes);
    }

    private static PredictionContext CreateContext(params RoutingOperation[] operations)
    {
        var context = new PredictionContext(
            new OrderInput("SO-1", "P-1", 1m, DateTimeOffset.UtcNow),
            new MaterialSnapshot(
                Array.Empty<MaterialProduct>(),
                Array.Empty<MaterialBomItem>(),
                Array.Empty<MaterialStock>(),
                Array.Empty<MaterialPurchaseOrder>()),
            new RoutingSnapshot(operations),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            new ShippingSnapshot());

        foreach (var operation in operations)
        {
            context.AddOperation(new Operation(operation.OperationReference, operation.StandardDurationMinutes));
        }

        return context;
    }
}
