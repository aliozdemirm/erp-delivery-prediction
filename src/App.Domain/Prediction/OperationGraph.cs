namespace App.Domain.Prediction;

// Internal graph-building/topological-sort helper for CriticalPathCalculator (SAD §10.2/§10.3:
// OperationGraph.HasCycle/TopologicalOrder). Not part of the public CPM contract approved for
// T-383 (CriticalPathStatus/OperationSchedule/CriticalPathResult/CriticalPathOutcome) — kept
// internal because nothing outside CriticalPathCalculator needs it.
internal sealed class OperationGraph
{
    public IReadOnlyList<string> TopologicalOrder { get; }
    public bool HasCycle { get; }
    public string? MissingPredecessorOperationRef { get; }
    public string? MissingPredecessorRef { get; }

    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _successorsByOperationRef;

    private OperationGraph(
        IReadOnlyList<string> topologicalOrder,
        bool hasCycle,
        string? missingPredecessorOperationRef,
        string? missingPredecessorRef,
        IReadOnlyDictionary<string, IReadOnlyList<string>> successorsByOperationRef)
    {
        TopologicalOrder = topologicalOrder;
        HasCycle = hasCycle;
        MissingPredecessorOperationRef = missingPredecessorOperationRef;
        MissingPredecessorRef = missingPredecessorRef;
        _successorsByOperationRef = successorsByOperationRef;
    }

    public IReadOnlyList<string> SuccessorsOf(string operationRef) =>
        _successorsByOperationRef.TryGetValue(operationRef, out var successors)
            ? successors
            : Array.Empty<string>();

    public static OperationGraph Build(
        IReadOnlyList<string> operationRefs,
        IReadOnlyDictionary<string, IReadOnlyList<string>> predecessorsByOperationRef)
    {
        ArgumentNullException.ThrowIfNull(operationRefs);
        ArgumentNullException.ThrowIfNull(predecessorsByOperationRef);

        var knownOperationRefs = new HashSet<string>(operationRefs, StringComparer.Ordinal);

        foreach (var operationRef in operationRefs)
        {
            foreach (var predecessor in PredecessorsOf(predecessorsByOperationRef, operationRef))
            {
                if (!knownOperationRefs.Contains(predecessor))
                {
                    return new OperationGraph(
                        Array.Empty<string>(),
                        hasCycle: false,
                        missingPredecessorOperationRef: operationRef,
                        missingPredecessorRef: predecessor,
                        successorsByOperationRef: new Dictionary<string, IReadOnlyList<string>>());
                }
            }
        }

        var successorsBuilder = operationRefs.ToDictionary(
            operationRef => operationRef,
            _ => new List<string>(),
            StringComparer.Ordinal);
        var remainingPredecessorCount = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var operationRef in operationRefs)
        {
            var predecessors = PredecessorsOf(predecessorsByOperationRef, operationRef);
            remainingPredecessorCount[operationRef] = predecessors.Count;

            foreach (var predecessor in predecessors)
            {
                successorsBuilder[predecessor].Add(operationRef);
            }
        }

        var successors = successorsBuilder.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value,
            StringComparer.Ordinal);

        var topologicalOrder = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (topologicalOrder.Count < operationRefs.Count)
        {
            var next = operationRefs.FirstOrDefault(
                operationRef => !visited.Contains(operationRef) && remainingPredecessorCount[operationRef] == 0);

            if (next is null)
            {
                return new OperationGraph(
                    Array.Empty<string>(),
                    hasCycle: true,
                    missingPredecessorOperationRef: null,
                    missingPredecessorRef: null,
                    successors);
            }

            topologicalOrder.Add(next);
            visited.Add(next);

            foreach (var successor in successors[next])
            {
                remainingPredecessorCount[successor]--;
            }
        }

        return new OperationGraph(
            topologicalOrder,
            hasCycle: false,
            missingPredecessorOperationRef: null,
            missingPredecessorRef: null,
            successors);
    }

    private static IReadOnlyList<string> PredecessorsOf(
        IReadOnlyDictionary<string, IReadOnlyList<string>> predecessorsByOperationRef,
        string operationRef) =>
        predecessorsByOperationRef.TryGetValue(operationRef, out var predecessors)
            ? predecessors
            : Array.Empty<string>();
}
