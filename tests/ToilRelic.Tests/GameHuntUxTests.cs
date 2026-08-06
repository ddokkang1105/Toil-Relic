using ToilRelic.Models;
using ToilRelic.Systems;

namespace ToilRelic.Tests;

[Collection(ConsoleCollection.Name)]
public sealed class GameHuntUxTests
{
    [Fact]
    public void Run_ContractShowsLegibleRewardsAndConfirmedSecondQuarryNeverRerolls()
    {
        using var fixture = new GameFixture();
        Assert.True(fixture.System.Save(new Player("Hunter")).Succeeded);
        var runtime = new FakeHuntRuntime(new CombatResult(true, false, false, "victory"), new Loot(2, 1, 1), 0.1d);

        var capture = CaptureUntilInputEnds(new ScriptedTextReader("1", "1", "2", "1", ""),
            () => new Game(fixture.System, runtime).Run());

        Assert.Equal("rust-golem", runtime.LastEnemyId);
        Assert.Contains("Rust Golem", capture.Output);
        Assert.Contains("35% chance: Rustguard Plate", capture.Output);
        Assert.Contains("Guaranteed first-win contribution: Rustheart Core", capture.Output);
        Assert.Contains("Project contribution acquired: Rustheart Core", capture.Output);
        var loaded = fixture.System.Load().Player!;
        Assert.Contains("rustheart-core", loaded.RelicProject.CompletedContributionIds);
        Assert.DoesNotContain(EquipmentCatalog.RewardWeaponId, loaded.OwnedEquipmentIds);
    }

    [Fact]
    public void Run_CancelContract_LeavesSaveBytesUnchangedAndDoesNotEnterCombat()
    {
        using var fixture = new GameFixture();
        Assert.True(fixture.System.Save(new Player("Hunter")).Succeeded);
        var before = File.ReadAllBytes(fixture.SavePath);
        var runtime = new FakeHuntRuntime(new CombatResult(true, false, false, "unused"), new Loot(1, 0, 0), 0d);

        CaptureUntilInputEnds(new ScriptedTextReader("1", "1", "0", ""),
            () => new Game(fixture.System, runtime).Run());

        Assert.Null(runtime.LastEnemyId);
        Assert.Equal(before, File.ReadAllBytes(fixture.SavePath));
    }

    [Fact]
    public void Run_ReplayStatesNoProjectProgressAndDefeatGrantsNothing()
    {
        using var fixture = new GameFixture();
        var player = new Player("Hunter");
        Assert.True(player.RelicProject.TryAddContribution("chitin-shard"));
        Assert.True(fixture.System.Save(player).Succeeded);
        var runtime = new FakeHuntRuntime(new CombatResult(false, false, true, "timeout"), new Loot(3, 1, 1), 0d);

        var capture = CaptureUntilInputEnds(new ScriptedTextReader("1", "1", "1", "1", ""),
            () => new Game(fixture.System, runtime).Run());

        Assert.Contains("Replay: no additional project progress", capture.Output);
        var loaded = fixture.System.Load().Player!;
        Assert.Equal(new[] { "chitin-shard" }, loaded.RelicProject.CompletedContributionIds);
        Assert.Equal(0, loaded.Inventory[ItemType.Junk]);
        Assert.DoesNotContain(EquipmentCatalog.VerminFangId, loaded.OwnedEquipmentIds);
    }

