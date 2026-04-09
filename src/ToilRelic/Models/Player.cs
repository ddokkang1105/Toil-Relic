namespace ToilRelic.Models;

public sealed class Player
{
    public string Name { get; }
    public int MaxHp { get; } = 30;
    public int Hp { get; private set; }
    public int Score { get; private set; }
    public int TreasureCount { get; private set; }

    private readonly Dictionary<ItemType, int> _inventory = new();

    public Player(string name)
    {
        Name = name;
        Hp = MaxHp;
        _inventory[ItemType.Junk] = 0;
        _inventory[ItemType.RelicPart] = 0;
        _inventory[ItemType.Treasure] = 0;
    }

    public IReadOnlyDictionary<ItemType, int> Inventory => _inventory;

    public void AddItem(ItemType type, int amount)
    {
        if (amount <= 0) return;
        _inventory[type] += amount;

        if (type == ItemType.Treasure)
        {
            TreasureCount += amount;
            Score += amount * 100;
        }
    }

    public bool Consume(ItemType type, int amount)
    {
        if (amount <= 0) return false;
        if (_inventory[type] < amount) return false;
        _inventory[type] -= amount;
        return true;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        Hp = Math.Max(0, Hp - amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        Hp = Math.Min(MaxHp, Hp + amount);
    }

    public void Rest()
    {
        Heal(MaxHp);
    }

    public bool IsAlive => Hp > 0;
}
