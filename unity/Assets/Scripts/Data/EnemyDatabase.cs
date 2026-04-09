using System.Collections.Generic;
using UnityEngine;

namespace ToilRelic.Unity.Data
{
    [CreateAssetMenu(menuName = "ToilRelic/Enemy Database", fileName = "EnemyDatabase")]
    public sealed class EnemyDatabase : ScriptableObject
    {
        [SerializeField] private List<EnemyData> enemies = new();

        public EnemyData GetRandom()
        {
            if (enemies.Count == 0)
            {
                return null;
            }

            return enemies[Random.Range(0, enemies.Count)];
        }
    }
}
