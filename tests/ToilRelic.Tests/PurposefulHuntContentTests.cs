using ToilRelic.Models;

namespace ToilRelic.Tests;

public sealed class PurposefulHuntContentTests
{
    [Fact]
    public void ProductionContent_MatchesSharedFixtureAndValidates()
    {
        var fixture = PurposefulHuntContractFixture.Load().Content;
        var contract = PurposefulHuntContent.FirstRelicContract;

        Assert.Equal(fixture.ProjectId, contract.Id);
        Assert.Equal(fixture.RelicEquipmentId, contract.RelicEquipmentId);
        Assert.Equal(fixture.Quarries.Select(quarry => quarry.Id), contract.Quarries.Select(quarry => quarry.Id));
        Assert.Equal(fixture.Quarries.Select(quarry => quarry.EnemyId), contract.Quarries.Select(quarry => quarry.EnemyId));
        Assert.Equal(fixture.Quarries.Select(quarry => quarry.Danger), contract.Quarries.Select(quarry => quarry.Danger.ToString()));
        Assert.Equal(fixture.Quarries.Select(quarry => quarry.ContributionId), contract.Quarries.Select(quarry => quarry.ContributionId));
        Assert.Equal(fixture.Quarries.Select(quarry => quarry.ContributionName), contract.Quarries.Select(quarry => quarry.ContributionDisplayName));
        Assert.Equal(fixture.Quarries.Select(quarry => quarry.ProfileId), contract.Quarries.Select(quarry => quarry.ProfileId));
        Assert.Equal(fixture.Profiles.Select(profile => profile.Id), PurposefulHuntContent.Profiles.Select(profile => profile.Id));
        Assert.Equal(fixture.Profiles.Select(profile => profile.EquipmentId), PurposefulHuntContent.Profiles.Select(profile => profile.EquipmentId));
        Assert.Equal(fixture.Profiles.Select(profile => profile.Chance), PurposefulHuntContent.Profiles.Select(profile => profile.Chance));
        Assert.True(contract.Validate(PurposefulHuntContent.Profiles).IsAvailable);

        foreach (var expected in fixture.Equipment)
        {
            Assert.True(EquipmentCatalog.TryGet(expected.Id, out var actual));
            Assert.Equal(expected.DisplayName, actual.DisplayName);
            Assert.Equal(expected.Category, actual.Category.ToString());
            Assert.Equal(expected.Attack, actual.AttackBonus);
            Assert.Equal(expected.Defense, actual.DefenseBonus);
            Assert.Equal(expected.DamageReduction, actual.DamageReductionBonus);
            Assert.Equal(expected.MaxHp, actual.MaxHpBonus);
        }
    }

    [Fact]
    public void EnemyLookup_ReturnsFreshBattleStateForStableId()
    {
        Assert.True(Enemy.TryCreate("rust-golem", out var first));
        Assert.True(Enemy.TryCreate("rust-golem", out var second));

        first.TakeDamage(3);

        Assert.Equal("rust-golem", first.Id);
        Assert.Equal("Rust Golem", first.Name);
        Assert.NotEqual(first.Hp, second.Hp);
        Assert.Equal("profile-rust-golem", second.EquipmentDropProfileId);
        Assert.False(Enemy.TryCreate("unknown", out _));
    }

    [Fact]
    public void Validation_ReturnsTypedFailuresWithoutFallback()
    {
        var production = PurposefulHuntContent.FirstRelicContract;
        var missingProfile = production.Validate(PurposefulHuntContent.Profiles.Take(2));
        Assert.Equal(HuntContentIssue.MissingProfile, missingProfile.Issue);

        var duplicateQuarry = new HuntContract(
            production.Id,
            production.DisplayName,
            production.RelicEquipmentId,
            new[] { production.Quarries[0], production.Quarries[0], production.Quarries[2] });
        Assert.Equal(HuntContentIssue.DuplicateQuarry, duplicateQuarry.Validate(PurposefulHuntContent.Profiles).Issue);

        var forbiddenProfiles = PurposefulHuntContent.Profiles
            .Select(profile => profile.Id == production.Quarries[0].ProfileId
                ? profile with { EquipmentId = EquipmentCatalog.RewardWeaponId }
                : profile)
            .ToArray();
        Assert.Equal(HuntContentIssue.ForbiddenProfileEquipment, production.Validate(forbiddenProfiles).Issue);
    }

    [Fact]
    public void EquipmentCatalog_KeepsLegacyDefinitionsAlongsidePurposefulHuntItems()
    {
        Assert.Equal(6, EquipmentCatalog.All.Count);
        Assert.True(EquipmentCatalog.TryGet(EquipmentCatalog.StarterWeaponId, out _));
        Assert.True(EquipmentCatalog.TryGet(EquipmentCatalog.RewardWeaponId, out var reward));
        Assert.True(reward.CanEquipTo(EquipmentSlot.PrimaryWeapon));
        Assert.True(EquipmentCatalog.TryGet(EquipmentCatalog.ToilboundRelicId, out var relic));
        Assert.True(relic.CanEquipTo(EquipmentSlot.Necklace));
    }
}
