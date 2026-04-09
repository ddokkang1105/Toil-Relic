using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToilRelic.Unity.Core
{
    public enum ItemType
    {
        Junk,
        RelicPart,
        Treasure
    }

    [Serializable]
    public sealed class InventorySlot
    {
        public ItemType type;
        public int amount;
    }

    [Serializable]
    public sealed class PlayerState
    {
        [SerializeField] private int maxHp = 30;
        [SerializeField] private int hp = 30;
        [SerializeField] private int level = 1;
        [SerializeField] private int experience;
        [SerializeField] private int treasureCount;
        [SerializeField] private List<InventorySlot> inventory = new();

        public int MaxHp => maxHp;
        public int Hp => hp;
        public int Level => level;
        public int Experience => experience;
        public int ExperienceToNextLevel => RequiredExperience(level);
        public float LevelProgressValue => level + (experience / (float)ExperienceToNextLevel);
        public int TreasureCount => treasureCount;
        public IReadOnlyList<InventorySlot> Inventory => inventory;

        public void InitDefaults()
        {
            if (inventory.Count > 0)
            {
                return;
            }

            inventory.Add(new InventorySlot { type = ItemType.Junk, amount = 0 });
            inventory.Add(new InventorySlot { type = ItemType.RelicPart, amount = 0 });
            inventory.Add(new InventorySlot { type = ItemType.Treasure, amount = 0 });
            hp = maxHp;
        }

        public LevelUpResult GainExperience(int amount)
        {
            if (amount <= 0)
            {
                return new LevelUpResult(false, 0, level);
            }

            experience += amount;
            var gainedLevels = 0;

            while (experience >= RequiredExperience(level))
            {
                experience -= RequiredExperience(level);
                level += 1;
                maxHp += 3;
                hp = maxHp;
                gainedLevels += 1;
            }

            return new LevelUpResult(gainedLevels > 0, gainedLevels, level);
        }

        public int GetAmount(ItemType type)
        {
            var idx = inventory.FindIndex(x => x.type == type);
            return idx >= 0 ? inventory[idx].amount : 0;
        }

        public void Add(ItemType type, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var idx = inventory.FindIndex(x => x.type == type);
            if (idx < 0)
            {
                inventory.Add(new InventorySlot { type = type, amount = amount });
            }
            else
            {
                inventory[idx].amount += amount;
            }

            if (type == ItemType.Treasure)
            {
                treasureCount += amount;
            }
        }

        public bool Consume(ItemType type, int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            var idx = inventory.FindIndex(x => x.type == type);
            if (idx < 0 || inventory[idx].amount < amount)
            {
                return false;
            }

            inventory[idx].amount -= amount;
            return true;
        }

        public void TakeDamage(int amount)
        {
            hp = Mathf.Max(0, hp - Mathf.Max(0, amount));
        }

        public void HealAll()
        {
            hp = maxHp;
        }

        public bool IsAlive => hp > 0;

        private static int RequiredExperience(int currentLevel)
        {
            return 20 + ((currentLevel - 1) * 10);
        }
    }

    public readonly struct LevelUpResult
    {
        public readonly bool LeveledUp;
        public readonly int LevelsGained;
        public readonly int NewLevel;

        public LevelUpResult(bool leveledUp, int levelsGained, int newLevel)
        {
            LeveledUp = leveledUp;
            LevelsGained = levelsGained;
            NewLevel = newLevel;
        }
    }
}
