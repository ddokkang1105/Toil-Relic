using System;
using System.Linq;
using ToilRelic.Unity.Core;
using ToilRelic.Unity.Data;

namespace ToilRelic.Unity.Systems
{
    public enum QuarryRewardStatus { Applied, NotVictory, Rejected }
    public enum ProfileRewardResult { NotEvaluated, Granted, Missed, AlreadyOwned }
    public enum ContributionRewardResult { NotEvaluated, Granted, AlreadyComplete }

    public sealed class QuarryVictoryCommand
    {
        public QuarryVictoryCommand(string quarryId, bool victory, float profileRoll, int experience,
            int junk, int relicPart, int healingPotion)
        {
            QuarryId = quarryId;
            Victory = victory;
            ProfileRoll = profileRoll;
            Experience = experience;
            Junk = junk;
            RelicPart = relicPart;
            HealingPotion = healingPotion;
        }

        public string QuarryId { get; }
        public bool Victory { get; }
        public float ProfileRoll { get; }
        public int Experience { get; }
        public int Junk { get; }
        public int RelicPart { get; }
        public int HealingPotion { get; }
    }

    public sealed class QuarryRewardOutcome
    {
        public QuarryRewardOutcome(QuarryRewardStatus status, ProfileRewardResult profileResult,
            ContributionRewardResult contributionResult, bool projectReady, bool saveRequested,
            string profileEquipmentId = null, string contributionId = null, LevelUpResult? levelUp = null)
        {
            Status = status;
            ProfileResult = profileResult;
            ContributionResult = contributionResult;
            ProjectReady = projectReady;
            SaveRequested = saveRequested;
            ProfileEquipmentId = profileEquipmentId;
            ContributionId = contributionId;
            LevelUp = levelUp;
        }

        public QuarryRewardStatus Status { get; }
        public ProfileRewardResult ProfileResult { get; }
        public ContributionRewardResult ContributionResult { get; }
        public bool ProjectReady { get; }
        public bool SaveRequested { get; }
        public string ProfileEquipmentId { get; }
        public string ContributionId { get; }
        public LevelUpResult? LevelUp { get; }

        public static QuarryRewardOutcome Unchanged(QuarryRewardStatus status, bool projectReady) =>
            new(status, ProfileRewardResult.NotEvaluated, ContributionRewardResult.NotEvaluated, projectReady, false);
    }

    public static class QuarryRewardSystem
    {
        public static QuarryRewardOutcome Resolve(PlayerState player, HuntContractData contract,
            EnemyDatabase enemyDatabase, EquipmentDropProfileDatabase profileDatabase, QuarryVictoryCommand command)
        {
            if (player == null || command == null)
            {
                return QuarryRewardOutcome.Unchanged(QuarryRewardStatus.Rejected, false);
            }

            if (!command.Victory)
            {
                return QuarryRewardOutcome.Unchanged(QuarryRewardStatus.NotVictory, player.RelicProject.IsReady);
            }

            if (contract == null || profileDatabase == null ||
                !contract.Validate(enemyDatabase, profileDatabase).IsAvailable ||
                !contract.TryGetQuarry(command.QuarryId, out var quarry) ||
                !profileDatabase.TryGet(quarry.profileId, out var profile) ||
                !RelicProjectState.IsCanonicalContributionId(quarry.contributionId) ||
                command.Experience < 0 || command.Junk < 0 || command.RelicPart < 0 || command.HealingPotion < 0 ||
                float.IsNaN(command.ProfileRoll) || command.ProfileRoll < 0f || command.ProfileRoll >= 1f ||
                !player.HasValidCurrentSaveData())
            {
                return QuarryRewardOutcome.Unchanged(QuarryRewardStatus.Rejected, player.RelicProject.IsReady);
            }

            var contributionResult = player.RelicProject.CompletedContributionIds.Contains(quarry.contributionId)
                ? ContributionRewardResult.AlreadyComplete
                : ContributionRewardResult.Granted;
            var profileResult = command.ProfileRoll >= profile.chance
                ? ProfileRewardResult.Missed
                : player.OwnedEquipmentIds.Contains(profile.equipmentId)
                    ? ProfileRewardResult.AlreadyOwned
                    : ProfileRewardResult.Granted;

            var postState = player.CloneForTransaction();
            postState.Add(ItemType.Junk, command.Junk);
            postState.Add(ItemType.RelicPart, command.RelicPart);
            postState.Add(ItemType.HealingPotion, command.HealingPotion);
            var levelUp = postState.GainExperience(command.Experience);
            if (profileResult == ProfileRewardResult.Granted && !postState.GrantEquipment(profile.equipmentId))
            {
                return QuarryRewardOutcome.Unchanged(QuarryRewardStatus.Rejected, player.RelicProject.IsReady);
            }

            if (contributionResult == ContributionRewardResult.Granted &&
                !postState.RelicProject.TryAddContribution(quarry.contributionId))
            {
                return QuarryRewardOutcome.Unchanged(QuarryRewardStatus.Rejected, player.RelicProject.IsReady);
            }

            player.CommitFrom(postState);
            return new QuarryRewardOutcome(QuarryRewardStatus.Applied, profileResult, contributionResult,
                postState.RelicProject.IsReady, true, profile.equipmentId, quarry.contributionId, levelUp);
        }
    }
}
