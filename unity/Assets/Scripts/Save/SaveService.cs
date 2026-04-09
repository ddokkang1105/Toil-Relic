using System;
using System.IO;
using ToilRelic.Unity.Core;
using UnityEngine;

namespace ToilRelic.Unity.Save
{
    [Serializable]
    internal sealed class SaveEnvelope
    {
        public PlayerState player;
    }

    public static class SaveService
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "toil_relic_save.json");

        public static void Save(PlayerState player)
        {
            var envelope = new SaveEnvelope { player = player };
            var json = JsonUtility.ToJson(envelope, prettyPrint: false);
            File.WriteAllText(SavePath, json);
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
