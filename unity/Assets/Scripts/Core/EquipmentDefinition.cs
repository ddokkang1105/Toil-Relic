using System;
using System.Collections.Generic;

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
        private static readonly Dictionary<string, EquipmentDefinition> Definitions = new(StringComparer.Ordinal)
        {
            { StarterWeaponId, new EquipmentDefinition(StarterWeaponId, "Starter Weapon", EquipmentCategory.PrimaryWeapon) },
            { RewardWeaponId, new EquipmentDefinition(RewardWeaponId, "Reward Weapon", EquipmentCategory.PrimaryWeapon, attackBonus: 2) }
        };
        public static bool TryGet(string id, out EquipmentDefinition definition) { if (string.IsNullOrEmpty(id)) { definition = null; return false; } return Definitions.TryGetValue(id, out definition); }
    }
}
