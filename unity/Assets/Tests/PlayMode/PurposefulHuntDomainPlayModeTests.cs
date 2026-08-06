using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ToilRelic.PlayModeTests
{
    public sealed class PurposefulHuntDomainPlayModeTests
    {
        private readonly List<UnityEngine.Object> created = new();
        private Type saveServiceType;
        private object previousSavePathOverride;
        private string saveDirectory;
        private string savePath;

        [Test]
        public void PureRewardAndForgeCommands_MatchSharedVectorsAndPreserveRejectedState()
        {
            var fixture = PurposefulHuntContractFixture.Load();
            BuildContentGraph(fixture.content, out var contract, out var enemyDatabase, out var profileDatabase);
            foreach (var vector in fixture.commands)
            {
                var player = CreatePlayer(vector.precompleted);
                var profileEquipmentId = ProfileEquipmentId(vector.quarryId);
                if (vector.preownedProfile)
                {
                    Assert.That(player.GetType().GetMethod("GrantEquipment").Invoke(player, new object[] { profileEquipmentId }), Is.EqualTo(true));
                }

                var before = JsonUtility.ToJson(player);
                var commandType = FindType("ToilRelic.Unity.Systems.QuarryVictoryCommand");
                var command = Activator.CreateInstance(commandType, vector.quarryId, vector.victory, vector.profileRoll,
                    vector.experience, vector.junk, vector.relicPart, vector.healingPotion);
                var result = FindType("ToilRelic.Unity.Systems.QuarryRewardSystem").GetMethod("Resolve")
                    .Invoke(null, new[] { player, contract, enemyDatabase, profileDatabase, command });

                Assert.That(GetProperty(result, "Status").ToString(), Is.EqualTo(vector.expectedStatus), vector.id);
                Assert.That(GetProperty(result, "ProfileResult").ToString(), Is.EqualTo(vector.expectedProfile), vector.id);
                Assert.That(GetProperty(result, "ContributionResult").ToString(), Is.EqualTo(vector.expectedContribution), vector.id);
                Assert.That(GetProperty(result, "ProjectReady"), Is.EqualTo(vector.expectedReady), vector.id);
                Assert.That(GetProperty(result, "SaveRequested"), Is.EqualTo(vector.expectedSaveRequested), vector.id);
                if (!vector.expectedSaveRequested)
                {
                    Assert.That(JsonUtility.ToJson(player), Is.EqualTo(before), vector.id);
                }
            }

            var unready = CreatePlayer(Array.Empty<string>());
            var forgeSystem = FindType("ToilRelic.Unity.Systems.RelicForgeSystem");
            var unreadyBefore = JsonUtility.ToJson(unready);
            var unreadyResult = forgeSystem.GetMethod("Forge").Invoke(null, new[] { unready, contract, enemyDatabase, profileDatabase });
            Assert.That(GetProperty(unreadyResult, "Status").ToString(), Is.EqualTo("NotReady"));
            Assert.That(JsonUtility.ToJson(unready), Is.EqualTo(unreadyBefore));

            var ready = CreatePlayer(new[] { "chitin-shard", "rustheart-core", "wraith-ash" });
            var forged = forgeSystem.GetMethod("Forge").Invoke(null, new[] { ready, contract, enemyDatabase, profileDatabase });
            Assert.That(GetProperty(forged, "Status").ToString(), Is.EqualTo("Forged"));
            Assert.That(GetProperty(forged, "SaveRequested"), Is.EqualTo(true));
            Assert.That(((IEnumerable)GetProperty(ready, "OwnedEquipmentIds")).Cast<string>(), Does.Contain("toilbound-relic"));
            Assert.That(((IEnumerable)GetProperty(ready, "EquippedEquipment")).Cast<object>()
                .Any(item => (string)GetField(item, "equipmentId") == "toilbound-relic"), Is.False);

            var forgedSnapshot = JsonUtility.ToJson(ready);
            var repeated = forgeSystem.GetMethod("Forge").Invoke(null, new[] { ready, contract, enemyDatabase, profileDatabase });
            Assert.That(GetProperty(repeated, "Status").ToString(), Is.EqualTo("AlreadyForged"));
            Assert.That(GetProperty(repeated, "SaveRequested"), Is.EqualTo(false));
            Assert.That(JsonUtility.ToJson(ready), Is.EqualTo(forgedSnapshot));

            var conflict = CreatePlayer(new[] { "chitin-shard", "rustheart-core", "wraith-ash" });
            Assert.That(conflict.GetType().GetMethod("GrantEquipment").Invoke(conflict, new object[] { "toilbound-relic" }), Is.EqualTo(true));
            var conflictSnapshot = JsonUtility.ToJson(conflict);
            var rejected = forgeSystem.GetMethod("Forge").Invoke(null, new[] { conflict, contract, enemyDatabase, profileDatabase });
            Assert.That(GetProperty(rejected, "Status").ToString(), Is.EqualTo("Rejected"));
            Assert.That(JsonUtility.ToJson(conflict), Is.EqualTo(conflictSnapshot));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in created)
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            created.Clear();
            if (saveServiceType != null)
            {
                saveServiceType.GetField("savePathOverride", BindingFlags.Static | BindingFlags.NonPublic)
                    .SetValue(null, previousSavePathOverride);
            }

            if (!string.IsNullOrEmpty(saveDirectory) && Directory.Exists(saveDirectory))
            {
                Directory.Delete(saveDirectory, recursive: true);
            }
        }

        [Test]
        public void RelicProjectState_CanonicalizesContributionOrder()
        {
            var migrationVectors = PurposefulHuntContractFixture.Load().migrations;
            Assert.That(migrationVectors.Select(vector => vector.id),
                Is.EqualTo(new[] { "legacy-empty-project", "current-ready", "legacy-project-mixed" }));
            Assert.That(migrationVectors.Select(vector => vector.expectedStatus),
                Is.EqualTo(new[] { "Loaded", "Loaded", "Unreadable" }));
            var project = Activator.CreateInstance(FindType("ToilRelic.Unity.Core.RelicProjectState"));
            var add = project.GetType().GetMethod("TryAddContribution");
            Assert.That(add.Invoke(project, new object[] { "wraith-ash" }), Is.EqualTo(true));
            Assert.That(add.Invoke(project, new object[] { "chitin-shard" }), Is.EqualTo(true));
            Assert.That(add.Invoke(project, new object[] { "rustheart-core" }), Is.EqualTo(true));

            var completed = ((IEnumerable)GetProperty(project, "CompletedContributionIds")).Cast<string>().ToArray();
            Assert.That(completed, Is.EqualTo(new[] { "chitin-shard", "rustheart-core", "wraith-ash" }));
            Assert.That(GetProperty(project, "IsReady"), Is.EqualTo(true));
            Assert.That(GetProperty(project, "IsForged"), Is.EqualTo(false));
        }

        [Test]
        public void SaveService_V3RoundTripsProjectAndLegacyDefaultsWithoutWriting()
        {
            ConfigureSavePath();
            var player = Activator.CreateInstance(FindType("ToilRelic.Unity.Core.PlayerState"));
            player.GetType().GetMethod("InitDefaults").Invoke(player, null);
            var project = GetProperty(player, "RelicProject");
            var add = project.GetType().GetMethod("TryAddContribution");
            foreach (var id in new[] { "wraith-ash", "chitin-shard", "rustheart-core" })
            {
                Assert.That(add.Invoke(project, new object[] { id }), Is.EqualTo(true));
            }

            var save = saveServiceType.GetMethod("Save").Invoke(null, new[] { player });
            Assert.That(GetProperty(save, "Succeeded"), Is.EqualTo(true));
            var currentJson = File.ReadAllText(savePath);
            Assert.That(currentJson, Does.Contain("\"version\":3"));
            Assert.That(currentJson, Does.Contain("\"relicProject\""));
            var currentLoad = saveServiceType.GetMethod("Load").Invoke(null, null);
            Assert.That(GetProperty(currentLoad, "Status").ToString(), Is.EqualTo("Loaded"),
                GetProperty(currentLoad, "Diagnostic")?.ToString());

            const string legacy = "{\"version\":2,\"player\":{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":[{\"type\":0,\"amount\":2}]}}";
            File.WriteAllText(savePath, legacy);
            var legacyResult = saveServiceType.GetMethod("Load").Invoke(null, null);
            Assert.That(GetProperty(legacyResult, "Status").ToString(), Is.EqualTo("Loaded"));
            var legacyPlayer = GetProperty(legacyResult, "Player");
            var legacyProject = GetProperty(legacyPlayer, "RelicProject");
            Assert.That(((IEnumerable)GetProperty(legacyProject, "CompletedContributionIds")).Cast<object>(), Is.Empty);
            Assert.That(File.ReadAllText(savePath), Is.EqualTo(legacy));
        }

        [Test]
        public void SaveService_RejectsMissingCurrentProjectAndLegacyProjectPayload()
        {
            ConfigureSavePath();
            const string currentMissingProject = "{\"version\":3,\"player\":{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":[{\"type\":0,\"amount\":2}],\"ownedEquipmentIds\":[\"starter-weapon\"],\"equippedEquipment\":[{\"slot\":0,\"equipmentId\":\"starter-weapon\"}],\"equipmentInitialized\":true}}";
            File.WriteAllText(savePath, currentMissingProject);
            Assert.That(GetProperty(saveServiceType.GetMethod("Load").Invoke(null, null), "Status").ToString(), Is.EqualTo("Unreadable"));
            Assert.That(File.ReadAllText(savePath), Is.EqualTo(currentMissingProject));

            const string legacyWithProject = "{\"version\":2,\"player\":{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":[{\"type\":0,\"amount\":2}],\"relicProject\":{\"completedContributionIds\":[],\"forged\":false}}}";
            File.WriteAllText(savePath, legacyWithProject);
            Assert.That(GetProperty(saveServiceType.GetMethod("Load").Invoke(null, null), "Status").ToString(), Is.EqualTo("Unreadable"));
            Assert.That(File.ReadAllText(savePath), Is.EqualTo(legacyWithProject));
        }

        [Test]
        public void ContentVectors_MatchNativeUnityGraphAndRejectForbiddenRewardWeapon()
        {
            var fixture = PurposefulHuntContractFixture.Load().content;
            var enemyDatabase = Create("ToilRelic.Unity.Data.EnemyDatabase");
            var enemyList = CreateList(enemyDatabase, "enemies");
            foreach (var quarry in fixture.quarries)
            {
                var enemy = Create("ToilRelic.Unity.Data.EnemyData");
                SetField(enemy, "id", quarry.enemyId);
                SetField(enemy, "displayName", quarry.id == "mine-vermin" ? "Mine Vermin" :
                    quarry.id == "rust-golem" ? "Rust Golem" : "Ruin Wraith");
                SetField(enemy, "equipmentDropProfileId", quarry.profileId);
                enemyList.Add(enemy);
            }

            var profileDatabase = Create("ToilRelic.Unity.Data.EquipmentDropProfileDatabase");
            var profileList = CreateList(profileDatabase, "profiles");
            foreach (var expected in fixture.profiles)
            {
                var profile = Create("ToilRelic.Unity.Data.EquipmentDropProfileData");
                SetField(profile, "id", expected.id);
                SetField(profile, "equipmentId", expected.equipmentId);
                SetField(profile, "chance", expected.chance);
                profileList.Add(profile);
            }

            var contract = Create("ToilRelic.Unity.Data.HuntContractData");
            SetField(contract, "projectId", fixture.projectId);
            SetField(contract, "displayName", "First Relic Project");
            SetField(contract, "relicEquipmentId", fixture.relicEquipmentId);
            var quarryList = CreateList(contract, "quarries");
            var quarryType = FindType("ToilRelic.Unity.Data.HuntQuarryData");
            var dangerType = FindType("ToilRelic.Unity.Data.HuntDanger");
            foreach (var expected in fixture.quarries)
            {
                var quarry = Activator.CreateInstance(quarryType);
                SetField(quarry, "id", expected.id);
                SetField(quarry, "enemyId", expected.enemyId);
                SetField(quarry, "danger", Enum.Parse(dangerType, expected.danger));
                SetField(quarry, "contributionId", expected.contributionId);
                SetField(quarry, "contributionDisplayName", expected.contributionName);
                SetField(quarry, "profileId", expected.profileId);
                quarryList.Add(quarry);
            }

            var validate = contract.GetType().GetMethod("Validate");
            var result = validate.Invoke(contract, new[] { enemyDatabase, profileDatabase });
            Assert.That(GetProperty(result, "IsAvailable"), Is.EqualTo(true));
            Assert.That(profileList.Cast<object>().Select(profile => (float)GetField(profile, "chance")),
                Is.All.EqualTo(0.35f).Within(0.0001f));

            var catalog = FindType("ToilRelic.Unity.Core.EquipmentCatalog");
            var all = (IEnumerable)catalog.GetProperty("All").GetValue(null);
            Assert.That(all.Cast<object>().Count(), Is.EqualTo(6));
            foreach (var equipment in fixture.equipment)
            {
                var arguments = new object[] { equipment.id, null };
                Assert.That(catalog.GetMethod("TryGet").Invoke(null, arguments), Is.EqualTo(true));
                Assert.That(GetProperty(arguments[1], "DisplayName"), Is.EqualTo(equipment.displayName));
                Assert.That(GetProperty(arguments[1], "Category").ToString(), Is.EqualTo(equipment.category));
            }

            SetField(profileList[0], "equipmentId", "reward-weapon");
            result = validate.Invoke(contract, new[] { enemyDatabase, profileDatabase });
            Assert.That(GetProperty(result, "Issue").ToString(), Is.EqualTo("ForbiddenProfileEquipment"));
        }

        [Test]
        public void EnemyDatabase_UsesStableIdWithoutRandomFallback()
        {
            var enemyDatabase = Create("ToilRelic.Unity.Data.EnemyDatabase");
            var enemyList = CreateList(enemyDatabase, "enemies");
            var enemy = Create("ToilRelic.Unity.Data.EnemyData");
            SetField(enemy, "id", "rust-golem");
            SetField(enemy, "displayName", "Rust Golem");
            SetField(enemy, "equipmentDropProfileId", "profile-rust-golem");
            enemyList.Add(enemy);

            var tryGet = enemyDatabase.GetType().GetMethod("TryGet");
            var found = new object[] { "rust-golem", null };
            Assert.That(tryGet.Invoke(enemyDatabase, found), Is.EqualTo(true));
            Assert.That(GetField(found[1], "displayName"), Is.EqualTo("Rust Golem"));
            var missing = new object[] { "unknown", null };
            Assert.That(tryGet.Invoke(enemyDatabase, missing), Is.EqualTo(false));
            Assert.That(missing[1], Is.Null);
        }

        private ScriptableObject Create(string typeName)
        {
            var instance = ScriptableObject.CreateInstance(FindType(typeName));
            created.Add(instance);
            return instance;
        }

        private object CreatePlayer(IEnumerable<string> contributions)
        {
            var player = Activator.CreateInstance(FindType("ToilRelic.Unity.Core.PlayerState"));
            player.GetType().GetMethod("InitDefaults").Invoke(player, null);
            var project = GetProperty(player, "RelicProject");
            var add = project.GetType().GetMethod("TryAddContribution");
            foreach (var contribution in contributions)
            {
                Assert.That(add.Invoke(project, new object[] { contribution }), Is.EqualTo(true));
            }

            return player;
        }

        private static string ProfileEquipmentId(string quarryId) => quarryId switch
        {
            "mine-vermin" => "vermin-fang",
            "rust-golem" => "rustguard-plate",
            "ruin-wraith" => "wraith-signet",
            _ => throw new ArgumentOutOfRangeException(nameof(quarryId), quarryId, null)
        };

        private void BuildContentGraph(HuntContentFixture fixture, out ScriptableObject contract,
            out ScriptableObject enemyDatabase, out ScriptableObject profileDatabase)
        {
            enemyDatabase = Create("ToilRelic.Unity.Data.EnemyDatabase");
            var enemyList = CreateList(enemyDatabase, "enemies");
            foreach (var expected in fixture.quarries)
            {
                var enemy = Create("ToilRelic.Unity.Data.EnemyData");
                SetField(enemy, "id", expected.enemyId);
                SetField(enemy, "displayName", expected.contributionName);
                SetField(enemy, "equipmentDropProfileId", expected.profileId);
                enemyList.Add(enemy);
            }

            profileDatabase = Create("ToilRelic.Unity.Data.EquipmentDropProfileDatabase");
            var profileList = CreateList(profileDatabase, "profiles");
            foreach (var expected in fixture.profiles)
            {
                var profile = Create("ToilRelic.Unity.Data.EquipmentDropProfileData");
                SetField(profile, "id", expected.id);
                SetField(profile, "equipmentId", expected.equipmentId);
                SetField(profile, "chance", expected.chance);
                profileList.Add(profile);
            }

            contract = Create("ToilRelic.Unity.Data.HuntContractData");
            SetField(contract, "projectId", fixture.projectId);
            SetField(contract, "displayName", "First Relic Project");
            SetField(contract, "relicEquipmentId", fixture.relicEquipmentId);
            var quarryList = CreateList(contract, "quarries");
            var quarryType = FindType("ToilRelic.Unity.Data.HuntQuarryData");
            var dangerType = FindType("ToilRelic.Unity.Data.HuntDanger");
            foreach (var expected in fixture.quarries)
            {
                var quarry = Activator.CreateInstance(quarryType);
                SetField(quarry, "id", expected.id);
                SetField(quarry, "enemyId", expected.enemyId);
                SetField(quarry, "danger", Enum.Parse(dangerType, expected.danger));
                SetField(quarry, "contributionId", expected.contributionId);
                SetField(quarry, "contributionDisplayName", expected.contributionName);
                SetField(quarry, "profileId", expected.profileId);
                quarryList.Add(quarry);
            }
        }

        private void ConfigureSavePath()
        {
            saveServiceType = FindType("ToilRelic.Unity.Save.SaveService");
            var field = saveServiceType.GetField("savePathOverride", BindingFlags.Static | BindingFlags.NonPublic);
            previousSavePathOverride = field.GetValue(null);
            saveDirectory = Path.Combine(Path.GetTempPath(), $"toil-relic-project-{Guid.NewGuid():N}");
            Directory.CreateDirectory(saveDirectory);
            savePath = Path.Combine(saveDirectory, "toil_relic_save.json");
            field.SetValue(null, savePath);
        }

        private static IList CreateList(object owner, string fieldName)
        {
            var field = owner.GetType().GetField(fieldName);
            var list = (IList)Activator.CreateInstance(field.FieldType);
            field.SetValue(owner, list);
            return list;
        }

        private static Type FindType(string fullName) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(type => type != null)
            ?? throw new InvalidOperationException($"Type not found: {fullName}");

        private static void SetField(object instance, string name, object value) =>
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(instance, value);

        private static object GetField(object instance, string name) =>
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(instance);

        private static object GetProperty(object instance, string name) =>
            instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(instance);
    }
}
