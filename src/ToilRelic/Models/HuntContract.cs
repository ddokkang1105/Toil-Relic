namespace ToilRelic.Models;

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

public sealed record HuntContentValidationResult(bool IsAvailable, HuntContentIssue Issue, string? ContentId)
{
    public static HuntContentValidationResult Available() => new(true, HuntContentIssue.None, null);

    public static HuntContentValidationResult Unavailable(HuntContentIssue issue, string? contentId) =>
        new(false, issue, contentId);
}

public sealed record HuntQuarry(
    string Id,
    string EnemyId,
    HuntDanger Danger,
    string ContributionId,
    string ContributionDisplayName,
    string ProfileId);

public sealed class HuntContract
{
    private readonly HuntQuarry[] _quarries;

    public HuntContract(string id, string displayName, string relicEquipmentId, IEnumerable<HuntQuarry> quarries)
    {
        Id = id;
        DisplayName = displayName;
        RelicEquipmentId = relicEquipmentId;
        _quarries = quarries?.ToArray() ?? Array.Empty<HuntQuarry>();
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string RelicEquipmentId { get; }
    public IReadOnlyList<HuntQuarry> Quarries => _quarries;

    public bool TryGetQuarry(string? id, out HuntQuarry quarry)
    {
        quarry = _quarries.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal))!;
        return quarry is not null;
    }

    public HuntContentValidationResult Validate(IEnumerable<EquipmentDropProfile> profiles)
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(DisplayName) || _quarries.Length != 3)
        {
            return HuntContentValidationResult.Unavailable(HuntContentIssue.InvalidContract, Id);
        }

        if (!EquipmentCatalog.TryGet(RelicEquipmentId, out var relicEquipment))
        {
            return HuntContentValidationResult.Unavailable(HuntContentIssue.MissingRelicEquipment, RelicEquipmentId);
        }

        if (!string.Equals(RelicEquipmentId, EquipmentCatalog.ToilboundRelicId, StringComparison.Ordinal) ||
            !relicEquipment.CanEquipTo(EquipmentSlot.Necklace))
        {
            return HuntContentValidationResult.Unavailable(HuntContentIssue.InvalidContract, RelicEquipmentId);
        }

        var profileList = profiles?.ToArray() ?? Array.Empty<EquipmentDropProfile>();
        var duplicateProfile = profileList.GroupBy(profile => profile.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateProfile is not null)
        {
            return HuntContentValidationResult.Unavailable(HuntContentIssue.DuplicateProfile, duplicateProfile.Key);
        }

        var profileMap = profileList.ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        var quarryIds = new HashSet<string>(StringComparer.Ordinal);
        var enemyIds = new HashSet<string>(StringComparer.Ordinal);
        var contributionIds = new HashSet<string>(StringComparer.Ordinal);
        var profileIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var quarry in _quarries)
        {
            if (quarry is null || string.IsNullOrWhiteSpace(quarry.Id) || string.IsNullOrWhiteSpace(quarry.EnemyId) ||
                string.IsNullOrWhiteSpace(quarry.ContributionId) || string.IsNullOrWhiteSpace(quarry.ContributionDisplayName) ||
                string.IsNullOrWhiteSpace(quarry.ProfileId))
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.InvalidQuarry, quarry?.Id);
            }

            if (!quarryIds.Add(quarry.Id) || !enemyIds.Add(quarry.EnemyId) ||
                !contributionIds.Add(quarry.ContributionId) || !profileIds.Add(quarry.ProfileId))
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.DuplicateQuarry, quarry.Id);
            }

            if (!Enemy.TryCreate(quarry.EnemyId, out _))
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.MissingEnemy, quarry.EnemyId);
            }

            if (!profileMap.TryGetValue(quarry.ProfileId, out var profile))
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.MissingProfile, quarry.ProfileId);
            }

            if (!profile.HasValidShape)
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.InvalidProfile, profile.Id);
            }

            if (string.Equals(profile.EquipmentId, EquipmentCatalog.RewardWeaponId, StringComparison.Ordinal) ||
                string.Equals(profile.EquipmentId, EquipmentCatalog.ToilboundRelicId, StringComparison.Ordinal))
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.ForbiddenProfileEquipment, profile.EquipmentId);
            }

            if (!EquipmentCatalog.TryGet(profile.EquipmentId, out _))
            {
                return HuntContentValidationResult.Unavailable(HuntContentIssue.MissingProfileEquipment, profile.EquipmentId);
            }
        }

        return HuntContentValidationResult.Available();
    }
}

public static class PurposefulHuntContent
{
    private static readonly EquipmentDropProfile[] ProductionProfiles =
    {
        new("profile-mine-vermin", EquipmentCatalog.VerminFangId, 0.35d),
        new("profile-rust-golem", EquipmentCatalog.RustguardPlateId, 0.35d),
        new("profile-ruin-wraith", EquipmentCatalog.WraithSignetId, 0.35d)
    };

    public static HuntContract FirstRelicContract { get; } = new(
        "first-relic-project",
        "First Relic Project",
        EquipmentCatalog.ToilboundRelicId,
        new[]
        {
            new HuntQuarry("mine-vermin", "mine-vermin", HuntDanger.Low, "chitin-shard", "Chitin Shard", "profile-mine-vermin"),
            new HuntQuarry("rust-golem", "rust-golem", HuntDanger.Medium, "rustheart-core", "Rustheart Core", "profile-rust-golem"),
            new HuntQuarry("ruin-wraith", "ruin-wraith", HuntDanger.High, "wraith-ash", "Wraith Ash", "profile-ruin-wraith")
        });

    public static IReadOnlyList<EquipmentDropProfile> Profiles => ProductionProfiles;
}
