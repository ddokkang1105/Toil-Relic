namespace ToilRelic.Models;

public sealed class RelicProjectState
{
    private readonly List<string> _completedContributionIds = new();
    private bool _forged;

    public IReadOnlyList<string> CompletedContributionIds => _completedContributionIds;
    public bool IsReady => _completedContributionIds.Count == CanonicalContributionIds.Count;
    public bool IsForged => _forged;

    public static IReadOnlyList<string> CanonicalContributionIds { get; } =
        PurposefulHuntContent.FirstRelicContract.Quarries
            .Select(quarry => quarry.ContributionId)
            .ToArray();

    public bool TryAddContribution(string? contributionId)
    {
        var canonicalIndex = IndexOfCanonical(contributionId);
        if (canonicalIndex < 0 || _completedContributionIds.Contains(contributionId!, StringComparer.Ordinal))
        {
            return false;
        }

        _completedContributionIds.Add(contributionId!);
        _completedContributionIds.Sort((left, right) => IndexOfCanonical(left).CompareTo(IndexOfCanonical(right)));
        return true;
    }

    public bool TryMarkForged()
    {
        if (!IsReady || _forged)
        {
            return false;
        }

        _forged = true;
        return true;
    }

    internal RelicProjectSaveData ToSaveData() => new()
    {
        CompletedContributionIds = new(_completedContributionIds),
        Forged = _forged
    };

    internal void LoadFromSaveData(RelicProjectSaveData? saveData)
    {
        _completedContributionIds.Clear();
        if (saveData is not null)
        {
            _completedContributionIds.AddRange(saveData.CompletedContributionIds);
            _forged = saveData.Forged;
        }
        else
        {
            _forged = false;
        }
    }

    internal static bool HasValidSaveState(
        RelicProjectSaveData? saveData,
        IReadOnlyCollection<string> ownedEquipmentIds,
        IReadOnlyCollection<EquippedEquipmentEntry> equippedEquipment)
    {
        if (saveData?.CompletedContributionIds is null)
        {
            return false;
        }

        var previousIndex = -1;
        foreach (var contributionId in saveData.CompletedContributionIds)
        {
            var currentIndex = IndexOfCanonical(contributionId);
            if (currentIndex <= previousIndex)
            {
                return false;
            }

            previousIndex = currentIndex;
        }

        var relicOwned = ownedEquipmentIds.Contains(EquipmentCatalog.ToilboundRelicId, StringComparer.Ordinal);
        var relicEquipped = equippedEquipment.Any(entry =>
            string.Equals(entry.EquipmentId, EquipmentCatalog.ToilboundRelicId, StringComparison.Ordinal));
        var ready = saveData.CompletedContributionIds.Count == CanonicalContributionIds.Count;
        if (!ready)
        {
            return !saveData.Forged && !relicOwned && !relicEquipped;
        }

        return saveData.Forged
            ? relicOwned && (!relicEquipped || equippedEquipment.Any(entry =>
                entry.Slot == EquipmentSlot.Necklace &&
                string.Equals(entry.EquipmentId, EquipmentCatalog.ToilboundRelicId, StringComparison.Ordinal)))
            : !relicOwned && !relicEquipped;
    }

    private static int IndexOfCanonical(string? contributionId)
    {
        for (var index = 0; index < CanonicalContributionIds.Count; index++)
        {
            if (string.Equals(CanonicalContributionIds[index], contributionId, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}
