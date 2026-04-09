using ToilRelic.Unity.Data;
using UnityEngine;

namespace ToilRelic.Unity.Systems
{
    public sealed class EnemyRuntime
    {
        public string Name { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int AttackMin { get; }
        public int AttackMax { get; }

        public EnemyRuntime(EnemyData data)
        {
            Name = data.displayName;
            MaxHp = data.maxHp;
            Hp = data.maxHp;
            AttackMin = data.attackMin;
            AttackMax = data.attackMax;
        }

        public void TakeDamage(int amount)
        {
            Hp = Mathf.Max(0, Hp - Mathf.Max(0, amount));
        }

        public bool IsAlive => Hp > 0;
    }

    public sealed class CombatSystem
    {
        public int RollPlayerAttack() => Random.Range(4, 9);

        public int RollEnemyAttack(EnemyRuntime enemy, bool playerDefending)
        {
            var raw = Random.Range(enemy.AttackMin, enemy.AttackMax + 1);
            return playerDefending ? Mathf.Max(0, raw - 3) : raw;
        }

        public bool TryFlee() => Random.value < 0.55f;
    }
}
