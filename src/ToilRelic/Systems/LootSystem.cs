namespace ToilRelic.Systems;

public sealed class LootSystem
{
    public Loot RollLoot()
    {
        var junk = Random.Shared.Next(1, 4);
        var relicPart = Random.Shared.NextDouble() < 0.25 ? 1 : 0;
        return new Loot(junk, relicPart);
    }
}

public sealed record Loot(int Junk, int RelicPart);
