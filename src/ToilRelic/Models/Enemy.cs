namespace ToilRelic.Models;

public sealed class Enemy
{
    public string Id { get; }
    public string Name { get; }
    public int Hp { get; private set; }
    public int Attack { get; }
    public int ExpReward { get; }
    public string? EquipmentDropProfileId { get; }

    public Enemy(string name, int hp, int attack, int expReward, string? equipmentDropProfileId = null)
        : this(CreateLegacyId(name), name, hp, attack, expReward, equipmentDropProfileId)
    {
    }

    public Enemy(string id, string name, int hp, int attack, int expReward, string? equipmentDropProfileId)
    {
        Id = id;
        Name = name;
        Hp = hp;
        Attack = attack;
        ExpReward = expReward;
        EquipmentDropProfileId = equipmentDropProfileId;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        Hp = Math.Max(0, Hp - amount);
    }

    public bool IsAlive => Hp > 0;

    private sealed record EnemyDefinition(
        string Id,
        string Name,
        int Hp,
        int Attack,
        int ExpReward,
        string? EquipmentDropProfileId);

    private static readonly EnemyDefinition[] Pool =
    {
        new("mine-vermin", "Mine Vermin", 10, 3, 10, "profile-mine-vermin"),
        new("rust-golem", "Rust Golem", 14, 4, 14, "profile-rust-golem"),
        new("ruin-wraith", "Ruin Wraith", 18, 5, 20, "profile-ruin-wraith"),
        new("mine-alpha-wolf", "Mine Alpha Wolf", 22, 6, 28, null)
    };

    public static bool TryCreate(string? id, out Enemy enemy)
    {
        var definition = Pool.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
        if (definition is null)
        {
            enemy = null!;
            return false;
        }

        enemy = Create(definition);
        return true;
    }

    public static Enemy RandomEnemy()
    {
        var idx = Random.Shared.Next(Pool.Length);
        return Create(Pool[idx]);
    }

    private static Enemy Create(EnemyDefinition definition) => new(
        definition.Id,
        definition.Name,
        definition.Hp,
        definition.Attack,
        definition.ExpReward,
        definition.EquipmentDropProfileId);

    private static string CreateLegacyId(string name) => string.IsNullOrWhiteSpace(name)
        ? "legacy-enemy"
        : $"legacy-{name.Trim().ToLowerInvariant().Replace(' ', '-')}";
}
