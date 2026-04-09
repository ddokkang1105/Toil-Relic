namespace ToilRelic.Models;

public sealed class Enemy
{
    public string Name { get; }
    public int Hp { get; private set; }
    public int Attack { get; }

    public Enemy(string name, int hp, int attack)
    {
        Name = name;
        Hp = hp;
        Attack = attack;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        Hp = Math.Max(0, Hp - amount);
    }

    public bool IsAlive => Hp > 0;

    private static readonly Enemy[] Pool =
    {
        new("광산 설치류", 10, 3),
        new("녹슨 자동골렘", 14, 4),
        new("유적 망령", 18, 5),
        new("깊은 갱도 야수", 22, 6)
    };

    public static Enemy RandomEnemy()
    {
        var idx = Random.Shared.Next(Pool.Length);
        var e = Pool[idx];
        return new Enemy(e.Name, e.Hp, e.Attack);
    }
}
