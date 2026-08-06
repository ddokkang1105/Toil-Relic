namespace ToilRelic.Models;

public enum EquipmentSlot
{
    PrimaryWeapon,
    SecondaryWeapon,
    Hat,
    Armor,
    Gloves,
    Shoes,
    Necklace,
    Belt,
    Ring1,
    Ring2,
    Earring1,
    Earring2
}

public enum EquipmentCategory
{
    PrimaryWeapon,
    SecondaryWeapon,
    Hat,
    Armor,
    Gloves,
    Shoes,
    Necklace,
    Belt,
    Ring,
    Earring
}

public sealed record EquipmentDefinition(
    string Id,
    string DisplayName,
    EquipmentCategory Category,
    int AttackBonus = 0,
    int DefenseBonus = 0,
    int DamageReductionBonus = 0,
    int MaxHpBonus = 0)
{
    public bool CanEquipTo(EquipmentSlot slot) => Category switch
    {
        EquipmentCategory.Ring => slot is EquipmentSlot.Ring1 or EquipmentSlot.Ring2,
        EquipmentCategory.Earring => slot is EquipmentSlot.Earring1 or EquipmentSlot.Earring2,
        _ => (EquipmentSlot)Category == slot
    };
}

public sealed record EquippedEquipmentEntry(EquipmentSlot Slot, string EquipmentId);

public static class EquipmentCatalog
{
    public const string StarterWeaponId = "starter-weapon";
    public const string RewardWeaponId = "reward-weapon";
    public const string VerminFangId = "vermin-fang";
    public const string RustguardPlateId = "rustguard-plate";
    public const string WraithSignetId = "wraith-signet";
    public const string ToilboundRelicId = "toilbound-relic";

    private static readonly Dictionary<string, EquipmentDefinition> Definitions = new(StringComparer.Ordinal)
    {
        [StarterWeaponId] = new(StarterWeaponId, "Starter Weapon", EquipmentCategory.PrimaryWeapon),
        [RewardWeaponId] = new(RewardWeaponId, "Reward Weapon", EquipmentCategory.PrimaryWeapon, AttackBonus: 2),
        [VerminFangId] = new(VerminFangId, "Vermin Fang", EquipmentCategory.SecondaryWeapon, AttackBonus: 1),
        [RustguardPlateId] = new(RustguardPlateId, "Rustguard Plate", EquipmentCategory.Armor, DefenseBonus: 2, MaxHpBonus: 5),
        [WraithSignetId] = new(WraithSignetId, "Wraith Signet", EquipmentCategory.Ring, DamageReductionBonus: 1),
        [ToilboundRelicId] = new(ToilboundRelicId, "Toilbound Relic", EquipmentCategory.Necklace, AttackBonus: 2, DefenseBonus: 2, DamageReductionBonus: 1, MaxHpBonus: 10)
    };

    public static IReadOnlyList<EquipmentDefinition> All => Definitions.Values
        .OrderBy(definition => definition.DisplayName, StringComparer.Ordinal)
        .ThenBy(definition => definition.Id, StringComparer.Ordinal)
        .ToArray();

    public static bool TryGet(string? id, out EquipmentDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            definition = null!;
            return false;
        }

        if (Definitions.TryGetValue(id, out var found))
        {
            definition = found;
            return true;
        }

        definition = null!;
        return false;
    }
}
