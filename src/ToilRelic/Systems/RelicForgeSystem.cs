using ToilRelic.Models;

namespace ToilRelic.Systems;

public enum ForgeStatus
{
    Forged,
    NotReady,
    AlreadyForged,
    Rejected
}

public sealed record ForgeOutcome(ForgeStatus Status, bool SaveRequested, string? EquipmentId = null);

public static class RelicForgeSystem
{
    public static ForgeOutcome Forge(Player? player, HuntContract? contract)
    {
        if (player is null || contract is null ||
            !contract.Validate(PurposefulHuntContent.Profiles).IsAvailable ||
            !EquipmentCatalog.TryGet(contract.RelicEquipmentId, out _))
        {
            return new ForgeOutcome(ForgeStatus.Rejected, false);
        }

        var save = player.ToSaveData();
        if (!RelicProjectState.HasValidSaveState(save.RelicProject, save.OwnedEquipmentIds, save.EquippedEquipment))
        {
            return new ForgeOutcome(ForgeStatus.Rejected, false);
        }

        if (player.RelicProject.IsForged)
        {
            return new ForgeOutcome(ForgeStatus.AlreadyForged, false, contract.RelicEquipmentId);
        }

        if (!player.RelicProject.IsReady)
        {
            return new ForgeOutcome(ForgeStatus.NotReady, false, contract.RelicEquipmentId);
        }

        if (player.OwnedEquipmentIds.Contains(contract.RelicEquipmentId, StringComparer.Ordinal))
        {
            return new ForgeOutcome(ForgeStatus.Rejected, false, contract.RelicEquipmentId);
        }

        var postState = player.CloneForTransaction();
        if (!postState.GrantEquipment(contract.RelicEquipmentId) || !postState.RelicProject.TryMarkForged())
        {
            return new ForgeOutcome(ForgeStatus.Rejected, false, contract.RelicEquipmentId);
        }

        player.CommitFrom(postState);
        return new ForgeOutcome(ForgeStatus.Forged, true, contract.RelicEquipmentId);
    }
}
