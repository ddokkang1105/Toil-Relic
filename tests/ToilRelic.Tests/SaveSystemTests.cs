using System.Text.Json;
using System.Text.Json.Nodes;
using ToilRelic.Models;
using ToilRelic.Systems;

namespace ToilRelic.Tests;

public sealed class SaveSystemTests
{
    public enum InvalidCoreValueCase
    {
        NonPositiveMaxHp,
        NegativeHp,
        HpAboveMaxHp,
        NonPositiveLevel,
        NegativeExperience,
        ExperienceAtLevelThreshold,
        NegativeTreasureCount,
        UndefinedItemType,
        NegativeInventoryAmount
    }

    [Fact]
    public void Load_MissingPath_ReturnsMissingWithoutPlayerOrDiagnostic()
    {
        using var fixture = new SaveFixture();

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Missing, result.Status);
        Assert.Null(result.Player);
        Assert.Null(result.Diagnostic);
    }

    [Fact]
    public void SaveThenLoad_CurrentFormat_PreservesRepresentativePlayerValues()
    {
        using var fixture = new SaveFixture();
        var player = new Player("Archivist");
        player.AddItem(ItemType.Junk, 3);
        player.GainExperience(20);

        var saveResult = fixture.System.Save(player);
        var loadResult = fixture.System.Load();

        Assert.True(saveResult.Succeeded);
        Assert.Null(saveResult.Diagnostic);
        Assert.Equal(LoadStatus.Loaded, loadResult.Status);
        Assert.NotNull(loadResult.Player);
        Assert.Equal("Archivist", loadResult.Player.Name);
        Assert.Equal(2, loadResult.Player.Level);
        Assert.Equal(3, loadResult.Player.Inventory[ItemType.Junk]);
    }

    [Fact]
    public void Load_LegacyFormatWithoutEquipmentFields_ReturnsLoaded()
    {
        using var fixture = new SaveFixture();
        var legacy = """
            {
              "Name": "Legacy Wanderer",
              "MaxHp": 30,
              "Hp": 18,
              "Level": 2,
              "Experience": 3,
              "TreasureCount": 0,
              "Inventory": {}
            }
            """;
        File.WriteAllText(fixture.SavePath, legacy);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Player);
        Assert.Equal("Legacy Wanderer", result.Player.Name);
        Assert.Equal(2, result.Player.Level);
    }

    [Fact]
    public void Load_MalformedJson_ReturnsUnreadableWithoutChangingBytes()
    {
        using var fixture = new SaveFixture();
        var original = "{ not valid json";
        File.WriteAllText(fixture.SavePath, original);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Unreadable, result.Status);
        Assert.Null(result.Player);
        Assert.False(string.IsNullOrWhiteSpace(result.Diagnostic));
        Assert.Equal(original, File.ReadAllText(fixture.SavePath));
    }

    [Fact]
    public void Load_EmptyJsonObject_ReturnsUnreadableWithoutChangingBytes()
    {
        using var fixture = new SaveFixture();
        var original = "{}";
        File.WriteAllText(fixture.SavePath, original);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Unreadable, result.Status);
        Assert.Null(result.Player);
        Assert.False(string.IsNullOrWhiteSpace(result.Diagnostic));
        Assert.Equal(original, File.ReadAllText(fixture.SavePath));
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("MaxHp")]
    [InlineData("Hp")]
    [InlineData("Level")]
    [InlineData("Experience")]
    [InlineData("TreasureCount")]
    [InlineData("Inventory")]
    public void Load_MissingRequiredCoreField_ReturnsUnreadableWithoutChangingBytes(string fieldName)
    {
        using var fixture = new SaveFixture();
        var save = CreateValidSaveJson();
        save.Remove(fieldName);
        var original = save.ToJsonString();
        File.WriteAllText(fixture.SavePath, original);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Unreadable, result.Status);
        Assert.Null(result.Player);
        Assert.Equal(original, File.ReadAllText(fixture.SavePath));
    }

    [Theory]
    [InlineData("Name", "42")]
    [InlineData("MaxHp", "\"100\"")]
    [InlineData("Hp", "\"50\"")]
    [InlineData("Level", "\"2\"")]
    [InlineData("Experience", "\"3\"")]
    [InlineData("TreasureCount", "\"0\"")]
    [InlineData("Inventory", "[]")]
    public void Load_RequiredCoreFieldWithWrongKind_ReturnsUnreadableWithoutChangingBytes(
        string fieldName,
        string replacementJson)
    {
        using var fixture = new SaveFixture();
        var save = CreateValidSaveJson();
        save[fieldName] = JsonNode.Parse(replacementJson);
        var original = save.ToJsonString();
        File.WriteAllText(fixture.SavePath, original);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Unreadable, result.Status);
        Assert.Null(result.Player);
        Assert.Equal(original, File.ReadAllText(fixture.SavePath));
    }

    [Theory]
    [InlineData(InvalidCoreValueCase.NonPositiveMaxHp)]
    [InlineData(InvalidCoreValueCase.NegativeHp)]
    [InlineData(InvalidCoreValueCase.HpAboveMaxHp)]
    [InlineData(InvalidCoreValueCase.NonPositiveLevel)]
    [InlineData(InvalidCoreValueCase.NegativeExperience)]
    [InlineData(InvalidCoreValueCase.ExperienceAtLevelThreshold)]
    [InlineData(InvalidCoreValueCase.NegativeTreasureCount)]
    [InlineData(InvalidCoreValueCase.UndefinedItemType)]
    [InlineData(InvalidCoreValueCase.NegativeInventoryAmount)]
    public void Load_ImpossibleCoreValue_ReturnsUnreadableWithoutChangingBytes(InvalidCoreValueCase invalidCase)
    {
        using var fixture = new SaveFixture();
        var save = CreateValidSaveJson();
        ApplyInvalidValue(save, invalidCase);
        var original = save.ToJsonString();
        File.WriteAllText(fixture.SavePath, original);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Unreadable, result.Status);
        Assert.Null(result.Player);
        Assert.False(string.IsNullOrWhiteSpace(result.Diagnostic));
        Assert.Equal(original, File.ReadAllText(fixture.SavePath));
    }

    [Fact]
    public void Load_LockedFile_ReturnsUnreadableWithoutChangingBytes()
    {
        using var fixture = new SaveFixture();
        var original = "locked save bytes";
        File.WriteAllText(fixture.SavePath, original);
        using var lockStream = new FileStream(fixture.SavePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var result = fixture.System.Load();

        Assert.Equal(LoadStatus.Unreadable, result.Status);
        Assert.Null(result.Player);
        Assert.False(string.IsNullOrWhiteSpace(result.Diagnostic));
        lockStream.Position = 0;
        using var reader = new StreamReader(lockStream, leaveOpen: true);
        Assert.Equal(original, reader.ReadToEnd());
    }

    [Fact]
    public void Save_BlockedParent_ReturnsFailureWithDeveloperDiagnostic()
    {
        using var fixture = new SaveFixture();
        var blockedParent = Path.Combine(fixture.DirectoryPath, "blocked");
        File.WriteAllText(blockedParent, "not a directory");
        var system = new SaveSystem(Path.Combine(blockedParent, "savegame.json"));

        var result = system.Save(new Player("Wanderer"));

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Diagnostic));
        Assert.DoesNotContain("Save: Failed", result.Diagnostic);
        Assert.DoesNotContain("Progress may not be saved", result.Diagnostic);
    }

    [Fact]
    public void Delete_MissingAndExistingPaths_ReturnSuccess()
    {
        using var fixture = new SaveFixture();

        var missingResult = fixture.System.Delete();
        File.WriteAllText(fixture.SavePath, "save bytes");
        var existingResult = fixture.System.Delete();

        Assert.True(missingResult.Succeeded);
        Assert.Null(missingResult.Diagnostic);
        Assert.True(existingResult.Succeeded);
        Assert.Null(existingResult.Diagnostic);
        Assert.False(File.Exists(fixture.SavePath));
    }

    [Fact]
    public void Delete_DirectoryPath_ReturnsFailureWithoutRemovingDirectory()
    {
        using var fixture = new SaveFixture();
        var system = new SaveSystem(fixture.DirectoryPath);

        var result = system.Delete();

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Diagnostic));
        Assert.True(Directory.Exists(fixture.DirectoryPath));
    }

    [Fact]
    public void Save_CurrentFormat_KeepsExistingJsonShape()
    {
        using var fixture = new SaveFixture();

        var result = fixture.System.Save(new Player("Wanderer"));
        using var document = JsonDocument.Parse(File.ReadAllText(fixture.SavePath));
        var propertyNames = document.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(result.Succeeded);
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Name", "MaxHp", "Hp", "Level", "Experience", "TreasureCount",
                "Inventory", "OwnedEquipmentIds", "EquippedWeaponId",
                "EquippedEquipment", "EquipmentInitialized"
            },
            propertyNames);
    }

    private static JsonObject CreateValidSaveJson() => new()
    {
        ["Name"] = "Wanderer",
        ["MaxHp"] = 100,
        ["Hp"] = 50,
        ["Level"] = 2,
        ["Experience"] = 3,
        ["TreasureCount"] = 0,
        ["Inventory"] = new JsonObject()
    };

    private static void ApplyInvalidValue(JsonObject save, InvalidCoreValueCase invalidCase)
    {
        switch (invalidCase)
        {
            case InvalidCoreValueCase.NonPositiveMaxHp:
                save["MaxHp"] = 0;
                save["Hp"] = 0;
                break;
            case InvalidCoreValueCase.NegativeHp:
                save["Hp"] = -1;
                break;
            case InvalidCoreValueCase.HpAboveMaxHp:
                save["Hp"] = 101;
                break;
            case InvalidCoreValueCase.NonPositiveLevel:
                save["Level"] = 0;
                break;
            case InvalidCoreValueCase.NegativeExperience:
                save["Experience"] = -1;
                break;
            case InvalidCoreValueCase.ExperienceAtLevelThreshold:
                save["Level"] = 1;
                save["Experience"] = 20;
                break;
            case InvalidCoreValueCase.NegativeTreasureCount:
                save["TreasureCount"] = -1;
                break;
            case InvalidCoreValueCase.UndefinedItemType:
                save["Inventory"] = new JsonObject { ["999"] = 1 };
                break;
            case InvalidCoreValueCase.NegativeInventoryAmount:
                save["Inventory"] = new JsonObject { [nameof(ItemType.Junk)] = -1 };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidCase), invalidCase, null);
        }
    }

    private sealed class SaveFixture : IDisposable
    {
        public SaveFixture()
        {
            DirectoryPath = Directory.CreateTempSubdirectory("toil-relic-tests-").FullName;
            SavePath = Path.Combine(DirectoryPath, "savegame.json");
            System = new SaveSystem(SavePath);
        }

        public string DirectoryPath { get; }
        public string SavePath { get; }
        public SaveSystem System { get; }

        public void Dispose()
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
