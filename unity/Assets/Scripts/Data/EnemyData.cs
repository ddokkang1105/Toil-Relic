using UnityEngine;

namespace ToilRelic.Unity.Data
{
    [CreateAssetMenu(menuName = "ToilRelic/Enemy", fileName = "EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        public string displayName = "Mine Vermin";
        public int maxHp = 12;
        public int attackMin = 2;
        public int attackMax = 5;
    }
}
