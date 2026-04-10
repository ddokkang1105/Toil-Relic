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

    public bool TrySave(Player player, out string message)
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

            message = $"저장 완료: {_savePath}";
            return true;
        }
        catch (Exception ex)
        {
            message = $"저장 중 오류: {ex.Message}";
            return false;
        }
    }

    public bool TryLoad(out Player player, out string message)
    {
        player = new Player("노역자");

        try
        {
            if (!File.Exists(_savePath))
            {
                message = "저장 파일이 없습니다.";
                return false;
            }

            var json = File.ReadAllText(_savePath);
            var saveData = JsonSerializer.Deserialize<PlayerSaveData>(json);
            if (saveData is null)
            {
                message = "저장 파일 파싱에 실패했습니다.";
                return false;
            }

            player = Player.FromSaveData(saveData);
            message = $"불러오기 완료: {_savePath}";
            return true;
        }
        catch (Exception ex)
        {
            message = $"불러오기 중 오류: {ex.Message}";
            return false;
        }
    }
}
