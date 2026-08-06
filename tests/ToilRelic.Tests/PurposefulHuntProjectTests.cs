using System.Text.Json;
using System.Text.Json.Nodes;
using ToilRelic.Models;
using ToilRelic.Systems;

namespace ToilRelic.Tests;

[Collection(ConsoleCollection.Name)]
public sealed class PurposefulHuntProjectTests
{
    [Fact]
    public void QuarryRewardVectors_AreAtomicAndMatchSharedOutcomes()
    {
        var fixture = PurposefulHuntContractFixture.Load();
        foreach (var vector in fixture.Commands)
        {
            var player = new Player(vector.Id);
            foreach (var contributionId in vector.Precompleted)
            {
                Assert.True(player.RelicProject.TryAddContribution(contributionId));
            }

            var quarry = PurposefulHuntContent.FirstRelicContract.Quarries.Single(item => item.Id == vector.QuarryId);
            var profile = PurposefulHuntContent.Profiles.Single(item => item.Id == quarry.ProfileId);
            if (vector.PreownedProfile)
            {
                Assert.True(player.GrantEquipment(profile.EquipmentId));
            }

            var before = JsonSerializer.Serialize(player.ToSaveData());
            var result = QuarryRewardSystem.Resolve(
                player,
                PurposefulHuntContent.FirstRelicContract,
                PurposefulHuntContent.Profiles,
                new QuarryVictoryCommand(
                    vector.QuarryId,
                    vector.Victory,
                    vector.ProfileRoll,
                    vector.Experience,
                    new GenericHuntReward(vector.Junk, vector.RelicPart, vector.HealingPotion)));

            Assert.Equal(vector.ExpectedStatus, result.Status.ToString());
            Assert.Equal(vector.ExpectedProfile, result.ProfileResult.ToString());
            Assert.Equal(vector.ExpectedContribution, result.ContributionResult.ToString());
            Assert.Equal(vector.ExpectedReady, result.ProjectReady);
            Assert.Equal(vector.ExpectedSaveRequested, result.SaveRequested);
            if (!vector.ExpectedSaveRequested)
            {
                Assert.Equal(before, JsonSerializer.Serialize(player.ToSaveData()));
            }
            else
            {
                Assert.Equal(vector.Junk, player.Inventory[ItemType.Junk]);
                Assert.Equal(vector.RelicPart, player.Inventory[ItemType.RelicPart]);
                Assert.Equal(vector.HealingPotion, player.Inventory[ItemType.HealingPotion]);
                Assert.DoesNotContain(EquipmentCatalog.RewardWeaponId, player.OwnedEquipmentIds);
            }
        }
    }

    [Fact]
    public void Forge_IsAtomicIdempotentAndRejectsConflictingOwnership()
    {
        var unready = new Player("Unready");
        var unreadyBefore = JsonSerializer.Serialize(unready.ToSaveData());
        var unreadyResult = RelicForgeSystem.Forge(unready, PurposefulHuntContent.FirstRelicContract);
        Assert.Equal(ForgeStatus.NotReady, unreadyResult.Status);
        Assert.False(unreadyResult.SaveRequested);
        Assert.Equal(unreadyBefore, JsonSerializer.Serialize(unready.ToSaveData()));

        var ready = CreateReadyPlayer();
        var forged = RelicForgeSystem.Forge(ready, PurposefulHuntContent.FirstRelicContract);
        Assert.Equal(ForgeStatus.Forged, forged.Status);
        Assert.True(forged.SaveRequested);
        Assert.True(ready.RelicProject.IsForged);
        Assert.Contains(EquipmentCatalog.ToilboundRelicId, ready.OwnedEquipmentIds);
        Assert.DoesNotContain(ready.EquippedEquipment, item => item.EquipmentId == EquipmentCatalog.ToilboundRelicId);

        var forgedSnapshot = JsonSerializer.Serialize(ready.ToSaveData());
        var repeated = RelicForgeSystem.Forge(ready, PurposefulHuntContent.FirstRelicContract);
        Assert.Equal(ForgeStatus.AlreadyForged, repeated.Status);
        Assert.False(repeated.SaveRequested);
        Assert.Equal(forgedSnapshot, JsonSerializer.Serialize(ready.ToSaveData()));

        var conflict = CreateReadyPlayer();
        Assert.True(conflict.GrantEquipment(EquipmentCatalog.ToilboundRelicId));
        var conflictSnapshot = JsonSerializer.Serialize(conflict.ToSaveData());
        var rejected = RelicForgeSystem.Forge(conflict, PurposefulHuntContent.FirstRelicContract);
        Assert.Equal(ForgeStatus.Rejected, rejected.Status);
        Assert.False(rejected.SaveRequested);
        Assert.Equal(conflictSnapshot, JsonSerializer.Serialize(conflict.ToSaveData()));
    }

