using ToilRelic.Models;
using ToilRelic.Systems;
using ToilRelic.Util;

namespace ToilRelic;

public sealed class Game
{
    private Player _player = new("Wanderer");
    private readonly CombatSystem _combat = new();
    private readonly LootSystem _loot = new();
    private readonly CraftingSystem _crafting = new();
    private readonly SaveSystem _save;
    private bool _running = true;

    public Game()
        : this(new SaveSystem())
    {
    }

    public Game(SaveSystem save)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
    }

    public void Run()
    {
        ConsoleUI.Header("Toil-Relic", "A survival crafting game.");
        InitializePlayer();
        while (_running)
        {
            ConsoleUI.Status(_player);
            ConsoleUI.Menu("Actions", new Dictionary<int, string> { { 1, "Hunt" }, { 2, "Inventory" }, { 3, "Craft treasure" }, { 4, "Rest" }, { 5, "Equipment" }, { 6, "Quit" } });
            switch (ConsoleUI.ReadInt("Select", 1, 6))
            {
                case 1: Hunt(); SaveProgress(); ConsoleUI.Pause(); break;
                case 2: ConsoleUI.Inventory(_player); break;
                case 3: Craft(); SaveProgress(); ConsoleUI.Pause(); break;
                case 4: Rest(); SaveProgress(); ConsoleUI.Pause(); break;
                case 5: ShowEquipment(); break;
                case 6: SaveProgress(); _running = false; break;
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

            if (loadResult.Status == LoadStatus.Loaded)
            {
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

    private void SaveProgress()
    {
        var result = _save.Save(_player);
        if (result.Succeeded)
        {
            Console.WriteLine("Save: Saved just now");
            Console.WriteLine();
            return;
        }

        WriteDiagnostic(result.Diagnostic);
        Console.WriteLine("Save: Failed");
        Console.WriteLine("Save failed. Progress may not be saved.");
        Console.WriteLine();
    }

    private static void WriteDiagnostic(string? diagnostic)
    {
        if (!string.IsNullOrWhiteSpace(diagnostic))
        {
            Console.Error.WriteLine(diagnostic);
        }
    }

    private void Hunt()
    {
        var enemy = Enemy.RandomEnemy();
        ConsoleUI.Section($"Hunt: {enemy.Name}", $"HP {enemy.Hp}");
        var result = _combat.Fight(_player, enemy);
        if (result.PlayerWon)
        {
            var loot = _loot.RollLoot(); var levelResult = _player.GainExperience(enemy.ExpReward);
            _player.AddItem(ItemType.Junk, loot.Junk); _player.AddItem(ItemType.RelicPart, loot.RelicPart); _player.AddItem(ItemType.HealingPotion, loot.HealingPotion);
            var rewardGranted = _player.GrantEquipment(EquipmentCatalog.RewardWeaponId);
            ConsoleUI.Section("Loot", BuildLootLog(loot.Junk, loot.RelicPart, loot.HealingPotion, enemy.ExpReward, rewardGranted));
            if (levelResult.LeveledUp) ConsoleUI.Section("Level up", $"Level {levelResult.NewLevel}");
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
        ConsoleUI.Equipment(_player);
        var slots = Enum.GetValues<EquipmentSlot>();
        var slotOptions = new Dictionary<int, string> { { 0, "Back" } };
        for (var index = 0; index < slots.Length; index++) slotOptions[index + 1] = slots[index].ToString();
        ConsoleUI.Menu("Equipment slot", slotOptions);
        var slotChoice = ConsoleUI.ReadInt("Select", 0, slots.Length);
        if (slotChoice == 0) return;
        var slot = slots[slotChoice - 1];
        var candidates = _player.OwnedEquipmentIds.Where(id => EquipmentCatalog.TryGet(id, out var item) && item.CanEquipTo(slot)).ToList();
        var itemOptions = new Dictionary<int, string> { { 0, slot == EquipmentSlot.PrimaryWeapon ? "Back" : "Unequip" } };
        for (var index = 0; index < candidates.Count; index++) { EquipmentCatalog.TryGet(candidates[index], out var item); itemOptions[index + 1] = item.DisplayName; }
        ConsoleUI.Menu($"{slot} equipment", itemOptions);
        var itemChoice = ConsoleUI.ReadInt("Select", 0, candidates.Count);
        var result = itemChoice == 0 ? slot == EquipmentSlot.PrimaryWeapon ? "Primary weapon cannot be unequipped." : _player.Unequip(slot) ? "Equipment removed." : "No equipment to remove." : _player.Equip(slot, candidates[itemChoice - 1]) ? "Equipment equipped." : "Equipment could not be equipped.";
        ConsoleUI.Section("Equipment", result);
        SaveProgress(); ConsoleUI.Pause();
    }

    private static string BuildLootLog(int junk, int relicPart, int healingPotion, int expReward, bool rewardWeaponGranted)
    {
        var parts = new List<string>();
        if (junk > 0) parts.Add($"Junk +{junk}"); if (relicPart > 0) parts.Add($"Relic parts +{relicPart}"); if (healingPotion > 0) parts.Add($"Potions +{healingPotion}"); if (expReward > 0) parts.Add($"EXP +{expReward}");
        if (rewardWeaponGranted && EquipmentCatalog.TryGet(EquipmentCatalog.RewardWeaponId, out var weapon)) parts.Add($"{weapon.DisplayName} acquired");
        return parts.Count > 0 ? string.Join(", ", parts) : "No loot";
    }
}
