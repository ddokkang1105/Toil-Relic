namespace ToilRelic.Models;

public sealed class Player
{
    private const int BaseMaxHp = 1000;
    private readonly Dictionary<ItemType, int> _inventory = new();
    private readonly List<string> _ownedEquipmentIds = new();
    private readonly List<EquippedEquipmentEntry> _equippedEquipment = new();
    private readonly RelicProjectState _relicProject = new();
    private bool _equipmentInitialized;

    public string Name { get; }
    public int MaxHp { get; private set; }
    public int Hp { get; private set; }
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int ExperienceToNextLevel => RequiredExperience(Level);
    public decimal LevelProgress => Level + (Experience / (decimal)ExperienceToNextLevel);
    public int TreasureCount { get; private set; }
    public IReadOnlyDictionary<ItemType, int> Inventory => _inventory;
    public IReadOnlyList<string> OwnedEquipmentIds => _ownedEquipmentIds;
    public IReadOnlyList<EquippedEquipmentEntry> EquippedEquipment => _equippedEquipment;
    public RelicProjectState RelicProject => _relicProject;
    public int AttackBonus => EquippedDefinitions.Sum(equipment => equipment.AttackBonus);
    public int DefenseBonus => EquippedDefinitions.Sum(equipment => equipment.DefenseBonus);
    public int DamageReductionBonus => EquippedDefinitions.Sum(equipment => equipment.DamageReductionBonus);
    public int EquipmentMaxHpBonus => EquippedDefinitions.Sum(equipment => equipment.MaxHpBonus);

    private IEnumerable<EquipmentDefinition> EquippedDefinitions => _equippedEquipment
        .Select(entry => EquipmentCatalog.TryGet(entry.EquipmentId, out var equipment) ? equipment : null)
        .Where(equipment => equipment is not null)!;

    public Player(string name)
    {
        Name = name;
        foreach (var type in Enum.GetValues<ItemType>()) _inventory[type] = 0;
        NormalizeEquipment();
        RecalculateMaxHp(healToFull: true);
    }

    public bool TryGetEquippedEquipment(EquipmentSlot slot, out EquipmentDefinition equipment)
    {
        var entry = _equippedEquipment.FirstOrDefault(candidate => candidate.Slot == slot);
        if (entry is not null && EquipmentCatalog.TryGet(entry.EquipmentId, out equipment)) return true;
        equipment = null!;
        return false;
    }

    public bool TryGetPrimaryWeapon(out EquipmentDefinition weapon) =>
        TryGetEquippedEquipment(EquipmentSlot.PrimaryWeapon, out weapon!);

    public bool GrantEquipment(string equipmentId)
    {
        if (!EquipmentCatalog.TryGet(equipmentId, out _) || _ownedEquipmentIds.Contains(equipmentId)) return false;
        _ownedEquipmentIds.Add(equipmentId);
        return true;
    }

    public bool Equip(EquipmentSlot slot, string equipmentId)
    {
        if (!_ownedEquipmentIds.Contains(equipmentId) ||
            !EquipmentCatalog.TryGet(equipmentId, out var equipment) ||
            !equipment.CanEquipTo(slot) ||
            _equippedEquipment.Any(entry => entry.EquipmentId == equipmentId && entry.Slot != slot)) return false;

        _equippedEquipment.RemoveAll(entry => entry.Slot == slot);
        _equippedEquipment.Add(new EquippedEquipmentEntry(slot, equipmentId));
        RecalculateMaxHp();
        return true;
    }

    public bool Unequip(EquipmentSlot slot)
    {
        if (slot == EquipmentSlot.PrimaryWeapon) return false;
        var removed = _equippedEquipment.RemoveAll(entry => entry.Slot == slot) > 0;
        if (removed) RecalculateMaxHp();
        return removed;
    }

    public int ReduceIncomingDamage(int amount) => Math.Max(0, amount - DefenseBonus - DamageReductionBonus);
    public void AddItem(ItemType type, int amount) { if (amount > 0) { _inventory[type] += amount; if (type == ItemType.Treasure) TreasureCount += amount; } }

    public LevelUpResult GainExperience(int amount)
    {
        if (amount <= 0) return new(false, 0, Level);
        Experience += amount;
        var gainedLevels = 0;
        while (Experience >= RequiredExperience(Level)) { Experience -= RequiredExperience(Level); Level++; gainedLevels++; }
        RecalculateMaxHp(healToFull: gainedLevels > 0);
        return new(gainedLevels > 0, gainedLevels, Level);
    }

