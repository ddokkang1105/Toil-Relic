namespace ToilRelic.Models;

public sealed class Player
{
    public string Name { get; }
    public int MaxHp { get; private set; } = 30;
    public int Hp { get; private set; }
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int ExperienceToNextLevel => RequiredExperience(Level);
    public decimal LevelProgress => Level + (Experience / (decimal)ExperienceToNextLevel);
    public int TreasureCount { get; private set; }

    private readonly Dictionary<ItemType, int> _inventory = new();

    public Player(string name)
    {
        Name = name;
        Hp = MaxHp;
        _inventory[ItemType.Junk] = 0;
        _inventory[ItemType.RelicPart] = 0;
        _inventory[ItemType.Treasure] = 0;
        _inventory[ItemType.HealingPotion] = 0;
    }

    public IReadOnlyDictionary<ItemType, int> Inventory => _inventory;

    public void AddItem(ItemType type, int amount)
    {
        if (amount <= 0) return;
        _inventory[type] += amount;

        if (type == ItemType.Treasure)
        {
            TreasureCount += amount;
        }
    }

    public LevelUpResult GainExperience(int amount)
    {
        if (amount <= 0) return new LevelUpResult(false, 0, Level);

        Experience += amount;
        var gainedLevels = 0;

        while (Experience >= RequiredExperience(Level))
        {
            Experience -= RequiredExperience(Level);
            Level += 1;
            MaxHp += 3;
            Hp = MaxHp;
            gainedLevels += 1;
        }

        return new LevelUpResult(gainedLevels > 0, gainedLevels, Level);
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

    public void ResetExperience()
    {
        Experience = 0;
    }

    public PlayerSaveData ToSaveData()
    {
        return new PlayerSaveData
        {
            Name = Name,
            MaxHp = MaxHp,
            Hp = Hp,
            Level = Level,
            Experience = Experience,
            TreasureCount = TreasureCount,
            Inventory = new Dictionary<ItemType, int>(_inventory)
        };
    }

    public static Player FromSaveData(PlayerSaveData saveData)
    {
        var name = string.IsNullOrWhiteSpace(saveData.Name) ? "노역자" : saveData.Name;
        var player = new Player(name)
        {
            MaxHp = Math.Max(1, saveData.MaxHp),
            Level = Math.Max(1, saveData.Level),
            Experience = Math.Max(0, saveData.Experience)
        };

        player.Experience = Math.Min(player.Experience, player.ExperienceToNextLevel - 1);
        player.Hp = Math.Clamp(saveData.Hp, 0, player.MaxHp);
        player.TreasureCount = Math.Max(0, saveData.TreasureCount);

        foreach (var type in Enum.GetValues<ItemType>())
        {
            player._inventory[type] = 0;
        }

        foreach (var kv in saveData.Inventory)
        {
            player._inventory[kv.Key] = Math.Max(0, kv.Value);
        }

        player.TreasureCount = player._inventory[ItemType.Treasure];
        return player;
    }

    private static int RequiredExperience(int currentLevel)
    {
        return 20 + ((currentLevel - 1) * 10);
    }
}

public sealed record LevelUpResult(bool LeveledUp, int LevelsGained, int NewLevel);
