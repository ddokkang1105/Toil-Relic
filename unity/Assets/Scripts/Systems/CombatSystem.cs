using ToilRelic.Unity.Data;
using UnityEngine;

namespace ToilRelic.Unity.Systems
{
    public sealed class EnemyRuntime
    {
        public string Id { get; }
        public string Name { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int AttackMin { get; }
        public int AttackMax { get; }
        public int ExpReward { get; }

        public EnemyRuntime(EnemyData data)
        {
            Id = data.id;
            Name = data.displayName;
            MaxHp = data.maxHp;
            Hp = data.maxHp;
            AttackMin = data.attackMin;
            AttackMax = data.attackMax;
            ExpReward = data.expReward;
        }

        public void TakeDamage(int amount)
        {
            Hp = Mathf.Max(0, Hp - Mathf.Max(0, amount));
        }

        public bool IsAlive => Hp > 0;
    }

    public sealed class CombatSystem
    {
        public int RollPlayerAttack(int attackBonus, EnemyIntent intent)
        {
            var rolled = Random.Range(4, 9) + attackBonus;
            return TacticalCombatRules.ApplyPlayerAttack(rolled, intent);
        }

        public int RollEnemyAttack(EnemyRuntime enemy, EnemyIntent intent, bool playerDefending)
        {
            var raw = Random.Range(enemy.AttackMin, enemy.AttackMax + 1);
            return TacticalCombatRules.ApplyEnemyAttack(raw, intent, playerDefending);
        }

        public bool TryFlee() => Random.value < 0.55f;
    }
}
