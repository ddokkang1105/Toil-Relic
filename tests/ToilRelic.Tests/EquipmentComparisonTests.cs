using System.Text.Json;
using ToilRelic.Models;

namespace ToilRelic.Tests;

[Collection(ConsoleCollection.Name)]
public sealed class EquipmentComparisonTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ComparisonCases_MatchCanonicalFixtureWithoutMutatingPlayer()
    {
        var fixture = LoadFixture();
        using var catalog = EquipmentCatalogFixtureScope.Install(fixture.Definitions.Select(ToDefinition));

        foreach (var testCase in fixture.ComparisonCases)
        {
            var player = CreatePlayer(testCase.OwnedIds, testCase.Equipped);
            var slot = Enum.Parse<EquipmentSlot>(testCase.Slot);
            var before = JsonSerializer.Serialize(player.ToSaveData());

            var result = EquipmentComparisonEvaluator.Compare(player, slot, testCase.CandidateId);

            Assert.Equal(testCase.ExpectedReason, result.Reason.ToString());
            Assert.Equal(testCase.ExpectedIsValid, result.IsValid);
            Assert.Equal(testCase.ExpectedCanCommit, result.CanCommit);
            Assert.Equal(slot, result.DestinationSlot);
            Assert.Equal(NullIfEmpty(testCase.ExpectedCurrentId), result.Current?.Id);
            Assert.Equal(NullIfEmpty(testCase.ExpectedCandidateId), result.Candidate?.Id);
            Assert.Equal(testCase.ExpectedProjectedAttackBonus, result.ProjectedAttackBonus);
            Assert.Equal(testCase.ExpectedProjectedDefenseBonus, result.ProjectedDefenseBonus);
            Assert.Equal(testCase.ExpectedProjectedDamageReductionBonus, result.ProjectedDamageReductionBonus);
            Assert.Equal(testCase.ExpectedProjectedEquipmentMaxHpBonus, result.ProjectedEquipmentMaxHpBonus);
            Assert.Equal(testCase.ExpectedProjectedMaxHpDelta, result.ProjectedMaxHp - player.MaxHp);
            Assert.Equal(
                testCase.ExpectedDeltas.Select(ToExpectedDelta),
                result.StatDeltas.Select(delta => new ExpectedDelta(
                    delta.Stat.ToString(), delta.CurrentValue, delta.CandidateValue, delta.Delta)));
            Assert.Equal(before, JsonSerializer.Serialize(player.ToSaveData()));
        }
    }

    [Fact]
    public void UnequipCases_MatchCanonicalFixtureWithoutMutatingPlayer()
    {
        var fixture = LoadFixture();
        using var catalog = EquipmentCatalogFixtureScope.Install(fixture.Definitions.Select(ToDefinition));

        foreach (var testCase in fixture.UnequipCases)
        {
            var player = CreatePlayer(testCase.OwnedIds, testCase.Equipped);
            var slot = Enum.Parse<EquipmentSlot>(testCase.Slot);
            var before = JsonSerializer.Serialize(player.ToSaveData());

            var result = EquipmentComparisonEvaluator.EvaluateUnequip(player, slot);

            Assert.Equal(testCase.ExpectedStatus, result.Status.ToString());
            Assert.Equal(testCase.ExpectedIsOccupied, result.IsOccupied);
            Assert.Equal(testCase.ExpectedIsMandatory, result.IsMandatory);
            Assert.Equal(testCase.ExpectedCanCommit, result.CanCommit);
            Assert.Equal(slot, result.DestinationSlot);
            Assert.Equal(NullIfEmpty(testCase.ExpectedCurrentId), result.Current?.Id);
            Assert.Equal(before, JsonSerializer.Serialize(player.ToSaveData()));
        }
    }

    [Fact]
    public void CatalogFixture_RepeatedInstallRestoresProductionDefinitions()
    {
        var fixture = LoadFixture();
        var productionIds = EquipmentCatalog.All.Select(definition => definition.Id).ToArray();
        Assert.Equal(6, productionIds.Length);

        for (var iteration = 0; iteration < 2; iteration++)
        {
            using (EquipmentCatalogFixtureScope.Install(fixture.Definitions.Select(ToDefinition)))
            {
                Assert.Equal(productionIds.Length + fixture.Definitions.Length, EquipmentCatalog.All.Count);
            }

            Assert.Equal(productionIds, EquipmentCatalog.All.Select(definition => definition.Id));
            Assert.True(EquipmentCatalog.TryGet(EquipmentCatalog.StarterWeaponId, out _));
            Assert.True(EquipmentCatalog.TryGet(EquipmentCatalog.RewardWeaponId, out _));
        }
    }

    private static Player CreatePlayer(IEnumerable<string> ownedIds, IEnumerable<EquippedFixtureEntry> equipped)
    {
        var player = new Player("Fixture Hero");
        foreach (var id in ownedIds)
        {
            if (!player.OwnedEquipmentIds.Contains(id))
            {
                Assert.True(player.GrantEquipment(id), $"Fixture equipment should be grantable: {id}");
            }
        }

        foreach (var entry in equipped)
        {
            Assert.True(player.Equip(Enum.Parse<EquipmentSlot>(entry.Slot), entry.EquipmentId),
                $"Fixture equipment should be equippable: {entry.EquipmentId} -> {entry.Slot}");
        }

        return player;
    }

    private static EquipmentComparisonFixture LoadFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "EquipmentComparisonContracts.json");
        Assert.True(File.Exists(path), $"Canonical comparison fixture was not copied to test output: {path}");
        return JsonSerializer.Deserialize<EquipmentComparisonFixture>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Canonical comparison fixture could not be parsed.");
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    private static ExpectedDelta ToExpectedDelta(StatDeltaFixture delta) =>
        new(delta.Stat, delta.CurrentValue, delta.CandidateValue, delta.Delta);

    private static EquipmentDefinition ToDefinition(EquipmentDefinitionFixture fixture) =>
        new(
            fixture.Id,
            fixture.DisplayName,
            Enum.Parse<EquipmentCategory>(fixture.Category),
            fixture.AttackBonus,
            fixture.DefenseBonus,
            fixture.DamageReductionBonus,
            fixture.MaxHpBonus);

    private sealed record ExpectedDelta(string Stat, int CurrentValue, int CandidateValue, int Delta);

    private sealed class EquipmentComparisonFixture
    {
        public EquipmentDefinitionFixture[] Definitions { get; set; } = [];
        public ComparisonCaseFixture[] ComparisonCases { get; set; } = [];
        public UnequipCaseFixture[] UnequipCases { get; set; } = [];
    }

    private sealed class EquipmentDefinitionFixture
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int AttackBonus { get; set; }
        public int DefenseBonus { get; set; }
        public int DamageReductionBonus { get; set; }
        public int MaxHpBonus { get; set; }
    }

    private sealed class ComparisonCaseFixture
    {
        public string Name { get; set; } = string.Empty;
        public string Slot { get; set; } = string.Empty;
        public string CandidateId { get; set; } = string.Empty;
        public string[] OwnedIds { get; set; } = [];
        public EquippedFixtureEntry[] Equipped { get; set; } = [];
        public string ExpectedReason { get; set; } = string.Empty;
        public bool ExpectedIsValid { get; set; }
        public bool ExpectedCanCommit { get; set; }
        public string? ExpectedCurrentId { get; set; }
        public string? ExpectedCandidateId { get; set; }
        public StatDeltaFixture[] ExpectedDeltas { get; set; } = [];
        public int ExpectedProjectedAttackBonus { get; set; }
        public int ExpectedProjectedDefenseBonus { get; set; }
        public int ExpectedProjectedDamageReductionBonus { get; set; }
        public int ExpectedProjectedEquipmentMaxHpBonus { get; set; }
        public int ExpectedProjectedMaxHpDelta { get; set; }
    }

    private sealed class UnequipCaseFixture
    {
        public string Name { get; set; } = string.Empty;
        public string Slot { get; set; } = string.Empty;
        public string[] OwnedIds { get; set; } = [];
        public EquippedFixtureEntry[] Equipped { get; set; } = [];
        public string ExpectedStatus { get; set; } = string.Empty;
        public bool ExpectedIsOccupied { get; set; }
        public bool ExpectedIsMandatory { get; set; }
        public bool ExpectedCanCommit { get; set; }
        public string? ExpectedCurrentId { get; set; }
    }

    private sealed class EquippedFixtureEntry
    {
        public string Slot { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
    }

    private sealed class StatDeltaFixture
    {
        public string Stat { get; set; } = string.Empty;
        public int CurrentValue { get; set; }
        public int CandidateValue { get; set; }
        public int Delta { get; set; }
    }
}
