using ToilRelic.Models;

namespace ToilRelic.Systems;

public sealed class CraftingSystem
{
    private const int JunkCost = 5;
    private const int RelicCost = 1;

    public CraftResult TryCraftTreasure(Player player)
    {
        var junkHave = player.Inventory[ItemType.Junk];
        var relicHave = player.Inventory[ItemType.RelicPart];

        if (junkHave < JunkCost || relicHave < RelicCost)
        {
            return new CraftResult(false, $"재료 부족: 잡템 {junkHave}/{JunkCost}, 보물 재료 {relicHave}/{RelicCost}");
        }

        player.Consume(ItemType.Junk, JunkCost);
        player.Consume(ItemType.RelicPart, RelicCost);
        player.AddItem(ItemType.Treasure, 1);

        return new CraftResult(true, "보물을 제작했다! 점수 +100");
    }
}

public sealed record CraftResult(bool Crafted, string Message);
