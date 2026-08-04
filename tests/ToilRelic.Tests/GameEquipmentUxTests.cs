using ToilRelic.Models;
using ToilRelic.Systems;

namespace ToilRelic.Tests;

[Collection(ConsoleCollection.Name)]
public sealed class GameEquipmentUxTests
{
    [Fact]
    public void Run_PreviewRewardThenCancel_LeavesSaveBytesUnchangedAndUnwindsStepwise()
    {
        using var fixture = new GameFixture();
        var player = CreatePlayerWithReward();
        Assert.True(fixture.System.Save(player).Succeeded);
        var before = File.ReadAllBytes(fixture.SavePath);

        var capture = CaptureUntilInputEnds(
            new ScriptedTextReader("1", "5", "1", "1", "0", "0", "0"),
            () => new Game(fixture.System).Run());

        Assert.Contains("Destination: PrimaryWeapon", capture.Output);
        Assert.Contains("Current: Starter Weapon", capture.Output);
        Assert.Contains("Candidate: Reward Weapon", capture.Output);
        Assert.Contains("Attack: 0 -> 2 (+2)", capture.Output);
        Assert.Contains("Projected: Attack +2, Defense +0, Damage reduction +0, Max HP ", capture.Output);
        Assert.Equal(before, File.ReadAllBytes(fixture.SavePath));
        Assert.Equal(2, CountOccurrences(capture.Output, "[Equipment slot]"));
        Assert.Equal(string.Empty, capture.Error);
    }

    [Fact]
    public void Run_PreviewStarterFromReward_ShowsNegativeAttackDeltaWithoutSaving()
    {
        using var fixture = new GameFixture();
        var player = CreatePlayerWithReward();
        Assert.True(player.Equip(EquipmentSlot.PrimaryWeapon, EquipmentCatalog.RewardWeaponId));
        Assert.True(fixture.System.Save(player).Succeeded);
        var before = File.ReadAllBytes(fixture.SavePath);

        var capture = CaptureUntilInputEnds(
            new ScriptedTextReader("1", "5", "1", "2", "0", "0", "0"),
            () => new Game(fixture.System).Run());

        Assert.Contains("Current: Reward Weapon", capture.Output);
        Assert.Contains("Candidate: Starter Weapon", capture.Output);
        Assert.Contains("Attack: 2 -> 0 (-2)", capture.Output);
        Assert.Contains("Projected: Attack +0, Defense +0, Damage reduction +0, Max HP ", capture.Output);
        Assert.Equal(before, File.ReadAllBytes(fixture.SavePath));
        Assert.Equal(string.Empty, capture.Error);
    }

    [Fact]
    public void Run_ConfirmReward_RevalidatesSavesAndReloadsReplacement()
    {
        using var fixture = new GameFixture();
        Assert.True(fixture.System.Save(CreatePlayerWithReward()).Succeeded);

        var capture = CaptureUntilInputEnds(
            new ScriptedTextReader("1", "5", "1", "1", "1"),
            () => new Game(fixture.System).Run());
        var loaded = fixture.System.Load();

        Assert.Contains("Equipment equipped.", capture.Output);
        Assert.Contains("Save: Saved just now", capture.Output);
        Assert.Equal(LoadStatus.Loaded, loaded.Status);
        Assert.True(loaded.Player!.TryGetPrimaryWeapon(out var equipped));
        Assert.Equal(EquipmentCatalog.RewardWeaponId, equipped.Id);
    }

    [Fact]
    public void Run_SameItemAndMandatoryUnequip_DoNotChangeSaveBytes()
    {
        using var fixture = new GameFixture();
        Assert.True(fixture.System.Save(CreatePlayerWithReward()).Succeeded);
        var before = File.ReadAllBytes(fixture.SavePath);

        var sameItem = CaptureUntilInputEnds(
            new ScriptedTextReader("1", "5", "1", "2", "1", "0"),
            () => new Game(fixture.System).Run());

        Assert.Contains("No equipment stat change (±0)", sameItem.Output);
        Assert.Contains("1. Equip (unavailable: That item is already equipped.)", sameItem.Output);
        Assert.Contains("잘못된 입력.", sameItem.Output);
        Assert.DoesNotContain("Equipment equipped.", sameItem.Output);
        Assert.Equal(before, File.ReadAllBytes(fixture.SavePath));

        var mandatoryUnequip = CaptureUntilInputEnds(
            new ScriptedTextReader("1", "5", "1", "3", "1", "0"),
            () => new Game(fixture.System).Run());

        Assert.Contains("1. Unequip (unavailable: Primary weapon cannot be unequipped.)", mandatoryUnequip.Output);
        Assert.Contains("잘못된 입력.", mandatoryUnequip.Output);
        Assert.DoesNotContain("Equipment removed.", mandatoryUnequip.Output);
        Assert.Equal(before, File.ReadAllBytes(fixture.SavePath));
    }

