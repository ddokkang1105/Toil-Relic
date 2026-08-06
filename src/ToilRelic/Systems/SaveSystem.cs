using System.Text.Json;
using ToilRelic.Models;

namespace ToilRelic.Systems;

public sealed class SaveSystem
{
    public const int CurrentSchemaVersion = 1;
    private const string SaveFileName = "savegame.json";
    private readonly string _savePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SaveSystem(string? savePath = null)
    {
        _savePath = savePath ?? Path.Combine(Directory.GetCurrentDirectory(), SaveFileName);
    }

    public PersistenceResult Save(Player player)
    {
        try
        {
            var directory = Path.GetDirectoryName(_savePath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(player.ToSaveData(), _jsonOptions);
            var tempPath = $"{_savePath}.tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _savePath, true);
            return PersistenceResult.Success();
        }
        catch (Exception ex)
        {
            return PersistenceResult.Failure(ex.ToString());
        }
    }

    public LoadResult Load()
    {
        try
        {
            var json = File.ReadAllText(_savePath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var isCurrent = root.TryGetProperty(nameof(PlayerSaveData.SchemaVersion), out var versionElement);
            if (isCurrent)
            {
                if (versionElement.ValueKind != JsonValueKind.Number ||
                    !versionElement.TryGetInt32(out var version) ||
                    version != CurrentSchemaVersion ||
                    !HasCurrentSaveShape(root))
                {
                    return LoadResult.Unreadable("The save file does not match the current player save format.");
                }
            }
            else if (root.TryGetProperty(nameof(PlayerSaveData.RelicProject), out _) || !HasCoreSaveShape(root))
            {
                return LoadResult.Unreadable("The save file does not match the supported legacy player save format.");
            }

            var saveData = root.Deserialize<PlayerSaveData>(_jsonOptions)!;
            if (!HasValidCoreValues(saveData) ||
                isCurrent && !HasValidCurrentState(saveData) ||
                !isCurrent && ContainsRelicState(saveData))
            {
                return LoadResult.Unreadable("The save file contains unsupported player values.");
            }

            return LoadResult.Loaded(Player.FromSaveData(saveData));
        }
        catch (FileNotFoundException)
        {
            return LoadResult.Missing();
        }
        catch (DirectoryNotFoundException)
        {
            return LoadResult.Missing();
        }
        catch (Exception ex)
        {
            return LoadResult.Unreadable(ex.ToString());
        }
    }

    private static bool HasCurrentSaveShape(JsonElement root) =>
        HasCoreSaveShape(root) &&
        HasStringArray(root, nameof(PlayerSaveData.OwnedEquipmentIds)) &&
        HasEquippedEquipmentArray(root, nameof(PlayerSaveData.EquippedEquipment)) &&
        HasProperty(root, nameof(PlayerSaveData.EquipmentInitialized), JsonValueKind.True, JsonValueKind.False) &&
        root.TryGetProperty(nameof(PlayerSaveData.RelicProject), out var project) &&
        project.ValueKind == JsonValueKind.Object &&
        HasStringArray(project, nameof(RelicProjectSaveData.CompletedContributionIds)) &&
        HasProperty(project, nameof(RelicProjectSaveData.Forged), JsonValueKind.True, JsonValueKind.False);

    private static bool HasCoreSaveShape(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object &&
        HasProperty(root, nameof(PlayerSaveData.Name), JsonValueKind.String) &&
        HasProperty(root, nameof(PlayerSaveData.MaxHp), JsonValueKind.Number) &&
        HasProperty(root, nameof(PlayerSaveData.Hp), JsonValueKind.Number) &&
        HasProperty(root, nameof(PlayerSaveData.Level), JsonValueKind.Number) &&
        HasProperty(root, nameof(PlayerSaveData.Experience), JsonValueKind.Number) &&
        HasProperty(root, nameof(PlayerSaveData.TreasureCount), JsonValueKind.Number) &&
        HasProperty(root, nameof(PlayerSaveData.Inventory), JsonValueKind.Object);

    private static bool HasProperty(JsonElement root, string name, params JsonValueKind[] expectedKinds) =>
        root.TryGetProperty(name, out var property) && expectedKinds.Contains(property.ValueKind);

    private static bool HasStringArray(JsonElement root, string name) =>
        root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Array &&
        property.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String);

    private static bool HasEquippedEquipmentArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Array) return false;
        return property.EnumerateArray().All(item =>
            item.ValueKind == JsonValueKind.Object &&
            HasProperty(item, nameof(EquippedEquipmentEntry.Slot), JsonValueKind.Number) &&
            HasProperty(item, nameof(EquippedEquipmentEntry.EquipmentId), JsonValueKind.String));
    }

    private static bool HasValidCoreValues(PlayerSaveData saveData) =>
        saveData.MaxHp > 0 && saveData.Hp >= 0 && saveData.Hp <= saveData.MaxHp &&
        saveData.Level > 0 && saveData.Experience >= 0 &&
        saveData.Experience < Player.RequiredExperience(saveData.Level) &&
        saveData.TreasureCount >= 0 && saveData.Inventory is not null &&
        saveData.Inventory.All(pair => Enum.IsDefined(pair.Key) && pair.Value >= 0);

    private static bool HasValidCurrentState(PlayerSaveData saveData)
    {
        if (saveData.SchemaVersion != CurrentSchemaVersion || !saveData.EquipmentInitialized ||
            saveData.OwnedEquipmentIds is null || saveData.EquippedEquipment is null || saveData.RelicProject is null)
        {
            return false;
        }

        var owned = new HashSet<string>(StringComparer.Ordinal);
        if (saveData.OwnedEquipmentIds.Any(id =>
                string.IsNullOrWhiteSpace(id) || !owned.Add(id) || !EquipmentCatalog.TryGet(id, out _)))
        {
            return false;
        }

        var equippedIds = new HashSet<string>(StringComparer.Ordinal);
        var equippedSlots = new HashSet<EquipmentSlot>();
        foreach (var entry in saveData.EquippedEquipment)
        {
            if (entry is null || !Enum.IsDefined(entry.Slot) || string.IsNullOrWhiteSpace(entry.EquipmentId) ||
                !owned.Contains(entry.EquipmentId) || !EquipmentCatalog.TryGet(entry.EquipmentId, out var equipment) ||
                !equipment.CanEquipTo(entry.Slot) || !equippedIds.Add(entry.EquipmentId) || !equippedSlots.Add(entry.Slot))
            {
                return false;
            }
        }

        if (!equippedSlots.Contains(EquipmentSlot.PrimaryWeapon)) return false;
        return RelicProjectState.HasValidSaveState(saveData.RelicProject, saveData.OwnedEquipmentIds, saveData.EquippedEquipment);
    }

    private static bool ContainsRelicState(PlayerSaveData saveData) =>
        saveData.OwnedEquipmentIds?.Contains(EquipmentCatalog.ToilboundRelicId, StringComparer.Ordinal) == true ||
        saveData.EquippedEquipment?.Any(entry =>
            string.Equals(entry.EquipmentId, EquipmentCatalog.ToilboundRelicId, StringComparison.Ordinal)) == true;

    public PersistenceResult Delete()
    {
        try
        {
            File.Delete(_savePath);
            return PersistenceResult.Success();
        }
        catch (Exception ex)
        {
            return PersistenceResult.Failure(ex.ToString());
        }
    }
}
