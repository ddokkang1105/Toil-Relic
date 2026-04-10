namespace ToilRelic.Models;

public sealed class PlayerSaveData
{
    public string Name { get; init; } = "노역자";
    public int MaxHp { get; init; } = 30;
    public int Hp { get; init; } = 30;
    public int Level { get; init; } = 1;
    public int Experience { get; init; }
    public int TreasureCount { get; init; }
    public Dictionary<ItemType, int> Inventory { get; init; } = new();
}
