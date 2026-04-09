namespace ToilRelic.Models;

public sealed class Enemy
{
    public string Name { get; }
    public int Hp { get; private set; }
    public int Attack { get; }
    public int ExpReward { get; }

    public Enemy(string name, int hp, int attack, int expReward)
    {
        Name = name;
        Hp = hp;
        Attack = attack;
        ExpReward = expReward;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        Hp = Math.Max(0, Hp - amount);
    }

    public bool IsAlive => Hp > 0;

    private static readonly Enemy[] Pool =
    {
        new("광산 설치류", 10, 3, 10),
        new("녹슨 자동골렘", 14, 4, 14),
        new("유적 망령", 18, 5, 20),
        new("깊은 갱도 야수", 22, 6, 28)
    };

    public static Enemy RandomEnemy()
    {
        var idx = Random.Shared.Next(Pool.Length);
        var e = Pool[idx];
        return new Enemy(e.Name, e.Hp, e.Attack, e.ExpReward);
    }
}