    [Fact]
    public void ProjectState_CanonicalizesEveryAcquisitionOrder()
    {
        var migrationVectors = PurposefulHuntContractFixture.Load().Migrations;
        Assert.Equal(
            new[] { "legacy-empty-project", "current-ready", "legacy-project-mixed" },
            migrationVectors.Select(vector => vector.Id));
        Assert.Equal(new[] { "Loaded", "Loaded", "Unreadable" }, migrationVectors.Select(vector => vector.ExpectedStatus));
        var ids = PurposefulHuntContent.FirstRelicContract.Quarries
            .Select(quarry => quarry.ContributionId)
            .ToArray();

        foreach (var order in Permutations(ids))
        {
            var state = new RelicProjectState();
            foreach (var id in order)
            {
                Assert.True(state.TryAddContribution(id));
            }

            Assert.Equal(ids, state.CompletedContributionIds);
            Assert.True(state.IsReady);
            Assert.False(state.IsForged);
        }
    }

    [Fact]
    public void SaveAndLoad_CurrentV1_RoundTripsReadyAndForgedStates()
    {
        using var readyFixture = new ProjectSaveFixture();
        var ready = CreateReadyPlayer();
        Assert.True(readyFixture.System.Save(ready).Succeeded);
        using (var document = JsonDocument.Parse(File.ReadAllText(readyFixture.SavePath)))
        {
            Assert.Equal(1, document.RootElement.GetProperty("SchemaVersion").GetInt32());
            Assert.False(document.RootElement.GetProperty("RelicProject").GetProperty("Forged").GetBoolean());
        }
        var loadedReady = readyFixture.System.Load().Player!;
        Assert.True(loadedReady.RelicProject.IsReady);
        Assert.False(loadedReady.RelicProject.IsForged);

        using var forgedFixture = new ProjectSaveFixture();
        var forged = CreateReadyPlayer();
        Assert.True(forged.GrantEquipment(EquipmentCatalog.ToilboundRelicId));
        Assert.True(forged.RelicProject.TryMarkForged());
        Assert.True(forged.Equip(EquipmentSlot.Necklace, EquipmentCatalog.ToilboundRelicId));
        Assert.True(forgedFixture.System.Save(forged).Succeeded);
        var loadedForged = forgedFixture.System.Load().Player!;
        Assert.True(loadedForged.RelicProject.IsForged);
        Assert.True(loadedForged.TryGetEquippedEquipment(EquipmentSlot.Necklace, out var relic));
        Assert.Equal(EquipmentCatalog.ToilboundRelicId, relic.Id);
    }

