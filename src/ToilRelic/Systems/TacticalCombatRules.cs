namespace ToilRelic.Systems;

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

    private static readonly EnemyIntent _directStrike = new(
        EnemyIntentKind.DirectStrike,
        "Direct Strike",
        "[BASIC]",
        "Defend -3");

    private static readonly EnemyIntent _powerAttack = new(
        EnemyIntentKind.PowerAttack,
        "Power Attack",
        "[POWER]",
        "Defend -6");

    private static readonly EnemyIntent _exposedOpening = new(
        EnemyIntentKind.ExposedOpening,
        "Exposed Opening",
        "[OPEN]",
        "Attack +3");

    public static EnemyIntent GetIntent(string? enemyId, int resolvedTurnIndex)
    {
        var turn = Math.Max(0, resolvedTurnIndex);
        return enemyId switch
        {
            "mine-vermin" => turn % 2 == 0 ? _exposedOpening : _powerAttack,
            "rust-golem" => turn % 2 == 0 ? _powerAttack : _exposedOpening,
            _ => _directStrike
        };
    }

    public static EnemyIntent Describe(EnemyIntentKind kind) => kind switch
    {
        EnemyIntentKind.PowerAttack => _powerAttack,
        EnemyIntentKind.ExposedOpening => _exposedOpening,
        _ => _directStrike
    };

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
