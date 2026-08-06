using System;
using System.Collections.Generic;

namespace ToilRelic.Unity.Core
{
    public enum SaveFeedbackStatus
    {
        Succeeded,
        Failed
    }

    public sealed class HuntQuarrySnapshot
    {
        public HuntQuarrySnapshot(string id, string enemyId, string enemyName, string danger,
            string profileEquipmentId, string profileEquipmentName, int profileChancePercent,
            string contributionId, string contributionName, bool completed)
        {
            Id = id;
            EnemyId = enemyId;
            EnemyName = enemyName;
            Danger = danger;
            ProfileEquipmentId = profileEquipmentId;
            ProfileEquipmentName = profileEquipmentName;
            ProfileChancePercent = profileChancePercent;
            ContributionId = contributionId;
            ContributionName = contributionName;
            Completed = completed;
        }

        public string Id { get; }
        public string EnemyId { get; }
        public string EnemyName { get; }
        public string Danger { get; }
        public string ProfileEquipmentId { get; }
        public string ProfileEquipmentName { get; }
        public int ProfileChancePercent { get; }
        public string ContributionId { get; }
        public string ContributionName { get; }
        public bool Completed { get; }
    }

    public sealed class HuntContractSnapshot
    {
        public HuntContractSnapshot(string projectId, string displayName, string revision,
            IReadOnlyList<HuntQuarrySnapshot> quarries)
        {
            ProjectId = projectId;
            DisplayName = displayName;
            Revision = revision;
            Quarries = quarries;
        }

        public string ProjectId { get; }
        public string DisplayName { get; }
        public string Revision { get; }
        public IReadOnlyList<HuntQuarrySnapshot> Quarries { get; }
    }

    public sealed class RelicProjectSnapshot
    {
        public RelicProjectSnapshot(int completed, int required, bool ready, bool forged)
        {
            Completed = completed;
            Required = required;
            Ready = ready;
            Forged = forged;
        }

        public int Completed { get; }
        public int Required { get; }
        public bool Ready { get; }
        public bool Forged { get; }
    }

    public static class GameEvents
    {
        public static event Action<PlayerState> PlayerChanged;
        public static event Action<string> BattleLog;
        public static event Action<string> BattleOutcome;
        public static event Action<string> LevelUp;
        public static event Action<SaveFeedbackStatus> SaveStatusChanged;
        public static event Action<string, int, int> EnemyChanged;
        public static event Action<GameState> StateChanged;
        public static event Action<BattlePhase> BattlePhaseChanged;
        public static event Action<HuntContractSnapshot> HuntContractPresented;
        public static event Action HuntContractClosed;
        public static event Action<RelicProjectSnapshot> RelicProjectChanged;
        public static event Action<EquipmentSlot, string> EquipmentFocusRequested;

        public static void RaisePlayerChanged(PlayerState player) => PlayerChanged?.Invoke(player);
        public static void RaiseBattleLog(string message) => BattleLog?.Invoke(message);
        public static void RaiseBattleOutcome(string message) => BattleOutcome?.Invoke(message);
        public static void RaiseLevelUp(string message) => LevelUp?.Invoke(message);
        public static void RaiseSaveStatusChanged(SaveFeedbackStatus status) => SaveStatusChanged?.Invoke(status);
        public static void RaiseEnemyChanged(string name, int hp, int maxHp) => EnemyChanged?.Invoke(name, hp, maxHp);
        public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);
        public static void RaiseBattlePhaseChanged(BattlePhase phase) => BattlePhaseChanged?.Invoke(phase);
        public static void RaiseHuntContractPresented(HuntContractSnapshot snapshot) => HuntContractPresented?.Invoke(snapshot);
        public static void RaiseHuntContractClosed() => HuntContractClosed?.Invoke();
        public static void RaiseRelicProjectChanged(RelicProjectSnapshot snapshot) => RelicProjectChanged?.Invoke(snapshot);
        public static void RaiseEquipmentFocusRequested(EquipmentSlot slot, string equipmentId) => EquipmentFocusRequested?.Invoke(slot, equipmentId);
    }
}
