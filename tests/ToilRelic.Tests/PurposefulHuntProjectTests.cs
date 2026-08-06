using System.Text.Json;
using System.Text.Json.Nodes;
using ToilRelic.Models;
using ToilRelic.Systems;

namespace ToilRelic.Tests;

[Collection(ConsoleCollection.Name)]
public sealed class PurposefulHuntProjectTests
{
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
    [InlineData("duplicate-contribution")]
    [InlineData("out-of-order")]
    [InlineData("relic-owned-before-forged")]
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
            case "duplicate-contribution":
                root["RelicProject"]!["CompletedContributionIds"] = new JsonArray("chitin-shard", "chitin-shard");
                break;
            case "out-of-order":
                root["RelicProject"]!["CompletedContributionIds"] = new JsonArray("wraith-ash", "chitin-shard");
                break;
            case "relic-owned-before-forged":
                root["OwnedEquipmentIds"]!.AsArray().Add(EquipmentCatalog.ToilboundRelicId);
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
