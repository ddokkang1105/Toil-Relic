namespace ToilRelic.Unity.Core
{
    public enum GameState
    {
        Title,
        Camp,
        Hunt,
        Battle,
        Result
    }

    // Kept separate from GameState so a presentation layer can wait for an
    // animation, VFX, or camera transition without changing game rules.
    public enum BattlePhase
    {
        None,
        PlayerAction,
        EnemyAction,
        Resolving
    }
}
