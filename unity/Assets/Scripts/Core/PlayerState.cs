using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToilRelic.Unity.Core
{
    public enum ItemType { Junk, RelicPart, Treasure, HealingPotion }
    [Serializable] public sealed class InventorySlot { public ItemType type; public int amount; }
    [Serializable]
    public sealed class PlayerState
    {
        [SerializeField] private int maxHp = 30;
        [SerializeField] private int hp = 30;
        [SerializeField] private int level = 1;
        [SerializeField] private int experience;
        [SerializeField] private int treasureCount;
        [SerializeField] private List<InventorySlot> inventory = new();
        [SerializeField] private List<string> ownedEquipmentIds = new();
        [SerializeField] private List<EquippedEquipmentEntry> equippedEquipment = new();
        [SerializeField] private bool equipmentInitialized;
        // Unity JsonUtility reads this legacy field from older saves; new saves do not write it.
        [SerializeField] private string equippedWeaponId;

        public int MaxHp => maxHp; public int Hp => hp; public int Level => level; public int Experience => experience;
        public int ExperienceToNextLevel => RequiredExperience(level); public float LevelProgressValue => level + experience / (float)ExperienceToNextLevel;
        public int TreasureCount => treasureCount; public IReadOnlyList<InventorySlot> Inventory => inventory; public IReadOnlyList<string> OwnedEquipmentIds => ownedEquipmentIds; public IReadOnlyList<EquippedEquipmentEntry> EquippedEquipment => equippedEquipment;
        private IEnumerable<EquipmentDefinition> EquippedDefinitions => equippedEquipment.Select(entry => EquipmentCatalog.TryGet(entry.equipmentId, out var item) ? item : null).Where(item => item != null);
        public int AttackBonus => EquippedDefinitions.Sum(item => item.AttackBonus); public int DefenseBonus => EquippedDefinitions.Sum(item => item.DefenseBonus); public int DamageReductionBonus => EquippedDefinitions.Sum(item => item.DamageReductionBonus); public int EquipmentMaxHpBonus => EquippedDefinitions.Sum(item => item.MaxHpBonus);
        public int ReduceIncomingDamage(int amount) => Mathf.Max(0, amount - DefenseBonus - DamageReductionBonus);

        public void InitDefaults()
        {
            inventory ??= new List<InventorySlot>(); ownedEquipmentIds ??= new List<string>(); equippedEquipment ??= new List<EquippedEquipmentEntry>();
            if (inventory.Count == 0) foreach (var type in Enum.GetValues(typeof(ItemType)).Cast<ItemType>()) inventory.Add(new InventorySlot { type = type, amount = 0 });
            NormalizeEquipment(); RecalculateMaxHp(hp <= 0);
        }
        public bool TryGetEquippedEquipment(EquipmentSlot slot, out EquipmentDefinition equipment)
        {
            var entry = equippedEquipment.FirstOrDefault(candidate => candidate.slot == slot);
            if (entry != null && EquipmentCatalog.TryGet(entry.equipmentId, out equipment))
            {
                return true;
            }

            equipment = null;
            return false;
        }
        public bool TryGetPrimaryWeapon(out EquipmentDefinition weapon) => TryGetEquippedEquipment(EquipmentSlot.PrimaryWeapon, out weapon);
        public bool GrantEquipment(string equipmentId) { if (!EquipmentCatalog.TryGet(equipmentId, out _) || ownedEquipmentIds.Contains(equipmentId)) return false; ownedEquipmentIds.Add(equipmentId); return true; }
        public bool Equip(EquipmentSlot slot, string equipmentId)
        {
            if (!ownedEquipmentIds.Contains(equipmentId) || !EquipmentCatalog.TryGet(equipmentId, out var item) || !item.CanEquipTo(slot) || equippedEquipment.Any(entry => entry.equipmentId == equipmentId && entry.slot != slot)) return false;
            equippedEquipment.RemoveAll(entry => entry.slot == slot); equippedEquipment.Add(new EquippedEquipmentEntry { slot = slot, equipmentId = equipmentId }); RecalculateMaxHp(); return true;
        }
        public bool Unequip(EquipmentSlot slot) { if (slot == EquipmentSlot.PrimaryWeapon) return false; var removed = equippedEquipment.RemoveAll(entry => entry.slot == slot) > 0; if (removed) RecalculateMaxHp(); return removed; }
        public LevelUpResult GainExperience(int amount) { if (amount <= 0) return new(false, 0, level); experience += amount; var gained = 0; while (experience >= RequiredExperience(level)) { experience -= RequiredExperience(level); level++; gained++; } RecalculateMaxHp(gained > 0); return new(gained > 0, gained, level); }
        public int GetAmount(ItemType type) { var item = inventory.FirstOrDefault(entry => entry.type == type); return item?.amount ?? 0; }
        public void Add(ItemType type, int amount) { if (amount <= 0) return; var item = inventory.FirstOrDefault(entry => entry.type == type); if (item == null) inventory.Add(new InventorySlot { type = type, amount = amount }); else item.amount += amount; if (type == ItemType.Treasure) treasureCount += amount; }
        public bool Consume(ItemType type, int amount) { var item = inventory.FirstOrDefault(entry => entry.type == type); if (amount <= 0 || item == null || item.amount < amount) return false; item.amount -= amount; return true; }
        public void TakeDamage(int amount) { if (amount > 0) hp = Mathf.Max(0, hp - ReduceIncomingDamage(amount)); } public void Heal(int amount) { if (amount > 0) hp = Mathf.Min(maxHp, hp + amount); } public void HealAll() => hp = maxHp; public bool IsAlive => hp > 0;
        private void NormalizeEquipment()
        {
            ownedEquipmentIds.RemoveAll(id => !EquipmentCatalog.TryGet(id, out _)); var unique = new HashSet<string>(StringComparer.Ordinal); ownedEquipmentIds.RemoveAll(id => !unique.Add(id));
            if (!equipmentInitialized) { ownedEquipmentIds.Clear(); equippedEquipment.Clear(); GrantEquipment(EquipmentCatalog.StarterWeaponId); equippedEquipment.Add(new EquippedEquipmentEntry { slot = EquipmentSlot.PrimaryWeapon, equipmentId = EquipmentCatalog.StarterWeaponId }); equipmentInitialized = true; equippedWeaponId = null; return; }
            if (!string.IsNullOrEmpty(equippedWeaponId)) equippedEquipment.Add(new EquippedEquipmentEntry { slot = EquipmentSlot.PrimaryWeapon, equipmentId = equippedWeaponId }); equippedWeaponId = null;
            var ids = new HashSet<string>(StringComparer.Ordinal); var slots = new HashSet<EquipmentSlot>(); equippedEquipment.RemoveAll(entry => entry == null || !ownedEquipmentIds.Contains(entry.equipmentId) || !EquipmentCatalog.TryGet(entry.equipmentId, out var item) || !item.CanEquipTo(entry.slot) || !ids.Add(entry.equipmentId) || !slots.Add(entry.slot));
            if (!TryGetPrimaryWeapon(out _)) { var primary = ownedEquipmentIds.FirstOrDefault(id => EquipmentCatalog.TryGet(id, out var item) && item.CanEquipTo(EquipmentSlot.PrimaryWeapon)); if (primary == null) { GrantEquipment(EquipmentCatalog.StarterWeaponId); primary = EquipmentCatalog.StarterWeaponId; } equippedEquipment.Add(new EquippedEquipmentEntry { slot = EquipmentSlot.PrimaryWeapon, equipmentId = primary }); }
        }
        private void RecalculateMaxHp(bool heal = false) { maxHp = 30 + EquipmentMaxHpBonus; hp = heal ? maxHp : Mathf.Clamp(hp, 0, maxHp); }
        private static int RequiredExperience(int currentLevel) => 20 + (currentLevel - 1) * 10;
    }
    public readonly struct LevelUpResult { public readonly bool LeveledUp; public readonly int LevelsGained; public readonly int NewLevel; public LevelUpResult(bool leveledUp, int levelsGained, int newLevel) { LeveledUp = leveledUp; LevelsGained = levelsGained; NewLevel = newLevel; } }
}
