using ToilRelic.Models;

namespace ToilRelic.Systems;

public enum QuarryRewardStatus
{
    Applied,
    NotVictory,
    Rejected
}

public enum ProfileRewardResult
{
    NotEvaluated,
    Granted,
    Missed,
    AlreadyOwned
}

public enum ContributionRewardResult
{
    NotEvaluated,
    Granted,
    AlreadyComplete
}

public sealed record GenericHuntReward(int Junk, int RelicPart, int HealingPotion)
{
    public bool IsValid => Junk >= 0 && RelicPart >= 0 && HealingPotion >= 0;
}

public sealed record QuarryVictoryCommand(
    string QuarryId,
    bool Victory,
    double ProfileRoll,
    int Experience,
    GenericHuntReward GenericReward);

public sealed record QuarryRewardOutcome(
    QuarryRewardStatus Status,
    ProfileRewardResult ProfileResult,
    ContributionRewardResult ContributionResult,
    bool ProjectReady,
    bool SaveRequested,
    string? ProfileEquipmentId = null,
    string? ContributionId = null,
    LevelUpResult? LevelUp = null)
{
    public static QuarryRewardOutcome Unchanged(QuarryRewardStatus status, bool projectReady) =>
        new(status, ProfileRewardResult.NotEvaluated, ContributionRewardResult.NotEvaluated, projectReady, false);
}

public static class QuarryRewardSystem
{
    public static QuarryRewardOutcome Resolve(
        Player? player,
        HuntContract? contract,
        IEnumerable<EquipmentDropProfile>? profiles,
        QuarryVictoryCommand? command)
    {
        if (player is null || command is null)
        {
            return QuarryRewardOutcome.Unchanged(QuarryRewardStatus.Rejected, false);
        }

        if (!command.Victory)
        {
            return QuarryRewardOutcome.Unchanged(QuarryRewardStatus.NotVictory, player.RelicProject.IsReady);
        }

        var profileList = profiles?.ToArray() ?? Array.Empty<EquipmentDropProfile>();
        if (contract is null || !contract.Validate(profileList).IsAvailable ||
            !contract.TryGetQuarry(command.QuarryId, out var quarry) ||
            profileList.SingleOrDefault(profile => string.Equals(profile.Id, quarry.ProfileId, StringComparison.Ordinal)) is not { } profile ||
            !RelicProjectState.CanonicalContributionIds.Contains(quarry.ContributionId, StringComparer.Ordinal) ||
            command.Experience < 0 || command.GenericReward is null || !command.GenericReward.IsValid ||
            double.IsNaN(command.ProfileRoll) || command.ProfileRoll < 0d || command.ProfileRoll >= 1d ||
            !HasValidPlayerState(player))
        {
            return QuarryRewardOutcome.Unchanged(QuarryRewardStatus.Rejected, player.RelicProject.IsReady);
        }

        var contributionResult = player.RelicProject.CompletedContributionIds.Contains(quarry.ContributionId, StringComparer.Ordinal)
            ? ContributionRewardResult.AlreadyComplete
            : ContributionRewardResult.Granted;
        var profileResult = command.ProfileRoll >= profile.Chance
            ? ProfileRewardResult.Missed
            : player.OwnedEquipmentIds.Contains(profile.EquipmentId, StringComparer.Ordinal)
                ? ProfileRewardResult.AlreadyOwned
                : ProfileRewardResult.Granted;

        var postState = player.CloneForTransaction();
        postState.AddItem(ItemType.Junk, command.GenericReward.Junk);
        postState.AddItem(ItemType.RelicPart, command.GenericReward.RelicPart);
        postState.AddItem(ItemType.HealingPotion, command.GenericReward.HealingPotion);
        var levelUp = postState.GainExperience(command.Experience);
        if (profileResult == ProfileRewardResult.Granted && !postState.GrantEquipment(profile.EquipmentId))
        {
            return QuarryRewardOutcome.Unchanged(QuarryRewardStatus.Rejected, player.RelicProject.IsReady);
        }

        if (contributionResult == ContributionRewardResult.Granted && !postState.RelicProject.TryAddContribution(quarry.ContributionId))
        {
            return QuarryRewardOutcome.Unchanged(QuarryRewardStatus.Rejected, player.RelicProject.IsReady);
        }

        player.CommitFrom(postState);
        return new QuarryRewardOutcome(
            QuarryRewardStatus.Applied,
            profileResult,
            contributionResult,
            postState.RelicProject.IsReady,
            true,
            profile.EquipmentId,
            quarry.ContributionId,
            levelUp);
    }

    private static bool HasValidPlayerState(Player player)
    {
        var save = player.ToSaveData();
        return RelicProjectState.HasValidSaveState(save.RelicProject, save.OwnedEquipmentIds, save.EquippedEquipment);
    }
}
