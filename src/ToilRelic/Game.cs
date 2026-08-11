using ToilRelic.Models;
using ToilRelic.Systems;
using ToilRelic.Util;

namespace ToilRelic;

public sealed class Game
{
    private const string RecoveryNotice = "Recovered a previous valid save. Recent progress may be missing.";

    private Player _player = new("Wanderer");
    private readonly CraftingSystem _crafting = new();
    private readonly SaveSystem _save;
    private readonly IHuntRuntime _huntRuntime;
    private readonly HuntContract _huntContract;
    private readonly IReadOnlyList<EquipmentDropProfile> _huntProfiles;
    private bool _running = true;
    private bool _recoveryNoticePending;
    private bool _recoveryNoticeShown;

    public Game(
        SaveSystem save,
        IHuntRuntime? huntRuntime = null,
        HuntContract? huntContract = null,
        IReadOnlyList<EquipmentDropProfile>? huntProfiles = null)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _huntRuntime = huntRuntime ?? new ProductionHuntRuntime();
        _huntContract = huntContract ?? PurposefulHuntContent.FirstRelicContract;
        _huntProfiles = huntProfiles ?? PurposefulHuntContent.Profiles;
    }

    public void Run()
    {
        ConsoleUI.Header("Toil-Relic", "A survival crafting game.");
        InitializePlayer();
        while (_running)
        {
            ConsoleUI.Status(_player);
            ConsoleUI.Menu("Actions", new Dictionary<int, string>
            {
                { 1, "Hunt Contract" },
                { 2, "Inventory" },
                { 3, "Craft treasure (optional materials)" },
                { 4, "Rest" },
                { 5, "Equipment" },
                { 6, "Quit" },
                { 7, ForgeActionLabel() }
            });
            switch (ConsoleUI.ReadInt("Select", 1, 7))
            {
                case 1: if (Hunt()) SaveProgress(); ConsoleUI.Pause(); break;
                case 2: ConsoleUI.Inventory(_player); break;
                case 3: Craft(); SaveProgress(); ConsoleUI.Pause(); break;
                case 4: Rest(); SaveProgress(); ConsoleUI.Pause(); break;
                case 5: ShowEquipment(); break;
                case 6: SaveProgress(); _running = false; break;
                case 7: ForgeRelic(); ConsoleUI.Pause(); break;
            }
        }
        ConsoleUI.Footer("Game over", $"Treasures: {_player.TreasureCount}, level: {_player.LevelProgress:F2}");
    }

    private void InitializePlayer()
    {
        while (true)
        {
            var loadResult = _save.Load();
            WriteDiagnostic(loadResult.Diagnostic);

            if (loadResult.Status is LoadStatus.Loaded or LoadStatus.Recovered)
            {
                _recoveryNoticePending = loadResult.RecoveryNoticePending;
                ShowRecoveryNoticeOnce();
                ConsoleUI.Section("Save", "Save found. Continue or start a new game.");
                ConsoleUI.Menu("Start", new Dictionary<int, string> { { 1, "Continue" }, { 2, "New Game" } });
                if (ConsoleUI.ReadInt("Select", 1, 2) == 1)
                {
                    _player = loadResult.Player!;
                    return;
                }

                if (TryStartNewGame())
                {
                    return;
                }

                continue;
            }

            if (loadResult.Status == LoadStatus.Missing)
            {
                ConsoleUI.Section("Save", "Start a new game to begin.");
                ConsoleUI.Menu("Start", new Dictionary<int, string> { { 1, "New Game" } });
                ConsoleUI.ReadInt("Select", 1, 1);
                _player = new Player("Wanderer");
                return;
            }

            ConsoleUI.Section("Save", "Save could not be read. Start New Game to replace it.");
            ConsoleUI.Menu("Start", new Dictionary<int, string> { { 1, "New Game" } });
            ConsoleUI.ReadInt("Select", 1, 1);
            if (TryStartNewGame())
            {
                return;
            }
        }
    }

    private bool TryStartNewGame()
    {
        var deleteResult = _save.Delete();
        if (!deleteResult.Succeeded)
        {
            WriteDiagnostic(deleteResult.Diagnostic);
            return false;
        }

        _player = new Player("Wanderer");
        return true;
    }

    private bool SaveProgress()
    {
        var result = _save.Save(_player);
        if (result.Succeeded)
        {
            _recoveryNoticePending = false;
            Console.WriteLine("Save: Saved just now");
            Console.WriteLine();
            return true;
        }

        WriteDiagnostic(result.Diagnostic);
        Console.WriteLine("Save: Failed");
        Console.WriteLine("Save failed. Progress may not be saved.");
        Console.WriteLine();
        return false;
    }

    private void ShowRecoveryNoticeOnce()
    {
        if (!_recoveryNoticePending || _recoveryNoticeShown)
        {
            return;
        }

        Console.WriteLine(RecoveryNotice);
        Console.WriteLine();
        _recoveryNoticeShown = true;
    }

    private static void WriteDiagnostic(string? diagnostic)
    {
        if (!string.IsNullOrWhiteSpace(diagnostic))
        {
            Console.Error.WriteLine(diagnostic);
        }
    }

    private bool Hunt()
    {
        var validation = _huntContract.Validate(_huntProfiles);
        if (!validation.IsAvailable)
        {
            ConsoleUI.Section("Hunt Contract unavailable", $"Content issue: {validation.Issue} ({validation.ContentId ?? "unknown"}).");
            return false;
        }

        ConsoleUI.HuntContract(_player, _huntContract, _huntProfiles);
        var quarryOptions = new Dictionary<int, string> { { 0, "Cancel" } };
        for (var index = 0; index < _huntContract.Quarries.Count; index++)
        {
            var quarry = _huntContract.Quarries[index];
            Enemy.TryCreate(quarry.EnemyId, out var optionEnemy);
            quarryOptions[index + 1] = optionEnemy.Name;
        }

        ConsoleUI.Menu("Choose quarry", quarryOptions);
        var selection = ConsoleUI.ReadInt("Select", 0, _huntContract.Quarries.Count);
        if (selection == 0) return false;

        var selected = _huntContract.Quarries[selection - 1];
        if (!Enemy.TryCreate(selected.EnemyId, out var enemy))
        {
            ConsoleUI.Section("Hunt Contract unavailable", $"Enemy content is missing: {selected.EnemyId}.");
            return false;
        }

        ConsoleUI.Section("Selected quarry", $"{enemy.Name} | Danger {selected.Danger}");
        ConsoleUI.Menu("Confirm hunt", new Dictionary<int, string> { { 0, "Cancel" }, { 1, "Begin hunt" } });
        if (ConsoleUI.ReadInt("Select", 0, 1) == 0) return false;

        ConsoleUI.Section($"Hunt: {enemy.Name}", $"HP {enemy.Hp}");
        var result = _huntRuntime.Fight(_player, enemy);
        if (result.PlayerWon)
        {
            var loot = _huntRuntime.RollLoot();
            var reward = QuarryRewardSystem.Resolve(
                _player,
                _huntContract,
                _huntProfiles,
                new QuarryVictoryCommand(
                    selected.Id,
                    true,
                    _huntRuntime.RollProfile(),
                    enemy.ExpReward,
                    new GenericHuntReward(loot.Junk, loot.RelicPart, loot.HealingPotion)));
            if (reward.Status != QuarryRewardStatus.Applied)
            {
                ConsoleUI.Section("Hunt reward failed", "Victory rewards could not be committed; no reward state changed.");
                return false;
            }

            ConsoleUI.Section("Loot", BuildLootLog(loot.Junk, loot.RelicPart, loot.HealingPotion, enemy.ExpReward));
            WriteRewardFacts(selected, reward);
            if (reward.LevelUp is { LeveledUp: true } levelResult)
            {
                ConsoleUI.Section("Level up", $"Level {levelResult.NewLevel}");
            }
        }

        return true;
    }

    private void WriteRewardFacts(HuntQuarry quarry, QuarryRewardOutcome reward)
    {
        if (reward.ProfileEquipmentId is not null && EquipmentCatalog.TryGet(reward.ProfileEquipmentId, out var profileEquipment))
        {
            var profileMessage = reward.ProfileResult switch
            {
                ProfileRewardResult.Granted => $"Profile equipment acquired: {profileEquipment.DisplayName}",
                ProfileRewardResult.AlreadyOwned => $"Profile equipment already owned: {profileEquipment.DisplayName}",
                _ => $"Profile reward missed: {profileEquipment.DisplayName}"
            };
            ConsoleUI.Section("Profile reward", profileMessage);
        }

        var contributionMessage = reward.ContributionResult == ContributionRewardResult.Granted
            ? $"Project contribution acquired: {quarry.ContributionDisplayName}"
            : $"Replay complete: {quarry.ContributionDisplayName} was already secured; no additional project progress.";
        ConsoleUI.Section("First Relic Project", contributionMessage + (reward.ProjectReady ? " Ready to forge." : string.Empty));
    }

    private string ForgeActionLabel()
    {
        if (_player.RelicProject.IsForged) return "Forge relic (completed)";
        if (_player.RelicProject.IsReady) return "Forge Toilbound Relic (ready)";
        return $"Forge relic (unavailable: {_player.RelicProject.CompletedContributionIds.Count}/3 contributions)";
    }

    private void ForgeRelic()
    {
        var outcome = RelicForgeSystem.Forge(_player, _huntContract);
        switch (outcome.Status)
        {
            case ForgeStatus.Forged:
                ConsoleUI.Section("Forge", "Toilbound Relic forged. It is owned and remains unequipped until confirmed.");
                if (!SaveProgress()) return;
                ConfirmEquip(EquipmentSlot.Necklace, EquipmentCatalog.ToilboundRelicId);
                return;
            case ForgeStatus.NotReady:
                ConsoleUI.Section("Forge", $"Not ready: {_player.RelicProject.CompletedContributionIds.Count}/3 contributions secured.");
                return;
            case ForgeStatus.AlreadyForged:
                ConsoleUI.Section("Forge", "Toilbound Relic was already forged; nothing was granted.");
                return;
            default:
                ConsoleUI.Section("Forge", "Relic state or content is invalid; nothing was changed.");
                return;
        }
    }

    private void Craft()
    {
        var result = _crafting.TryCraftTreasure(_player);
        ConsoleUI.Section("Craft", result.Message);
    }

    private void Rest()
    {
        _player.Rest();
        ConsoleUI.Section("Rest", "HP restored.");
    }

    private void ShowEquipment()
    {
        var slots = Enum.GetValues<EquipmentSlot>();
        while (true)
        {
            ConsoleUI.Equipment(_player);
            var slotOptions = new Dictionary<int, string> { { 0, "Back" } };
            for (var index = 0; index < slots.Length; index++)
            {
                slotOptions[index + 1] = slots[index].ToString();
            }

            ConsoleUI.Menu("Equipment slot", slotOptions);
            var slotChoice = ConsoleUI.ReadInt("Select", 0, slots.Length);
            if (slotChoice == 0)
            {
                return;
            }

            ShowEquipmentSlot(slots[slotChoice - 1]);
        }
    }

    private void ShowEquipmentSlot(EquipmentSlot slot)
    {
        while (true)
        {
            var candidates = GetEquipmentCandidates(slot);
            var unequip = EquipmentComparisonEvaluator.EvaluateUnequip(_player, slot);
            var noCandidates = candidates.Count == 0;
            ConsoleUI.EquipmentSlot(slot, unequip.Current);
            if (noCandidates)
            {
                ConsoleUI.Section("Equipment", "No compatible owned equipment is available for this slot.");
            }

            var itemOptions = new Dictionary<int, string> { { 0, "Back" } };
            for (var index = 0; index < candidates.Count; index++)
            {
                itemOptions[index + 1] = ConsoleUI.EquipmentOption(candidates[index]);
            }

            var unavailableEquipChoice = candidates.Count + 1;
            var unequipChoice = unavailableEquipChoice;
            if (noCandidates)
            {
                itemOptions[unavailableEquipChoice] =
                    "Equip (unavailable: No compatible owned equipment is available.)";
                unequipChoice++;
            }

            itemOptions[unequipChoice] = unequip.CanCommit
                ? "Unequip"
                : $"Unequip (unavailable: {UnequipRejection(unequip)})";
            ConsoleUI.Menu($"{slot} equipment", itemOptions);

            var itemChoice = ConsoleUI.ReadInt("Select", 0, noCandidates ? 0 : unequipChoice);
            if (itemChoice == 0)
            {
                return;
            }

            if (itemChoice == unequipChoice)
            {
                ConfirmUnequip(slot);
                continue;
            }

            ConfirmEquip(slot, candidates[itemChoice - 1].Id);
        }
    }

    private void ConfirmEquip(EquipmentSlot slot, string candidateId)
    {
        var preview = EquipmentComparisonEvaluator.Compare(_player, slot, candidateId);
        ConsoleUI.EquipmentComparison(preview);
        var equipLabel = preview.CanCommit
            ? "Equip"
            : $"Equip (unavailable: {ComparisonRejection(preview.Reason)})";
        ConsoleUI.Menu("Equip confirmation", new Dictionary<int, string> { { 0, "Back" }, { 1, equipLabel } });
        if (!preview.CanCommit)
        {
            ConsoleUI.ReadInt("Select", 0, 0);
            return;
        }

        if (ConsoleUI.ReadInt("Select", 0, 1) == 0)
        {
            return;
        }

        var current = EquipmentComparisonEvaluator.Compare(_player, slot, candidateId);
        if (!current.CanCommit)
        {
            ConsoleUI.Section("Equipment", ComparisonRejection(current.Reason));
            return;
        }

        if (!_player.Equip(slot, candidateId))
        {
            ConsoleUI.Section("Equipment", "Equipment could not be equipped.");
            return;
        }

        ConsoleUI.Section("Equipment", "Equipment equipped.");
        ConsoleUI.EquipmentSlot(slot, current.Candidate);
        SaveProgress();
    }

    private void ConfirmUnequip(EquipmentSlot slot)
    {
        var preview = EquipmentComparisonEvaluator.EvaluateUnequip(_player, slot);
        ConsoleUI.UnequipPreview(preview);
        var unequipLabel = preview.CanCommit
            ? "Unequip"
            : $"Unequip (unavailable: {UnequipRejection(preview)})";
        ConsoleUI.Menu("Unequip confirmation", new Dictionary<int, string> { { 0, "Back" }, { 1, unequipLabel } });
        if (!preview.CanCommit)
        {
            ConsoleUI.ReadInt("Select", 0, 0);
            return;
        }

        if (ConsoleUI.ReadInt("Select", 0, 1) == 0)
        {
            return;
        }

        var current = EquipmentComparisonEvaluator.EvaluateUnequip(_player, slot);
        if (!current.CanCommit)
        {
            ConsoleUI.Section("Equipment", UnequipRejection(current));
            return;
        }

        if (!_player.Unequip(slot))
        {
            ConsoleUI.Section("Equipment", "Equipment could not be removed.");
            return;
        }

        ConsoleUI.Section("Equipment", "Equipment removed.");
        ConsoleUI.EquipmentSlot(slot, current: null);
        SaveProgress();
    }

    private List<EquipmentDefinition> GetEquipmentCandidates(EquipmentSlot slot) => EquipmentCatalog.All
        .Where(item => EquipmentComparisonEvaluator.IsCandidateAvailable(_player, slot, item.Id))
        .ToList();

    private static string ComparisonRejection(EquipmentComparisonReason reason) => reason switch
    {
        EquipmentComparisonReason.SameItem => "That item is already equipped.",
        EquipmentComparisonReason.UnknownCandidate => "That equipment is no longer available.",
        EquipmentComparisonReason.CandidateNotOwned => "That equipment is not owned.",
        EquipmentComparisonReason.IncompatibleSlot => "That equipment does not fit this slot.",
        EquipmentComparisonReason.CandidateEquippedElsewhere => "That equipment is already equipped in another slot.",
        _ => "Equipment could not be equipped."
    };

    private static string UnequipRejection(UnequipEligibilityResult result) => result.Status switch
    {
        UnequipEligibilityStatus.MandatoryPrimaryWeapon => "Primary weapon cannot be unequipped.",
        UnequipEligibilityStatus.EmptySlot => "No equipment to remove.",
        _ => "Equipment could not be removed."
    };

    private static string BuildLootLog(int junk, int relicPart, int healingPotion, int expReward)
    {
        var parts = new List<string>();
        if (junk > 0) parts.Add($"Junk +{junk}"); if (relicPart > 0) parts.Add($"Relic parts +{relicPart}"); if (healingPotion > 0) parts.Add($"Potions +{healingPotion}"); if (expReward > 0) parts.Add($"EXP +{expReward}");
        return parts.Count > 0 ? string.Join(", ", parts) : "No loot";
    }
}
