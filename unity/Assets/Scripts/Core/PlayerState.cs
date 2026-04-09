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
        [SerializeField] private int score;
        [SerializeField] private int treasureCount;
        [SerializeField] private List<InventorySlot> inventory = new();

        public int MaxHp => maxHp;
        public int Hp => hp;
        public int Score => score;
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
                score += amount * 100;
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
    }
}
