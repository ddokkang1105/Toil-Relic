using System;
using System.IO;
using UnityEngine;

namespace ToilRelic.PlayModeTests
{
    internal static class PurposefulHuntContractFixture
    {
        public static PurposefulHuntFixture Load()
        {
            var path = Path.Combine(Application.dataPath, "Tests", "Fixtures", "PurposefulHuntContracts.json");
            return JsonUtility.FromJson<PurposefulHuntFixture>(File.ReadAllText(path));
        }
    }

    [Serializable]
    internal sealed class PurposefulHuntFixture
    {
        public int schemaVersion;
        public HuntContentFixture content;
        public HuntMigrationFixture[] migrations;
    }

    [Serializable]
    internal sealed class HuntMigrationFixture
    {
        public string id;
        public int consoleVersion;
        public int unityVersion;
        public string expectedStatus;
        public string expectedProjectState;
    }

    [Serializable]
    internal sealed class HuntContentFixture
    {
        public string projectId;
        public string relicEquipmentId;
        public HuntQuarryFixture[] quarries;
        public HuntProfileFixture[] profiles;
        public HuntEquipmentFixture[] equipment;
    }

    [Serializable]
    internal sealed class HuntQuarryFixture
    {
        public string id;
        public string enemyId;
        public string danger;
        public string contributionId;
        public string contributionName;
        public string profileId;
    }

    [Serializable]
    internal sealed class HuntProfileFixture
    {
        public string id;
        public string equipmentId;
        public float chance;
    }

    [Serializable]
    internal sealed class HuntEquipmentFixture
    {
        public string id;
        public string displayName;
        public string category;
        public int attack;
        public int defense;
        public int damageReduction;
        public int maxHp;
    }
}