    [Fact]
    public void Run_RingEquippedInRingOne_IsAbsentFromRingTwoCandidates()
    {
        using var catalog = EquipmentCatalogFixtureScope.Install(CurrentRing, AllStatRing);
        using var fixture = new GameFixture();
        var player = new Player("Wanderer");
        Assert.True(player.GrantEquipment(CurrentRing.Id));
        Assert.True(player.GrantEquipment(AllStatRing.Id));
        Assert.True(player.Equip(EquipmentSlot.Ring1, CurrentRing.Id));
        Assert.True(fixture.System.Save(player).Succeeded);
        var before = File.ReadAllBytes(fixture.SavePath);

        var capture = CaptureUntilInputEnds(
            new ScriptedTextReader("1", "5", "9", "0", "10"),
            () => new Game(fixture.System).Run());
        var ringOneStart = capture.Output.IndexOf("[Ring1]", StringComparison.Ordinal);
        var ringTwoStart = capture.Output.IndexOf("[Ring2]", ringOneStart + 1, StringComparison.Ordinal);
        var ringOneMenu = capture.Output[ringOneStart..ringTwoStart];
        var ringTwoMenu = capture.Output[ringTwoStart..];

        Assert.Contains("Current Ring", ringOneMenu);
        Assert.Contains("All-Stat Ring", ringOneMenu);
        Assert.DoesNotContain("Current Ring", ringTwoMenu);
        Assert.Contains("All-Stat Ring", ringTwoMenu);
        Assert.Equal(before, File.ReadAllBytes(fixture.SavePath));
    }

    [Fact]
    public void Run_CandidateRemovedAfterPreview_IsRejectedWithoutSaving()
    {
        using var catalog = EquipmentCatalogFixtureScope.Install();
        using var fixture = new GameFixture();
        Assert.True(fixture.System.Save(CreatePlayerWithReward()).Succeeded);
        var before = File.ReadAllBytes(fixture.SavePath);
        var input = new ScriptedTextReader(
            new InputStep("1"),
            new InputStep("5"),
            new InputStep("1"),
            new InputStep("1"),
            new InputStep("1", () => Assert.True(catalog.Remove(EquipmentCatalog.RewardWeaponId))));

        var capture = CaptureUntilInputEnds(input, () => new Game(fixture.System).Run());

        Assert.Contains("That equipment is no longer available.", capture.Output);
        Assert.Equal(before, File.ReadAllBytes(fixture.SavePath));
        Assert.DoesNotContain("Equipment equipped.", capture.Output);
    }

    [Fact]
    public void Run_OptionalEquipment_EquipsAndUnequipsWithSuccessOnlySaves()
    {
        using var catalog = EquipmentCatalogFixtureScope.Install(OptionalArmor);
        using var fixture = new GameFixture();
        var player = new Player("Wanderer");
        Assert.True(player.GrantEquipment(OptionalArmor.Id));
        Assert.True(fixture.System.Save(player).Succeeded);

        CaptureUntilInputEnds(
            new ScriptedTextReader("1", "5", "4", "1", "1"),
            () => new Game(fixture.System).Run());
        var equipped = fixture.System.Load().Player!;

        Assert.True(equipped.TryGetEquippedEquipment(EquipmentSlot.Armor, out var armor));
        Assert.Equal(OptionalArmor.Id, armor.Id);
        var equippedBytes = File.ReadAllBytes(fixture.SavePath);

        var emptyAttempt = CaptureUntilInputEnds(
            new ScriptedTextReader("1", "5", "3", "1", "1"),
            () => new Game(fixture.System).Run());

        Assert.Contains("No equipment to remove.", emptyAttempt.Output);
        Assert.Contains("No compatible owned equipment is available for this slot.", emptyAttempt.Output);
        Assert.Contains(
            "Equip (unavailable: No compatible owned equipment is available.)",
            emptyAttempt.Output);
        Assert.Contains("Select (0-0):", emptyAttempt.Output);
        Assert.DoesNotContain("Equipment equipped.", emptyAttempt.Output);
        Assert.DoesNotContain("Equipment removed.", emptyAttempt.Output);
        Assert.Equal(equippedBytes, File.ReadAllBytes(fixture.SavePath));

        CaptureUntilInputEnds(
            new ScriptedTextReader("1", "5", "4", "2", "1"),
            () => new Game(fixture.System).Run());
        var unequipped = fixture.System.Load().Player!;

        Assert.False(unequipped.TryGetEquippedEquipment(EquipmentSlot.Armor, out _));
    }

