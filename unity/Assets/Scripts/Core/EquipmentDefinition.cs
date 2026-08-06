using System;
using System.Collections.Generic;
using System.Linq;

namespace ToilRelic.Unity.Core
{
    public enum EquipmentSlot { PrimaryWeapon, SecondaryWeapon, Hat, Armor, Gloves, Shoes, Necklace, Belt, Ring1, Ring2, Earring1, Earring2 }
    public enum EquipmentCategory { PrimaryWeapon, SecondaryWeapon, Hat, Armor, Gloves, Shoes, Necklace, Belt, Ring, Earring }

    [Serializable]
    public sealed class EquippedEquipmentEntry { public EquipmentSlot slot; public string equipmentId; }

    [Serializable]
    public sealed class EquipmentDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public EquipmentCategory Category { get; }
        public int AttackBonus { get; }
        public int DefenseBonus { get; }
        public int DamageReductionBonus { get; }
        public int MaxHpBonus { get; }
        public EquipmentDefinition(string id, string displayName, EquipmentCategory category, int attackBonus = 0, int defenseBonus = 0, int damageReductionBonus = 0, int maxHpBonus = 0)
        { Id = id; DisplayName = displayName; Category = category; AttackBonus = attackBonus; DefenseBonus = defenseBonus; DamageReductionBonus = damageReductionBonus; MaxHpBonus = maxHpBonus; }
        public bool CanEquipTo(EquipmentSlot slot) => Category == EquipmentCategory.Ring ? slot == EquipmentSlot.Ring1 || slot == EquipmentSlot.Ring2 : Category == EquipmentCategory.Earring ? slot == EquipmentSlot.Earring1 || slot == EquipmentSlot.Earring2 : (EquipmentSlot)Category == slot;
    }

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
            { StarterWeaponId, new EquipmentDefinition(StarterWeaponId, "Starter Weapon", EquipmentCategory.PrimaryWeapon) },
            { RewardWeaponId, new EquipmentDefinition(RewardWeaponId, "Reward Weapon", EquipmentCategory.PrimaryWeapon, attackBonus: 2) },
            { VerminFangId, new EquipmentDefinition(VerminFangId, "Vermin Fang", EquipmentCategory.SecondaryWeapon, attackBonus: 1) },
            { RustguardPlateId, new EquipmentDefinition(RustguardPlateId, "Rustguard Plate", EquipmentCategory.Armor, defenseBonus: 2, maxHpBonus: 5) },
            { WraithSignetId, new EquipmentDefinition(WraithSignetId, "Wraith Signet", EquipmentCategory.Ring, damageReductionBonus: 1) },
            { ToilboundRelicId, new EquipmentDefinition(ToilboundRelicId, "Toilbound Relic", EquipmentCategory.Necklace, attackBonus: 2, defenseBonus: 2, damageReductionBonus: 1, maxHpBonus: 10) }
        };
        public static IReadOnlyList<EquipmentDefinition> All => Definitions.Values
            .OrderBy(definition => definition.DisplayName, StringComparer.Ordinal)
            .ThenBy(definition => definition.Id, StringComparer.Ordinal)
            .ToArray();
        public static bool TryGet(string id, out EquipmentDefinition definition) { if (string.IsNullOrEmpty(id)) { definition = null; return false; } return Definitions.TryGetValue(id, out definition); }
    }
}