    [Fact]
    public void Run_StaleProfileAtVictoryRejectsRewardsWithoutSaving()
    {
        using var fixture = new GameFixture();
        Assert.True(fixture.System.Save(new Player("Hunter")).Succeeded);
        var before = File.ReadAllBytes(fixture.SavePath);
        var profiles = PurposefulHuntContent.Profiles.ToList();
        var runtime = new FakeHuntRuntime(
            new CombatResult(true, false, false, "victory"),
            new Loot(2, 1, 1),
            0d,
            () => profiles[0] = profiles[0] with { EquipmentId = EquipmentCatalog.RewardWeaponId });

        var capture = CaptureUntilInputEnds(new ScriptedTextReader("1", "1", "1", "1", ""),
            () => new Game(fixture.System, runtime, huntProfiles: profiles).Run());

        Assert.Contains("Hunt reward failed", capture.Output);
        Assert.DoesNotContain("Save:", capture.Output);
        Assert.Equal(before, File.ReadAllBytes(fixture.SavePath));
        var loaded = fixture.System.Load().Player!;
        Assert.Empty(loaded.RelicProject.CompletedContributionIds);
        Assert.Equal(0, loaded.Inventory[ItemType.Junk]);
        Assert.DoesNotContain(EquipmentCatalog.VerminFangId, loaded.OwnedEquipmentIds);
    }

    [Fact]
    public void Run_ReadyForgeGrantsRelicOnceAndOpensNecklaceComparisonWithoutAutoEquip()
    {
        using var fixture = new GameFixture();
        var player = new Player("Smith");
        foreach (var id in new[] { "chitin-shard", "rustheart-core", "wraith-ash" })
        {
            Assert.True(player.RelicProject.TryAddContribution(id));
        }
        Assert.True(fixture.System.Save(player).Succeeded);

        var capture = CaptureUntilInputEnds(new ScriptedTextReader("1", "7", "0", ""),
            () => new Game(fixture.System, new FakeHuntRuntime()).Run());

        Assert.Contains("Toilbound Relic forged", capture.Output);
        Assert.Contains("Destination: Necklace", capture.Output);
        Assert.Contains("Candidate: Toilbound Relic", capture.Output);
        var loaded = fixture.System.Load().Player!;
        Assert.True(loaded.RelicProject.IsForged);
        Assert.Contains(EquipmentCatalog.ToilboundRelicId, loaded.OwnedEquipmentIds);
        Assert.False(loaded.TryGetEquippedEquipment(EquipmentSlot.Necklace, out _));
    }

    private static ConsoleCapture CaptureUntilInputEnds(TextReader input, Action action)
    {
        var originalIn = Console.In;
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        var ended = false;
        try
        {
            Console.SetIn(input);
            Console.SetOut(output);
            Console.SetError(error);
            try { action(); } catch (EndOfInputException) { ended = true; }
            Assert.True(ended);
            return new ConsoleCapture(output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private sealed record ConsoleCapture(string Output, string Error);

    private sealed class ScriptedTextReader(params string[] values) : TextReader
    {
        private readonly Queue<string> values = new(values);
        public override string? ReadLine() => values.Count > 0 ? values.Dequeue() : throw new EndOfInputException();
    }

    private sealed class EndOfInputException : Exception;

    private sealed class FakeHuntRuntime : IHuntRuntime
    {
        private readonly CombatResult _combat;
        private readonly Loot _loot;
        private readonly double _profileRoll;
        private readonly Action? _onFight;

        public FakeHuntRuntime(
            CombatResult? combat = null,
            Loot? loot = null,
            double profileRoll = 0d,
            Action? onFight = null)
        {
            _combat = combat ?? new CombatResult(true, false, false, "victory");
            _loot = loot ?? new Loot(1, 0, 0);
            _profileRoll = profileRoll;
            _onFight = onFight;
        }

        public string? LastEnemyId { get; private set; }
        public CombatResult Fight(Player player, Enemy enemy)
        {
            LastEnemyId = enemy.Id;
            _onFight?.Invoke();
            return _combat;
        }
        public Loot RollLoot() => _loot;
        public double RollProfile() => _profileRoll;
    }

    private sealed class GameFixture : IDisposable
    {
        private readonly string directory = Directory.CreateTempSubdirectory("toil-relic-hunt-ux-").FullName;
        public string SavePath => Path.Combine(directory, "savegame.json");
        public SaveSystem System => new(SavePath);
        public void Dispose() => Directory.Delete(directory, recursive: true);
    }
}
