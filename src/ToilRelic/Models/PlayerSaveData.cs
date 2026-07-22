namespace ToilRelic.Models;

public sealed class PlayerSaveData
{
    public string Name { get; init; } = "노역자";
    public int MaxHp { get; init; } = 1000;
    public int Hp { get; init; } = 1000;
    public int Level { get; init; } = 1;
    public int Experience { get; init; }
    public int TreasureCount { get; init; }
    public Dictionary<ItemType, int> Inventory { get; init; } = new();
    public List<string> OwnedEquipmentIds { get; init; } = new();
    // Kept only to read the pre-slot save format; new saves use EquippedEquipment.
    public string? EquippedWeaponId { get; init; }
    public List<EquippedEquipmentEntry> EquippedEquipment { get; init; } = new();
    public bool EquipmentInitialized { get; init; }
}
