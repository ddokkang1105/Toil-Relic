using System;
using System.IO;
using ToilRelic.Unity.Core;
using UnityEngine;

namespace ToilRelic.Unity.Save
{
    [Serializable]
    internal sealed class SaveEnvelope
    {
        public int version = SaveService.CurrentSaveVersion;
        public PlayerState player;
    }

    public static class SaveService
    {
        public const int CurrentSaveVersion = 2;
        private static string SavePath => Path.Combine(Application.persistentDataPath, "toil_relic_save.json");
        // Play Mode tests can redirect only a save write to a guaranteed-invalid path.
        private static string saveWritePathOverride;

        public static bool HasSaveFile() => File.Exists(SavePath);

        public static bool TryDelete(out string error)
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TrySave(PlayerState player, out string error)
        {
            try
            {
                var envelope = new SaveEnvelope { player = player };
                var json = JsonUtility.ToJson(envelope, prettyPrint: false);
                File.WriteAllText(saveWritePathOverride ?? SavePath, json);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TryLoad(out PlayerState player)
        {
            player = null;
            if (!File.Exists(SavePath))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                if (envelope == null || envelope.player == null)
                {
                    return false;
                }

                player = envelope.player;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
