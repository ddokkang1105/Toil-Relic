using System.Text.Json;
using ToilRelic.Systems;

namespace ToilRelic.Tests;

[Collection(ConsoleCollection.Name)]
public sealed class TacticalHuntGrammarTests
{
    [Fact]
    public void SharedFixture_DescribesCanonicalIntentsAndProfiles()
    {
        var fixture = TacticalHuntGrammarFixture.Load();

        Assert.Equal(1, fixture.SchemaVersion);
        foreach (var expected in fixture.Intents)
        {
            var kind = Enum.Parse<EnemyIntentKind>(expected.Kind);
            var actual = TacticalCombatRules.Describe(kind);
            Assert.Equal(expected.Label, actual.Label);
            Assert.Equal(expected.Marker, actual.Marker);
            Assert.Equal(expected.Cue, actual.Cue);
            Assert.Equal(expected.PlayerAttackBonus,
                TacticalCombatRules.ApplyPlayerAttack(10, actual) - 10);
            Assert.Equal(expected.EnemyDamageBonus,
                TacticalCombatRules.ApplyEnemyAttack(10, actual, false) - 10);
            Assert.Equal(expected.DefendReduction,
                TacticalCombatRules.ApplyEnemyAttack(10, actual, false) -
                TacticalCombatRules.ApplyEnemyAttack(10, actual, true));
        }

        foreach (var profile in fixture.Profiles)
        {
            for (var turn = 0; turn < profile.Sequence.Length; turn++)
            {
                Assert.Equal(profile.Sequence[turn],
                    TacticalCombatRules.GetIntent(profile.EnemyId, turn).Kind.ToString());
            }
        }
    }

    [Fact]
    public void PowerAttackAndOpening_RewardDifferentResponses()
    {
        var power = TacticalCombatRules.Describe(EnemyIntentKind.PowerAttack);
        var opening = TacticalCombatRules.Describe(EnemyIntentKind.ExposedOpening);

        Assert.Equal(10, TacticalCombatRules.ApplyEnemyAttack(8, power, false));
        Assert.Equal(4, TacticalCombatRules.ApplyEnemyAttack(8, power, true));
        Assert.Equal(11, TacticalCombatRules.ApplyPlayerAttack(8, opening));
        Assert.Equal(8, TacticalCombatRules.ApplyPlayerAttack(8, power));
    }

    [Fact]
    public void Fight_RevealsLockedIntentBeforeInputAndInvalidPotionDoesNotAdvanceIt()
    {
        var player = new ToilRelic.Models.Player("Hunter");
        var enemy = new ToilRelic.Models.Enemy("mine-vermin", "Test Vermin", 1, 0, 0, null);
        var captured = CaptureFight(player, enemy, new Random(1), "3", "1");

        Assert.True(captured.Result.PlayerWon);
        Assert.False(captured.Result.PlayerFled);
        Assert.False(captured.Result.TimeExpired);
        Assert.Contains("HP is already full.", captured.Result.Log);
        Assert.Equal(2, CountOccurrences(captured.Output, "Exposed Opening [OPEN]"));
    }

    [Fact]
    public void Fight_DefendAndValidPotionResolveSuccessiveLockedIntents()
    {
        const int seed = 19;
        var player = new ToilRelic.Models.Player("Hunter");
        player.TakeDamage(20);
        player.AddItem(ToilRelic.Models.ItemType.HealingPotion, 1);
        var hpBefore = player.Hp;
        var enemy = new ToilRelic.Models.Enemy("mine-vermin", "Test Vermin", 1, 0, 0, null);
        var expectedRandom = new Random(seed);
        var opening = TacticalCombatRules.GetIntent(enemy.Id, 0);
        var power = TacticalCombatRules.GetIntent(enemy.Id, 1);
        var openingDamage = TacticalCombatRules.ApplyEnemyAttack(expectedRandom.Next(0, 3), opening, true);
        var powerDamage = TacticalCombatRules.ApplyEnemyAttack(expectedRandom.Next(0, 3), power, false);

        var captured = CaptureFight(player, enemy, new Random(seed), "2", "3", "1");

        Assert.True(captured.Result.PlayerWon);
        Assert.Equal(0, player.Inventory[ToilRelic.Models.ItemType.HealingPotion]);
        Assert.Equal(hpBefore - openingDamage + 12 - powerDamage, player.Hp);
        Assert.Equal(2, CountOccurrences(captured.Output, "Exposed Opening [OPEN]"));
        Assert.Equal(1, CountOccurrences(captured.Output, "Power Attack [POWER]"));
        Assert.Contains("You brace for the revealed intent.", captured.Result.Log);
        Assert.Contains("You used a healing potion", captured.Result.Log);
    }

    [Fact]
    public void Fight_MissingPotionRetainsIntentUntilAValidAction()
    {
        var player = new ToilRelic.Models.Player("Hunter");
        player.TakeDamage(1);
        var enemy = new ToilRelic.Models.Enemy("mine-vermin", "Test Vermin", 1, 0, 0, null);

        var captured = CaptureFight(player, enemy, new Random(2), "3", "1");

        Assert.True(captured.Result.PlayerWon);
        Assert.Contains("No healing potion in inventory.", captured.Result.Log);
        Assert.Equal(2, CountOccurrences(captured.Output, "Exposed Opening [OPEN]"));
        Assert.DoesNotContain("Power Attack [POWER]", captured.Output);
    }

