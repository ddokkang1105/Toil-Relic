using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ToilRelic.PlayModeTests
{
    public sealed class PurposefulHuntRuntimePlayModeTests
    {
        private readonly List<UnityEngine.Object> created = new();
        private Type saveServiceType;
        private object previousSavePathOverride;
        private string saveDirectory;

        [SetUp]
        public void SetUp()
        {
            saveServiceType = FindType("ToilRelic.Unity.Save.SaveService");
            var field = saveServiceType.GetField("savePathOverride", BindingFlags.Static | BindingFlags.NonPublic);
            previousSavePathOverride = field.GetValue(null);
            saveDirectory = Path.Combine(Path.GetTempPath(), $"toil-relic-runtime-{Guid.NewGuid():N}");
            Directory.CreateDirectory(saveDirectory);
            field.SetValue(null, Path.Combine(saveDirectory, "save.json"));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in created.Where(instance => instance != null).Reverse())
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
            created.Clear();
            saveServiceType.GetField("savePathOverride", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, previousSavePathOverride);
            if (Directory.Exists(saveDirectory)) Directory.Delete(saveDirectory, true);
        }

        [UnityTest]
        public IEnumerator ConfirmedSecondQuarry_RemainsAuthorityThroughVictoryAndMutableContentChanges()
        {
            var manager = CreateManager(out var profiles);
            yield return null;
            Invoke(manager, "StartNewGame");
            Invoke(manager, "StartHunt");
            var snapshot = GetProperty(manager, "PresentedHuntContract");
            var revision = (string)GetProperty(snapshot, "Revision");

            Assert.That(Invoke(manager, "ConfirmHunt", "rust-golem", revision), Is.EqualTo(true));
            Assert.That(GetProperty(manager, "CurrentQuarryId"), Is.EqualTo("rust-golem"));
            var profileList = (IList)GetField(profiles, "profiles");
            SetField(profileList[1], "chance", 0.01f);

            Invoke(manager, "Attack");

            Assert.That(GetProperty(manager, "CurrentState").ToString(), Is.EqualTo("Camp"));
            var player = GetProperty(manager, "Player");
            Assert.That(((IEnumerable)GetProperty(player, "OwnedEquipmentIds")).Cast<string>(), Does.Contain("rustguard-plate"));
            Assert.That(((IEnumerable)GetProperty(player, "OwnedEquipmentIds")).Cast<string>(), Does.Not.Contain("reward-weapon"));
            var project = GetProperty(player, "RelicProject");
            Assert.That(((IEnumerable)GetProperty(project, "CompletedContributionIds")).Cast<string>(),
                Is.EqualTo(new[] { "rustheart-core" }));
            Assert.That(GetProperty(manager, "CurrentQuarryId"), Is.Null);
            Assert.That(GetProperty(saveServiceType.GetMethod("Load").Invoke(null, null), "Status").ToString(), Is.EqualTo("Loaded"));
        }

        [UnityTest]
        public IEnumerator StaleConfirmation_IsRejectedBeforeBattleOrMutation()
        {
            var manager = CreateManager(out var profiles);
            yield return null;
            Invoke(manager, "StartNewGame");
            var player = GetProperty(manager, "Player");
            var before = JsonUtility.ToJson(player);
            Invoke(manager, "StartHunt");
            var snapshot = GetProperty(manager, "PresentedHuntContract");
            var revision = (string)GetProperty(snapshot, "Revision");
            var profileList = (IList)GetField(profiles, "profiles");
            SetField(profileList[0], "chance", 0.5f);

            Assert.That(Invoke(manager, "ConfirmHunt", "mine-vermin", revision), Is.EqualTo(false));
            Assert.That(GetProperty(manager, "CurrentState").ToString(), Is.EqualTo("Camp"));
            Assert.That(GetProperty(manager, "CurrentQuarryId"), Is.Null);
            Assert.That(JsonUtility.ToJson(player), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator CancelledContract_CannotBeConfirmedFromCapturedSnapshot()
        {
            var manager = CreateManager(out _);
            yield return null;
            Invoke(manager, "StartNewGame");
            var player = GetProperty(manager, "Player");
            var before = JsonUtility.ToJson(player);
            Invoke(manager, "StartHunt");
            var snapshot = GetProperty(manager, "PresentedHuntContract");
            var revision = (string)GetProperty(snapshot, "Revision");

            Invoke(manager, "CancelHunt");

            Assert.That(GetProperty(manager, "PresentedHuntContract"), Is.Null);
            Assert.That(Invoke(manager, "ConfirmHunt", "mine-vermin", revision), Is.EqualTo(false));
            Assert.That(GetProperty(manager, "CurrentState").ToString(), Is.EqualTo("Camp"));
            Assert.That(GetProperty(manager, "CurrentQuarryId"), Is.Null);
            Assert.That(JsonUtility.ToJson(player), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator ControllerOwnsCampLocalLifecycle_AndForgeKeepsRelicUnequipped()
        {
            var manager = CreateManager(out _);
            yield return null;
            Invoke(manager, "StartNewGame");

            var campPanel = CreateGameObject("CampPanel");
            var contractPanel = CreateGameObject("ContractPanel");
            var controllerObject = CreateGameObject("ContractController", active: false);
            var controller = controllerObject.AddComponent(FindType("ToilRelic.Unity.UI.HuntContractPanelController"));
            SetField(controller, "gameManager", manager);
            SetField(controller, "campMenuPanel", campPanel);
            SetField(controller, "contractPanel", contractPanel);
            SetField(controller, "huntEntryButton", CreateButton("HuntEntry"));
            SetField(controller, "quarryRowsContainer", CreateGameObject("Rows").transform);
            SetField(controller, "titleText", CreateText("Title"));
            SetField(controller, "detailsText", CreateText("Details"));
            SetField(controller, "projectText", CreateText("Project"));
            SetField(controller, "confirmButton", CreateButton("Confirm"));
            SetField(controller, "cancelButton", CreateButton("Cancel"));
            SetField(controller, "forgeButton", CreateButton("Forge"));
            controllerObject.SetActive(true);

            Invoke(controller, "OpenContract");
            Assert.That(GetProperty(controller, "IsOpen"), Is.EqualTo(true));
            Assert.That(((IEnumerable)GetProperty(controller, "QuarryButtons")).Cast<object>().Count(), Is.EqualTo(3));
            Invoke(controller, "SelectQuarry", "ruin-wraith");
            Assert.That(GetProperty(controller, "SelectedQuarryId"), Is.EqualTo("ruin-wraith"));
            Invoke(controller, "Cancel");
            Assert.That(GetProperty(controller, "IsOpen"), Is.EqualTo(false));
            Assert.That(campPanel.activeSelf, Is.True);
            Assert.That(contractPanel.activeSelf, Is.False);

            var player = GetProperty(manager, "Player");
            var project = GetProperty(player, "RelicProject");
            foreach (var id in new[] { "chitin-shard", "rustheart-core", "wraith-ash" })
            {
                Assert.That(Invoke(project, "TryAddContribution", id), Is.EqualTo(true));
            }
            var forged = Invoke(manager, "ForgeRelic");
            Assert.That(GetProperty(forged, "Status").ToString(), Is.EqualTo("Forged"));
            Assert.That(((IEnumerable)GetProperty(player, "OwnedEquipmentIds")).Cast<string>(), Does.Contain("toilbound-relic"));
            Assert.That(((IEnumerable)GetProperty(player, "EquippedEquipment")).Cast<object>()
                .Any(item => (string)GetField(item, "equipmentId") == "toilbound-relic"), Is.False);
            var saved = saveServiceType.GetMethod("Load").Invoke(null, null);
            Assert.That(GetProperty(saved, "Status").ToString(), Is.EqualTo("Loaded"));
            var repeated = Invoke(manager, "ForgeRelic");
            Assert.That(GetProperty(repeated, "Status").ToString(), Is.EqualTo("AlreadyForged"));
        }

        private Component CreateManager(out ScriptableObject profileDatabase)
        {
            var fixture = PurposefulHuntContractFixture.Load().content;
            var managerObject = CreateGameObject("GameManager", active: false);
            var manager = managerObject.AddComponent(FindType("ToilRelic.Unity.Core.GameManager"));

            var enemyDatabase = CreateScriptable("ToilRelic.Unity.Data.EnemyDatabase");
            var enemyList = CreateList(enemyDatabase, "enemies");
            foreach (var expected in fixture.quarries)
            {
                var enemy = CreateScriptable("ToilRelic.Unity.Data.EnemyData");
                SetField(enemy, "id", expected.enemyId);
                SetField(enemy, "displayName", expected.contributionName.Replace(" Shard", " Vermin").Replace("heart Core", " Golem").Replace(" Ash", " Wraith"));
                SetField(enemy, "maxHp", 1);
                SetField(enemy, "attackMin", 0);
                SetField(enemy, "attackMax", 0);
                SetField(enemy, "expReward", 1);
                SetField(enemy, "equipmentDropProfileId", expected.profileId);
                enemyList.Add(enemy);
            }

            profileDatabase = CreateScriptable("ToilRelic.Unity.Data.EquipmentDropProfileDatabase");
            var profileList = CreateList(profileDatabase, "profiles");
            foreach (var expected in fixture.profiles)
            {
                var profile = CreateScriptable("ToilRelic.Unity.Data.EquipmentDropProfileData");
                SetField(profile, "id", expected.id);
                SetField(profile, "equipmentId", expected.equipmentId);
                SetField(profile, "chance", 1f);
                profileList.Add(profile);
            }

            var contract = CreateScriptable("ToilRelic.Unity.Data.HuntContractData");
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

            var dropTable = CreateScriptable("ToilRelic.Unity.Data.DropTableData");
            SetField(dropTable, "junkMin", 0);
            SetField(dropTable, "junkMax", 0);
            SetField(dropTable, "relicPartChance", 0f);
            SetField(dropTable, "healingPotionChance", 0f);
            SetField(manager, "enemyDatabase", enemyDatabase);
            SetField(manager, "equipmentDropProfiles", profileDatabase);
            SetField(manager, "huntContract", contract);
            SetField(manager, "dropTable", dropTable);
            managerObject.SetActive(true);
            return manager;
        }

        private GameObject CreateGameObject(string name, bool active = true)
        {
            var instance = new GameObject(name);
            instance.SetActive(active);
            created.Add(instance);
            return instance;
        }

        private ScriptableObject CreateScriptable(string typeName)
        {
            var instance = ScriptableObject.CreateInstance(FindType(typeName));
            created.Add(instance);
            return instance;
        }

        private Button CreateButton(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            created.Add(gameObject);
            return gameObject.GetComponent<Button>();
        }

        private Text CreateText(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            created.Add(gameObject);
            return gameObject.GetComponent<Text>();
        }

        private static IList CreateList(object owner, string fieldName)
        {
            var field = owner.GetType().GetField(fieldName);
            var list = (IList)Activator.CreateInstance(field.FieldType);
            field.SetValue(owner, list);
            return list;
        }

        private static object Invoke(object instance, string name, params object[] arguments) =>
            instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(method => method.Name == name && method.GetParameters().Length == arguments.Length)
                .Invoke(instance, arguments);

        private static void SetField(object instance, string name, object value) =>
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(instance, value);

        private static object GetField(object instance, string name) =>
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(instance);

        private static object GetProperty(object instance, string name) =>
            instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(instance);

        private static Type FindType(string fullName) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(type => type != null)
            ?? throw new InvalidOperationException($"Type not found: {fullName}");
    }
}
