using UnityEngine;

namespace ToilRelic.Unity.Data
{
    [CreateAssetMenu(menuName = "ToilRelic/Drop Table", fileName = "DropTableData")]
    public sealed class DropTableData : ScriptableObject
    {
        public int junkMin = 1;
        public int junkMax = 3;
        [Range(0f, 1f)] public float relicPartChance = 0.25f;
        [Range(0f, 1f)] public float healingPotionChance = 0.35f;
    }
}
