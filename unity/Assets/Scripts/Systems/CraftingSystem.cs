using ToilRelic.Unity.Core;

namespace ToilRelic.Unity.Systems
{
    public readonly struct CraftResult
    {
        public readonly bool Crafted;
        public readonly string Message;

        public CraftResult(bool crafted, string message)
        {
            Crafted = crafted;
            Message = message;
        }
    }

    public sealed class CraftingSystem
    {
        private const int JunkCost = 5;
        private const int RelicPartCost = 1;

        public CraftResult TryCraftTreasure(PlayerState player)
        {
            var junk = player.GetAmount(ItemType.Junk);
            var relic = player.GetAmount(ItemType.RelicPart);

            if (junk < JunkCost || relic < RelicPartCost)
            {
                return new CraftResult(false, $"Need junk {junk}/{JunkCost}, relic part {relic}/{RelicPartCost}");
            }

            player.Consume(ItemType.Junk, JunkCost);
            player.Consume(ItemType.RelicPart, RelicPartCost);
            player.Add(ItemType.Treasure, 1);
            return new CraftResult(true, "Treasure crafted. Score +100");
        }
    }
}