    [Fact]
    public void Load_VersionlessLegacy_DefaultsEmptyProjectWithoutWritingBytes()
    {
        using var fixture = new ProjectSaveFixture();
        const string legacy = "{\"Name\":\"Legacy\",\"MaxHp\":30,\"Hp\":18,\"Level\":2,\"Experience\":3,\"TreasureCount\":0,\"Inventory\":{}}";
        File.WriteAllText(fixture.SavePath, legacy);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Loaded, result.Status);
        Assert.Empty(result.Player!.RelicProject.CompletedContributionIds);
        Assert.False(result.Player.RelicProject.IsForged);
        Assert.Equal(legacy, File.ReadAllText(fixture.SavePath));
    }

    [Theory]
    [InlineData("missing-project")]
    [InlineData("null-project")]
    [InlineData("wrong-kind-project")]
    [InlineData("missing-contributions")]
    [InlineData("null-contributions")]
    [InlineData("wrong-kind-contributions")]
    [InlineData("missing-forged")]
    [InlineData("null-forged")]
    [InlineData("wrong-kind-forged")]
    [InlineData("unknown-contribution")]
    [InlineData("duplicate-contribution")]
    [InlineData("out-of-order")]
    [InlineData("incomplete-forged")]
    [InlineData("relic-owned-before-forged")]
    [InlineData("forged-without-relic")]
    [InlineData("relic-equipped-without-ownership")]
    [InlineData("forged-relic-in-wrong-slot")]
    [InlineData("negative-version")]
    [InlineData("future-version")]
    [InlineData("legacy-carries-project")]
    public void Load_InvalidProjectEnvelope_IsUnreadableWithoutChangingBytes(string invalidCase)
    {
        using var fixture = new ProjectSaveFixture();
        Assert.True(fixture.System.Save(new Player("Validator")).Succeeded);
        var root = JsonNode.Parse(File.ReadAllText(fixture.SavePath))!.AsObject();
        switch (invalidCase)
        {
            case "missing-project":
                root.Remove("RelicProject");
                break;
            case "null-project":
                root["RelicProject"] = null;
                break;
            case "wrong-kind-project":
                root["RelicProject"] = new JsonArray();
                break;
            case "missing-contributions":
                root["RelicProject"]!.AsObject().Remove("CompletedContributionIds");
                break;
            case "null-contributions":
                root["RelicProject"]!["CompletedContributionIds"] = null;
                break;
            case "wrong-kind-contributions":
                root["RelicProject"]!["CompletedContributionIds"] = new JsonObject();
                break;
            case "missing-forged":
                root["RelicProject"]!.AsObject().Remove("Forged");
                break;
            case "null-forged":
                root["RelicProject"]!["Forged"] = null;
                break;
            case "wrong-kind-forged":
                root["RelicProject"]!["Forged"] = "false";
                break;
            case "unknown-contribution":
                root["RelicProject"]!["CompletedContributionIds"] = new JsonArray("unknown");
                break;
            case "duplicate-contribution":
                root["RelicProject"]!["CompletedContributionIds"] = new JsonArray("chitin-shard", "chitin-shard");
                break;
            case "out-of-order":
                root["RelicProject"]!["CompletedContributionIds"] = new JsonArray("wraith-ash", "chitin-shard");
                break;
            case "incomplete-forged":
                root["RelicProject"]!["Forged"] = true;
                break;
            case "relic-owned-before-forged":
                root["OwnedEquipmentIds"]!.AsArray().Add(EquipmentCatalog.ToilboundRelicId);
                break;
            case "forged-without-relic":
                SetReadyProject(root, forged: true);
                break;
            case "relic-equipped-without-ownership":
                AddRelicEquipment(root, EquipmentSlot.Necklace, ownRelic: false);
                break;
            case "forged-relic-in-wrong-slot":
                SetReadyProject(root, forged: true);
                AddRelicEquipment(root, EquipmentSlot.Ring1, ownRelic: true);
                break;
            case "negative-version":
                root["SchemaVersion"] = -1;
                break;
            case "future-version":
                root["SchemaVersion"] = SaveSystem.CurrentSchemaVersion + 1;
                break;
            case "legacy-carries-project":
                root.Remove("SchemaVersion");
                break;
        }

        var original = root.ToJsonString();
        File.WriteAllText(fixture.SavePath, original);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Unreadable, result.Status);
        Assert.Null(result.Player);
        Assert.Equal(original, File.ReadAllText(fixture.SavePath));
    }

    private static void SetReadyProject(JsonObject root, bool forged)
    {
        root["RelicProject"]!["CompletedContributionIds"] =
            new JsonArray("chitin-shard", "rustheart-core", "wraith-ash");
        root["RelicProject"]!["Forged"] = forged;
    }

    private static void AddRelicEquipment(JsonObject root, EquipmentSlot slot, bool ownRelic)
    {
        if (ownRelic)
        {
            root["OwnedEquipmentIds"]!.AsArray().Add(EquipmentCatalog.ToilboundRelicId);
        }

        root["EquippedEquipment"]!.AsArray().Add(new JsonObject
        {
            ["Slot"] = (int)slot,
            ["EquipmentId"] = EquipmentCatalog.ToilboundRelicId
        });
    }

    private static Player CreateReadyPlayer()
    {
        var player = new Player("Smith");
        foreach (var quarry in PurposefulHuntContent.FirstRelicContract.Quarries.Reverse())
        {
            Assert.True(player.RelicProject.TryAddContribution(quarry.ContributionId));
        }

        return player;
    }

    private static IEnumerable<string[]> Permutations(string[] ids)
    {
        foreach (var first in ids)
        foreach (var second in ids.Where(id => id != first))
        foreach (var third in ids.Where(id => id != first && id != second))
        {
            yield return new[] { first, second, third };
        }
    }

    private sealed class ProjectSaveFixture : IDisposable
    {
        private readonly string directory = Directory.CreateTempSubdirectory("toil-relic-project-").FullName;

        public ProjectSaveFixture()
        {
            SavePath = Path.Combine(directory, "savegame.json");
            System = new SaveSystem(SavePath);
        }

        public string SavePath { get; }
        public SaveSystem System { get; }

        public void Dispose() => Directory.Delete(directory, recursive: true);
    }
}
