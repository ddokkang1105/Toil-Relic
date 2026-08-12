using System;

namespace ToilRelic.Unity.Systems
{
    public enum EnemyIntentKind
    {
        DirectStrike,
        PowerAttack,
        ExposedOpening
    }

    public readonly struct EnemyIntent
    {
        public EnemyIntent(EnemyIntentKind kind, string label, string marker, string cue)
        {
            Kind = kind;
            Label = label;
            Marker = marker;
            Cue = cue;
        }

        public EnemyIntentKind Kind { get; }
        public string Label { get; }
        public string Marker { get; }
        public string Cue { get; }
        public string DisplayLabel => $"{Label} {Marker}";
    }

    public static class TacticalCombatRules
    {
        public const int OpeningAttackBonus = 3;
        public const int PowerDamageBonus = 2;
        public const int StandardDefendReduction = 3;
        public const int PowerDefendReduction = 6;

        private static readonly EnemyIntent DirectStrike = new EnemyIntent(
            EnemyIntentKind.DirectStrike,
            "Direct Strike",
            "[BASIC]",
            "Defend -3");

        private static readonly EnemyIntent PowerAttack = new EnemyIntent(
            EnemyIntentKind.PowerAttack,
            "Power Attack",
            "[POWER]",
            "Defend -6");

        private static readonly EnemyIntent ExposedOpening = new EnemyIntent(
            EnemyIntentKind.ExposedOpening,
            "Exposed Opening",
            "[OPEN]",
            "Attack +3");

        public static EnemyIntent GetIntent(string enemyId, int resolvedTurnIndex)
        {
            var turn = Math.Max(0, resolvedTurnIndex);
            switch (enemyId)
            {
                case "mine-vermin":
                    return turn % 2 == 0 ? ExposedOpening : PowerAttack;
                case "rust-golem":
                    return turn % 2 == 0 ? PowerAttack : ExposedOpening;
                default:
                    return DirectStrike;
            }
        }

        public static EnemyIntent Describe(EnemyIntentKind kind)
        {
            switch (kind)
            {
                case EnemyIntentKind.PowerAttack:
                    return PowerAttack;
                case EnemyIntentKind.ExposedOpening:
                    return ExposedOpening;
                default:
                    return DirectStrike;
            }
        }

        public static int ApplyPlayerAttack(int rolledDamage, EnemyIntent intent)
        {
            var bonus = intent.Kind == EnemyIntentKind.ExposedOpening ? OpeningAttackBonus : 0;
            return Math.Max(0, rolledDamage + bonus);
        }

        public static int ApplyEnemyAttack(int rolledDamage, EnemyIntent intent, bool playerDefending)
        {
            var damage = Math.Max(0, rolledDamage);
            if (intent.Kind == EnemyIntentKind.PowerAttack)
            {
                damage += PowerDamageBonus;
            }

            if (!playerDefending)
            {
                return damage;
            }

            var reduction = intent.Kind == EnemyIntentKind.PowerAttack
                ? PowerDefendReduction
                : StandardDefendReduction;
            return Math.Max(0, damage - reduction);
        }
    }
}