    [Fact]
    public void Fight_NonlethalAttackThenSuccessfulFleeStopsBeforeAnotherEnemyResponse()
    {
        var seed = FindSeedForFleeAfterOneResolvedTurn(shouldSucceed: true);
        var player = new ToilRelic.Models.Player("Hunter");
        var enemy = new ToilRelic.Models.Enemy("mine-vermin", "Durable Vermin", 100, 0, 0, null);

        var captured = CaptureFight(player, enemy, new Random(seed), "1", "4");

        Assert.False(captured.Result.PlayerWon);
        Assert.True(captured.Result.PlayerFled);
        Assert.Contains("Durable Vermin hits Hunter", captured.Result.Log);
        Assert.Contains("Escape successful.", captured.Result.Log);
        Assert.Equal(1, CountOccurrences(captured.Result.Log, "Durable Vermin hits Hunter"));
        Assert.Contains("Power Attack [POWER]", captured.Output);
    }

    [Fact]
    public void Fight_FailedFleeConsumesIntentAndAllowsTheNextAction()
    {
        var seed = FindSeedForFirstFlee(shouldSucceed: false);
        var player = new ToilRelic.Models.Player("Hunter");
        var enemy = new ToilRelic.Models.Enemy("mine-vermin", "Test Vermin", 1, 0, 0, null);

        var captured = CaptureFight(player, enemy, new Random(seed), "4", "1");

        Assert.True(captured.Result.PlayerWon);
        Assert.False(captured.Result.PlayerFled);
        Assert.Contains("Escape failed.", captured.Result.Log);
        Assert.Contains("Test Vermin hits Hunter", captured.Result.Log);
        Assert.Contains("Power Attack [POWER]", captured.Output);
    }

    [Fact]
    public void Fight_LethalEnemyResponseReturnsDefeat()
    {
        var player = new ToilRelic.Models.Player("Hunter");
        player.TakeDamage(player.MaxHp - 1);
        var enemy = new ToilRelic.Models.Enemy("unknown-enemy", "Lethal Enemy", 100, 10, 0, null);

        var captured = CaptureFight(player, enemy, new Random(3), "2");

        Assert.False(captured.Result.PlayerWon);
        Assert.False(captured.Result.PlayerFled);
        Assert.False(player.IsAlive);
        Assert.Contains("You collapsed.", captured.Result.Log);
    }

    private static CapturedFight CaptureFight(
        ToilRelic.Models.Player player,
        ToilRelic.Models.Enemy enemy,
        Random random,
        params string[] actions)
    {
        var originalIn = Console.In;
        var originalOut = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetIn(new ScriptedTextReader(actions));
            Console.SetOut(output);
            var result = new CombatSystem(random).Fight(player, enemy);
            return new CapturedFight(result, output.ToString());
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }
    }

    private static int FindSeedForFirstFlee(bool shouldSucceed) =>
        FindSeed(random => (random.NextDouble() < 0.55d) == shouldSucceed);

    private static int FindSeedForFleeAfterOneResolvedTurn(bool shouldSucceed) => FindSeed(random =>
    {
        random.Next(4, 9);
        random.Next(0, 3);
        return (random.NextDouble() < 0.55d) == shouldSucceed;
    });

    private static int FindSeed(Func<Random, bool> predicate)
    {
        for (var seed = 0; seed < 1000; seed++)
        {
            if (predicate(new Random(seed)))
            {
                return seed;
            }
        }

        throw new InvalidOperationException("No deterministic combat seed matched the requested outcome.");
    }

    private static int CountOccurrences(string value, string marker)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(marker, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += marker.Length;
        }

        return count;
    }

    private sealed class ScriptedTextReader(params string[] values) : TextReader
    {
        private readonly Queue<string> values = new(values);
        public override string? ReadLine() => values.Dequeue();
    }

    private sealed record CapturedFight(CombatResult Result, string Output);
}

internal static class TacticalHuntGrammarFixture
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static TacticalHuntGrammarContract Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "TacticalHuntGrammarContracts.json");
        return JsonSerializer.Deserialize<TacticalHuntGrammarContract>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("Tactical Hunt Grammar fixture is empty.");
    }
}

internal sealed class TacticalHuntGrammarContract
{
    public int SchemaVersion { get; set; }
    public TacticalIntentFixture[] Intents { get; set; } = Array.Empty<TacticalIntentFixture>();
    public TacticalProfileFixture[] Profiles { get; set; } = Array.Empty<TacticalProfileFixture>();
}

internal sealed class TacticalIntentFixture
{
    public string Kind { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Marker { get; set; } = string.Empty;
    public string Cue { get; set; } = string.Empty;
    public int PlayerAttackBonus { get; set; }
    public int EnemyDamageBonus { get; set; }
    public int DefendReduction { get; set; }
}

internal sealed class TacticalProfileFixture
{
    public string EnemyId { get; set; } = string.Empty;
    public string[] Sequence { get; set; } = Array.Empty<string>();
}
