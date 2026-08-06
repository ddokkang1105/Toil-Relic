using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToilRelic.Unity.Core
{
    [Serializable]
    public sealed class RelicProjectState
    {
        private static readonly string[] CanonicalIds =
        {
            "chitin-shard",
            "rustheart-core",
            "wraith-ash"
        };

        [SerializeField] private List<string> completedContributionIds = new();
        [SerializeField] private bool forged;

        public IReadOnlyList<string> CompletedContributionIds => completedContributionIds;
        public bool IsReady => completedContributionIds.Count == CanonicalIds.Length;
        public bool IsForged => forged;

        public bool TryAddContribution(string contributionId)
        {
            var canonicalIndex = Array.IndexOf(CanonicalIds, contributionId);
            if (canonicalIndex < 0 || completedContributionIds.Contains(contributionId))
            {
                return false;
            }

            completedContributionIds.Add(contributionId);
            completedContributionIds.Sort((left, right) =>
                Array.IndexOf(CanonicalIds, left).CompareTo(Array.IndexOf(CanonicalIds, right)));
            return true;
        }

        public bool TryMarkForged()
        {
            if (!IsReady || forged)
            {
                return false;
            }

            forged = true;
            return true;
        }

        internal static bool HasValidSaveState(
            RelicProjectState project,
            IReadOnlyCollection<string> ownedEquipmentIds,
            IReadOnlyCollection<EquippedEquipmentEntry> equippedEquipment)
        {
            if (project?.completedContributionIds == null)
            {
                return false;
            }

            var previousIndex = -1;
            foreach (var contributionId in project.completedContributionIds)
            {
                var currentIndex = Array.IndexOf(CanonicalIds, contributionId);
                if (currentIndex <= previousIndex)
                {
                    return false;
                }

                previousIndex = currentIndex;
            }

            var relicOwned = ownedEquipmentIds.Contains(EquipmentCatalog.ToilboundRelicId);
            var relicEquipped = equippedEquipment.Any(entry =>
                entry != null && string.Equals(entry.equipmentId, EquipmentCatalog.ToilboundRelicId, StringComparison.Ordinal));
            if (!project.IsReady)
            {
                return !project.forged && !relicOwned && !relicEquipped;
            }

            return project.forged
                ? relicOwned && (!relicEquipped || equippedEquipment.Any(entry =>
                    entry != null && entry.slot == EquipmentSlot.Necklace &&
                    string.Equals(entry.equipmentId, EquipmentCatalog.ToilboundRelicId, StringComparison.Ordinal)))
                : !relicOwned && !relicEquipped;
        }
    }
}
