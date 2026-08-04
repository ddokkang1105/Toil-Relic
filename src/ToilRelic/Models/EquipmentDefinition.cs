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

    private static readonly Dictionary<string, EquipmentDefinition> Definitions = new(StringComparer.Ordinal)
    {
        [StarterWeaponId] = new(StarterWeaponId, "Starter Weapon", EquipmentCategory.PrimaryWeapon),
        [RewardWeaponId] = new(RewardWeaponId, "Reward Weapon", EquipmentCategory.PrimaryWeapon, AttackBonus: 2)
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
