using System.Collections.Generic;
using UnityEngine;

namespace ToilRelic.Unity.Data
{
    [CreateAssetMenu(menuName = "ToilRelic/Enemy Database", fileName = "EnemyDatabase")]
    public sealed class EnemyDatabase : ScriptableObject
    {
        public List<EnemyData> enemies = new();

        public bool TryGet(string id, out EnemyData enemy)
        {
            enemy = null;
            if (string.IsNullOrEmpty(id) || enemies == null)
            {
                return false;
            }

            foreach (var candidate in enemies)
            {
                if (candidate != null && string.Equals(candidate.id, id, System.StringComparison.Ordinal))
                {
                    enemy = candidate;
                    return true;
                }
            }

            return false;
        }

        public EnemyData GetRandom()
        {
            if (enemies == null || enemies.Count == 0)
            {
                return null;
            }

            return enemies[Random.Range(0, enemies.Count)];
        }
    }
}
