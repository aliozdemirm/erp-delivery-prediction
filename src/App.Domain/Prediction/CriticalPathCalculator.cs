namespace App.Domain.Prediction;

public sealed class CriticalPathCalculator : ICriticalPathCalculator
{
    public CriticalPathOutcome Calculate(PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var durationByOperationRef = new Dictionary<string, long>(StringComparer.Ordinal);
        var operationRefs = new List<string>();

        foreach (var operation in context.Operations)
        {
            if (operation.OperationReference is null)
            {
                continue;
            }

            durationByOperationRef[operation.OperationReference] = operation.DurationMinutes;
            operationRefs.Add(operation.OperationReference);
        }

        var predecessorsByOperationRef = context.RoutingSnapshot.Operations
            .ToDictionary(
                routingOperation => routingOperation.OperationReference,
                routingOperation => routingOperation.PredecessorOperations,
                StringComparer.Ordinal);

        var graph = OperationGraph.Build(operationRefs, predecessorsByOperationRef);

        if (graph.MissingPredecessorOperationRef is not null)
        {
            return CriticalPathOutcome.Failure(
                CriticalPathStatus.MissingPredecessorReference,
                $"Operation '{graph.MissingPredecessorOperationRef}' references unknown predecessor " +
                $"'{graph.MissingPredecessorRef}'.");
        }

        if (graph.HasCycle)
        {
            return CriticalPathOutcome.Failure(
                CriticalPathStatus.CycleDetected,
                "The operation graph contains a cycle and has no valid topological order.");
        }

        var earliestStart = new Dictionary<string, long>(StringComparer.Ordinal);
        var earliestFinish = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var operationRef in graph.TopologicalOrder)
        {
            var predecessors = GetPredecessors(predecessorsByOperationRef, operationRef);
            var start = predecessors.Count == 0
                ? 0L
                : predecessors.Max(predecessor => earliestFinish[predecessor]);

            earliestStart[operationRef] = start;
            earliestFinish[operationRef] = start + durationByOperationRef[operationRef];
        }

        var totalWorkingMinutes = graph.TopologicalOrder.Count == 0
            ? 0L
            : earliestFinish.Values.Max();

        var latestStart = new Dictionary<string, long>(StringComparer.Ordinal);
        var latestFinish = new Dictionary<string, long>(StringComparer.Ordinal);

        for (var index = graph.TopologicalOrder.Count - 1; index >= 0; index--)
        {
            var operationRef = graph.TopologicalOrder[index];
            var successors = graph.SuccessorsOf(operationRef);
            var finish = successors.Count == 0
                ? totalWorkingMinutes
                : successors.Min(successor => latestStart[successor]);

            latestFinish[operationRef] = finish;
            latestStart[operationRef] = finish - durationByOperationRef[operationRef];
        }

        var schedule = new List<OperationSchedule>();
        var criticalOperationRefs = new List<string>();

        foreach (var operationRef in graph.TopologicalOrder)
        {
            var slack = latestStart[operationRef] - earliestStart[operationRef];

            schedule.Add(new OperationSchedule(
                operationRef,
                earliestStart[operationRef],
                earliestFinish[operationRef],
                latestStart[operationRef],
                latestFinish[operationRef],
                slack));

            if (slack == 0)
            {
                criticalOperationRefs.Add(operationRef);
            }
        }

        var result = new CriticalPathResult(
            Array.AsReadOnly(criticalOperationRefs.ToArray()),
            totalWorkingMinutes,
            Array.AsReadOnly(schedule.ToArray()));

        return CriticalPathOutcome.Success(result);
    }

    private static IReadOnlyList<string> GetPredecessors(
        IReadOnlyDictionary<string, IReadOnlyList<string>> predecessorsByOperationRef,
        string operationRef) =>
        predecessorsByOperationRef.TryGetValue(operationRef, out var predecessors)
            ? predecessors
            : Array.Empty<string>();
}