    public bool Consume(ItemType type, int amount) { if (amount <= 0 || _inventory[type] < amount) return false; _inventory[type] -= amount; return true; }
    public void TakeDamage(int amount) { if (amount > 0) Hp = Math.Max(0, Hp - ReduceIncomingDamage(amount)); }
    public void Heal(int amount) { if (amount > 0) Hp = Math.Min(MaxHp, Hp + amount); }
    public void Rest() => Heal(MaxHp);
    public bool IsAlive => Hp > 0;
    public void ResetExperience() => Experience = 0;

    public PlayerSaveData ToSaveData() => new()
    {
        Name = Name, MaxHp = MaxHp, Hp = Hp, Level = Level, Experience = Experience, TreasureCount = TreasureCount,
        SchemaVersion = 1, Inventory = new(_inventory), OwnedEquipmentIds = new(_ownedEquipmentIds), EquippedEquipment = new(_equippedEquipment),
        EquipmentInitialized = _equipmentInitialized, RelicProject = _relicProject.ToSaveData()
    };

    public static Player FromSaveData(PlayerSaveData saveData)
    {
        var player = new Player(string.IsNullOrWhiteSpace(saveData.Name) ? "Wanderer" : saveData.Name) { Level = Math.Max(1, saveData.Level), Experience = Math.Max(0, saveData.Experience) };
        player.Experience = Math.Min(player.Experience, player.ExperienceToNextLevel - 1);
        foreach (var type in Enum.GetValues<ItemType>()) player._inventory[type] = 0;
        foreach (var pair in saveData.Inventory ?? new()) player._inventory[pair.Key] = Math.Max(0, pair.Value);
        player._ownedEquipmentIds.Clear();
        player._ownedEquipmentIds.AddRange(saveData.OwnedEquipmentIds ?? new());
        player._equippedEquipment.Clear();
        player._equippedEquipment.AddRange(saveData.EquippedEquipment ?? new());
        if (!string.IsNullOrWhiteSpace(saveData.EquippedWeaponId)) player._equippedEquipment.Add(new(EquipmentSlot.PrimaryWeapon, saveData.EquippedWeaponId));
        player._equipmentInitialized = saveData.EquipmentInitialized;
        player._relicProject.LoadFromSaveData(saveData.RelicProject);
        player.NormalizeEquipment();
        player.RecalculateMaxHp();
        player.Hp = Math.Clamp(saveData.Hp, 0, player.MaxHp);
        player.TreasureCount = player._inventory[ItemType.Treasure];
        return player;
    }

    private void NormalizeEquipment()
    {
        _ownedEquipmentIds.RemoveAll(id => !EquipmentCatalog.TryGet(id, out _));
        var uniqueOwned = new HashSet<string>(StringComparer.Ordinal);
        _ownedEquipmentIds.RemoveAll(id => !uniqueOwned.Add(id));
        if (!_equipmentInitialized)
        {
            _ownedEquipmentIds.Clear();
            _equippedEquipment.Clear();
            GrantEquipment(EquipmentCatalog.StarterWeaponId);
            _equippedEquipment.Add(new(EquipmentSlot.PrimaryWeapon, EquipmentCatalog.StarterWeaponId));
            _equipmentInitialized = true;
            return;
        }

        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        var usedSlots = new HashSet<EquipmentSlot>();
        _equippedEquipment.RemoveAll(entry => !_ownedEquipmentIds.Contains(entry.EquipmentId) || !EquipmentCatalog.TryGet(entry.EquipmentId, out var equipment) || !equipment.CanEquipTo(entry.Slot) || !usedIds.Add(entry.EquipmentId) || !usedSlots.Add(entry.Slot));
        if (!TryGetPrimaryWeapon(out _))
        {
            var primary = _ownedEquipmentIds.FirstOrDefault(id => EquipmentCatalog.TryGet(id, out var item) && item.CanEquipTo(EquipmentSlot.PrimaryWeapon));
            if (primary is null) { GrantEquipment(EquipmentCatalog.StarterWeaponId); primary = EquipmentCatalog.StarterWeaponId; }
            _equippedEquipment.RemoveAll(entry => entry.Slot == EquipmentSlot.PrimaryWeapon);
            _equippedEquipment.Add(new(EquipmentSlot.PrimaryWeapon, primary));
        }
    }

    private void RecalculateMaxHp(bool healToFull = false)
    {
        MaxHp = CalculateBaseMaxHp(Level) + EquipmentMaxHpBonus;
        Hp = healToFull ? MaxHp : Math.Clamp(Hp, 0, MaxHp);
    }
    internal static int RequiredExperience(int currentLevel) => 20 + ((currentLevel - 1) * 10);
    private static int CalculateBaseMaxHp(int level) { var safeLevel = Math.Max(1, level); return BaseMaxHp + (int)Math.Round(safeLevel + (100m * (1m + safeLevel / 100m) * safeLevel), MidpointRounding.AwayFromZero); }
}

public sealed record LevelUpResult(bool LeveledUp, int LevelsGained, int NewLevel);
