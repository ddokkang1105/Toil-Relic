using System;

namespace ToilRelic.Unity.Core
{
    public static class GameEvents
    {
        public static event Action<PlayerState> PlayerChanged;
        public static event Action<string> BattleLog;
        public static event Action<string, int, int> EnemyChanged;
        public static event Action<GameState> StateChanged;

        public static void RaisePlayerChanged(PlayerState player) => PlayerChanged?.Invoke(player);
        public static void RaiseBattleLog(string message) => BattleLog?.Invoke(message);
        public static void RaiseEnemyChanged(string name, int hp, int maxHp) => EnemyChanged?.Invoke(name, hp, maxHp);
        public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);
    }
}
