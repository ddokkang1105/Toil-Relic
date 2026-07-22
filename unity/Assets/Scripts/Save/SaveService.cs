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
        private static string savePathOverride;
        private static string SavePath => savePathOverride ?? Path.Combine(Application.persistentDataPath, "toil_relic_save.json");

        public static bool HasSaveFile() => File.Exists(SavePath);

        public static SaveOperationResult Delete()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                return SaveOperationResult.Success();
            }
            catch (Exception exception)
            {
                return SaveOperationResult.Failure(exception.ToString());
            }
        }

        public static SaveOperationResult Save(PlayerState player)
        {
            try
            {
                var envelope = new SaveEnvelope { player = player };
                var json = JsonUtility.ToJson(envelope, prettyPrint: false);
                File.WriteAllText(SavePath, json);
                return SaveOperationResult.Success();
            }
            catch (Exception exception)
            {
                return SaveOperationResult.Failure(exception.ToString());
            }
        }

        public static SaveLoadResult Load()
        {
            if (!File.Exists(SavePath))
            {
                return SaveLoadResult.Missing();
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                if (envelope == null || envelope.player == null)
                {
                    return SaveLoadResult.Unreadable("The save did not contain player data.");
                }

                return SaveLoadResult.Loaded(envelope.player);
            }
            catch (Exception exception)
            {
                return SaveLoadResult.Unreadable(exception.ToString());
            }
        }
    }
}
