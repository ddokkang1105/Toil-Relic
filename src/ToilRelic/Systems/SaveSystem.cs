using System.Text.Json;
using ToilRelic.Models;

namespace ToilRelic.Systems;

public sealed class SaveSystem
{
    private const string SaveFileName = "savegame.json";
    private readonly string _savePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public SaveSystem(string? savePath = null)
    {
        _savePath = savePath ?? Path.Combine(Directory.GetCurrentDirectory(), SaveFileName);
    }

    public PersistenceResult Save(Player player)
    {
        try
        {
            var directory = Path.GetDirectoryName(_savePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

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
            if (!HasCoreSaveShape(document.RootElement))
            {
                return LoadResult.Unreadable("The save file does not match the supported player save format.");
            }

            var saveData = document.RootElement.Deserialize<PlayerSaveData>(_jsonOptions)!;
            if (!HasValidCoreValues(saveData))
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

    private static bool HasCoreSaveShape(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object &&
        HasProperty(root, "Name", JsonValueKind.String) &&
        HasProperty(root, "MaxHp", JsonValueKind.Number) &&
        HasProperty(root, "Hp", JsonValueKind.Number) &&
        HasProperty(root, "Level", JsonValueKind.Number) &&
        HasProperty(root, "Experience", JsonValueKind.Number) &&
        HasProperty(root, "TreasureCount", JsonValueKind.Number) &&
        HasProperty(root, "Inventory", JsonValueKind.Object);

    private static bool HasProperty(JsonElement root, string name, JsonValueKind expectedKind) =>
        root.TryGetProperty(name, out var property) && property.ValueKind == expectedKind;

    private static bool HasValidCoreValues(PlayerSaveData saveData) =>
        saveData.MaxHp > 0 &&
        saveData.Hp >= 0 &&
        saveData.Hp <= saveData.MaxHp &&
        saveData.Level > 0 &&
        saveData.Experience >= 0 &&
        saveData.TreasureCount >= 0 &&
        saveData.Inventory.All(pair => Enum.IsDefined(pair.Key) && pair.Value >= 0);

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
