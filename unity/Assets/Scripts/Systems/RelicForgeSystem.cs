using System;
using System.Linq;
using ToilRelic.Unity.Core;
using ToilRelic.Unity.Data;

namespace ToilRelic.Unity.Systems
{
    public enum ForgeStatus { Forged, NotReady, AlreadyForged, Rejected }

    public sealed class ForgeOutcome
    {
        public ForgeOutcome(ForgeStatus status, bool saveRequested, string equipmentId = null)
        {
            Status = status;
            SaveRequested = saveRequested;
            EquipmentId = equipmentId;
        }

        public ForgeStatus Status { get; }
        public bool SaveRequested { get; }
        public string EquipmentId { get; }
    }

    public static class RelicForgeSystem
    {
        public static ForgeOutcome Forge(PlayerState player, HuntContractData contract,
            EnemyDatabase enemyDatabase, EquipmentDropProfileDatabase profileDatabase)
        {
            if (player == null || contract == null ||
                !contract.Validate(enemyDatabase, profileDatabase).IsAvailable ||
                !EquipmentCatalog.TryGet(contract.relicEquipmentId, out _))
            {
                return new ForgeOutcome(ForgeStatus.Rejected, false);
            }

            if (!player.HasValidCurrentSaveData())
            {
                return new ForgeOutcome(ForgeStatus.Rejected, false, contract.relicEquipmentId);
            }

            if (player.RelicProject.IsForged)
            {
                return new ForgeOutcome(ForgeStatus.AlreadyForged, false, contract.relicEquipmentId);
            }

            if (!player.RelicProject.IsReady)
            {
                return new ForgeOutcome(ForgeStatus.NotReady, false, contract.relicEquipmentId);
            }

            if (player.OwnedEquipmentIds.Contains(contract.relicEquipmentId))
            {
                return new ForgeOutcome(ForgeStatus.Rejected, false, contract.relicEquipmentId);
            }

            var postState = player.CloneForTransaction();
            if (!postState.GrantEquipment(contract.relicEquipmentId) || !postState.RelicProject.TryMarkForged())
            {
                return new ForgeOutcome(ForgeStatus.Rejected, false, contract.relicEquipmentId);
            }

            player.CommitFrom(postState);
            return new ForgeOutcome(ForgeStatus.Forged, true, contract.relicEquipmentId);
        }
    }
}
