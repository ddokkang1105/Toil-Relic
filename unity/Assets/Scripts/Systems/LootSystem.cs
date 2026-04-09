using ToilRelic.Unity.Data;
using UnityEngine;

namespace ToilRelic.Unity.Systems
{
    public readonly struct LootRoll
    {
        public readonly int Junk;
        public readonly int RelicPart;

        public LootRoll(int junk, int relicPart)
        {
            Junk = junk;
            RelicPart = relicPart;
        }
    }

    public sealed class LootSystem
    {
        public LootRoll Roll(DropTableData dropTable)
        {
            var junk = Random.Range(dropTable.junkMin, dropTable.junkMax + 1);
            var relicPart = Random.value < dropTable.relicPartChance ? 1 : 0;
            return new LootRoll(junk, relicPart);
        }
    }
}
