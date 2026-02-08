using EquipmentSolver.Core.Solver;

namespace EquipmentSolver.Tests.Solver;

public class SolverEngineTests
{
    // --- Helpers ---

    private static SlotInput Slot(int id, string name, params ItemInput[] items) =>
        new(id, name, [.. items]);

    private static ItemInput Item(int id, string name, params (int statId, double value)[] stats) =>
        new(id, name, stats.ToDictionary(s => s.statId, s => s.value));

    private static ConstraintInput Constraint(int statId, string op, double value) =>
        new(statId, op, value);

    private static PriorityInput Priority(int statId, double weight) =>
        new(statId, weight);

    // --- Tests ---

    [Fact]
    public void EmptySlots_ReturnsNoResults()
    {
        var result = SolverEngine.Solve([], [Constraint(1, "<=", 100)], [Priority(1, 1.0)]);
        Assert.Empty(result.Results);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public void EmptyPriorities_ReturnsNoResults()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head", Item(1, "Helmet", (1, 10)))
        };
        var result = SolverEngine.Solve(slots, [], []);
        Assert.Empty(result.Results);
    }

    [Fact]
    public void SingleSlotSingleItem_ReturnsOptimalResult()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head", Item(1, "Helmet", (1, 10)))
        };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, [], priorities);

        Assert.NotEmpty(result.Results);
        Assert.Equal(10, result.Results[0].Score);
        Assert.Equal(1, result.Results[0].Assignments[0].EquipmentId);
    }

    [Fact]
    public void SingleSlotMultipleItems_PicksBestItem()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head",
                Item(1, "Leather Cap", (1, 5)),
                Item(2, "Iron Helm", (1, 15)),
                Item(3, "Dragon Helm", (1, 25)))
        };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, [], priorities, topN: 3);

        Assert.Equal(3, result.Results.Count);
        Assert.Equal(25, result.Results[0].Score); // Dragon Helm
        Assert.Equal(15, result.Results[1].Score); // Iron Helm
        Assert.Equal(5, result.Results[2].Score);  // Leather Cap
    }

    [Fact]
    public void MultipleSlots_FindsBestCombination()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head", Item(1, "Helmet", (1, 10), (2, 5))),
            Slot(2, "Chest", Item(2, "Plate", (1, 20), (2, 15)))
        };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, [], priorities);

        Assert.Equal(30, result.Results[0].Score); // 10 + 20
        Assert.Equal(2, result.Results[0].Assignments.Count);
    }

    [Fact]
    public void Constraint_ExcludesSolutionsViolatingLimit()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head",
                Item(1, "Heavy Helm", (1, 30), (2, 20)),   // armor=30, weight=20
                Item(2, "Light Helm", (1, 10), (2, 5))),   // armor=10, weight=5
            Slot(2, "Chest",
                Item(3, "Heavy Plate", (1, 50), (2, 25)),  // armor=50, weight=25
                Item(4, "Light Shirt", (1, 15), (2, 8)))   // armor=15, weight=8
        };
        // Maximize armor, but weight <= 30
        var constraints = new List<ConstraintInput> { Constraint(2, "<=", 30) };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, constraints, priorities, topN: 10);

        // All returned solutions must have weight <= 30
        foreach (var r in result.Results)
        {
            double totalWeight = r.StatTotals.GetValueOrDefault(2);
            Assert.True(totalWeight <= 30, $"Weight {totalWeight} exceeds limit 30");
        }

        // Best valid combo: Light Helm(5) + Heavy Plate(25) = 30 weight, 60 armor
        Assert.Equal(60, result.Results[0].Score);
    }

    [Fact]
    public void Constraint_GreaterThanOrEqual()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Ring",
                Item(1, "Fire Ring", (1, 5), (2, 20)),  // armor=5, fire_res=20
                Item(2, "Iron Ring", (1, 15), (2, 0)))  // armor=15, fire_res=0
        };
        // Maximize armor, but fire_res >= 10
        var constraints = new List<ConstraintInput> { Constraint(2, ">=", 10) };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, constraints, priorities);

        // Only Fire Ring satisfies fire_res >= 10
        Assert.NotEmpty(result.Results);
        Assert.Equal(1, result.Results[0].Assignments[0].EquipmentId);
    }

    [Fact]
    public void EmptySlot_AllowedWhenBetter()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head", Item(1, "Cursed Helm", (1, -10)))  // Negative armor
        };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, [], priorities);

        // Empty slot (score=0) should beat Cursed Helm (score=-10)
        Assert.Equal(0, result.Results[0].Score);
        Assert.Null(result.Results[0].Assignments[0].EquipmentId);
    }

    [Fact]
    public void SharedItem_CanOnlyGoInOneSlot()
    {
        // Gold Ring is compatible with both Ring1 and Ring2
        var goldRing = Item(1, "Gold Ring", (1, 10));
        var silverRing = Item(2, "Silver Ring", (1, 5));

        var slots = new List<SlotInput>
        {
            Slot(1, "Ring1", goldRing, silverRing),
            Slot(2, "Ring2", goldRing, silverRing)
        };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, [], priorities);

        // Best result: Gold Ring in one slot, Silver Ring in the other = 15
        Assert.Equal(15, result.Results[0].Score);

        // Verify no result has the same item in multiple slots
        foreach (var r in result.Results)
        {
            var equippedIds = r.Assignments
                .Where(a => a.EquipmentId.HasValue)
                .Select(a => a.EquipmentId!.Value)
                .ToList();
            Assert.Equal(equippedIds.Count, equippedIds.Distinct().Count());
        }
    }

    [Fact]
    public void TopN_ReturnsRequestedCount()
    {
        var items = Enumerable.Range(1, 20)
            .Select(i => Item(i, $"Item {i}", (1, i * 5.0)))
            .ToArray();

        var slots = new List<SlotInput> { Slot(1, "Slot", items) };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result3 = SolverEngine.Solve(slots, [], priorities, topN: 3);
        var result7 = SolverEngine.Solve(slots, [], priorities, topN: 7);

        Assert.Equal(3, result3.Results.Count);
        Assert.Equal(7, result7.Results.Count);

        // Results should be sorted descending by score
        for (int i = 1; i < result7.Results.Count; i++)
            Assert.True(result7.Results[i - 1].Score >= result7.Results[i].Score);
    }

    [Fact]
    public void MultiplePriorities_WeightsApplied()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head",
                Item(1, "Armor Focus", (1, 20), (2, 5)),    // armor=20, fire=5
                Item(2, "Fire Focus", (1, 5), (2, 20)))     // armor=5, fire=20
        };

        // Armor weight=1.0, Fire weight=0.5
        var priorities = new List<PriorityInput>
        {
            Priority(1, 1.0),
            Priority(2, 0.5)
        };

        var result = SolverEngine.Solve(slots, [], priorities);

        // Armor Focus: 20*1.0 + 5*0.5 = 22.5
        // Fire Focus: 5*1.0 + 20*0.5 = 15
        Assert.Equal(22.5, result.Results[0].Score);
        Assert.Equal(1, result.Results[0].Assignments[0].EquipmentId);
    }

    [Fact]
    public void NegativePriorityWeight_MinimizesStat()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head",
                Item(1, "Heavy Helm", (1, 20), (2, 15)),
                Item(2, "Light Helm", (1, 10), (2, 3)))
        };

        // Maximize armor (+1.0), minimize weight (-1.0)
        var priorities = new List<PriorityInput>
        {
            Priority(1, 1.0),
            Priority(2, -1.0)
        };

        var result = SolverEngine.Solve(slots, [], priorities);

        // Heavy: 20*1.0 + 15*(-1.0) = 5
        // Light: 10*1.0 + 3*(-1.0) = 7
        Assert.Equal(7, result.Results[0].Score);
        Assert.Equal(2, result.Results[0].Assignments[0].EquipmentId);
    }

    [Fact]
    public void StatTotals_CorrectlySummed()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head", Item(1, "Helmet", (1, 10), (2, 5))),
            Slot(2, "Chest", Item(2, "Plate", (1, 20), (2, 15))),
            Slot(3, "Legs", Item(3, "Greaves", (1, 12), (2, 8)))
        };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, [], priorities);

        var best = result.Results[0];
        Assert.Equal(42, best.StatTotals[1]); // armor: 10+20+12
        Assert.Equal(28, best.StatTotals[2]); // weight: 5+15+8
    }

    [Fact]
    public void NoValidCombinations_ReturnsEmpty()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head", Item(1, "Helmet", (1, 10), (2, 50)))
        };
        // Weight <= 10, but helmet has weight 50
        var constraints = new List<ConstraintInput> { Constraint(2, "<=", 10) };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, constraints, priorities);

        // The only valid solution is empty slot (weight=0, armor=0)
        Assert.Single(result.Results);
        Assert.Null(result.Results[0].Assignments[0].EquipmentId);
        Assert.Equal(0, result.Results[0].Score);
    }

    [Fact]
    public void Cancellation_StopsEarly()
    {
        // Create a large search space
        var items = Enumerable.Range(1, 30)
            .Select(i => Item(i, $"Item {i}", (1, i * 1.0)))
            .ToArray();

        var slots = Enumerable.Range(1, 8)
            .Select(i => Slot(i, $"Slot{i}", items))
            .ToList();

        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var result = SolverEngine.Solve(slots, [], priorities, cancellationToken: cts.Token);

        Assert.True(result.TimedOut);
        // Should still return whatever best results were found so far
    }

    [Fact]
    public void EqualityConstraint_Works()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Ring",
                Item(1, "Ring A", (1, 10)),
                Item(2, "Ring B", (1, 15)),
                Item(3, "Ring C", (1, 20)))
        };
        // Exact match: stat 1 must equal 15
        var constraints = new List<ConstraintInput> { Constraint(1, "==", 15) };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, constraints, priorities);

        Assert.Single(result.Results);
        Assert.Equal(15, result.Results[0].Score);
        Assert.Equal(2, result.Results[0].Assignments[0].EquipmentId);
    }

    [Fact]
    public void MultipleConstraints_AllMustBeSatisfied()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head",
                Item(1, "A", (1, 20), (2, 10), (3, 5)),  // armor, weight, fire_res
                Item(2, "B", (1, 10), (2, 5), (3, 15)),
                Item(3, "C", (1, 15), (2, 8), (3, 10)))
        };
        // weight <= 8 AND fire_res >= 10
        var constraints = new List<ConstraintInput>
        {
            Constraint(2, "<=", 8),
            Constraint(3, ">=", 10)
        };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, constraints, priorities);

        foreach (var r in result.Results)
        {
            double weight = r.StatTotals.GetValueOrDefault(2);
            double fireRes = r.StatTotals.GetValueOrDefault(3);
            Assert.True(weight <= 8, $"Weight {weight} exceeds 8");
            Assert.True(fireRes >= 10, $"Fire res {fireRes} < 10");
        }
    }

    [Fact]
    public void NoItems_InSlot_OnlyEmptySlotPossible()
    {
        var slots = new List<SlotInput>
        {
            Slot(1, "Head"),  // No items
            Slot(2, "Chest", Item(1, "Plate", (1, 20)))
        };
        var priorities = new List<PriorityInput> { Priority(1, 1.0) };

        var result = SolverEngine.Solve(slots, [], priorities);

        Assert.NotEmpty(result.Results);
        // Head slot should be empty in the best result
        Assert.Null(result.Results[0].Assignments[0].EquipmentId);
        Assert.Equal(20, result.Results[0].Score);
    }
}
