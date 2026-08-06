using System;
using System.Collections.Generic;
using System.Linq;
using ToilRelic.Unity.Core;
using UnityEngine;

namespace ToilRelic.Unity.Data
{
    public enum HuntDanger
    {
        Low,
        Medium,
        High
    }

    public enum HuntContentIssue
    {
        None,
        InvalidContract,
        InvalidQuarry,
        DuplicateQuarry,
        MissingEnemy,
        MissingProfile,
        DuplicateProfile,
        InvalidProfile,
        MissingProfileEquipment,
        ForbiddenProfileEquipment,
        MissingRelicEquipment
    }

    public sealed class HuntContentValidationResult
    {
        private HuntContentValidationResult(bool isAvailable, HuntContentIssue issue, string contentId)
        {
            IsAvailable = isAvailable;
            Issue = issue;
            ContentId = contentId;
        }

        public bool IsAvailable { get; }
        public HuntContentIssue Issue { get; }
        public string ContentId { get; }

        public static HuntContentValidationResult Available() => new(true, HuntContentIssue.None, null);
        public static HuntContentValidationResult Unavailable(HuntContentIssue issue, string contentId) => new(false, issue, contentId);
    }

    [Serializable]
    public sealed class HuntQuarryData
    {
        public string id;
        public string enemyId;
        public HuntDanger danger;
        public string contributionId;
        public string contributionDisplayName;
        public string profileId;
    }

    [CreateAssetMenu(menuName = "ToilRelic/Hunt Contract", fileName = "HuntContractData")]
    public sealed class HuntContractData : ScriptableObject
    {
        public string projectId;
        public string displayName;
        public string relicEquipmentId;
        public List<HuntQuarryData> quarries = new();

        public bool TryGetQuarry(string id, out HuntQuarryData quarry)
        {
            quarry = quarries?.FirstOrDefault(candidate =>
                candidate != null && string.Equals(candidate.id, id, StringComparison.Ordinal));
            return quarry != null;
        }

        public HuntContentValidationResult Validate(
            EnemyDatabase enemyDatabase,
            EquipmentDropProfileDatabase profileDatabase)
        {
            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(displayName) ||
                quarries == null || quarries.Count != 3)
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.InvalidContract, projectId);
            }

            if (!EquipmentCatalog.TryGet(relicEquipmentId, out var relicEquipment))
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.MissingRelicEquipment, relicEquipmentId);
            }

            if (!string.Equals(relicEquipmentId, EquipmentCatalog.ToilboundRelicId, StringComparison.Ordinal) ||
                !relicEquipment.CanEquipTo(EquipmentSlot.Necklace))
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.InvalidContract, relicEquipmentId);
            }

            if (enemyDatabase == null || profileDatabase == null)
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.InvalidContract, projectId);
            }

            if (enemyDatabase.enemies == null || profileDatabase.profiles == null)
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.InvalidContract, projectId);
            }

            var duplicateProfile = profileDatabase.profiles
                .Where(profile => profile != null)
                .GroupBy(profile => profile.id, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateProfile != null)
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.DuplicateProfile, duplicateProfile.Key);
            }

            var quarryIds = new HashSet<string>(StringComparer.Ordinal);
            var enemyIds = new HashSet<string>(StringComparer.Ordinal);
            var contributionIds = new HashSet<string>(StringComparer.Ordinal);
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var quarry in quarries)
            {
                if (quarry == null || string.IsNullOrWhiteSpace(quarry.id) || string.IsNullOrWhiteSpace(quarry.enemyId) ||
                    string.IsNullOrWhiteSpace(quarry.contributionId) || string.IsNullOrWhiteSpace(quarry.contributionDisplayName) ||
                    string.IsNullOrWhiteSpace(quarry.profileId))
                {
                    return HuntContentValidationResult.Unavailable(HuntContentIssue.InvalidQuarry, quarry?.id);
                }

                if (!quarryIds.Add(quarry.id) || !enemyIds.Add(quarry.enemyId) ||
                    !contributionIds.Add(quarry.contributionId) || !profileIds.Add(quarry.profileId))
                {
                    return HuntContentValidationResult.Unavailable(HuntContentIssue.DuplicateQuarry, quarry.id);
                }

                if (!enemyDatabase.TryGet(quarry.enemyId, out _))
                {
                    return HuntContentValidationResult.Unavailable(HuntContentIssue.MissingEnemy, quarry.enemyId);
                }

                if (!profileDatabase.TryGet(quarry.profileId, out var profile))
                {
                    return HuntContentValidationResult.Unavailable(HuntContentIssue.MissingProfile, quarry.profileId);
                }

                if (!profile.HasValidShape)
                {
                    return HuntContentValidationResult.Unavailable(HuntContentIssue.InvalidProfile, profile.id);
                }

                if (string.Equals(profile.equipmentId, EquipmentCatalog.RewardWeaponId, StringComparison.Ordinal) ||
                    string.Equals(profile.equipmentId, EquipmentCatalog.ToilboundRelicId, StringComparison.Ordinal))
                {
                    return HuntContentValidationResult.Unavailable(HuntContentIssue.ForbiddenProfileEquipment, profile.equipmentId);
                }

                if (!EquipmentCatalog.TryGet(profile.equipmentId, out _))
                {
                    return HuntContentValidationResult.Unavailable(HuntContentIssue.MissingProfileEquipment, profile.equipmentId);
                }
            }

            return HuntContentValidationResult.Available();
        }
    }
}