    [Fact]
    public void Run_EquipmentSaveFailure_KeepsMutationVisibleAndLeavesWarningAsFinalOutcome()
    {
        using var fixture = new GameFixture();
        Assert.True(fixture.System.Save(CreatePlayerWithReward()).Succeeded);
        var input = new ScriptedTextReader(
            new InputStep("1"),
            new InputStep("5"),
            new InputStep("1"),
            new InputStep("1"),
            new InputStep("1", fixture.BlockSaveDirectory));

        var capture = CaptureUntilInputEnds(input, () => new Game(fixture.System).Run());
        var equippedIndex = capture.Output.IndexOf("Equipment equipped.", StringComparison.Ordinal);
        var refreshedIndex = capture.Output.IndexOf("Current: Reward Weapon", equippedIndex, StringComparison.Ordinal);
        var failedIndex = capture.Output.LastIndexOf("Save: Failed", StringComparison.Ordinal);
        var warningIndex = capture.Output.LastIndexOf("Save failed. Progress may not be saved.", StringComparison.Ordinal);

        Assert.True(equippedIndex >= 0);
        Assert.True(refreshedIndex > equippedIndex);
        Assert.True(failedIndex > equippedIndex);
        Assert.True(warningIndex > failedIndex);
        Assert.DoesNotContain("Save: Saved just now", capture.Output[equippedIndex..]);
        Assert.False(string.IsNullOrWhiteSpace(capture.Error));
    }

    private static readonly EquipmentDefinition OptionalArmor = new(
        "fixture-armor",
        "Bulwark",
        EquipmentCategory.Armor,
        AttackBonus: 1,
        DefenseBonus: 3,
        DamageReductionBonus: 2,
        MaxHpBonus: 25);

    private static readonly EquipmentDefinition CurrentRing = new(
        "current-ring",
        "Current Ring",
        EquipmentCategory.Ring,
        AttackBonus: 1,
        DefenseBonus: 2,
        DamageReductionBonus: 3,
        MaxHpBonus: 4);

    private static readonly EquipmentDefinition AllStatRing = new(
        "all-stat-ring",
        "All-Stat Ring",
        EquipmentCategory.Ring,
        AttackBonus: 5,
        DefenseBonus: 7,
        DamageReductionBonus: 11,
        MaxHpBonus: 13);

    private static Player CreatePlayerWithReward()
    {
        var player = new Player("Wanderer");
        Assert.True(player.GrantEquipment(EquipmentCatalog.RewardWeaponId));
        return player;
    }

    private static ConsoleCapture CaptureUntilInputEnds(TextReader input, Action action)
    {
        var originalIn = Console.In;
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        var reachedEnd = false;

        try
        {
            Console.SetIn(input);
            Console.SetOut(output);
            Console.SetError(error);
            try
            {
                action();
            }
            catch (EndOfInputException)
            {
                reachedEnd = true;
            }

            Assert.True(reachedEnd, "The scripted session should return to an input boundary.");
            return new ConsoleCapture(output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        for (var offset = 0; (offset = value.IndexOf(search, offset, StringComparison.Ordinal)) >= 0; offset += search.Length)
        {
            count++;
        }

        return count;
    }

    private sealed record ConsoleCapture(string Output, string Error);
    private sealed record InputStep(string Value, Action? BeforeRead = null);

    private sealed class ScriptedTextReader : TextReader
    {
        private readonly Queue<InputStep> _steps;

        public ScriptedTextReader(params string[] values)
            : this(values.Select(value => new InputStep(value)).ToArray())
        {
        }

        public ScriptedTextReader(params InputStep[] steps)
        {
            _steps = new Queue<InputStep>(steps);
        }

        public override string? ReadLine()
        {
            if (_steps.Count == 0)
            {
                throw new EndOfInputException();
            }

            var step = _steps.Dequeue();
            step.BeforeRead?.Invoke();
            return step.Value;
        }
    }

    private sealed class EndOfInputException : Exception
    {
    }

    private sealed class GameFixture : IDisposable
    {
        private bool _saveDirectoryBlocked;

        public GameFixture()
        {
            DirectoryPath = Directory.CreateTempSubdirectory("toil-relic-equipment-tests-").FullName;
            SaveDirectoryPath = Directory.CreateDirectory(Path.Combine(DirectoryPath, "save-parent")).FullName;
            SavePath = Path.Combine(SaveDirectoryPath, "savegame.json");
            System = new SaveSystem(SavePath);
        }

        public string DirectoryPath { get; }
        public string SaveDirectoryPath { get; }
        public string SavePath { get; }
        public SaveSystem System { get; }

        public void BlockSaveDirectory()
        {
            File.Delete(SavePath);
            Directory.Delete(SaveDirectoryPath);
            File.WriteAllText(SaveDirectoryPath, "not a directory");
            _saveDirectoryBlocked = true;
        }

        public void Dispose()
        {
            if (_saveDirectoryBlocked)
            {
                File.Delete(SaveDirectoryPath);
            }

            Directory.Delete(DirectoryPath, recursive: true);
        }
    }

}
