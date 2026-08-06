using System.Text.Json;

namespace ToilRelic.Tests;

internal static class PurposefulHuntContractFixture
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static PurposefulHuntFixture Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "PurposefulHuntContracts.json");
        return JsonSerializer.Deserialize<PurposefulHuntFixture>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("Purposeful Hunt fixture is empty.");
    }
}

internal sealed class PurposefulHuntFixture
{
    public int SchemaVersion { get; set; }
    public HuntContentFixture Content { get; set; } = new();
}

internal sealed class HuntContentFixture
{
    public string ProjectId { get; set; } = string.Empty;
    public string RelicEquipmentId { get; set; } = string.Empty;
    public HuntQuarryFixture[] Quarries { get; set; } = Array.Empty<HuntQuarryFixture>();
    public HuntProfileFixture[] Profiles { get; set; } = Array.Empty<HuntProfileFixture>();
    public HuntEquipmentFixture[] Equipment { get; set; } = Array.Empty<HuntEquipmentFixture>();
}

internal sealed class HuntQuarryFixture
{
    public string Id { get; set; } = string.Empty;
    public string EnemyId { get; set; } = string.Empty;
    public string Danger { get; set; } = string.Empty;
    public string ContributionId { get; set; } = string.Empty;
    public string ContributionName { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
}

internal sealed class HuntProfileFixture
{
    public string Id { get; set; } = string.Empty;
    public string EquipmentId { get; set; } = string.Empty;
    public double Chance { get; set; }
}

internal sealed class HuntEquipmentFixture
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int DamageReduction { get; set; }
    public int MaxHp { get; set; }
}
