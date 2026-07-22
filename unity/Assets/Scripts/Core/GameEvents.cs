using System;

namespace ToilRelic.Unity.Core
{
    public static class GameEvents
    {
        public static event Action<PlayerState> PlayerChanged;
        public static event Action<string> BattleLog;
        public static event Action<string> BattleOutcome;
        public static event Action<string> LevelUp;
        public static event Action<string> SaveFailed;
        public static event Action<string, int, int> EnemyChanged;
        public static event Action<GameState> StateChanged;
        public static event Action<BattlePhase> BattlePhaseChanged;

        public static void RaisePlayerChanged(PlayerState player) => PlayerChanged?.Invoke(player);
        public static void RaiseBattleLog(string message) => BattleLog?.Invoke(message);
        public static void RaiseBattleOutcome(string message) => BattleOutcome?.Invoke(message);
        public static void RaiseLevelUp(string message) => LevelUp?.Invoke(message);
        public static void RaiseSaveFailed(string message) => SaveFailed?.Invoke(message);
        public static void RaiseEnemyChanged(string name, int hp, int maxHp) => EnemyChanged?.Invoke(name, hp, maxHp);
        public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);
        public static void RaiseBattlePhaseChanged(BattlePhase phase) => BattlePhaseChanged?.Invoke(phase);
    }
}
