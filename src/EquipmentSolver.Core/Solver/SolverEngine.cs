using System.Diagnostics;

namespace EquipmentSolver.Core.Solver;

/// <summary>
/// Branch-and-bound solver that finds optimal equipment loadouts.
/// </summary>
public static class SolverEngine
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Find optimal equipment loadouts using branch-and-bound with constraint pruning.
    /// Items compatible with multiple slots can only occupy one slot at a time.
    /// Empty slots are allowed (contribute zero to all stats).
    /// </summary>
    public static SolverOutput Solve(
        List<SlotInput> slots,
        List<ConstraintInput> constraints,
        List<PriorityInput> priorities,
        int topN = 5,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (slots.Count == 0 || priorities.Count == 0)
        {
            stopwatch.Stop();
            return new SolverOutput([], false, stopwatch.Elapsed, 0);
        }

        using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var token = linkedCts.Token;

        // Sort items in each slot by estimated score (descending) for better pruning
        var priorityLookup = priorities.ToDictionary(p => p.StatTypeId, p => p.Weight);
        var sortedSlots = slots.Select(s => s with
        {
            Items = [.. s.Items.OrderByDescending(item => ComputeItemScore(item, priorityLookup))]
        }).ToList();

        var context = new SearchContext(sortedSlots, constraints, priorities, priorityLookup, topN, token);
        context.PrecomputeBounds();
        context.Search(0);

        stopwatch.Stop();

        var finalResults = context.GetResults();
        return new SolverOutput(finalResults, context.TimedOut, stopwatch.Elapsed, context.CombinationsEvaluated);
    }

    private static double ComputeItemScore(ItemInput item, Dictionary<int, double> priorityLookup)
    {
        double score = 0;
        foreach (var (statId, value) in item.Stats)
        {
            if (priorityLookup.TryGetValue(statId, out double weight))
                score += value * weight;
        }
        return score;
    }

    /// <summary>
    /// Mutable search state for the DFS traversal.
    /// </summary>
    private sealed class SearchContext
    {
        private readonly List<SlotInput> _slots;
        private readonly List<ConstraintInput> _constraints;
        private readonly List<PriorityInput> _priorities;
        private readonly Dictionary<int, double> _priorityLookup;
        private readonly int _topN;
        private readonly CancellationToken _token;

        // Pre-computed bounds for pruning
        private double[] _suffixMaxScore = null!;
        private Dictionary<int, double[]> _suffixMinStat = null!;
        private Dictionary<int, double[]> _suffixMaxStat = null!;

        // Mutable DFS state
        private readonly Dictionary<int, double> _currentStats = [];
        private readonly HashSet<int> _usedItems = [];
        private readonly (int? EquipmentId, string? Name, Dictionary<int, double>? Stats)[] _currentAssignments;
        private double _currentScore;

        // Results (sorted ascending by score — worst score at index 0 for easy eviction)
        private readonly SortedList<double, SolverResult> _results = new(new DuplicateKeyComparer());

        public bool TimedOut { get; private set; }
        public long CombinationsEvaluated { get; private set; }

        public SearchContext(
            List<SlotInput> slots,
            List<ConstraintInput> constraints,
            List<PriorityInput> priorities,
            Dictionary<int, double> priorityLookup,
            int topN,
            CancellationToken token)
        {
            _slots = slots;
            _constraints = constraints;
            _priorities = priorities;
            _priorityLookup = priorityLookup;
            _topN = topN;
            _token = token;
            _currentAssignments = new (int?, string?, Dictionary<int, double>?)[slots.Count];
        }

        /// <summary>
        /// Pre-compute per-slot stat bounds and score upper bounds for pruning.
        /// </summary>
        public void PrecomputeBounds()
        {
            int slotCount = _slots.Count;
            var constraintStatIds = _constraints.Select(c => c.StatTypeId).Distinct().ToHashSet();

            // Per-slot bounds
            var slotMaxScores = new double[slotCount];
            var slotMinStats = new Dictionary<int, double[]>();
            var slotMaxStats = new Dictionary<int, double[]>();

            foreach (int statId in constraintStatIds)
            {
                slotMinStats[statId] = new double[slotCount];
                slotMaxStats[statId] = new double[slotCount];
            }

            for (int i = 0; i < slotCount; i++)
            {
                var slot = _slots[i];
                double maxScore = 0; // Empty slot score = 0

                foreach (var item in slot.Items)
                {
                    double itemScore = ComputeItemScore(item, _priorityLookup);
                    maxScore = Math.Max(maxScore, itemScore);
                }
                slotMaxScores[i] = maxScore;

                foreach (int statId in constraintStatIds)
                {
                    double min = 0, max = 0; // Empty slot contributes 0
                    foreach (var item in slot.Items)
                    {
                        double val = item.Stats.GetValueOrDefault(statId);
                        min = Math.Min(min, val);
                        max = Math.Max(max, val);
                    }
                    slotMinStats[statId][i] = min;
                    slotMaxStats[statId][i] = max;
                }
            }

            // Suffix sums (from index i to end)
            _suffixMaxScore = new double[slotCount + 1];
            _suffixMinStat = [];
            _suffixMaxStat = [];

            for (int i = slotCount - 1; i >= 0; i--)
                _suffixMaxScore[i] = _suffixMaxScore[i + 1] + slotMaxScores[i];

            foreach (int statId in constraintStatIds)
            {
                var minArr = new double[slotCount + 1];
                var maxArr = new double[slotCount + 1];
                for (int i = slotCount - 1; i >= 0; i--)
                {
                    minArr[i] = minArr[i + 1] + slotMinStats[statId][i];
                    maxArr[i] = maxArr[i + 1] + slotMaxStats[statId][i];
                }
                _suffixMinStat[statId] = minArr;
                _suffixMaxStat[statId] = maxArr;
            }
        }

        public void Search(int depth)
        {
            if (_token.IsCancellationRequested)
            {
                TimedOut = true;
                return;
            }

            // Leaf node — we've assigned all slots
            if (depth == _slots.Count)
            {
                CombinationsEvaluated++;
                if (!SatisfiesAllConstraints())
                    return;

                RecordResult();
                return;
            }

            // Score-based pruning: even the best remaining items can't beat our worst top-N result
            if (_results.Count >= _topN && _currentScore + _suffixMaxScore[depth] <= _results.Keys[0])
                return;

            // Constraint-based pruning
            if (!CanSatisfyConstraints(depth))
                return;

            var slot = _slots[depth];

            // Try empty slot (contributes 0 to everything)
            _currentAssignments[depth] = (null, null, null);
            Search(depth + 1);
            if (TimedOut) return;

            // Try each compatible item (sorted best-first for better pruning)
            foreach (var item in slot.Items)
            {
                if (_usedItems.Contains(item.EquipmentId))
                    continue;

                // Apply item
                double itemScore = ApplyItem(item, depth);

                // Score-based pruning after applying this item
                if (_results.Count < _topN || _currentScore + _suffixMaxScore[depth + 1] > _results.Keys[0])
                    Search(depth + 1);

                if (TimedOut) return;

                // Undo item
                UndoItem(item, itemScore, depth);
            }
        }

        public List<SolverResult> GetResults() =>
            [.. _results.Values.OrderByDescending(r => r.Score)];

        private double ApplyItem(ItemInput item, int depth)
        {
            _usedItems.Add(item.EquipmentId);

            double itemScore = 0;
            foreach (var (statId, value) in item.Stats)
            {
                _currentStats.TryGetValue(statId, out double prev);
                _currentStats[statId] = prev + value;
            }
            foreach (var priority in _priorities)
            {
                if (item.Stats.TryGetValue(priority.StatTypeId, out double statValue))
                    itemScore += statValue * priority.Weight;
            }
            _currentScore += itemScore;
            _currentAssignments[depth] = (item.EquipmentId, item.Name, item.Stats);

            return itemScore;
        }

        private void UndoItem(ItemInput item, double itemScore, int depth)
        {
            _currentScore -= itemScore;
            foreach (var (statId, value) in item.Stats)
                _currentStats[statId] -= value;
            _usedItems.Remove(item.EquipmentId);
        }

        private bool SatisfiesAllConstraints()
        {
            foreach (var c in _constraints)
            {
                double value = _currentStats.GetValueOrDefault(c.StatTypeId);
                bool satisfied = c.Operator switch
                {
                    "<=" => value <= c.Value,
                    ">=" => value >= c.Value,
                    "==" => Math.Abs(value - c.Value) < 0.0001,
                    "<" => value < c.Value,
                    ">" => value > c.Value,
                    _ => true
                };
                if (!satisfied) return false;
            }
            return true;
        }

        private bool CanSatisfyConstraints(int depth)
        {
            foreach (var c in _constraints)
            {
                double current = _currentStats.GetValueOrDefault(c.StatTypeId);
                double minRemaining = _suffixMinStat.TryGetValue(c.StatTypeId, out var minArr) ? minArr[depth] : 0;
                double maxRemaining = _suffixMaxStat.TryGetValue(c.StatTypeId, out var maxArr) ? maxArr[depth] : 0;

                bool canSatisfy = c.Operator switch
                {
                    "<=" => current + minRemaining <= c.Value,
                    ">=" => current + maxRemaining >= c.Value,
                    "==" => current + minRemaining <= c.Value + 0.0001 && current + maxRemaining >= c.Value - 0.0001,
                    "<" => current + minRemaining < c.Value,
                    ">" => current + maxRemaining > c.Value,
                    _ => true
                };
                if (!canSatisfy) return false;
            }
            return true;
        }

        private void RecordResult()
        {
            var assignments = new List<SlotAssignment>(_slots.Count);
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var (eqId, eqName, itemStats) = _currentAssignments[i];
                assignments.Add(new SlotAssignment(slot.SlotId, slot.SlotName, eqId, eqName, itemStats ?? []));
            }

            var result = new SolverResult(assignments, _currentScore, new Dictionary<int, double>(_currentStats));

            if (_results.Count < _topN)
            {
                _results.Add(_currentScore, result);
            }
            else if (_currentScore > _results.Keys[0])
            {
                _results.RemoveAt(0);
                _results.Add(_currentScore, result);
            }
        }
    }

    /// <summary>
    /// Comparer that allows duplicate keys in SortedList by never returning 0.
    /// </summary>
    private sealed class DuplicateKeyComparer : IComparer<double>
    {
        public int Compare(double x, double y)
        {
            int result = x.CompareTo(y);
            return result == 0 ? 1 : result;
        }
    }
}
