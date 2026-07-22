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

    public bool HasSaveFile()
    {
        return File.Exists(_savePath);
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
            if (!File.Exists(_savePath))
            {
                return LoadResult.Missing();
            }

            var json = File.ReadAllText(_savePath);
            var saveData = JsonSerializer.Deserialize<PlayerSaveData>(json);
            return saveData is null
                ? LoadResult.Unreadable("The save file contained no player data.")
                : LoadResult.Loaded(Player.FromSaveData(saveData));
        }
        catch (Exception ex)
        {
            return LoadResult.Unreadable(ex.ToString());
        }
    }

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

    public bool TrySave(Player player, out string message)
    {
        var result = Save(player);
        message = result.Diagnostic ?? string.Empty;
        return result.Succeeded;
    }

    public bool TryLoad(out Player player, out string message)
    {
        var result = Load();
        player = result.Player ?? new Player("Wanderer");
        message = result.Diagnostic ?? string.Empty;
        return result.Status == LoadStatus.Loaded;
    }
}
