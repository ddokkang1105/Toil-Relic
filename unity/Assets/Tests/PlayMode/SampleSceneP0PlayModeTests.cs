using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ToilRelic.PlayModeTests
{
    public sealed class SampleSceneP0PlayModeTests
    {
        private const string GameManagerTypeName = "ToilRelic.Unity.Core.GameManager";
        private const string GameActionBridgeTypeName = "ToilRelic.Unity.UI.GameActionBridge";
        private const string TitleMenuControllerTypeName = "ToilRelic.Unity.UI.TitleMenuController";
        private const string HudControllerTypeName = "ToilRelic.Unity.UI.HudController";
        private const string BattlePanelControllerTypeName = "ToilRelic.Unity.UI.BattlePanelController";
        private const string GameStatusControllerTypeName = "ToilRelic.Unity.UI.GameStatusController";
        private const string StatePanelControllerTypeName = "ToilRelic.Unity.UI.StatePanelController";
        private const string EquipmentPanelControllerTypeName = "ToilRelic.Unity.UI.EquipmentPanelController";
        private const string GameEventsTypeName = "ToilRelic.Unity.Core.GameEvents";
        private const string CombatSystemTypeName = "ToilRelic.Unity.Systems.CombatSystem";
        private const string PlayModeActionContractsCategory = "PlayModeActionContracts";
        private static readonly Vector2 WidescreenVirtualSize = new Vector2(800f, 450f);
        private static readonly Vector2 StandardVirtualSize = new Vector2(800f, 600f);
        private const float MinimumTopRegionGap = 16f;
        private const float MinimumStatusGlyphGap = 24f;
        private const string SaveServiceTypeName = "ToilRelic.Unity.Save.SaveService";
        private readonly List<UnityEngine.Object> fixtureObjects = new();
        private Type fixtureSaveServiceType;
        private object previousSavePathOverride;
        private string fixtureSaveDirectory;
        private string fixtureSavePath;
        private bool fixtureOverrideInstalled;
        private CatalogFixtureScope equipmentCatalogScope;

        [Serializable]
        private sealed class EquipmentComparisonContractFixture
        {
            public EquipmentDefinitionFixture[] definitions;
            public ComparisonCaseFixture[] comparisonCases;
            public UnequipCaseFixture[] unequipCases;
        }

        [Serializable]
        private sealed class EquipmentDefinitionFixture
        {
            public string id;
            public string displayName;
            public string category;
            public int attackBonus;
            public int defenseBonus;
            public int damageReductionBonus;
            public int maxHpBonus;
        }

        [Serializable]
        private sealed class ComparisonCaseFixture
        {
            public string name;
            public string slot;
            public string candidateId;
            public string[] ownedIds;
            public EquippedFixtureEntry[] equipped;
            public string expectedReason;
            public bool expectedIsValid;
            public bool expectedCanCommit;
            public string expectedCurrentId;
            public string expectedCandidateId;
            public StatDeltaFixture[] expectedDeltas;
            public int expectedProjectedAttackBonus;
            public int expectedProjectedDefenseBonus;
            public int expectedProjectedDamageReductionBonus;
            public int expectedProjectedEquipmentMaxHpBonus;
            public int expectedProjectedMaxHpDelta;
        }

        [Serializable]
        private sealed class UnequipCaseFixture
        {
            public string name;
            public string slot;
            public string[] ownedIds;
            public EquippedFixtureEntry[] equipped;
            public string expectedStatus;
            public bool expectedIsOccupied;
            public bool expectedIsMandatory;
            public bool expectedCanCommit;
            public string expectedCurrentId;
        }

        [Serializable]
        private sealed class EquippedFixtureEntry
        {
            public string slot;
            public string equipmentId;
        }

        [Serializable]
        private sealed class StatDeltaFixture
        {
            public string stat;
            public int currentValue;
            public int candidateValue;
            public int delta;
        }

        private sealed class CatalogFixtureScope : IDisposable
        {
            private readonly IDictionary definitions;
            private readonly List<DictionaryEntry> snapshot;
            private bool disposed;

            private CatalogFixtureScope(IDictionary definitions)
            {
                this.definitions = definitions;
                snapshot = new List<DictionaryEntry>();
                var enumerator = definitions.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    snapshot.Add(enumerator.Entry);
                }
            }

            public static CatalogFixtureScope Install(
                Type catalogType,
                Type definitionType,
                Type categoryType,
                IEnumerable<EquipmentDefinitionFixture> fixtures)
            {
                var field = catalogType.GetField("Definitions", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, "EquipmentCatalog.Definitions must remain available for scoped tests.");
                var definitions = field.GetValue(null) as IDictionary;
                Assert.That(definitions, Is.Not.Null, "EquipmentCatalog.Definitions must implement IDictionary.");
                var scope = new CatalogFixtureScope(definitions);

                try
                {
                    foreach (var fixture in fixtures)
                    {
                        var definition = Activator.CreateInstance(
                            definitionType,
                            fixture.id,
                            fixture.displayName,
                            Enum.Parse(categoryType, fixture.category),
                            fixture.attackBonus,
                            fixture.defenseBonus,
                            fixture.damageReductionBonus,
                            fixture.maxHpBonus);
                        definitions.Add(fixture.id, definition);
                    }

                    return scope;
                }
                catch
                {
                    scope.Dispose();
                    throw;
                }
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                definitions.Clear();
                foreach (var pair in snapshot)
                {
                    definitions.Add(pair.Key, pair.Value);
                }

                disposed = true;
            }
        }

        private sealed class ReflectedStringEventRecorder : IDisposable
        {
            private readonly EventInfo eventInfo;
            private readonly Action<string> handler;
            private readonly List<string> messages = new();
            private bool subscribed;

            public IReadOnlyList<string> Messages => messages;

            public ReflectedStringEventRecorder(Type eventSourceType, string eventName)
            {
                Assert.That(eventSourceType, Is.Not.Null, $"Event source type is required for {eventName}.");
                eventInfo = eventSourceType.GetEvent(eventName, BindingFlags.Public | BindingFlags.Static);
                Assert.That(eventInfo, Is.Not.Null, $"Expected static event '{eventName}' was not found on {eventSourceType.FullName}.");
                Assert.That(eventInfo.EventHandlerType, Is.EqualTo(typeof(Action<string>)),
                    $"Event '{eventName}' must use Action<string> for assembly-neutral observation.");

                handler = messages.Add;
                eventInfo.AddEventHandler(null, handler);
                subscribed = true;
            }

            public void Dispose()
            {
                if (!subscribed)
                {
                    return;
                }

                eventInfo.RemoveEventHandler(null, handler);
                subscribed = false;
            }
        }

        private sealed class ReflectedEventRecorder : IDisposable
        {
            private readonly EventInfo eventInfo;
            private readonly Delegate handler;
            private bool subscribed;

            public IList<object> Values { get; } = new List<object>();

            public ReflectedEventRecorder(Type eventSourceType, string eventName, ICollection<string> order)
            {
                eventInfo = eventSourceType.GetEvent(eventName, BindingFlags.Public | BindingFlags.Static);
                Assert.That(eventInfo, Is.Not.Null, $"Expected static event '{eventName}' was not found.");
                var invoke = eventInfo.EventHandlerType.GetMethod("Invoke");
                var parameters = invoke.GetParameters();
                Assert.That(parameters.Length, Is.EqualTo(1), $"Event '{eventName}' must have one argument.");

                var value = Expression.Parameter(parameters[0].ParameterType, "value");
                var record = new Action<object>(recorded =>
                {
                    Values.Add(recorded);
                    order.Add(eventName);
                });
                handler = Expression.Lambda(
                    eventInfo.EventHandlerType,
                    Expression.Invoke(Expression.Constant(record), Expression.Convert(value, typeof(object))),
                    value).Compile();
                eventInfo.AddEventHandler(null, handler);
                subscribed = true;
            }

            public void Dispose()
            {
                if (!subscribed)
                {
                    return;
                }

                eventInfo.RemoveEventHandler(null, handler);
                subscribed = false;
            }
        }

        private sealed class RandomStateScope : IDisposable
        {
            private readonly UnityEngine.Random.State originalState = UnityEngine.Random.state;
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                UnityEngine.Random.state = originalState;
                disposed = true;
            }
        }

        [UnitySetUp]
        public IEnumerator LoadSampleScene()
        {
            var setupCompleted = false;
            try
            {
                fixtureSaveServiceType = FindType(SaveServiceTypeName);
                Assert.That(fixtureSaveServiceType, Is.Not.Null, "SaveService must be loaded before scene setup.");
                previousSavePathOverride = GetPrivateStaticField(fixtureSaveServiceType, "savePathOverride");
                fixtureSaveDirectory = Path.Combine(Path.GetTempPath(), $"toil-relic-unity-tests-{Guid.NewGuid():N}");
                Directory.CreateDirectory(fixtureSaveDirectory);
                fixtureSavePath = Path.Combine(fixtureSaveDirectory, "toil_relic_save.json");
                SetPrivateStaticField(fixtureSaveServiceType, "savePathOverride", fixtureSavePath);
                fixtureOverrideInstalled = true;

                var operation = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
                Assert.That(operation, Is.Not.Null, "SampleScene must be included in the project.");
                yield return operation;
                yield return null;
                setupCompleted = true;
            }
            finally
            {
                if (!setupCompleted)
                {
                    CleanupSaveFixtureState();
                }
            }
        }

        [UnityTearDown]
        public IEnumerator CleanupSaveFixture()
        {
            equipmentCatalogScope?.Dispose();
            equipmentCatalogScope = null;
            CleanupSaveFixtureState();
            yield return null;
        }

        [UnityTest]
        public IEnumerator P0_SceneHasGameManager()
        {
            yield return null;
            Assert.That(FindComponent(GameManagerTypeName), Is.Not.Null,
                "P0 setup: SampleScene needs an active GameManager component.");
        }

        [UnityTest]
        public IEnumerator P0_GameManagerHasEnemyAndDropTable()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);

            Assert.That(GetPrivateField(gameManager, "enemyDatabase"), Is.Not.Null,
                "P0 setup: GameManager.enemyDatabase must reference an EnemyDatabase with entries.");
            Assert.That(GetPrivateField(gameManager, "dropTable"), Is.Not.Null,
                "P0 setup: GameManager.dropTable must reference a DropTableData asset.");
        }

        [UnityTest]
        public IEnumerator P0_SceneHasUiActionBridge()
        {
            yield return null;
            Assert.That(FindComponent(GameActionBridgeTypeName), Is.Not.Null,
                "P0 setup: SampleScene needs a UIActions object with GameActionBridge.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentComparisonContractsMatchCanonicalFixture()
        {
            yield return null;
            var fixture = LoadEquipmentComparisonFixture();
            var playerType = FindType("ToilRelic.Unity.Core.PlayerState");
            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            var definitionType = FindType("ToilRelic.Unity.Core.EquipmentDefinition");
            var categoryType = FindType("ToilRelic.Unity.Core.EquipmentCategory");
            var catalogType = FindType("ToilRelic.Unity.Core.EquipmentCatalog");
            var evaluatorType = FindType("ToilRelic.Unity.Core.EquipmentComparisonEvaluator");

            Assert.That(playerType, Is.Not.Null);
            Assert.That(slotType, Is.Not.Null);
            Assert.That(definitionType, Is.Not.Null);
            Assert.That(categoryType, Is.Not.Null);
            Assert.That(catalogType, Is.Not.Null);
            Assert.That(evaluatorType, Is.Not.Null);
            Assert.That(GetCatalogCount(catalogType), Is.EqualTo(2));

            equipmentCatalogScope = CatalogFixtureScope.Install(
                catalogType, definitionType, categoryType, fixture.definitions);
            try
            {
                var compare = evaluatorType.GetMethod("Compare", BindingFlags.Public | BindingFlags.Static);
                var evaluateUnequip = evaluatorType.GetMethod("EvaluateUnequip", BindingFlags.Public | BindingFlags.Static);
                Assert.That(compare, Is.Not.Null);
                Assert.That(evaluateUnequip, Is.Not.Null);

                foreach (var testCase in fixture.comparisonCases)
                {
                    var player = CreateEquipmentFixturePlayer(playerType, slotType, testCase.ownedIds, testCase.equipped);
                    var slot = Enum.Parse(slotType, testCase.slot);
                    var before = JsonUtility.ToJson(player);
                    var currentMaxHp = (int)GetPublicProperty(player, "MaxHp");

                    var result = compare.Invoke(null, new[] { player, slot, testCase.candidateId });

                    Assert.That(GetPublicProperty(result, "Reason").ToString(), Is.EqualTo(testCase.expectedReason), testCase.name);
                    Assert.That(GetPublicProperty(result, "IsValid"), Is.EqualTo(testCase.expectedIsValid), testCase.name);
                    Assert.That(GetPublicProperty(result, "CanCommit"), Is.EqualTo(testCase.expectedCanCommit), testCase.name);
                    Assert.That(GetEquipmentId(GetPublicProperty(result, "Current")), Is.EqualTo(NullIfEmpty(testCase.expectedCurrentId)), testCase.name);
                    Assert.That(GetEquipmentId(GetPublicProperty(result, "Candidate")), Is.EqualTo(NullIfEmpty(testCase.expectedCandidateId)), testCase.name);
                    Assert.That(GetPublicProperty(result, "ProjectedAttackBonus"), Is.EqualTo(testCase.expectedProjectedAttackBonus), testCase.name);
                    Assert.That(GetPublicProperty(result, "ProjectedDefenseBonus"), Is.EqualTo(testCase.expectedProjectedDefenseBonus), testCase.name);
                    Assert.That(GetPublicProperty(result, "ProjectedDamageReductionBonus"), Is.EqualTo(testCase.expectedProjectedDamageReductionBonus), testCase.name);
                    Assert.That(GetPublicProperty(result, "ProjectedEquipmentMaxHpBonus"), Is.EqualTo(testCase.expectedProjectedEquipmentMaxHpBonus), testCase.name);
                    Assert.That((int)GetPublicProperty(result, "ProjectedMaxHp") - currentMaxHp,
                        Is.EqualTo(testCase.expectedProjectedMaxHpDelta), testCase.name);

                    var actualDeltas = ((IEnumerable)GetPublicProperty(result, "StatDeltas"))
                        .Cast<object>()
                        .Select(FormatDelta)
                        .ToArray();
                    var expectedDeltas = testCase.expectedDeltas
                        .Select(delta => $"{delta.stat}:{delta.currentValue}:{delta.candidateValue}:{delta.delta}")
                        .ToArray();
                    Assert.That(actualDeltas, Is.EqualTo(expectedDeltas), testCase.name);
                    Assert.That(JsonUtility.ToJson(player), Is.EqualTo(before), testCase.name);
                }

                foreach (var testCase in fixture.unequipCases)
                {
                    var player = CreateEquipmentFixturePlayer(playerType, slotType, testCase.ownedIds, testCase.equipped);
                    var slot = Enum.Parse(slotType, testCase.slot);
                    var before = JsonUtility.ToJson(player);

                    var result = evaluateUnequip.Invoke(null, new[] { player, slot });

                    Assert.That(GetPublicProperty(result, "Status").ToString(), Is.EqualTo(testCase.expectedStatus), testCase.name);
                    Assert.That(GetPublicProperty(result, "IsOccupied"), Is.EqualTo(testCase.expectedIsOccupied), testCase.name);
                    Assert.That(GetPublicProperty(result, "IsMandatory"), Is.EqualTo(testCase.expectedIsMandatory), testCase.name);
                    Assert.That(GetPublicProperty(result, "CanCommit"), Is.EqualTo(testCase.expectedCanCommit), testCase.name);
                    Assert.That(GetEquipmentId(GetPublicProperty(result, "Current")), Is.EqualTo(NullIfEmpty(testCase.expectedCurrentId)), testCase.name);
                    Assert.That(JsonUtility.ToJson(player), Is.EqualTo(before), testCase.name);
                }
            }
            finally
            {
                equipmentCatalogScope.Dispose();
                equipmentCatalogScope = null;
            }

            Assert.That(GetCatalogCount(catalogType), Is.EqualTo(2));
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentCatalogFixtureRepeatedInstallRestoresProductionDefinitions()
        {
            yield return null;
            var fixture = LoadEquipmentComparisonFixture();
            var catalogType = FindType("ToilRelic.Unity.Core.EquipmentCatalog");
            var definitionType = FindType("ToilRelic.Unity.Core.EquipmentDefinition");
            var categoryType = FindType("ToilRelic.Unity.Core.EquipmentCategory");
            Assert.That(GetCatalogCount(catalogType), Is.EqualTo(2));

            for (var iteration = 0; iteration < 2; iteration++)
            {
                equipmentCatalogScope = CatalogFixtureScope.Install(
                    catalogType, definitionType, categoryType, fixture.definitions);
                Assert.That(GetCatalogCount(catalogType), Is.EqualTo(2 + fixture.definitions.Length));
                equipmentCatalogScope.Dispose();
                equipmentCatalogScope = null;
                Assert.That(GetCatalogCount(catalogType), Is.EqualTo(2));
            }
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentCommandsApplyOrRejectWithExactEventAndSaveBoundaries()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var player = GetPrivateField(gameManager, "player");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(player, new object[] { "reward-weapon" }), Is.True);
            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            var primaryWeapon = Enum.Parse(slotType, "PrimaryWeapon");
            var eventsType = FindType(GameEventsTypeName);
            var order = new List<string>();
            using var playerChanged = new ReflectedEventRecorder(eventsType, "PlayerChanged", order);
            using var stateChanged = new ReflectedEventRecorder(eventsType, "StateChanged", order);
            using var battleLog = new ReflectedEventRecorder(eventsType, "BattleLog", order);
            using var saveStatus = new ReflectedEventRecorder(eventsType, "SaveStatusChanged", order);

            var equip = gameManager.GetType().GetMethod("EquipEquipment", new[] { slotType, typeof(string) });
            Assert.That(equip, Is.Not.Null, "GameManager must expose the typed equipment command.");
            var applied = equip.Invoke(gameManager, new[] { primaryWeapon, "reward-weapon" });

            Assert.That(GetPublicProperty(applied, "Applied"), Is.True);
            Assert.That(GetPublicProperty(applied, "ComparisonReason").ToString(), Is.EqualTo("None"));
            Assert.That(order, Is.EqualTo(new[] { "PlayerChanged", "BattleLog", "SaveStatusChanged" }));
            Assert.That(stateChanged.Values, Is.Empty);
            Assert.That(saveStatus.Values.Single().ToString(), Is.EqualTo("Succeeded"));
            Assert.That(File.Exists(fixtureSavePath), Is.True);

            var savedBytes = File.ReadAllText(fixtureSavePath);
            order.Clear();
            playerChanged.Values.Clear();
            battleLog.Values.Clear();
            saveStatus.Values.Clear();
            var rejected = equip.Invoke(gameManager, new[] { primaryWeapon, "reward-weapon" });

            Assert.That(GetPublicProperty(rejected, "Applied"), Is.False);
            Assert.That(GetPublicProperty(rejected, "ComparisonReason").ToString(), Is.EqualTo("SameItem"));
            Assert.That(order, Is.Empty, "Rejected commands must emit no game events.");
            Assert.That(File.ReadAllText(fixtureSavePath), Is.EqualTo(savedBytes), "Rejected commands must not save.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentCommandSaveFailureKeepsMutationAndSaveStatusLast()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var player = GetPrivateField(gameManager, "player");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(player, new object[] { "reward-weapon" }), Is.True);
            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            var primaryWeapon = Enum.Parse(slotType, "PrimaryWeapon");
            var eventsType = FindType(GameEventsTypeName);
            var order = new List<string>();
            using var playerChanged = new ReflectedEventRecorder(eventsType, "PlayerChanged", order);
            using var stateChanged = new ReflectedEventRecorder(eventsType, "StateChanged", order);
            using var battleLog = new ReflectedEventRecorder(eventsType, "BattleLog", order);
            using var saveStatus = new ReflectedEventRecorder(eventsType, "SaveStatusChanged", order);
            var originalSavePath = GetPrivateStaticField(fixtureSaveServiceType, "savePathOverride");
            object outcome;

            try
            {
                var invalidSavePath = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString(), "toil_relic_save.json");
                SetPrivateStaticField(fixtureSaveServiceType, "savePathOverride", invalidSavePath);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save write failed"));
                outcome = gameManager.GetType().GetMethod("EquipEquipment", new[] { slotType, typeof(string) })
                    .Invoke(gameManager, new[] { primaryWeapon, "reward-weapon" });
            }
            finally
            {
                SetPrivateStaticField(fixtureSaveServiceType, "savePathOverride", originalSavePath);
            }

            Assert.That(GetPublicProperty(outcome, "Applied"), Is.True);
            Assert.That(order, Is.EqualTo(new[] { "PlayerChanged", "BattleLog", "SaveStatusChanged" }));
            Assert.That(stateChanged.Values, Is.Empty);
            Assert.That(saveStatus.Values.Single().ToString(), Is.EqualTo("Failed"));
            var equipped = new object[] { primaryWeapon, null };
            Assert.That((bool)player.GetType().GetMethod("TryGetEquippedEquipment").Invoke(player, equipped), Is.True);
            Assert.That(GetPublicProperty(equipped[1], "Id"), Is.EqualTo("reward-weapon"),
                "A failed save must not roll back the applied equipment mutation.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_UnequipCommandPersistsOptionalRemovalWithExactEventOrder()
        {
            yield return null;
            var fixture = LoadEquipmentComparisonFixture();
            equipmentCatalogScope = CatalogFixtureScope.Install(
                FindType("ToilRelic.Unity.Core.EquipmentCatalog"),
                FindType("ToilRelic.Unity.Core.EquipmentDefinition"),
                FindType("ToilRelic.Unity.Core.EquipmentCategory"),
                fixture.definitions);
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var player = GetPrivateField(gameManager, "player");
            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            var ring1 = Enum.Parse(slotType, "Ring1");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(
                player, new object[] { "current-ring" }), Is.True);
            Assert.That((bool)player.GetType().GetMethod("Equip").Invoke(
                player, new[] { ring1, "current-ring" }), Is.True);
            var eventsType = FindType(GameEventsTypeName);
            var order = new List<string>();
            using var playerChanged = new ReflectedEventRecorder(eventsType, "PlayerChanged", order);
            using var stateChanged = new ReflectedEventRecorder(eventsType, "StateChanged", order);
            using var battleLog = new ReflectedEventRecorder(eventsType, "BattleLog", order);
            using var saveStatus = new ReflectedEventRecorder(eventsType, "SaveStatusChanged", order);

            var outcome = gameManager.GetType().GetMethod("UnequipEquipment", new[] { slotType })
                .Invoke(gameManager, new[] { ring1 });

            Assert.That(GetPublicProperty(outcome, "Applied"), Is.True);
            Assert.That(GetPublicProperty(outcome, "UnequipStatus").ToString(), Is.EqualTo("OccupiedOptional"));
            Assert.That(order, Is.EqualTo(new[] { "PlayerChanged", "BattleLog", "SaveStatusChanged" }));
            Assert.That(stateChanged.Values, Is.Empty);
            Assert.That(battleLog.Values.Single().ToString(), Is.EqualTo("Unequipped Current Ring."));
            Assert.That(saveStatus.Values.Single().ToString(), Is.EqualTo("Succeeded"));
            Assert.That(File.Exists(fixtureSavePath), Is.True);
            var equipped = new object[] { ring1, null };
            Assert.That((bool)player.GetType().GetMethod("TryGetEquippedEquipment").Invoke(player, equipped), Is.False);

            var loadResult = fixtureSaveServiceType.GetMethod("Load").Invoke(null, null);
            Assert.That(GetPublicProperty(loadResult, "Status").ToString(), Is.EqualTo("Loaded"));
            var loadedPlayer = GetPublicProperty(loadResult, "Player");
            var loadedEquipped = new object[] { ring1, null };
            Assert.That((bool)loadedPlayer.GetType().GetMethod("TryGetEquippedEquipment")
                .Invoke(loadedPlayer, loadedEquipped), Is.False,
                "A successful optional unequip must remain empty after reload.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_UnequipCommandRejectsPrimaryAndEmptySlotsWithoutEventsOrSave()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            var primaryWeapon = Enum.Parse(slotType, "PrimaryWeapon");
            var ring1 = Enum.Parse(slotType, "Ring1");
            var eventsType = FindType(GameEventsTypeName);
            var order = new List<string>();
            using var playerChanged = new ReflectedEventRecorder(eventsType, "PlayerChanged", order);
            using var stateChanged = new ReflectedEventRecorder(eventsType, "StateChanged", order);
            using var battleLog = new ReflectedEventRecorder(eventsType, "BattleLog", order);
            using var saveStatus = new ReflectedEventRecorder(eventsType, "SaveStatusChanged", order);
            var unequip = gameManager.GetType().GetMethod("UnequipEquipment", new[] { slotType });

            var primaryOutcome = unequip.Invoke(gameManager, new[] { primaryWeapon });
            var emptyOutcome = unequip.Invoke(gameManager, new[] { ring1 });

            Assert.That(GetPublicProperty(primaryOutcome, "Applied"), Is.False);
            Assert.That(GetPublicProperty(primaryOutcome, "UnequipStatus").ToString(),
                Is.EqualTo("MandatoryPrimaryWeapon"));
            Assert.That(GetPublicProperty(emptyOutcome, "Applied"), Is.False);
            Assert.That(GetPublicProperty(emptyOutcome, "UnequipStatus").ToString(), Is.EqualTo("EmptySlot"));
            Assert.That(order, Is.Empty, "Rejected unequip commands must emit no game events.");
            Assert.That(playerChanged.Values, Is.Empty);
            Assert.That(stateChanged.Values, Is.Empty);
            Assert.That(battleLog.Values, Is.Empty);
            Assert.That(saveStatus.Values, Is.Empty);
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Rejected unequip commands must not create a save file.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_UnequipSaveFailureKeepsRemovalAndSaveStatusLast()
        {
            yield return null;
            var fixture = LoadEquipmentComparisonFixture();
            equipmentCatalogScope = CatalogFixtureScope.Install(
                FindType("ToilRelic.Unity.Core.EquipmentCatalog"),
                FindType("ToilRelic.Unity.Core.EquipmentDefinition"),
                FindType("ToilRelic.Unity.Core.EquipmentCategory"),
                fixture.definitions);
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var player = GetPrivateField(gameManager, "player");
            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            var ring1 = Enum.Parse(slotType, "Ring1");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(
                player, new object[] { "current-ring" }), Is.True);
            Assert.That((bool)player.GetType().GetMethod("Equip").Invoke(
                player, new[] { ring1, "current-ring" }), Is.True);
            var eventsType = FindType(GameEventsTypeName);
            var order = new List<string>();
            using var playerChanged = new ReflectedEventRecorder(eventsType, "PlayerChanged", order);
            using var stateChanged = new ReflectedEventRecorder(eventsType, "StateChanged", order);
            using var battleLog = new ReflectedEventRecorder(eventsType, "BattleLog", order);
            using var saveStatus = new ReflectedEventRecorder(eventsType, "SaveStatusChanged", order);
            var originalSavePath = GetPrivateStaticField(fixtureSaveServiceType, "savePathOverride");
            object outcome;

            try
            {
                var invalidSavePath = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString(), "toil_relic_save.json");
                SetPrivateStaticField(fixtureSaveServiceType, "savePathOverride", invalidSavePath);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save write failed"));
                outcome = gameManager.GetType().GetMethod("UnequipEquipment", new[] { slotType })
                    .Invoke(gameManager, new[] { ring1 });
            }
            finally
            {
                SetPrivateStaticField(fixtureSaveServiceType, "savePathOverride", originalSavePath);
            }

            Assert.That(GetPublicProperty(outcome, "Applied"), Is.True);
            Assert.That(order, Is.EqualTo(new[] { "PlayerChanged", "BattleLog", "SaveStatusChanged" }));
            Assert.That(stateChanged.Values, Is.Empty);
            Assert.That(battleLog.Values.Single().ToString(), Is.EqualTo("Unequipped Current Ring."));
            Assert.That(saveStatus.Values.Single().ToString(), Is.EqualTo("Failed"));
            var equipped = new object[] { ring1, null };
            Assert.That((bool)player.GetType().GetMethod("TryGetEquippedEquipment").Invoke(player, equipped), Is.False,
                "A failed save must not roll back the optional equipment removal.");
            Assert.That(File.Exists(fixtureSavePath), Is.False);
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentPanelPreviewActionsFocusAndLifecycleStayLocal()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var player = GetPrivateField(gameManager, "player");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(player, new object[] { "reward-weapon" }), Is.True);
            var controller = CreateEquipmentPanelController(gameManager);
            var campMenu = (GameObject)GetPrivateField(controller, "campMenuPanel");
            var equipmentPanel = (GameObject)GetPrivateField(controller, "equipmentPanel");
            var equipButton = (Button)GetPrivateField(controller, "equipButton");
            var unequipButton = (Button)GetPrivateField(controller, "unequipButton");
            var backButton = (Button)GetPrivateField(controller, "backButton");
            var validationText = (Text)GetPrivateField(controller, "validationText");
            var comparisonText = (Text)GetPrivateField(controller, "comparisonText");
            var order = new List<string>();
            var eventsType = FindType(GameEventsTypeName);
            using var playerChanged = new ReflectedEventRecorder(eventsType, "PlayerChanged", order);
            using var stateChanged = new ReflectedEventRecorder(eventsType, "StateChanged", order);
            using var battleLog = new ReflectedEventRecorder(eventsType, "BattleLog", order);
            using var saveStatus = new ReflectedEventRecorder(eventsType, "SaveStatusChanged", order);

            controller.GetType().GetMethod("OpenEquipment").Invoke(controller, null);
            yield return null;

            Assert.That(GetPublicProperty(controller, "IsOpen"), Is.True);
            Assert.That(campMenu.activeSelf, Is.False);
            Assert.That(equipmentPanel.activeSelf, Is.True);
            var slotButtons = ((IEnumerable)GetPublicProperty(controller, "SlotButtons")).Cast<Button>().ToArray();
            Assert.That(slotButtons, Has.Length.EqualTo(12));
            Assert.That(slotButtons.Select(GetButtonLabel), Is.EqualTo(new[]
            {
                "Primary Weapon", "Secondary Weapon", "Hat", "Armor", "Gloves", "Shoes",
                "Necklace", "Belt", "Ring 1", "Ring 2", "Earring 1", "Earring 2"
            }));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(slotButtons[0].gameObject));
            Assert.That(backButton.gameObject.activeSelf, Is.True);
            Assert.That(backButton.interactable, Is.True);
            Assert.That(equipButton.gameObject.activeSelf, Is.True);
            Assert.That(unequipButton.gameObject.activeSelf, Is.True);

            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            controller.GetType().GetMethod("SelectSlot").Invoke(controller, new[] { Enum.Parse(slotType, "PrimaryWeapon") });
            yield return null;
            var candidates = ((IEnumerable)GetPublicProperty(controller, "CandidateButtons")).Cast<Button>().ToArray();
            Assert.That(candidates.Select(GetButtonLabel), Is.EqualTo(new[] { "Reward Weapon", "Starter Weapon" }));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(candidates[0].gameObject));

            controller.GetType().GetMethod("SelectCandidate").Invoke(controller, new object[] { "reward-weapon" });
            Assert.That(equipButton.interactable, Is.True);
            Assert.That(unequipButton.interactable, Is.False);
            Assert.That(comparisonText.text, Does.Contain("Reward Weapon"));
            controller.GetType().GetMethod("SelectCandidate").Invoke(controller, new object[] { "starter-weapon" });
            Assert.That(equipButton.interactable, Is.False, "A same-item preview must not be committable.");

            controller.GetType().GetMethod("SelectSlot").Invoke(controller, new[] { Enum.Parse(slotType, "Hat") });
            Assert.That(((IEnumerable)GetPublicProperty(controller, "CandidateButtons")).Cast<object>(), Is.Empty);
            Assert.That(comparisonText.text, Does.Contain("No compatible owned equipment"));
            Assert.That(equipButton.interactable, Is.False);
            Assert.That(unequipButton.interactable, Is.False,
                "An empty optional slot must keep Unequip visible but disabled.");
            Assert.That(validationText.text, Is.Empty);
            Assert.That(order, Is.Empty, "Open, slot selection, and preview must emit no game events.");
            Assert.That(File.Exists(fixtureSavePath), Is.False, "Preview-only interaction must not save.");

            controller.GetType().GetMethod("BackToCamp").Invoke(controller, null);
            yield return null;
            Assert.That(GetPublicProperty(controller, "IsOpen"), Is.False);
            Assert.That(campMenu.activeSelf, Is.True);
            Assert.That(equipmentPanel.activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.EqualTo(((Button)GetPrivateField(controller, "equipmentEntryButton")).gameObject));

            controller.GetType().GetMethod("OpenEquipment").Invoke(controller, null);
            controller.gameObject.SetActive(false);
            Assert.That(GetPublicProperty(controller, "IsOpen"), Is.False);
            Assert.That(GetPublicProperty(controller, "SelectedCandidateId"), Is.Null);
            Assert.That(order, Is.Empty);
            Assert.That(File.Exists(fixtureSavePath), Is.False);
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentPanelClosesAndClearsWhenCampStateExits()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var player = GetPrivateField(gameManager, "player");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(
                player, new object[] { "reward-weapon" }), Is.True);
            var controller = CreateEquipmentPanelController(gameManager);
            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            var primaryWeapon = Enum.Parse(slotType, "PrimaryWeapon");
            var equipmentPanel = (GameObject)GetPrivateField(controller, "equipmentPanel");
            var comparisonText = (Text)GetPrivateField(controller, "comparisonText");
            var totalsText = (Text)GetPrivateField(controller, "totalsText");
            var validationText = (Text)GetPrivateField(controller, "validationText");
            var order = new List<string>();
            var eventsType = FindType(GameEventsTypeName);
            using var playerChanged = new ReflectedEventRecorder(eventsType, "PlayerChanged", order);
            using var stateChanged = new ReflectedEventRecorder(eventsType, "StateChanged", order);
            using var battleLog = new ReflectedEventRecorder(eventsType, "BattleLog", order);
            using var saveStatus = new ReflectedEventRecorder(eventsType, "SaveStatusChanged", order);

            controller.GetType().GetMethod("OpenEquipment").Invoke(controller, null);
            controller.GetType().GetMethod("SelectSlot").Invoke(controller, new[] { primaryWeapon });
            controller.GetType().GetMethod("SelectCandidate").Invoke(
                controller, new object[] { "reward-weapon" });
            Assert.That(GetPublicProperty(controller, "IsOpen"), Is.True);
            Assert.That(GetPublicProperty(controller, "SelectedSlot").ToString(), Is.EqualTo("PrimaryWeapon"));
            Assert.That(GetPublicProperty(controller, "SelectedCandidateId"), Is.EqualTo("reward-weapon"));
            Assert.That(order, Is.Empty, "Opening and previewing equipment must emit no game events.");

            var sentinelSaveBytes = new byte[] { 0x54, 0x52, 0x43, 0x31 };
            File.WriteAllBytes(fixtureSavePath, sentinelSaveBytes);
            gameManager.GetType().GetMethod("StartHunt").Invoke(gameManager, null);
            yield return null;

            Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Battle"));
            Assert.That(GetPublicProperty(controller, "IsOpen"), Is.False);
            Assert.That(GetPublicProperty(controller, "SelectedSlot"), Is.Null);
            Assert.That(GetPublicProperty(controller, "SelectedCandidateId"), Is.Null);
            Assert.That(GetPublicProperty(controller, "CurrentComparison"), Is.Null);
            Assert.That(GetPublicProperty(controller, "CurrentUnequipEligibility"), Is.Null);
            Assert.That(((IEnumerable)GetPublicProperty(controller, "SlotButtons")).Cast<object>(), Is.Empty);
            Assert.That(((IEnumerable)GetPublicProperty(controller, "CandidateButtons")).Cast<object>(), Is.Empty);
            Assert.That(equipmentPanel.activeSelf, Is.False);
            Assert.That(comparisonText.text, Is.Empty);
            Assert.That(totalsText.text, Is.Empty);
            Assert.That(validationText.text, Is.Empty);
            Assert.That(File.ReadAllBytes(fixtureSavePath), Is.EqualTo(sentinelSaveBytes),
                "Leaving Camp must not save or rewrite existing bytes.");
            Assert.That(order, Is.EqualTo(new[] { "StateChanged", "BattleLog" }),
                "The equipment controller must not add gameplay or save events to the normal hunt transition.");
            Assert.That(stateChanged.Values.Single().ToString(), Is.EqualTo("Battle"));
            Assert.That(playerChanged.Values, Is.Empty);
            Assert.That(saveStatus.Values, Is.Empty);
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentPanelShowsNegativeDeltaForCanonicalRewardDowngrade()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var player = GetPrivateField(gameManager, "player");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(
                player, new object[] { "reward-weapon" }), Is.True);
            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            var primaryWeapon = Enum.Parse(slotType, "PrimaryWeapon");
            Assert.That((bool)player.GetType().GetMethod("Equip").Invoke(
                player, new[] { primaryWeapon, "reward-weapon" }), Is.True);
            var controller = CreateEquipmentPanelController(gameManager);

            controller.GetType().GetMethod("OpenEquipment").Invoke(controller, null);
            controller.GetType().GetMethod("SelectSlot").Invoke(controller, new[] { primaryWeapon });
            controller.GetType().GetMethod("SelectCandidate").Invoke(
                controller, new object[] { "starter-weapon" });

            var comparisonText = (Text)GetPrivateField(controller, "comparisonText");
            var totalsText = (Text)GetPrivateField(controller, "totalsText");
            Assert.That(comparisonText.text, Does.Contain("Current: Reward Weapon"));
            Assert.That(comparisonText.text, Does.Contain("Candidate: Starter Weapon"));
            Assert.That(comparisonText.text, Does.Contain("ATK 2 -> 0 (-2)"));
            Assert.That(totalsText.text, Does.Contain("Result: ATK +0"));
            Assert.That(((Button)GetPrivateField(controller, "equipButton")).interactable, Is.True);
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "A negative-delta preview must remain read-only until confirmation.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentPanelFiltersOtherSlotCandidatesAndShowsZeroStatEqualityFallback()
        {
            yield return null;
            var fixture = LoadEquipmentComparisonFixture();
            equipmentCatalogScope = CatalogFixtureScope.Install(
                FindType("ToilRelic.Unity.Core.EquipmentCatalog"),
                FindType("ToilRelic.Unity.Core.EquipmentDefinition"),
                FindType("ToilRelic.Unity.Core.EquipmentCategory"),
                fixture.definitions);
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var player = GetPrivateField(gameManager, "player");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(
                player, new object[] { "current-ring" }), Is.True);
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(
                player, new object[] { "all-stat-ring" }), Is.True);
            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            var ring1 = Enum.Parse(slotType, "Ring1");
            var ring2 = Enum.Parse(slotType, "Ring2");
            var primaryWeapon = Enum.Parse(slotType, "PrimaryWeapon");
            Assert.That((bool)player.GetType().GetMethod("Equip").Invoke(
                player, new[] { ring1, "current-ring" }), Is.True);
            var controller = RequireComponent(EquipmentPanelControllerTypeName);
            controller.GetType().GetMethod("OpenEquipment").Invoke(controller, null);

            controller.GetType().GetMethod("SelectSlot").Invoke(controller, new[] { ring1 });
            var ringOneCandidates = ((IEnumerable)GetPublicProperty(controller, "CandidateButtons"))
                .Cast<Button>()
                .Select(GetButtonLabel)
                .ToArray();
            Assert.That(ringOneCandidates, Does.Contain("Current Ring"),
                "The item in the selected physical slot must remain comparable.");
            Assert.That(ringOneCandidates, Does.Contain("All-Stat Ring With A Deliberately Long Fixture Name"));

            controller.GetType().GetMethod("SelectSlot").Invoke(controller, new[] { ring2 });
            var ringTwoCandidates = ((IEnumerable)GetPublicProperty(controller, "CandidateButtons"))
                .Cast<Button>()
                .Select(GetButtonLabel)
                .ToArray();
            Assert.That(ringTwoCandidates, Does.Not.Contain("Current Ring"),
                "An ID equipped in Ring 1 must not be offered for Ring 2.");
            Assert.That(ringTwoCandidates, Does.Contain("All-Stat Ring With A Deliberately Long Fixture Name"));

            controller.GetType().GetMethod("SelectSlot").Invoke(controller, new[] { primaryWeapon });
            controller.GetType().GetMethod("SelectCandidate").Invoke(controller, new object[] { "starter-weapon" });
            var comparisonText = (Text)GetPrivateField(controller, "comparisonText");
            Assert.That(comparisonText.text, Does.Contain("No equipment stat change (±0)"));
            Assert.That(((Button)GetPrivateField(controller, "equipButton")).interactable, Is.False);
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Candidate filtering and equality preview must remain read-only.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentPanelShowsLocalRejectionThenRefreshesInPlaceAfterSuccess()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var player = GetPrivateField(gameManager, "player");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(player, new object[] { "reward-weapon" }), Is.True);
            var controller = CreateEquipmentPanelController(gameManager);
            var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
            var primaryWeapon = Enum.Parse(slotType, "PrimaryWeapon");
            var order = new List<string>();
            var eventsType = FindType(GameEventsTypeName);
            using var playerChanged = new ReflectedEventRecorder(eventsType, "PlayerChanged", order);
            using var stateChanged = new ReflectedEventRecorder(eventsType, "StateChanged", order);
            using var battleLog = new ReflectedEventRecorder(eventsType, "BattleLog", order);
            using var saveStatus = new ReflectedEventRecorder(eventsType, "SaveStatusChanged", order);

            controller.GetType().GetMethod("OpenEquipment").Invoke(controller, null);
            controller.GetType().GetMethod("SelectCandidate").Invoke(controller, new object[] { "reward-weapon" });
            ((IList)GetPrivateField(player, "ownedEquipmentIds")).Remove("reward-weapon");
            controller.GetType().GetMethod("EquipSelected").Invoke(controller, null);

            var validationText = (Text)GetPrivateField(controller, "validationText");
            Assert.That(validationText.text, Is.EqualTo("That equipment is not owned."));
            Assert.That(GetPublicProperty(controller, "SelectedCandidateId"), Is.Null,
                "A rejected stale selection must be cleared during refresh.");
            Assert.That(order, Is.Empty, "A locally displayed command rejection must emit no game events.");
            Assert.That(File.Exists(fixtureSavePath), Is.False);

            controller.GetType().GetMethod("SelectSlot").Invoke(controller, new[] { primaryWeapon });
            Assert.That(validationText.text, Is.Empty, "The next selection must clear local rejection text.");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(player, new object[] { "reward-weapon" }), Is.True);
            controller.GetType().GetMethod("Refresh").Invoke(controller, null);
            controller.GetType().GetMethod("SelectCandidate").Invoke(controller, new object[] { "reward-weapon" });
            controller.GetType().GetMethod("EquipSelected").Invoke(controller, null);
            yield return null;

            Assert.That(GetPublicProperty(controller, "IsOpen"), Is.True, "Successful commands keep the equipment screen open.");
            Assert.That(order, Is.EqualTo(new[] { "PlayerChanged", "BattleLog", "SaveStatusChanged" }));
            Assert.That(stateChanged.Values, Is.Empty);
            Assert.That(File.Exists(fixtureSavePath), Is.True);
            var equipped = new object[] { primaryWeapon, null };
            Assert.That((bool)player.GetType().GetMethod("TryGetEquippedEquipment").Invoke(player, equipped), Is.True);
            Assert.That(GetPublicProperty(equipped[1], "Id"), Is.EqualTo("reward-weapon"));
            Assert.That(((Button)GetPrivateField(controller, "equipButton")).interactable, Is.False,
                "The refreshed same-item preview must not allow another commit.");

            var savedBytes = File.ReadAllText(fixtureSavePath);
            controller.gameObject.SetActive(false);
            Assert.That(File.ReadAllText(fixtureSavePath), Is.EqualTo(savedBytes), "Disable cleanup must not save.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentSceneWiresDedicatedCampLocalPanelAndFixedActions()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            var stateController = RequireComponent(StatePanelControllerTypeName);
            var controller = RequireComponent(EquipmentPanelControllerTypeName);
            var campRoot = RequireRectTransform("CampPanel");
            var campMenu = RequireRectTransform("CampActionMenu");
            var equipmentPanel = RequireRectTransform("EquipmentPanel");

            Assert.That(controller.transform, Is.EqualTo(campRoot),
                "EquipmentPanelController must live on the Camp state root.");
            Assert.That(GetPrivateField(stateController, "campPanel"), Is.EqualTo(campRoot.gameObject),
                "StatePanelController must toggle the Camp root, not either Camp-local child panel.");
            Assert.That(GetPrivateField(controller, "gameManager"), Is.EqualTo(gameManager));
            Assert.That(GetPrivateField(controller, "campMenuPanel"), Is.EqualTo(campMenu.gameObject));
            Assert.That(GetPrivateField(controller, "equipmentPanel"), Is.EqualTo(equipmentPanel.gameObject));
            Assert.That(campMenu.parent, Is.EqualTo(campRoot));
            Assert.That(equipmentPanel.parent, Is.EqualTo(campRoot));

            var equipmentEntry = RequireRectTransform("EquipmentButton").GetComponent<Button>();
            var back = RequireRectTransform("BackButton").GetComponent<Button>();
            var equip = RequireRectTransform("EquipButton").GetComponent<Button>();
            var unequip = RequireRectTransform("UnequipButton").GetComponent<Button>();
            Assert.That(GetPrivateField(controller, "equipmentEntryButton"), Is.EqualTo(equipmentEntry));
            Assert.That(GetPrivateField(controller, "backButton"), Is.EqualTo(back));
            Assert.That(GetPrivateField(controller, "equipButton"), Is.EqualTo(equip));
            Assert.That(GetPrivateField(controller, "unequipButton"), Is.EqualTo(unequip));
            Assert.That(GetPrivateField(controller, "slotRowsContainer"),
                Is.EqualTo(RequireRectTransform("SlotRowsContainer")));
            Assert.That(GetPrivateField(controller, "candidateRowsContainer"),
                Is.EqualTo(RequireRectTransform("CandidateRowsContainer")));
            Assert.That(GetPrivateField(controller, "comparisonText"),
                Is.EqualTo(RequireRectTransform("ComparisonText").GetComponent<Text>()));
            Assert.That(GetPrivateField(controller, "totalsText"),
                Is.EqualTo(RequireRectTransform("TotalsText").GetComponent<Text>()));
            Assert.That(GetPrivateField(controller, "validationText"),
                Is.EqualTo(RequireRectTransform("ValidationText").GetComponent<Text>()));

            AssertPersistentAction(equipmentEntry, EquipmentPanelControllerTypeName, "OpenEquipment");
            AssertPersistentAction(back, EquipmentPanelControllerTypeName, "BackToCamp");
            AssertPersistentAction(equip, EquipmentPanelControllerTypeName, "EquipSelected");
            AssertPersistentAction(unequip, EquipmentPanelControllerTypeName, "UnequipSelected");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentPointerFlowOpensPreviewsConfirmsUnequipsAndReturnsControl()
        {
            yield return null;
            var fixture = LoadEquipmentComparisonFixture();
            var catalogType = FindType("ToilRelic.Unity.Core.EquipmentCatalog");
            equipmentCatalogScope = CatalogFixtureScope.Install(
                catalogType,
                FindType("ToilRelic.Unity.Core.EquipmentDefinition"),
                FindType("ToilRelic.Unity.Core.EquipmentCategory"),
                fixture.definitions);

            try
            {
                var gameManager = RequireComponent(GameManagerTypeName);
                EnterCampState(gameManager);
                yield return null;
                var player = GetPrivateField(gameManager, "player");
                Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(
                    player, new object[] { "current-ring" }), Is.True);
                var controller = RequireComponent(EquipmentPanelControllerTypeName);
                var equipmentEntry = RequireRectTransform("EquipmentButton").GetComponent<Button>();
                Canvas.ForceUpdateCanvases();

                AssertTopRaycastReaches(equipmentEntry);
                ExecutePointerClick(equipmentEntry);
                yield return null;

                Assert.That(GetPublicProperty(controller, "IsOpen"), Is.True);
                Assert.That(((GameObject)GetPrivateField(controller, "campMenuPanel")).activeSelf, Is.False);
                Assert.That(((GameObject)GetPrivateField(controller, "equipmentPanel")).activeSelf, Is.True);
                var slots = ((IEnumerable)GetPublicProperty(controller, "SlotButtons")).Cast<Button>().ToArray();
                ExecutePointerClick(slots[8]);
                yield return null;

                var candidates = ((IEnumerable)GetPublicProperty(controller, "CandidateButtons")).Cast<Button>().ToArray();
                var ring = candidates.Single(candidate => GetButtonLabel(candidate) == "Current Ring");
                ExecutePointerClick(ring);
                var comparisonText = (Text)GetPrivateField(controller, "comparisonText");
                Assert.That(comparisonText.text, Does.Contain("Current Ring"),
                    "A real candidate-row pointer click must update the preview before confirmation.");

                var equip = (Button)GetPrivateField(controller, "equipButton");
                Assert.That(equip.interactable, Is.True);
                ExecutePointerClick(equip);
                yield return null;

                var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
                var ring1 = Enum.Parse(slotType, "Ring1");
                var equipped = new object[] { ring1, null };
                Assert.That((bool)player.GetType().GetMethod("TryGetEquippedEquipment").Invoke(player, equipped), Is.True);
                Assert.That(GetEquipmentId(equipped[1]), Is.EqualTo("current-ring"));

                var unequip = (Button)GetPrivateField(controller, "unequipButton");
                Assert.That(unequip.interactable, Is.True);
                ExecutePointerClick(unequip);
                yield return null;
                equipped[1] = null;
                Assert.That((bool)player.GetType().GetMethod("TryGetEquippedEquipment").Invoke(player, equipped), Is.False);

                var back = (Button)GetPrivateField(controller, "backButton");
                ExecutePointerClick(back);
                yield return null;
                Assert.That(GetPublicProperty(controller, "IsOpen"), Is.False);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(equipmentEntry.gameObject),
                    "Back must return non-pointer control to the Camp Equipment entry.");
            }
            finally
            {
                equipmentCatalogScope.Dispose();
                equipmentCatalogScope = null;
            }
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentNavigationUsesExplicitCampSlotCandidateActionOrder()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var player = GetPrivateField(gameManager, "player");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(
                player, new object[] { "reward-weapon" }), Is.True);

            var campButtons = new[]
            {
                RequireRectTransform("HuntButton").GetComponent<Button>(),
                RequireRectTransform("RestButton").GetComponent<Button>(),
                RequireRectTransform("Craft TreasureButton").GetComponent<Button>(),
                RequireRectTransform("EquipmentButton").GetComponent<Button>()
            };
            AssertExplicitVerticalCycle(campButtons);

            var controller = RequireComponent(EquipmentPanelControllerTypeName);
            controller.GetType().GetMethod("OpenEquipment").Invoke(controller, null);
            controller.GetType().GetMethod("SelectCandidate").Invoke(controller, new object[] { "reward-weapon" });
            yield return null;

            var slots = ((IEnumerable)GetPublicProperty(controller, "SlotButtons")).Cast<Button>().ToArray();
            var candidates = ((IEnumerable)GetPublicProperty(controller, "CandidateButtons")).Cast<Button>().ToArray();
            var back = (Button)GetPrivateField(controller, "backButton");
            var equip = (Button)GetPrivateField(controller, "equipButton");
            var unequip = (Button)GetPrivateField(controller, "unequipButton");
            Assert.That(slots, Has.Length.EqualTo(12));
            for (var index = 0; index < slots.Length; index++)
            {
                var navigation = slots[index].navigation;
                Assert.That(navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                Assert.That(navigation.selectOnUp, Is.EqualTo(slots[(index - 1 + slots.Length) % slots.Length]));
                Assert.That(navigation.selectOnDown, Is.EqualTo(slots[(index + 1) % slots.Length]));
                Assert.That(navigation.selectOnLeft, Is.EqualTo(back));
                Assert.That(navigation.selectOnRight, Is.EqualTo(candidates[0]));
            }

            for (var index = 0; index < candidates.Length; index++)
            {
                var navigation = candidates[index].navigation;
                Assert.That(navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                Assert.That(navigation.selectOnUp, Is.EqualTo(candidates[(index - 1 + candidates.Length) % candidates.Length]));
                Assert.That(navigation.selectOnDown, Is.EqualTo(candidates[(index + 1) % candidates.Length]));
                Assert.That(navigation.selectOnLeft, Is.EqualTo(slots[0]));
                Assert.That(navigation.selectOnRight, Is.EqualTo(equip));
            }

            Assert.That(equip.navigation.selectOnDown, Is.EqualTo(unequip));
            Assert.That(unequip.navigation.selectOnDown, Is.EqualTo(back));
            Assert.That(back.navigation.selectOnDown, Is.EqualTo(slots[0]));
            Assert.That(back.navigation.selectOnRight, Is.EqualTo(slots[0]));
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_EquipmentSlotNavigationKeepsFinalRowVisibleAndReopenResetsScroll()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            EnterCampState(gameManager);
            var controller = RequireComponent(EquipmentPanelControllerTypeName);
            controller.GetType().GetMethod("OpenEquipment").Invoke(controller, null);
            yield return null;
            Canvas.ForceUpdateCanvases();

            var slots = ((IEnumerable)GetPublicProperty(controller, "SlotButtons")).Cast<Button>().ToArray();
            var slotRows = (Transform)GetPrivateField(controller, "slotRowsContainer");
            var scrollRect = slotRows.GetComponentInParent<ScrollRect>();
            Assert.That(slots, Has.Length.EqualTo(12));
            Assert.That(scrollRect, Is.Not.Null);
            Assert.That(scrollRect.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f),
                "Opening equipment must start the slot viewport at the first row.");

            EventSystem.current.SetSelectedGameObject(slots[0].gameObject);
            for (var index = 1; index < slots.Length; index++)
            {
                var move = new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Down };
                ExecuteEvents.Execute(
                    EventSystem.current.currentSelectedGameObject,
                    move,
                    ExecuteEvents.moveHandler);
                yield return null;
            }

            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(slots[^1].gameObject));
            Canvas.ForceUpdateCanvases();
            AssertRectFullyInsideViewport(scrollRect.viewport, slots[^1].GetComponent<RectTransform>());
            Assert.That(scrollRect.verticalNormalizedPosition, Is.LessThan(1f),
                "Selecting the final slot row must move the viewport away from the initial top position.");

            controller.GetType().GetMethod("BackToCamp").Invoke(controller, null);
            controller.GetType().GetMethod("OpenEquipment").Invoke(controller, null);
            yield return null;
            Canvas.ForceUpdateCanvases();

            var reopenedSlots = ((IEnumerable)GetPublicProperty(controller, "SlotButtons")).Cast<Button>().ToArray();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(reopenedSlots[0].gameObject));
            Assert.That(scrollRect.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f),
                "Reopening equipment must reset the slot viewport to the first row.");
            AssertRectFullyInsideViewport(scrollRect.viewport, reopenedSlots[0].GetComponent<RectTransform>());
        }

        [UnityTest]
        public IEnumerator P0_GameActionBridgeReferencesGameManager()
        {
            yield return null;
            var bridge = RequireComponent(GameActionBridgeTypeName);
            Assert.That(GetPrivateField(bridge, "gameManager"), Is.Not.Null,
                "P0 setup: GameActionBridge.gameManager must reference the scene GameManager.");
        }

        [UnityTest]
        public IEnumerator P0_ButtonsBindAllCoreGameActions()
        {
            yield return null;
            var expectedActions = new HashSet<string>
            {
                "StartHunt", "Rest", "CraftTreasure", "Attack", "Defend", "Flee", "UsePotion"
            };
            var boundActions = new HashSet<string>(StringComparer.Ordinal);

            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                for (var index = 0; index < button.onClick.GetPersistentEventCount(); index++)
                {
                    var target = button.onClick.GetPersistentTarget(index);
                    if (target != null && target.GetType().FullName == GameActionBridgeTypeName)
                    {
                        boundActions.Add(button.onClick.GetPersistentMethodName(index));
                    }
                }
            }

            var missingActions = expectedActions.Except(boundActions).OrderBy(action => action).ToArray();
            Assert.That(missingActions, Is.Empty,
                $"P0 setup: buttons must bind every core action through GameActionBridge. Missing: {string.Join(", ", missingActions)}.");
        }

        [UnityTest]
        public IEnumerator P0_TitleMenuBindsContinueAndNewGame()
        {
            yield return null;
            Assert.That(FindComponent(TitleMenuControllerTypeName), Is.Not.Null,
                "P0 setup: SampleScene needs a TitleMenuController for the initial entry screen.");

            var expectedActions = new HashSet<string> { "ContinueGame", "StartNewGame", "Quit" };
            var boundActions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                for (var index = 0; index < button.onClick.GetPersistentEventCount(); index++)
                {
                    var target = button.onClick.GetPersistentTarget(index);
                    if (target != null && target.GetType().FullName == GameActionBridgeTypeName)
                    {
                        boundActions.Add(button.onClick.GetPersistentMethodName(index));
                    }
                }
            }

            var missingActions = expectedActions.Except(boundActions).OrderBy(action => action).ToArray();
            Assert.That(missingActions, Is.Empty,
                $"P0 setup: title buttons must bind Continue, New Game, and Quit. Missing: {string.Join(", ", missingActions)}.");
        }

        [UnityTest]
        public IEnumerator P0_InitialEntryShowsTitleAndMatchesContinueAvailability()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            var titleMenu = RequireComponent(TitleMenuControllerTypeName);
            var titlePanel = titleMenu.gameObject;
            var state = GetPrivateField(gameManager, "state");
            var continueButton = GetPrivateField(titleMenu, "continueButton") as Button;
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;

            Assert.That(state.ToString(), Is.EqualTo("Title"));
            Assert.That(titlePanel.activeInHierarchy, Is.True);
            Assert.That(continueButton, Is.Not.Null);

            var hasSavedGame = (bool)gameManager.GetType().GetProperty("HasSavedGame").GetValue(gameManager);
            Assert.That(hasSavedGame, Is.False);
            Assert.That(gameManager.GetType().GetProperty("CurrentSaveLoadStatus").GetValue(gameManager).ToString(), Is.EqualTo("Missing"));
            Assert.That(continueButton.interactable, Is.False);
            Assert.That(messageText.text, Is.EqualTo("Start a new game to begin."));
        }

        [UnityTest]
        public IEnumerator P0_ValidSaveEnablesContinueAndLoadsCurrentFormat()
        {
            var gameManager = RequireComponent(GameManagerTypeName);
            var player = GetPrivateField(gameManager, "player");
            SetPrivateField(player, "level", 3);
            var saveResult = fixtureSaveServiceType.GetMethod("Save").Invoke(null, new[] { player });
            Assert.That((bool)saveResult.GetType().GetProperty("Succeeded").GetValue(saveResult), Is.True);
            Assert.That(File.ReadAllText(fixtureSavePath), Does.Contain("\"version\":2"));

            yield return ReloadSampleScene();

            gameManager = RequireComponent(GameManagerTypeName);
            var titleMenu = RequireComponent(TitleMenuControllerTypeName);
            var continueButton = GetPrivateField(titleMenu, "continueButton") as Button;
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var loadedPlayer = GetPrivateField(gameManager, "player");

            Assert.That((bool)gameManager.GetType().GetProperty("HasSavedGame").GetValue(gameManager), Is.True);
            Assert.That(gameManager.GetType().GetProperty("CurrentSaveLoadStatus").GetValue(gameManager).ToString(), Is.EqualTo("Loaded"));
            Assert.That(continueButton.interactable, Is.True);
            Assert.That(messageText.text, Is.EqualTo("Save found. Continue or start a new game."));
            Assert.That((int)loadedPlayer.GetType().GetProperty("Level").GetValue(loadedPlayer), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator P0_LegacySaveWithoutEquipmentFieldsLoadsAndNormalizes()
        {
            const string legacy = "{\"version\":2,\"player\":{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":[{\"type\":0,\"amount\":2},{\"type\":1,\"amount\":1},{\"type\":2,\"amount\":0},{\"type\":3,\"amount\":0}]}}";
            File.WriteAllText(fixtureSavePath, legacy);

            yield return ReloadSampleScene();

            var gameManager = RequireComponent(GameManagerTypeName);
            var loadedPlayer = GetPrivateField(gameManager, "player");
            var tryGetPrimaryWeapon = loadedPlayer.GetType().GetMethod("TryGetPrimaryWeapon");
            var primaryWeaponArguments = new object[] { null };

            Assert.That(gameManager.GetType().GetProperty("CurrentSaveLoadStatus").GetValue(gameManager).ToString(), Is.EqualTo("Loaded"));
            Assert.That((int)loadedPlayer.GetType().GetProperty("Level").GetValue(loadedPlayer), Is.EqualTo(2));
            Assert.That((int)loadedPlayer.GetType().GetProperty("Hp").GetValue(loadedPlayer), Is.EqualTo(18));
            Assert.That((bool)tryGetPrimaryWeapon.Invoke(loadedPlayer, primaryWeaponArguments), Is.True);
            Assert.That(primaryWeaponArguments[0], Is.Not.Null);
            Assert.That(File.ReadAllText(fixtureSavePath), Is.EqualTo(legacy));
        }

        [UnityTest]
        public IEnumerator P0_VersionOneSaveWithoutEquipmentFieldsLoadsAndNormalizes()
        {
            const string original = "{\"version\":1,\"player\":{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":3,\"treasureCount\":4,\"inventory\":[{\"type\":0,\"amount\":2},{\"type\":1,\"amount\":1},{\"type\":2,\"amount\":4},{\"type\":3,\"amount\":0}]}}";
            yield return AssertHistoricalSaveLoadsAndNormalizes(original, expectedLevel: 2, expectedExperience: 3);
        }

        [UnityTest]
        public IEnumerator P0_AuthenticVersionlessScoreSaveLoadsAndNormalizes()
        {
            const string original = "{\"player\":{\"maxHp\":30,\"hp\":18,\"score\":400,\"treasureCount\":4,\"inventory\":[{\"type\":0,\"amount\":2},{\"type\":1,\"amount\":1},{\"type\":2,\"amount\":4}]}}";
            yield return AssertHistoricalSaveLoadsAndNormalizes(original, expectedLevel: 1, expectedExperience: 0);
        }

        [UnityTest]
        public IEnumerator P0_CurrentSaveRequiresEveryStableCorePlayerField()
        {
            yield return null;
            const string complete = "{\"version\":2,\"player\":{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":[{\"type\":0,\"amount\":2}]}}";
            var requiredFields = new[]
            {
                (Name: "maxHp", JsonFragment: "\"maxHp\":30,"),
                (Name: "hp", JsonFragment: "\"hp\":18,"),
                (Name: "level", JsonFragment: "\"level\":2,"),
                (Name: "experience", JsonFragment: "\"experience\":3,"),
                (Name: "treasureCount", JsonFragment: "\"treasureCount\":0,"),
                (Name: "inventory", JsonFragment: ",\"inventory\":[{\"type\":0,\"amount\":2}]")
            };

            var failures = new List<string>();
            foreach (var requiredField in requiredFields)
            {
                var original = complete.Replace(requiredField.JsonFragment, string.Empty);
                CollectUnreadableWithoutMutationFailures($"missing {requiredField.Name}", original, failures);
            }

            Assert.That(failures, Is.Empty, string.Join("; ", failures));
        }

        [UnityTest]
        public IEnumerator P0_CurrentSaveRejectsInvalidCorePlayerValues()
        {
            yield return null;
            const string inventory = "[{\"type\":0,\"amount\":2}]";
            var invalidPlayers = new[]
            {
                (Name: "non-positive maxHp", Json: $"{{\"version\":2,\"player\":{{\"maxHp\":0,\"hp\":0,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":{inventory}}}}}"),
                (Name: "negative hp", Json: $"{{\"version\":2,\"player\":{{\"maxHp\":30,\"hp\":-1,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":{inventory}}}}}"),
                (Name: "hp above maxHp", Json: $"{{\"version\":2,\"player\":{{\"maxHp\":30,\"hp\":31,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":{inventory}}}}}"),
                (Name: "non-positive level", Json: $"{{\"version\":2,\"player\":{{\"maxHp\":30,\"hp\":18,\"level\":0,\"experience\":3,\"treasureCount\":0,\"inventory\":{inventory}}}}}"),
                (Name: "negative experience", Json: $"{{\"version\":2,\"player\":{{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":-1,\"treasureCount\":0,\"inventory\":{inventory}}}}}"),
                (Name: "negative treasureCount", Json: $"{{\"version\":2,\"player\":{{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":3,\"treasureCount\":-1,\"inventory\":{inventory}}}}}"),
                (Name: "empty inventory", Json: "{\"version\":2,\"player\":{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":[]}}"),
                (Name: "negative inventory amount", Json: "{\"version\":2,\"player\":{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":[{\"type\":0,\"amount\":-1}]}}"),
                (Name: "unknown inventory type", Json: "{\"version\":2,\"player\":{\"maxHp\":30,\"hp\":18,\"level\":2,\"experience\":3,\"treasureCount\":0,\"inventory\":[{\"type\":999,\"amount\":1}]}}")
            };

            var failures = new List<string>();
            foreach (var invalidPlayer in invalidPlayers)
            {
                CollectUnreadableWithoutMutationFailures(invalidPlayer.Name, invalidPlayer.Json, failures);
            }

            Assert.That(failures, Is.Empty, string.Join("; ", failures));
        }

        [UnityTest]
        public IEnumerator P0_PartialCurrentSaveDisablesContinueWithoutChangingBytes()
        {
            const string original = "{\"version\":2,\"player\":{\"maxHp\":30,\"level\":2,\"inventory\":[{\"type\":0,\"amount\":2}]}}";
            File.WriteAllText(fixtureSavePath, original);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save load failed"));

            yield return ReloadSampleScene();

            AssertUnreadableTitleState(original);
        }

        [UnityTest]
        public IEnumerator P0_UnreadableSaveDisablesContinueWithoutChangingBytes()
        {
            const string original = "{ unreadable save";
            File.WriteAllText(fixtureSavePath, original);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save load failed"));

            yield return ReloadSampleScene();

            AssertUnreadableTitleState(original);
        }

        [UnityTest]
        public IEnumerator P0_StructurallyInvalidSaveDisablesContinueWithoutChangingBytes()
        {
            const string original = "{\"version\":2,\"player\":{}}";
            File.WriteAllText(fixtureSavePath, original);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save load failed"));

            yield return ReloadSampleScene();

            AssertUnreadableTitleState(original);
        }

        [UnityTest]
        public IEnumerator P0_UnsupportedSaveVersionDisablesContinueWithoutChangingBytes()
        {
            var gameManager = RequireComponent(GameManagerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var saveResult = fixtureSaveServiceType.GetMethod("Save").Invoke(null, new[] { player });
            Assert.That((bool)saveResult.GetType().GetProperty("Succeeded").GetValue(saveResult), Is.True);

            var original = File.ReadAllText(fixtureSavePath).Replace("\"version\":2", "\"version\":999");
            Assert.That(original, Does.Contain("\"version\":999"));
            File.WriteAllText(fixtureSavePath, original);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save load failed"));

            yield return ReloadSampleScene();

            AssertUnreadableTitleState(original);
        }

        [UnityTest]
        public IEnumerator P0_ReplacementFailureKeepsUnreadableTitleState()
        {
            const string original = "locked unreadable save";
            File.WriteAllText(fixtureSavePath, original);
            using (var lockStream = new FileStream(fixtureSavePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save load failed"));
                yield return ReloadSampleScene();

                var gameManager = RequireComponent(GameManagerTypeName);
                var originalPlayer = GetPrivateField(gameManager, "player");
                var status = RequireComponent(GameStatusControllerTypeName);
                var messageText = GetPrivateField(status, "messageText") as Text;
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save delete failed"));
                gameManager.GetType().GetMethod("StartNewGame").Invoke(gameManager, null);
                yield return null;

                Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Title"));
                Assert.That((bool)gameManager.GetType().GetProperty("HasSavedGame").GetValue(gameManager), Is.False);
                Assert.That(gameManager.GetType().GetProperty("CurrentSaveLoadStatus").GetValue(gameManager).ToString(), Is.EqualTo("Unreadable"));
                Assert.That(GetPrivateField(gameManager, "player"), Is.SameAs(originalPlayer));
                Assert.That(messageText.text, Is.EqualTo("Save could not be read. Start New Game to replace it."));
                lockStream.Position = 0;
                using var reader = new StreamReader(lockStream, System.Text.Encoding.UTF8, true, 1024, true);
                Assert.That(reader.ReadToEnd(), Is.EqualTo(original));
            }
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_NewGameButtonReplacesValidFixtureSave()
        {
            var gameManager = RequireComponent(GameManagerTypeName);
            var seededPlayer = GetPrivateField(gameManager, "player");
            SetPrivateField(seededPlayer, "hp", 12);
            SetPrivateField(seededPlayer, "level", 3);
            SetPrivateField(seededPlayer, "experience", 7);
            var seededInventory = new[]
            {
                (Name: "Junk", Amount: 3),
                (Name: "RelicPart", Amount: 2),
                (Name: "Treasure", Amount: 1),
                (Name: "HealingPotion", Amount: 4)
            };
            foreach (var item in seededInventory)
            {
                AddItem(seededPlayer, item.Name, item.Amount);
            }

            var seededPlayerType = seededPlayer.GetType();
            var equipmentSlotType = seededPlayerType.Assembly.GetType("ToilRelic.Unity.Core.EquipmentSlot");
            var grantEquipment = seededPlayerType.GetMethod("GrantEquipment");
            var equip = seededPlayerType.GetMethod("Equip");
            Assert.That(equipmentSlotType, Is.Not.Null, "New Game setup: EquipmentSlot must be available.");
            Assert.That(grantEquipment, Is.Not.Null, "New Game setup: PlayerState.GrantEquipment must be available.");
            Assert.That(equip, Is.Not.Null, "New Game setup: PlayerState.Equip must be available.");
            Assert.That((bool)grantEquipment.Invoke(seededPlayer, new object[] { "reward-weapon" }), Is.True,
                "New Game setup: the fixture player must own a non-starter weapon.");
            Assert.That((bool)equip.Invoke(seededPlayer,
                new[] { Enum.Parse(equipmentSlotType, "PrimaryWeapon"), "reward-weapon" }), Is.True,
                "New Game setup: the fixture player must equip the non-starter weapon.");

            var saveResult = fixtureSaveServiceType.GetMethod("Save").Invoke(null, new[] { seededPlayer });
            Assert.That((bool)saveResult.GetType().GetProperty("Succeeded").GetValue(saveResult), Is.True,
                "New Game setup: a valid isolated fixture save must be created.");

            yield return ReloadSampleScene();

            gameManager = RequireComponent(GameManagerTypeName);
            Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Title"),
                "New Game setup: the valid fixture save must leave the game on Title.");
            Assert.That(gameManager.GetType().GetProperty("CurrentSaveLoadStatus").GetValue(gameManager).ToString(), Is.EqualTo("Loaded"),
                "New Game setup: the fixture save must be classified as loaded.");
            Assert.That(File.Exists(fixtureSavePath), Is.True,
                "New Game setup: the prior fixture save must exist before the click.");

            var loadedPlayer = GetPrivateField(gameManager, "player");
            foreach (var item in seededInventory)
            {
                Assert.That(GetItemAmount(loadedPlayer, item.Name), Is.GreaterThan(0),
                    $"New Game setup: the loaded fixture player's {item.Name} count must be nonzero.");
            }

            var tryGetLoadedPrimaryWeapon = loadedPlayer.GetType().GetMethod("TryGetPrimaryWeapon");
            var loadedPrimaryWeaponArguments = new object[] { null };
            Assert.That(tryGetLoadedPrimaryWeapon, Is.Not.Null,
                "New Game setup: PlayerState.TryGetPrimaryWeapon must be available.");
            Assert.That((bool)tryGetLoadedPrimaryWeapon.Invoke(loadedPlayer, loadedPrimaryWeaponArguments), Is.True,
                "New Game setup: the loaded fixture player must have an equipped weapon.");
            Assert.That(loadedPrimaryWeaponArguments[0], Is.Not.Null,
                "New Game setup: the loaded fixture weapon definition must be available.");
            Assert.That(loadedPrimaryWeaponArguments[0].GetType().GetProperty("Id").GetValue(loadedPrimaryWeaponArguments[0]),
                Is.EqualTo("reward-weapon"),
                "New Game setup: the loaded fixture player must still equip the non-starter weapon.");

            ClickVisibleActionButton("New GameButton", "StartNewGame");
            yield return null;

            gameManager = RequireComponent(GameManagerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var playerType = player.GetType();
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;

            Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Camp"),
                "New Game: the visible action must enter Camp.");
            Assert.That(gameManager.GetType().GetProperty("CurrentSaveLoadStatus").GetValue(gameManager).ToString(), Is.EqualTo("Missing"),
                "New Game: deleting the prior save must reset the load status to Missing.");
            Assert.That((bool)gameManager.GetType().GetProperty("HasSavedGame").GetValue(gameManager), Is.False,
                "New Game: no saved-game offer may remain after replacement.");
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "New Game: the prior fixture save must be deleted without an immediate replacement save.");
            Assert.That(GetPrivateStaticField(fixtureSaveServiceType, "savePathOverride"), Is.EqualTo(fixtureSavePath),
                "New Game: the action must keep all save effects inside the fixture override.");
            Assert.That(messageText.text, Is.EqualTo("A new expedition begins. Hunt, craft, and survive."),
                "New Game: the new-expedition feedback must remain visible.");
            Assert.That((int)playerType.GetProperty("MaxHp").GetValue(player), Is.EqualTo(30),
                "New Game: the replacement player must have default maximum HP.");
            Assert.That((int)playerType.GetProperty("Hp").GetValue(player), Is.EqualTo(30),
                "New Game: the replacement player must start at full HP.");
            Assert.That((int)playerType.GetProperty("Level").GetValue(player), Is.EqualTo(1),
                "New Game: the replacement player must start at level 1.");
            Assert.That((int)playerType.GetProperty("Experience").GetValue(player), Is.Zero,
                "New Game: the replacement player must start with zero experience.");
            Assert.That((int)playerType.GetProperty("TreasureCount").GetValue(player), Is.Zero,
                "New Game: the replacement player must start with zero treasure.");

            foreach (var item in seededInventory)
            {
                Assert.That(GetItemAmount(player, item.Name), Is.Zero,
                    $"New Game: the replacement player's {item.Name} count must be zero.");
            }

            var tryGetPrimaryWeapon = playerType.GetMethod("TryGetPrimaryWeapon");
            var primaryWeaponArguments = new object[] { null };
            Assert.That((bool)tryGetPrimaryWeapon.Invoke(player, primaryWeaponArguments), Is.True,
                "New Game: the replacement player must own and equip the starter weapon.");
            Assert.That(primaryWeaponArguments[0], Is.Not.Null,
                "New Game: the equipped starter weapon definition must be available.");
            Assert.That(primaryWeaponArguments[0].GetType().GetProperty("Id").GetValue(primaryWeaponArguments[0]),
                Is.EqualTo("starter-weapon"),
                "New Game: the replacement player's primary weapon must be the starter weapon.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_CraftButtonConsumesExactCostAndPersistsReward()
        {
            var gameManager = RequireComponent(GameManagerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var playerType = player.GetType();
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);

            AddItem(player, "Junk", 5);
            AddItem(player, "RelicPart", 1);
            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            yield return null;
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Craft success setup: no save may exist before the visible action.");

            ClickVisibleActionButton("Craft TreasureButton", "CraftTreasure");
            yield return null;

            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var saveStatusText = RequireSaveStatusText(status);
            Assert.That(GetItemAmount(player, "Junk"), Is.Zero,
                "Craft success: the exact five Junk cost must be consumed.");
            Assert.That(GetItemAmount(player, "RelicPart"), Is.Zero,
                "Craft success: the exact one Relic Part cost must be consumed.");
            Assert.That(GetItemAmount(player, "Treasure"), Is.EqualTo(1),
                "Craft success: one Treasure inventory item must be granted.");
            Assert.That((int)playerType.GetProperty("TreasureCount").GetValue(player), Is.EqualTo(1),
                "Craft success: the treasure total must increase by one.");
            Assert.That(messageText.text, Is.EqualTo("Treasure crafted. Treasure +1"),
                "Craft success: the result feedback must remain visible.");
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.True,
                "Craft success: the save result row must be visible.");
            Assert.That(saveStatusText.text, Is.EqualTo("Save: Saved just now"),
                "Craft success: the action must report a successful save.");
            Assert.That(File.Exists(fixtureSavePath), Is.True,
                "Craft success: the changed progress must be written to the fixture save.");
            Assert.That(gameManager.GetType().GetProperty("CurrentSaveLoadStatus").GetValue(gameManager).ToString(), Is.EqualTo("Loaded"),
                "Craft success: the successful save must update the load status.");

            var loadResult = fixtureSaveServiceType.GetMethod("Load").Invoke(null, null);
            Assert.That(loadResult.GetType().GetProperty("Status").GetValue(loadResult).ToString(), Is.EqualTo("Loaded"),
                "Craft success persistence: the fixture save must load successfully.");
            var persistedPlayer = loadResult.GetType().GetProperty("Player").GetValue(loadResult);
            Assert.That(GetItemAmount(persistedPlayer, "Junk"), Is.Zero,
                "Craft success persistence: Junk must remain consumed.");
            Assert.That(GetItemAmount(persistedPlayer, "RelicPart"), Is.Zero,
                "Craft success persistence: Relic Part must remain consumed.");
            Assert.That(GetItemAmount(persistedPlayer, "Treasure"), Is.EqualTo(1),
                "Craft success persistence: the Treasure inventory reward must be saved.");
            Assert.That((int)persistedPlayer.GetType().GetProperty("TreasureCount").GetValue(persistedPlayer), Is.EqualTo(1),
                "Craft success persistence: the treasure total must be saved.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_CraftButtonInsufficientMaterialsPersistsUnchangedState()
        {
            var gameManager = RequireComponent(GameManagerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var playerType = player.GetType();
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);

            AddItem(player, "Junk", 4);
            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            yield return null;
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Craft failure setup: no save may exist before the visible action.");

            ClickVisibleActionButton("Craft TreasureButton", "CraftTreasure");
            yield return null;

            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var saveStatusText = RequireSaveStatusText(status);
            Assert.That(GetItemAmount(player, "Junk"), Is.EqualTo(4),
                "Craft failure: insufficient Junk must remain unchanged.");
            Assert.That(GetItemAmount(player, "RelicPart"), Is.Zero,
                "Craft failure: the zero Relic Part count must remain unchanged.");
            Assert.That(GetItemAmount(player, "Treasure"), Is.Zero,
                "Craft failure: no Treasure inventory item may be granted.");
            Assert.That((int)playerType.GetProperty("TreasureCount").GetValue(player), Is.Zero,
                "Craft failure: the treasure total must remain unchanged.");
            Assert.That(messageText.text, Is.EqualTo("Need junk 4/5, relic part 0/1"),
                "Craft failure: the unmet material requirement must remain visible.");
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.True,
                "Craft failure: the save result row must be visible.");
            Assert.That(saveStatusText.text, Is.EqualTo("Save: Saved just now"),
                "Craft failure: the unchanged progress must still report a successful save.");
            Assert.That(File.Exists(fixtureSavePath), Is.True,
                "Craft failure: the unchanged progress must be written to the fixture save.");
            Assert.That(gameManager.GetType().GetProperty("CurrentSaveLoadStatus").GetValue(gameManager).ToString(), Is.EqualTo("Loaded"),
                "Craft failure: the successful save must update the load status.");

            var loadResult = fixtureSaveServiceType.GetMethod("Load").Invoke(null, null);
            Assert.That(loadResult.GetType().GetProperty("Status").GetValue(loadResult).ToString(), Is.EqualTo("Loaded"),
                "Craft failure persistence: the fixture save must load successfully.");
            var persistedPlayer = loadResult.GetType().GetProperty("Player").GetValue(loadResult);
            Assert.That(GetItemAmount(persistedPlayer, "Junk"), Is.EqualTo(4),
                "Craft failure persistence: Junk must remain unchanged.");
            Assert.That(GetItemAmount(persistedPlayer, "RelicPart"), Is.Zero,
                "Craft failure persistence: Relic Part must remain unchanged.");
            Assert.That(GetItemAmount(persistedPlayer, "Treasure"), Is.Zero,
                "Craft failure persistence: no Treasure inventory item may be saved.");
            Assert.That((int)persistedPlayer.GetType().GetProperty("TreasureCount").GetValue(persistedPlayer), Is.Zero,
                "Craft failure persistence: the treasure total must remain unchanged.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_PotionButtonConsumesPotionAndCompletesEnemyResponse()
        {
            yield return EnterBattle();
            var gameManager = RequireComponent(GameManagerTypeName);
            var battlePanel = RequireComponent(BattlePanelControllerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var playerType = player.GetType();
            var publishPlayer = gameManager.GetType().GetMethod("PublishPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
            var logText = GetPrivateField(battlePanel, "logText") as Text;

            playerType.GetMethod("TakeDamage").Invoke(player, new object[] { 15 });
            AddItem(player, "HealingPotion", 1);
            Assert.That(publishPlayer, Is.Not.Null, "Potion success setup: GameManager.PublishPlayer must exist.");
            publishPlayer.Invoke(gameManager, null);
            InstallHarmlessDurableEnemy(gameManager, "P0 Potion Enemy");
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Potion success setup: no save may exist before the visible action.");

            using var randomState = PreserveRandomState();
            using var battleLog = ObserveStringGameEvent(gameManager, "BattleLog");
            ClickVisibleActionButton("PotionButton", "UsePotion");
            yield return null;

            const string recoveryMessage = "You used a healing potion and recovered 12 HP.";
            const string enemyResponse = "P0 Potion Enemy hits you for 1.";
            Assert.That(battleLog.Messages.Count, Is.EqualTo(2),
                "Potion success: exactly the recovery and enemy-response events must be published.");
            Assert.That(battleLog.Messages[0], Is.EqualTo(recoveryMessage),
                "Potion success: recovery feedback must be published before the enemy response.");
            Assert.That(battleLog.Messages[1], Is.EqualTo(enemyResponse),
                "Potion success: the harmless enemy response must follow recovery feedback.");
            Assert.That(logText, Is.Not.Null, "Potion success: the accumulated battle log must be wired.");
            var recoveryIndex = logText.text.IndexOf(recoveryMessage, StringComparison.Ordinal);
            var enemyResponseIndex = logText.text.IndexOf(enemyResponse, StringComparison.Ordinal);
            Assert.That(recoveryIndex, Is.GreaterThanOrEqualTo(0),
                "Potion success: recovery feedback must remain visible in the battle log.");
            Assert.That(enemyResponseIndex, Is.GreaterThan(recoveryIndex),
                "Potion success: the visible enemy response must follow the recovery feedback.");
            Assert.That(GetItemAmount(player, "HealingPotion"), Is.Zero,
                "Potion success: exactly one healing potion must be consumed.");
            Assert.That((int)playerType.GetProperty("Hp").GetValue(player), Is.EqualTo(26),
                "Potion success: HP must reflect 12 recovery followed by one fixed enemy damage.");
            Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Battle"),
                "Potion success: the encounter must remain active.");
            Assert.That(GetPrivateField(gameManager, "battlePhase").ToString(), Is.EqualTo("PlayerAction"),
                "Potion success: control must return after the enemy response.");
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Potion success: a nonterminal action must not write a save.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_PotionButtonAtFullHpPreservesPotionAndControl()
        {
            yield return EnterBattle();
            var gameManager = RequireComponent(GameManagerTypeName);
            var battlePanel = RequireComponent(BattlePanelControllerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var playerType = player.GetType();
            var publishPlayer = gameManager.GetType().GetMethod("PublishPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
            var logText = GetPrivateField(battlePanel, "logText") as Text;
            var initialHp = (int)playerType.GetProperty("Hp").GetValue(player);

            AddItem(player, "HealingPotion", 1);
            Assert.That(publishPlayer, Is.Not.Null, "Potion guard setup: GameManager.PublishPlayer must exist.");
            publishPlayer.Invoke(gameManager, null);
            Assert.That(initialHp, Is.EqualTo((int)playerType.GetProperty("MaxHp").GetValue(player)),
                "Potion guard setup: the player must begin at full HP.");
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Potion guard setup: no save may exist before the visible action.");

            using var battleLog = ObserveStringGameEvent(gameManager, "BattleLog");
            ClickVisibleActionButton("PotionButton", "UsePotion");
            yield return null;

            Assert.That(battleLog.Messages.Count, Is.EqualTo(1),
                "Potion guard: no enemy-response event may follow the full-HP rejection.");
            Assert.That(battleLog.Messages[0], Is.EqualTo("HP is already full."),
                "Potion guard: the full-HP reason must be published.");
            Assert.That(logText, Is.Not.Null, "Potion guard: the accumulated battle log must be wired.");
            Assert.That(logText.text, Does.Contain("HP is already full."),
                "Potion guard: the rejection reason must remain visible.");
            Assert.That((int)playerType.GetProperty("Hp").GetValue(player), Is.EqualTo(initialHp),
                "Potion guard: HP must remain unchanged.");
            Assert.That(GetItemAmount(player, "HealingPotion"), Is.EqualTo(1),
                "Potion guard: the healing potion must not be consumed.");
            Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Battle"),
                "Potion guard: the encounter must remain active.");
            Assert.That(GetPrivateField(gameManager, "battlePhase").ToString(), Is.EqualTo("PlayerAction"),
                "Potion guard: player control must remain available.");
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Potion guard: the rejected action must not write a save.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_AttackButtonKeepsDurableEnemyAndReturnsControl()
        {
            yield return EnterBattle();
            var gameManager = RequireComponent(GameManagerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var playerType = player.GetType();
            var attackBonus = (int)playerType.GetProperty("AttackBonus").GetValue(player);
            var playerHpBefore = (int)playerType.GetProperty("Hp").GetValue(player);
            var enemy = InstallHarmlessDurableEnemy(gameManager, "P0 Durable Attack Enemy");
            var enemyType = enemy.GetType();
            var enemyHpBefore = (int)enemyType.GetProperty("Hp").GetValue(enemy);

            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Attack setup: no save may exist before the visible action.");

            using var randomState = PreserveRandomState();
            using var battleLog = ObserveStringGameEvent(gameManager, "BattleLog");
            using var battleOutcome = ObserveStringGameEvent(gameManager, "BattleOutcome");
            ClickVisibleActionButton("AttackButton", "Attack");
            yield return null;

            var enemyHpAfter = (int)enemyType.GetProperty("Hp").GetValue(enemy);
            var playerDamage = enemyHpBefore - enemyHpAfter;
            var playerHitMessage = $"You hit P0 Durable Attack Enemy for {playerDamage}.";
            const string enemyResponse = "P0 Durable Attack Enemy hits you for 1.";

            Assert.That(playerDamage, Is.InRange(4 + attackBonus, 8 + attackBonus),
                "Attack: enemy HP must decrease within the runtime player-attack bounds.");
            Assert.That((bool)enemyType.GetProperty("IsAlive").GetValue(enemy), Is.True,
                "Attack: the durable enemy must survive the single visible action.");
            Assert.That(GetPrivateField(gameManager, "currentEnemy"), Is.SameAs(enemy),
                "Attack: the same enemy encounter must remain active.");
            Assert.That((int)playerType.GetProperty("Hp").GetValue(player), Is.EqualTo(playerHpBefore - 1),
                "Attack: the player must receive the fixed one-damage enemy response.");
            Assert.That(battleLog.Messages.Count, Is.EqualTo(2),
                "Attack: exactly the player hit and enemy-response events must be published.");
            Assert.That(battleLog.Messages[0], Is.EqualTo(playerHitMessage),
                "Attack: player-hit feedback must precede the enemy response.");
            Assert.That(battleLog.Messages[1], Is.EqualTo(enemyResponse),
                "Attack: the fixed enemy response must follow player-hit feedback.");
            Assert.That(battleOutcome.Messages, Is.Empty,
                "Attack: a nonlethal action must not publish a terminal outcome.");
            Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Battle"),
                "Attack: the encounter must remain in Battle.");
            Assert.That(GetPrivateField(gameManager, "battlePhase").ToString(), Is.EqualTo("PlayerAction"),
                "Attack: control must return after the enemy response.");
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Attack: a nonterminal action must not write a save.");
        }

        [UnityTest]
        [Category(PlayModeActionContractsCategory)]
        public IEnumerator P0_FleeButtonFailureKeepsEncounterAndReturnsControl()
        {
            yield return EnterBattle();
            var gameManager = RequireComponent(GameManagerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var playerType = player.GetType();
            var playerHpBefore = (int)playerType.GetProperty("Hp").GetValue(player);
            var enemy = InstallHarmlessDurableEnemy(gameManager, "P0 Failed Flee Enemy");

            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Failed Flee setup: no save may exist before the visible action.");

            using var randomState = PreserveRandomState();
            var failingSeed = FindFirstFailingFleeSeed(gameManager);
            using var battleLog = ObserveStringGameEvent(gameManager, "BattleLog");
            using var battleOutcome = ObserveStringGameEvent(gameManager, "BattleOutcome");
            UnityEngine.Random.InitState(failingSeed);
            ClickVisibleActionButton("FleeButton", "Flee");
            yield return null;

            const string failedFleeMessage = "Escape failed.";
            const string enemyResponse = "P0 Failed Flee Enemy hits you for 1.";
            Assert.That(battleLog.Messages.Count, Is.EqualTo(2),
                "Failed Flee: exactly the failure and enemy-response events must be published.");
            Assert.That(battleLog.Messages[0], Is.EqualTo(failedFleeMessage),
                "Failed Flee: failure feedback must precede the enemy response.");
            Assert.That(battleLog.Messages[1], Is.EqualTo(enemyResponse),
                "Failed Flee: the fixed enemy response must follow failure feedback.");
            Assert.That(battleLog.Messages, Does.Not.Contain("Escape successful."),
                "Failed Flee: no successful-escape feedback may be published.");
            Assert.That(battleOutcome.Messages, Is.Empty,
                "Failed Flee: no terminal escape outcome may be published.");
            Assert.That(GetPrivateField(gameManager, "currentEnemy"), Is.SameAs(enemy),
                "Failed Flee: the same enemy encounter must remain active.");
            Assert.That((int)playerType.GetProperty("Hp").GetValue(player), Is.EqualTo(playerHpBefore - 1),
                "Failed Flee: the player must receive the fixed one-damage enemy response.");
            Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Battle"),
                "Failed Flee: the encounter must remain in Battle.");
            Assert.That(GetPrivateField(gameManager, "battlePhase").ToString(), Is.EqualTo("PlayerAction"),
                "Failed Flee: control must return after the enemy response.");
            Assert.That(File.Exists(fixtureSavePath), Is.False,
                "Failed Flee: a nonterminal action must not write a save.");
        }

        [UnityTest]
        public IEnumerator P0_HudIsWiredAndDisplaysPlayerState()
        {
            yield return null;
            var hud = RequireComponent(HudControllerTypeName);
            var hpText = GetPrivateField(hud, "hpText") as Text;
            var levelText = GetPrivateField(hud, "levelText") as Text;
            var inventoryText = GetPrivateField(hud, "invText") as Text;
            var equipmentText = GetPrivateField(hud, "equipmentText") as Text;

            Assert.That(hpText, Is.Not.Null);
            Assert.That(levelText, Is.Not.Null);
            Assert.That(inventoryText, Is.Not.Null);
            Assert.That(equipmentText, Is.Not.Null);
            Assert.That(hpText.text, Does.StartWith("HP "));
            Assert.That(levelText.text, Does.StartWith("Lv "));
            Assert.That(inventoryText.text, Does.Contain("Junk "));
            Assert.That(inventoryText.text, Does.Contain("Part "));
            Assert.That(inventoryText.text, Does.Contain("Potion "));
            Assert.That(inventoryText.text, Does.Contain("Treasure "));
            Assert.That(equipmentText.text, Does.StartWith("Wpn "));
            Assert.That(equipmentText.text, Does.Contain("ATK +"));
            Assert.That(equipmentText.text, Does.Contain("DEF +"));
        }

        [UnityTest]
        public IEnumerator P0_HudRefreshesWhenPlayerStateChanges()
        {
            yield return null;
            var hud = RequireComponent(HudControllerTypeName);
            var hpText = GetPrivateField(hud, "hpText") as Text;
            var gameManager = RequireComponent(GameManagerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var playerType = player.GetType();
            var gameEventsType = gameManager.GetType().Assembly.GetType("ToilRelic.Unity.Core.GameEvents");

            playerType.GetMethod("TakeDamage").Invoke(player, new object[] { 5 });
            gameEventsType.GetMethod("RaisePlayerChanged").Invoke(null, new[] { player });

            Assert.That(hpText.text, Is.EqualTo("HP 25/30"));
        }

        [UnityTest]
        public IEnumerator P0_TitleAndCampLayoutsUseDistinctActionGeometryBelowTopRegions()
        {
            yield return null;
            var hudRect = RequireRectTransform("Hud");
            var statusRect = RequireRectTransform("GameStatus");
            var titleRect = RequireRectTransform("TitlePanel");
            var campRect = RequireRectTransform("CampActionMenu");
            var topRegionBottom = Mathf.Min(
                CalculateVirtualRect(hudRect, WidescreenVirtualSize).yMin,
                CalculateVirtualRect(statusRect, WidescreenVirtualSize).yMin);

            AssertPanelFitsBelowTopRegion(titleRect, topRegionBottom);
            AssertPanelFitsBelowTopRegion(campRect, topRegionBottom);
            Assert.That(campRect.sizeDelta.y, Is.GreaterThan(titleRect.sizeDelta.y),
                "The four-action Camp menu must use geometry distinct from the three-action title menu.");
        }

        [UnityTest]
        public IEnumerator P0_BattlePanelFitsBelowVisibleStatusAtWidescreenFloor()
        {
            yield return null;
            var statusRect = RequireRectTransform("GameStatus");
            var messageRect = RequireRectTransform("MessageText");
            var battleRect = RequireRectTransform("BattlePanel");

            foreach (var viewport in new[] { WidescreenVirtualSize, StandardVirtualSize })
            {
                AssertVirtualViewportMargins(battleRect, viewport, MinimumTopRegionGap);
                var statusBounds = CalculateVirtualRect(statusRect, viewport);
                var statusBottom = statusBounds.yMax
                    + messageRect.anchoredPosition.y
                    - messageRect.sizeDelta.y;
                var battleTop = CalculateVirtualRect(battleRect, viewport).yMax;
                Assert.That(statusBottom - battleTop, Is.GreaterThanOrEqualTo(MinimumTopRegionGap),
                    $"BattlePanel must remain at least {MinimumTopRegionGap} virtual pixels below the active GameStatus rows at {viewport.x}x{viewport.y}.");
            }
        }

        [UnityTest]
        public IEnumerator P0_BattleTextUsesReadableNonoverlappingRenderedBounds()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            var battlePanel = RequireComponent(BattlePanelControllerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var enemyText = GetPrivateField(battlePanel, "enemyText") as Text;
            var phaseText = GetPrivateField(battlePanel, "phaseText") as Text;
            var logText = GetPrivateField(battlePanel, "logText") as Text;
            var messageText = GetPrivateField(status, "messageText") as Text;
            var canvasRect = enemyText.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);
            var gameEventsType = gameManager.GetType().Assembly.GetType(GameEventsTypeName);

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Battle") });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "A wild Ruin Wraith appears." });
            RaiseSaveStatus(gameEventsType, "Failed");
            enemyText.text = "Enemy: Ruin Wraith (18/18)";
            logText.text = "You used a healing potion and recovered 12 HP.\nRuin Wraith hits you for 6.";

            Assert.That(messageText.text, Is.EqualTo(
                "A wild Ruin Wraith appears.\nSave failed. Progress may not be saved."));
            AssertTextContract(enemyText, requireSingleVisualLine: true);
            AssertTextContract(logText, requireSingleVisualLine: false);

            foreach (var phase in new[]
                     {
                         "Your turn — choose an action.",
                         "Enemy turn — resolving attack.",
                         "Resolving battle result..."
                     })
            {
                phaseText.text = phase;
                Canvas.ForceUpdateCanvases();
                AssertTextContract(phaseText, requireSingleVisualLine: true);
                AssertGeneratedGlyphsInsideRect(phaseText, canvasRect);
            }

            phaseText.text = "Your turn — choose an action.";
            Canvas.ForceUpdateCanvases();

            var enemyRect = CalculateRectInAncestor(canvasRect, enemyText.rectTransform);
            var phaseRect = CalculateRectInAncestor(canvasRect, phaseText.rectTransform);
            var logRect = CalculateRectInAncestor(canvasRect, logText.rectTransform);
            AssertVerticalRectOrder(enemyRect, phaseRect, enemyText.name, phaseText.name);
            AssertVerticalRectOrder(phaseRect, logRect, phaseText.name, logText.name);
            var enemyGlyphs = CalculateGeneratedGlyphBounds(enemyText, canvasRect);
            AssertGeneratedGlyphsInsideRect(enemyText, canvasRect, enemyGlyphs);
            AssertGeneratedGlyphsInsideRect(logText, canvasRect);

            var messageGlyphs = CalculateGeneratedGlyphBounds(messageText, canvasRect);
            Assert.That(messageGlyphs.yMin - enemyGlyphs.yMax, Is.GreaterThanOrEqualTo(MinimumStatusGlyphGap),
                "The generated status glyphs must remain at least 24 common-Canvas pixels above EnemyText glyphs.");
        }

        [UnityTest]
        public IEnumerator P0_BattleActionsUseTwoByTwoSpatialNavigationAndNewestTwoLogs()
        {
            yield return EnterBattle();
            var gameManager = RequireComponent(GameManagerTypeName);
            var battlePanel = RequireComponent(BattlePanelControllerTypeName);
            var logText = GetPrivateField(battlePanel, "logText") as Text;
            var gameEventsType = gameManager.GetType().Assembly.GetType(GameEventsTypeName);
            var attack = RequireRectTransform("AttackButton").GetComponent<Button>();
            var defend = RequireRectTransform("DefendButton").GetComponent<Button>();
            var flee = RequireRectTransform("FleeButton").GetComponent<Button>();
            var potion = RequireRectTransform("PotionButton").GetComponent<Button>();

            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "This older line must be discarded." });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "You used a healing potion and recovered 12 HP." });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "Ruin Wraith hits you for 6." });
            yield return null;

            Assert.That((int)GetPrivateField(battlePanel, "maxLogLines"), Is.EqualTo(2));
            Assert.That(logText.text.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)), Is.EqualTo(new[]
            {
                "You used a healing potion and recovered 12 HP.",
                "Ruin Wraith hits you for 6."
            }), "The battle surface must retain the newest two non-empty logical log lines.");

            AssertBattleButtonGeometry(attack, defend, flee, potion);
            AssertPersistentAction(attack, GameActionBridgeTypeName, "Attack");
            AssertPersistentAction(defend, GameActionBridgeTypeName, "Defend");
            AssertPersistentAction(flee, GameActionBridgeTypeName, "Flee");
            AssertPersistentAction(potion, GameActionBridgeTypeName, "UsePotion");
            AssertExplicitNavigation(attack, up: flee, down: flee, left: defend, right: defend);
            AssertExplicitNavigation(defend, up: potion, down: potion, left: attack, right: attack);
            AssertExplicitNavigation(flee, up: attack, down: attack, left: potion, right: potion);
            AssertExplicitNavigation(potion, up: defend, down: defend, left: flee, right: flee);

            AssertTopRaycastReaches(attack);
            AssertTopRaycastReaches(defend);
            AssertTopRaycastReaches(flee);
            AssertTopRaycastReaches(potion);

            EventSystem.current.SetSelectedGameObject(attack.gameObject);
            MoveSelection(MoveDirection.Right);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(defend.gameObject));
            MoveSelection(MoveDirection.Down);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(potion.gameObject));
            MoveSelection(MoveDirection.Left);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(flee.gameObject));
            MoveSelection(MoveDirection.Up);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(attack.gameObject));
        }

        [UnityTest]
        public IEnumerator P0_TitleAndCampButtonsKeepAccessibleGeometry()
        {
            yield return null;
            AssertPanelButtons(
                "TitlePanel",
                ("ContinueButton", GameActionBridgeTypeName, "ContinueGame"),
                ("New GameButton", GameActionBridgeTypeName, "StartNewGame"),
                ("QuitButton", GameActionBridgeTypeName, "Quit"));
            AssertPanelButtons(
                "CampActionMenu",
                ("HuntButton", GameActionBridgeTypeName, "StartHunt"),
                ("RestButton", GameActionBridgeTypeName, "Rest"),
                ("Craft TreasureButton", GameActionBridgeTypeName, "CraftTreasure"),
                ("EquipmentButton", EquipmentPanelControllerTypeName, "OpenEquipment"));
        }

        [UnityTest]
        public IEnumerator P0_EquipmentLayoutMeetsViewportTypographyGapAndBoundedRowContracts()
        {
            yield return null;
            var fixture = LoadEquipmentComparisonFixture();
            equipmentCatalogScope = CatalogFixtureScope.Install(
                FindType("ToilRelic.Unity.Core.EquipmentCatalog"),
                FindType("ToilRelic.Unity.Core.EquipmentDefinition"),
                FindType("ToilRelic.Unity.Core.EquipmentCategory"),
                fixture.definitions);

            try
            {
                var gameManager = RequireComponent(GameManagerTypeName);
                EnterCampState(gameManager);
                var player = GetPrivateField(gameManager, "player");
                Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(
                    player, new object[] { "long-name-armor" }), Is.True);
                var controller = RequireComponent(EquipmentPanelControllerTypeName);
                controller.GetType().GetMethod("OpenEquipment").Invoke(controller, null);
                var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
                controller.GetType().GetMethod("SelectSlot").Invoke(controller, new[] { Enum.Parse(slotType, "Armor") });
                yield return null;
                Canvas.ForceUpdateCanvases();

                var equipmentRect = RequireRectTransform("EquipmentPanel");
                var hudRect = RequireRectTransform("Hud");
                var statusRect = RequireRectTransform("GameStatus");
                foreach (var viewport in new[] { new Vector2(800f, 600f), WidescreenVirtualSize })
                {
                    var bounds = CalculateVirtualRect(equipmentRect, viewport);
                    var topRegionBottom = Mathf.Min(
                        CalculateVirtualRect(hudRect, viewport).yMin,
                        CalculateVirtualRect(statusRect, viewport).yMin);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(16f));
                    Assert.That(viewport.x - bounds.xMax, Is.GreaterThanOrEqualTo(16f));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(16f));
                    Assert.That(topRegionBottom - bounds.yMax, Is.GreaterThanOrEqualTo(MinimumTopRegionGap),
                        $"EquipmentPanel must remain separate from HUD/status at virtual {viewport.x}x{viewport.y}.");
                }

                var slotScroll = RequireRectTransform("SlotScrollView");
                var candidateScroll = RequireRectTransform("CandidateScrollView");
                var detailPanel = RequireRectTransform("EquipmentDetailPanel");
                AssertPositiveHorizontalGap(slotScroll, candidateScroll);
                AssertPositiveHorizontalGap(candidateScroll, detailPanel);

                var back = RequireRectTransform("BackButton");
                var equip = RequireRectTransform("EquipButton");
                var unequip = RequireRectTransform("UnequipButton");
                AssertPositiveHorizontalGap(back, equip);
                AssertPositiveHorizontalGap(equip, unequip);

                var slotRows = (Transform)GetPrivateField(controller, "slotRowsContainer");
                var candidateRows = (Transform)GetPrivateField(controller, "candidateRowsContainer");
                var grid = slotRows.GetComponent<GridLayoutGroup>();
                var vertical = candidateRows.GetComponent<VerticalLayoutGroup>();
                Assert.That(grid, Is.Not.Null);
                Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
                Assert.That(grid.constraintCount, Is.EqualTo(2));
                Assert.That(grid.cellSize.y, Is.GreaterThanOrEqualTo(44f));
                Assert.That(grid.spacing.x, Is.GreaterThan(0f));
                Assert.That(grid.spacing.y, Is.GreaterThan(0f));
                Assert.That(vertical, Is.Not.Null);
                Assert.That(vertical.spacing, Is.GreaterThan(0f));
                Assert.That(slotRows.GetComponent<ContentSizeFitter>(), Is.Not.Null);
                Assert.That(candidateRows.GetComponent<ContentSizeFitter>(), Is.Not.Null);
                Assert.That(slotRows.parent.GetComponent<Mask>(), Is.Not.Null);
                Assert.That(candidateRows.parent.GetComponent<Mask>(), Is.Not.Null);
                Assert.That(slotRows.parent.parent.GetComponent<ScrollRect>(), Is.Not.Null);
                Assert.That(candidateRows.parent.parent.GetComponent<ScrollRect>(), Is.Not.Null);

                var runtimeButtons = ((IEnumerable)GetPublicProperty(controller, "SlotButtons")).Cast<Button>()
                    .Concat(((IEnumerable)GetPublicProperty(controller, "CandidateButtons")).Cast<Button>())
                    .ToArray();
                Assert.That(runtimeButtons.Take(12), Has.Count.EqualTo(12));
                foreach (var button in runtimeButtons)
                {
                    Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f),
                        $"{button.name} must retain a 44 virtual-pixel pointer target.");
                }

                foreach (var fixedButton in new[] { back, equip, unequip })
                {
                    Assert.That(fixedButton.sizeDelta.y, Is.GreaterThanOrEqualTo(44f));
                }

                var longCandidate = ((IEnumerable)GetPublicProperty(controller, "CandidateButtons")).Cast<Button>().Single();
                var longLabel = longCandidate.GetComponentInChildren<Text>();
                Assert.That(longLabel.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
                Assert.That(longLabel.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
                Assert.That(longLabel.rectTransform.rect.width,
                    Is.LessThanOrEqualTo(candidateRows.GetComponent<RectTransform>().rect.width));
                AssertTextTypography(equipmentRect.GetComponentsInChildren<Text>(true));
            }
            finally
            {
                equipmentCatalogScope.Dispose();
                equipmentCatalogScope = null;
            }
        }

        [UnityTest]
        public IEnumerator P0_BootstrapAndCommittedSceneShareEquipmentObjectAndBindingContract()
        {
            yield return null;
            var bootstrapPath = Path.Combine(Application.dataPath, "Editor", "ToilRelicSceneBootstrap.cs");
            Assert.That(File.Exists(bootstrapPath), Is.True);
            var source = File.ReadAllText(bootstrapPath);
            var requiredSourceTokens = new[]
            {
                "RegenerateSceneAtPath(ScenePath)",
                "CreateStretchRoot(\"CampPanel\"",
                "CreatePanel(\"CampActionMenu\"",
                "CreatePanel(\"EquipmentPanel\"",
                "\"SlotRowsContainer\"",
                "\"CandidateRowsContainer\"",
                "FindProperty(\"equipmentEntryButton\")",
                "FindProperty(\"comparisonText\")",
                "equipmentController.OpenEquipment",
                "equipmentController.BackToCamp",
                "equipmentController.EquipSelected",
                "equipmentController.UnequipSelected"
            };
            foreach (var token in requiredSourceTokens)
            {
                Assert.That(source, Does.Contain(token), $"Bootstrap source is missing scene-contract token: {token}");
            }

            var controller = RequireComponent(EquipmentPanelControllerTypeName);
            Assert.That(controller.transform, Is.EqualTo(RequireRectTransform("CampPanel")));
            Assert.That(((GameObject)GetPrivateField(controller, "campMenuPanel")).name, Is.EqualTo("CampActionMenu"));
            Assert.That(((GameObject)GetPrivateField(controller, "equipmentPanel")).name, Is.EqualTo("EquipmentPanel"));
            AssertPersistentAction((Button)GetPrivateField(controller, "equipmentEntryButton"),
                EquipmentPanelControllerTypeName, "OpenEquipment");
            AssertPersistentAction((Button)GetPrivateField(controller, "backButton"),
                EquipmentPanelControllerTypeName, "BackToCamp");
            AssertPersistentAction((Button)GetPrivateField(controller, "equipButton"),
                EquipmentPanelControllerTypeName, "EquipSelected");
            AssertPersistentAction((Button)GetPrivateField(controller, "unequipButton"),
                EquipmentPanelControllerTypeName, "UnequipSelected");
        }

        [UnityTest]
        public IEnumerator P0_HudAndStatusMeetCompactTypographyContracts()
        {
            yield return null;
            var hudRect = RequireRectTransform("Hud");
            var statusRect = RequireRectTransform("GameStatus");
            var status = RequireComponent(GameStatusControllerTypeName);
            var saveStatusText = GetPrivateField(status, "saveStatusText") as Text;
            var hudTexts = hudRect.GetComponentsInChildren<Text>(true);
            var statusTexts = statusRect.GetComponentsInChildren<Text>(true);

            Assert.That(hudRect.sizeDelta.x, Is.LessThanOrEqualTo(344f));
            Assert.That(statusRect.sizeDelta.x, Is.LessThanOrEqualTo(400f));
            Assert.That(statusRect.sizeDelta.y, Is.EqualTo(120f));
            Assert.That(saveStatusText, Is.Not.Null, "The generated scene must wire the auxiliary save row.");
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.False,
                "The save row must be hidden before the first save result.");
            Assert.That(saveStatusText.fontSize, Is.EqualTo(16));
            Assert.That(saveStatusText.color, Is.EqualTo(Color.white));
            Assert.That(saveStatusText.alignment, Is.EqualTo(TextAnchor.MiddleRight));
            Assert.That(saveStatusText.resizeTextForBestFit, Is.False);
            Assert.That(saveStatusText.rectTransform.sizeDelta.y, Is.EqualTo(22f));
            Assert.That(saveStatusText.rectTransform.anchoredPosition.y, Is.EqualTo(-98f));
            AssertTextTypography(hudTexts);
            AssertTextTypography(statusTexts);
        }

        [UnityTest]
        public IEnumerator P0_StatusFitsThreeLogicalMessagesAtReadableSize()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var gameEventsType = gameManager.GetType().Assembly.GetType("ToilRelic.Unity.Core.GameEvents");
            RequireSaveStatusText(status);

            gameEventsType.GetMethod("RaiseBattleOutcome").Invoke(
                null,
                new object[] { "Win. Loot: junk +3, relic part +2, healing potion +1, EXP +20, Reward Weapon." });
            gameEventsType.GetMethod("RaiseLevelUp").Invoke(
                null,
                new object[] { "Level up! +1 -> Lv.2. HP fully restored." });
            RaiseSaveStatus(gameEventsType, "Failed");
            Canvas.ForceUpdateCanvases();

            Assert.That(messageText, Is.Not.Null);
            Assert.That(messageText.text.Split('\n'), Has.Length.EqualTo(3));
            Assert.That(messageText.fontSize, Is.GreaterThanOrEqualTo(16));
            Assert.That(messageText.resizeTextForBestFit, Is.False);
            Assert.That(messageText.preferredHeight,
                Is.LessThanOrEqualTo(messageText.rectTransform.rect.height + 0.01f),
                "The status message rectangle must fit outcome, level-up, and save-failure lines without clipping.");
        }

        [UnityTest]
        public IEnumerator P0_CampSaveSuccessPreservesActionMessage()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var saveStatusText = RequireSaveStatusText(status);
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            gameManager.GetType().GetMethod("Rest").Invoke(gameManager, null);
            yield return null;

            Assert.That(messageText.text, Is.EqualTo("You rest and recover to full HP."));
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.True);
            Assert.That(saveStatusText.text, Is.EqualTo("Save: Saved just now"));
        }

        [UnityTest]
        public IEnumerator P0_SaveFeedbackPersistsFailureAndClearsContextualSuccess()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var saveStatusText = RequireSaveStatusText(status);
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);
            var gameEventsType = gameManager.GetType().Assembly.GetType("ToilRelic.Unity.Core.GameEvents");

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "Crafting complete." });
            RaiseSaveStatus(gameEventsType, "Succeeded");
            Assert.That(messageText.text, Is.EqualTo("Crafting complete."));
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.True);
            Assert.That(saveStatusText.text, Is.EqualTo("Save: Saved just now"));

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Battle") });
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.False);
            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.False,
                "A contextual success must not reappear after leaving Camp.");

            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "You rest and recover to full HP." });
            RaiseSaveStatus(gameEventsType, "Failed");
            Assert.That(messageText.text, Is.EqualTo("You rest and recover to full HP.\nSave failed. Progress may not be saved."));
            Assert.That(saveStatusText.text, Is.EqualTo("Save: Failed"));
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.True);

            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "Choose an action." });
            Assert.That(messageText.text, Is.EqualTo("Choose an action.\nSave failed. Progress may not be saved."));
            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Battle") });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "A wild Mine Vermin appears." });
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.False);
            Assert.That(messageText.text, Is.EqualTo("A wild Mine Vermin appears.\nSave failed. Progress may not be saved."));

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.True);
            Assert.That(saveStatusText.text, Is.EqualTo("Save: Failed"));
            RaiseSaveStatus(gameEventsType, "Succeeded");
            Assert.That(messageText.text, Is.EqualTo("A wild Mine Vermin appears."));
            Assert.That(saveStatusText.text, Is.EqualTo("Save: Saved just now"));
        }

        [UnityTest]
        public IEnumerator P0_TerminalFailurePreservesOutcomeAndLevelUp()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var saveStatusText = RequireSaveStatusText(status);
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);
            var gameEventsType = gameManager.GetType().Assembly.GetType("ToilRelic.Unity.Core.GameEvents");

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            gameEventsType.GetMethod("RaiseBattleOutcome").Invoke(null, new object[] { "Win. Loot preserved." });
            gameEventsType.GetMethod("RaiseLevelUp").Invoke(null, new object[] { "Level up preserved." });
            RaiseSaveStatus(gameEventsType, "Failed");

            Assert.That(messageText.text, Is.EqualTo(
                "Win. Loot preserved.\nLevel up preserved.\nSave failed. Progress may not be saved."));
            Assert.That(saveStatusText.text, Is.EqualTo("Save: Failed"));
        }

        [UnityTest]
        public IEnumerator P0_CaptureLayoutEvidenceWhenRequested()
        {
            var evidenceDirectory = Environment.GetEnvironmentVariable("TOIL_RELIC_LAYOUT_EVIDENCE_DIR");
            if (string.IsNullOrWhiteSpace(evidenceDirectory))
            {
                Assert.Ignore("Set TOIL_RELIC_LAYOUT_EVIDENCE_DIR to capture viewport-specific layout evidence.");
            }

            Directory.CreateDirectory(evidenceDirectory);
            yield return CaptureStableScreenshot(evidenceDirectory, "title-1280x720.png", 1280, 720);
            yield return CaptureStableScreenshot(evidenceDirectory, "title-800x600.png", 800, 600);

            var gameManager = RequireComponent(GameManagerTypeName);
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);
            var gameEventsType = gameManager.GetType().Assembly.GetType("ToilRelic.Unity.Core.GameEvents");
            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "You rest and recover to full HP." });
            RaiseSaveStatus(gameEventsType, "Succeeded");
            yield return CaptureStableScreenshot(evidenceDirectory, "camp-success-1280x720.png", 1280, 720);
            yield return CaptureStableScreenshot(evidenceDirectory, "camp-success-800x600.png", 800, 600);

            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "Crafting complete." });
            RaiseSaveStatus(gameEventsType, "Failed");
            yield return CaptureStableScreenshot(evidenceDirectory, "camp-failure-1280x720.png", 1280, 720);
            yield return CaptureStableScreenshot(evidenceDirectory, "camp-failure-800x600.png", 800, 600);

            var fixture = LoadEquipmentComparisonFixture();
            equipmentCatalogScope = CatalogFixtureScope.Install(
                FindType("ToilRelic.Unity.Core.EquipmentCatalog"),
                FindType("ToilRelic.Unity.Core.EquipmentDefinition"),
                FindType("ToilRelic.Unity.Core.EquipmentCategory"),
                fixture.definitions);
            var equipmentController = RequireComponent(EquipmentPanelControllerTypeName);
            try
            {
                var player = GetPrivateField(gameManager, "player");
                var grantEquipment = player.GetType().GetMethod("GrantEquipment");
                Assert.That((bool)grantEquipment.Invoke(player, new object[] { "current-ring" }), Is.True);
                Assert.That((bool)grantEquipment.Invoke(player, new object[] { "all-stat-ring" }), Is.True);
                Assert.That((bool)grantEquipment.Invoke(player, new object[] { "long-name-armor" }), Is.True);
                var slotType = FindType("ToilRelic.Unity.Core.EquipmentSlot");
                equipmentController.GetType().GetMethod("OpenEquipment").Invoke(equipmentController, null);

                var slotButtons = ((IEnumerable)GetPublicProperty(equipmentController, "SlotButtons"))
                    .Cast<Button>()
                    .ToArray();
                EventSystem.current.SetSelectedGameObject(slotButtons[0].gameObject);
                for (var index = 1; index < slotButtons.Length; index++)
                {
                    var move = new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Down };
                    ExecuteEvents.Execute(
                        EventSystem.current.currentSelectedGameObject,
                        move,
                        ExecuteEvents.moveHandler);
                    yield return null;
                }

                var slotRows = (Transform)GetPrivateField(equipmentController, "slotRowsContainer");
                var slotScrollRect = slotRows.GetComponentInParent<ScrollRect>();
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(slotButtons[^1].gameObject));
                Canvas.ForceUpdateCanvases();
                AssertRectFullyInsideViewport(slotScrollRect.viewport, slotButtons[^1].GetComponent<RectTransform>());
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-final-slot-focus-1280x720.png", 1280, 720);
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-final-slot-focus-800x600.png", 800, 600);

                equipmentController.GetType().GetMethod("BackToCamp").Invoke(equipmentController, null);
                equipmentController.GetType().GetMethod("OpenEquipment").Invoke(equipmentController, null);
                yield return null;

                equipmentController.GetType().GetMethod("SelectSlot").Invoke(
                    equipmentController, new[] { Enum.Parse(slotType, "PrimaryWeapon") });
                equipmentController.GetType().GetMethod("SelectCandidate").Invoke(
                    equipmentController, new object[] { "starter-weapon" });
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-zero-stat-same-item-1280x720.png", 1280, 720);
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-zero-stat-same-item-800x600.png", 800, 600);

                equipmentController.GetType().GetMethod("SelectSlot").Invoke(
                    equipmentController, new[] { Enum.Parse(slotType, "Hat") });
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-empty-1280x720.png", 1280, 720);
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-empty-800x600.png", 800, 600);

                equipmentController.GetType().GetMethod("SelectSlot").Invoke(
                    equipmentController, new[] { Enum.Parse(slotType, "Armor") });
                equipmentController.GetType().GetMethod("SelectCandidate").Invoke(
                    equipmentController, new object[] { "long-name-armor" });
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-long-name-preview-1280x720.png", 1280, 720);
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-long-name-preview-800x600.png", 800, 600);

                equipmentController.GetType().GetMethod("SelectSlot").Invoke(
                    equipmentController, new[] { Enum.Parse(slotType, "Ring1") });
                equipmentController.GetType().GetMethod("SelectCandidate").Invoke(
                    equipmentController, new object[] { "current-ring" });
                equipmentController.GetType().GetMethod("EquipSelected").Invoke(equipmentController, null);
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-success-1280x720.png", 1280, 720);
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-success-800x600.png", 800, 600);

                var invalidSavePath = Path.Combine(
                    Application.temporaryCachePath, Guid.NewGuid().ToString("N"), "toil_relic_save.json");
                SetPrivateStaticField(fixtureSaveServiceType, "savePathOverride", invalidSavePath);
                equipmentController.GetType().GetMethod("SelectCandidate").Invoke(
                    equipmentController, new object[] { "all-stat-ring" });
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save write failed"));
                equipmentController.GetType().GetMethod("EquipSelected").Invoke(equipmentController, null);
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-save-failure-1280x720.png", 1280, 720);
                yield return CaptureStableScreenshot(
                    evidenceDirectory, "equipment-save-failure-800x600.png", 800, 600);
            }
            finally
            {
                SetPrivateStaticField(fixtureSaveServiceType, "savePathOverride", fixtureSavePath);
                equipmentController.GetType().GetMethod("BackToCamp").Invoke(equipmentController, null);
                equipmentCatalogScope.Dispose();
                equipmentCatalogScope = null;
            }

            var status = RequireComponent(GameStatusControllerTypeName);
            var battlePanel = RequireComponent(BattlePanelControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var saveStatusText = RequireSaveStatusText(status);
            var phaseText = GetPrivateField(battlePanel, "phaseText") as Text;
            var logText = GetPrivateField(battlePanel, "logText") as Text;

            yield return EnterBattle();
            gameEventsType.GetMethod("RaiseEnemyChanged").Invoke(null, new object[] { "Ruin Wraith", 18, 18 });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(
                null, new object[] { "You used a healing potion and recovered 12 HP." });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(
                null, new object[] { "Ruin Wraith hits you for 6." });
            yield return null;

            Assert.That(phaseText.text, Is.EqualTo("Your turn — choose an action."));
            Assert.That(logText.text, Is.EqualTo(
                "You used a healing potion and recovered 12 HP.\nRuin Wraith hits you for 6."));
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.False,
                "Battle evidence must hide the contextual SaveStatus row.");
            Assert.That(messageText.text, Is.EqualTo(
                "Ruin Wraith hits you for 6.\nSave failed. Progress may not be saved."));
            yield return CaptureStableScreenshot(evidenceDirectory, "battle-failure-1280x720.png", 1280, 720);
            yield return CaptureStableScreenshot(evidenceDirectory, "battle-failure-800x600.png", 800, 600);

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            RaiseSaveStatus(gameEventsType, "Succeeded");
            yield return EnterBattle();
            gameEventsType.GetMethod("RaiseEnemyChanged").Invoke(null, new object[] { "Ruin Wraith", 18, 18 });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(
                null, new object[] { "You used a healing potion and recovered 12 HP." });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(
                null, new object[] { "Ruin Wraith hits you for 6." });
            yield return null;

            Assert.That(phaseText.text, Is.EqualTo("Your turn — choose an action."));
            Assert.That(logText.text, Is.EqualTo(
                "You used a healing potion and recovered 12 HP.\nRuin Wraith hits you for 6."));
            Assert.That(saveStatusText.gameObject.activeInHierarchy, Is.False,
                "Battle evidence must hide the contextual SaveStatus row.");
            Assert.That(messageText.text, Is.EqualTo("Ruin Wraith hits you for 6."));
            yield return CaptureStableScreenshot(evidenceDirectory, "battle-normal-1280x720.png", 1280, 720);
            yield return CaptureStableScreenshot(evidenceDirectory, "battle-normal-800x600.png", 800, 600);
        }

        [UnityTest]
        public IEnumerator P0_BattleUiShowsStateAndDisablesActionsOutsidePlayerPhase()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            var battlePanel = RequireComponent(BattlePanelControllerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var stateText = GetPrivateField(status, "stateText") as Text;
            var messageText = GetPrivateField(status, "messageText") as Text;
            var enemyText = GetPrivateField(battlePanel, "enemyText") as Text;
            var phaseText = GetPrivateField(battlePanel, "phaseText") as Text;
            var attackButton = GetPrivateField(battlePanel, "attackButton") as Button;
            var defendButton = GetPrivateField(battlePanel, "defendButton") as Button;
            var fleeButton = GetPrivateField(battlePanel, "fleeButton") as Button;
            var potionButton = GetPrivateField(battlePanel, "potionButton") as Button;
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var phaseType = GetPrivateField(gameManager, "battlePhase").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);
            var changeBattlePhase = gameManager.GetType().GetMethod("ChangeBattlePhase", BindingFlags.Instance | BindingFlags.NonPublic);
            var gameEventsType = gameManager.GetType().Assembly.GetType("ToilRelic.Unity.Core.GameEvents");

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            gameManager.GetType().GetMethod("StartHunt").Invoke(gameManager, null);
            yield return null;

            Assert.That(stateText.text, Is.EqualTo("State: Battle"));
            Assert.That(enemyText.text, Does.StartWith("Enemy: "));
            Assert.That(phaseText.text, Does.Contain("Your turn"));
            Assert.That(attackButton.interactable, Is.True);
            Assert.That(defendButton.interactable, Is.True);
            Assert.That(fleeButton.interactable, Is.True);
            Assert.That(potionButton.interactable, Is.True);

            changeBattlePhase.Invoke(gameManager, new[] { Enum.Parse(phaseType, "EnemyAction") });
            yield return null;

            Assert.That(attackButton.interactable, Is.False);
            Assert.That(defendButton.interactable, Is.False);
            Assert.That(fleeButton.interactable, Is.False);
            Assert.That(potionButton.interactable, Is.False);

            changeBattlePhase.Invoke(gameManager, new[] { Enum.Parse(phaseType, "Resolving") });
            yield return null;

            Assert.That(attackButton.interactable, Is.False);
            Assert.That(defendButton.interactable, Is.False);
            Assert.That(fleeButton.interactable, Is.False);
            Assert.That(potionButton.interactable, Is.False);

            changeBattlePhase.Invoke(gameManager, new[] { Enum.Parse(phaseType, "PlayerAction") });
            yield return null;

            Assert.That(attackButton.interactable, Is.True);
            Assert.That(defendButton.interactable, Is.True);
            Assert.That(fleeButton.interactable, Is.True);
            Assert.That(potionButton.interactable, Is.True);

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            yield return null;

            Assert.That(enemyText.text, Is.EqualTo("Enemy: -"));
            Assert.That(phaseText.text, Is.Empty);
            Assert.That((GetPrivateField(battlePanel, "logText") as Text).text, Is.Empty);
            Assert.That(attackButton.interactable, Is.False);
            Assert.That(defendButton.interactable, Is.False);
            Assert.That(fleeButton.interactable, Is.False);
            Assert.That(potionButton.interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator P0_VictoryReturnsToCampWithVisibleOutcome()
        {
            yield return EnterBattle();
            var gameManager = RequireComponent(GameManagerTypeName);
            var battlePanel = RequireComponent(BattlePanelControllerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var enemy = GetPrivateField(gameManager, "currentEnemy");
            var messageText = GetPrivateField(status, "messageText") as Text;
            var attackButton = GetPrivateField(battlePanel, "attackButton") as Button;

            SetPrivateField(player, "level", 1);
            SetPrivateField(player, "experience", 19);
            enemy.GetType().GetMethod("TakeDamage").Invoke(enemy, new object[] { 999 });
            gameManager.GetType().GetMethod("Attack").Invoke(gameManager, null);
            yield return null;

            AssertTerminalCampState(gameManager, messageText, attackButton, "Win.");
            Assert.That(messageText.text, Does.StartWith("Win."));
            Assert.That(messageText.text, Does.Contain("\nLevel up!"));
        }

        [UnityTest]
        public IEnumerator P0_SaveFailurePreservesTerminalOutcome()
        {
            var gameManager = RequireComponent(GameManagerTypeName);
            var battlePanel = RequireComponent(BattlePanelControllerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var attackButton = GetPrivateField(battlePanel, "attackButton") as Button;
            var saveServiceType = fixtureSaveServiceType;
            var originalSavePath = GetPrivateStaticField(saveServiceType, "savePathOverride");

            yield return EnterBattle();
            var enemy = GetPrivateField(gameManager, "currentEnemy");
            try
            {
                var invalidSavePath = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString(), "toil_relic_save.json");
                SetPrivateStaticField(saveServiceType, "savePathOverride", invalidSavePath);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save write failed"));
                enemy.GetType().GetMethod("TakeDamage").Invoke(enemy, new object[] { 999 });
                gameManager.GetType().GetMethod("Attack").Invoke(gameManager, null);
            }
            finally
            {
                SetPrivateStaticField(saveServiceType, "savePathOverride", originalSavePath);
            }
            yield return null;

            AssertTerminalCampState(gameManager, messageText, attackButton, "Win.");
            Assert.That(messageText.text, Does.Contain("\nSave failed. Progress may not be saved."));
        }

        [UnityTest]
        public IEnumerator P0_RestSaveFailureRemainsVisible()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var saveServiceType = fixtureSaveServiceType;
            var originalSavePath = GetPrivateStaticField(saveServiceType, "savePathOverride");
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            try
            {
                var invalidSavePath = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString(), "toil_relic_save.json");
                SetPrivateStaticField(saveServiceType, "savePathOverride", invalidSavePath);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save write failed"));
                gameManager.GetType().GetMethod("Rest").Invoke(gameManager, null);
            }
            finally
            {
                SetPrivateStaticField(saveServiceType, "savePathOverride", originalSavePath);
            }
            yield return null;

            Assert.That(messageText.text, Is.EqualTo(
                "You rest and recover to full HP.\nSave failed. Progress may not be saved."));
        }

        [UnityTest]
        public IEnumerator P0_EquipSaveFailureRemainsVisible()
        {
            yield return null;
            var gameManager = RequireComponent(GameManagerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var saveServiceType = fixtureSaveServiceType;
            var originalSavePath = GetPrivateStaticField(saveServiceType, "savePathOverride");
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            var player = GetPrivateField(gameManager, "player");
            Assert.That((bool)player.GetType().GetMethod("GrantEquipment").Invoke(player, new object[] { "reward-weapon" }), Is.True);
            try
            {
                var invalidSavePath = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString(), "toil_relic_save.json");
                SetPrivateStaticField(saveServiceType, "savePathOverride", invalidSavePath);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save write failed"));
                gameManager.GetType().GetMethod("EquipRewardWeapon").Invoke(gameManager, null);
            }
            finally
            {
                SetPrivateStaticField(saveServiceType, "savePathOverride", originalSavePath);
            }
            yield return null;

            Assert.That(messageText.text, Is.EqualTo(
                "Equipped Reward Weapon.\nSave failed. Progress may not be saved."));
        }

        [UnityTest]
        public IEnumerator P0_DefeatReturnsToCampWithVisibleOutcome()
        {
            yield return EnterBattle();
            var gameManager = RequireComponent(GameManagerTypeName);
            var battlePanel = RequireComponent(BattlePanelControllerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var player = GetPrivateField(gameManager, "player");
            var lethalEnemy = CreateEnemyRuntime(maxHp: 100, attackMin: 30, attackMax: 30, expReward: 1);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var attackButton = GetPrivateField(battlePanel, "attackButton") as Button;

            SetPrivateField(gameManager, "currentEnemy", lethalEnemy);
            player.GetType().GetMethod("TakeDamage").Invoke(player, new object[] { 29 });
            gameManager.GetType().GetMethod("Defend").Invoke(gameManager, null);
            yield return null;

            AssertTerminalCampState(gameManager, messageText, attackButton, "collapsed");
        }

        [UnityTest]
        public IEnumerator P0_SuccessfulFleeReturnsToCampWithVisibleOutcome()
        {
            yield return EnterBattle();
            var gameManager = RequireComponent(GameManagerTypeName);
            var battlePanel = RequireComponent(BattlePanelControllerTypeName);
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var attackButton = GetPrivateField(battlePanel, "attackButton") as Button;

            var originalRandomState = UnityEngine.Random.state;
            try
            {
                var player = GetPrivateField(gameManager, "player");
                var healAll = player.GetType().GetMethod("HealAll");
                for (var seed = 0; seed < 20 && GetPrivateField(gameManager, "state").ToString() == "Battle"; seed++)
                {
                    healAll.Invoke(player, null);
                    UnityEngine.Random.InitState(seed);
                    gameManager.GetType().GetMethod("Flee").Invoke(gameManager, null);
                }
            }
            finally
            {
                UnityEngine.Random.state = originalRandomState;
            }
            yield return null;

            AssertTerminalCampState(gameManager, messageText, attackButton, "Escape successful.");
        }

        private static IEnumerator EnterBattle()
        {
            var gameManager = RequireComponent(GameManagerTypeName);
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);
            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
            gameManager.GetType().GetMethod("StartHunt").Invoke(gameManager, null);
            yield return null;
            Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Battle"));
        }

        private static void AssertTerminalCampState(Component gameManager, Text messageText, Button attackButton, string outcome)
        {
            Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Camp"));
            Assert.That(messageText.text, Does.Contain(outcome));
            Assert.That(attackButton.interactable, Is.False);
        }

        private static void AssertPanelFitsBelowTopRegion(RectTransform panelRect, float topRegionBottom)
        {
            var panelTop = CalculateVirtualRect(panelRect, WidescreenVirtualSize).yMax;
            Assert.That(topRegionBottom - panelTop, Is.GreaterThanOrEqualTo(MinimumTopRegionGap),
                $"{panelRect.name} must remain at least {MinimumTopRegionGap} virtual pixels below the HUD and status regions.");
        }

        private static void AssertPersistentAction(Button button, string targetTypeName, string methodName)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.onClick.GetPersistentEventCount(), Is.EqualTo(1),
                $"{button.name} must retain exactly one persistent action.");
            var target = button.onClick.GetPersistentTarget(0);
            Assert.That(target, Is.Not.Null, $"{button.name} must retain a persistent target.");
            Assert.That(target.GetType().FullName, Is.EqualTo(targetTypeName),
                $"{button.name} must target {targetTypeName}.");
            Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo(methodName),
                $"{button.name} must remain bound to {methodName}.");
        }

        private static void AssertExplicitVerticalCycle(IReadOnlyList<Button> buttons)
        {
            for (var index = 0; index < buttons.Count; index++)
            {
                var navigation = buttons[index].navigation;
                Assert.That(navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                Assert.That(navigation.selectOnUp,
                    Is.EqualTo(buttons[(index - 1 + buttons.Count) % buttons.Count]));
                Assert.That(navigation.selectOnDown, Is.EqualTo(buttons[(index + 1) % buttons.Count]));
            }
        }

        private static void AssertPositiveHorizontalGap(RectTransform left, RectTransform right)
        {
            Assert.That(left.parent, Is.EqualTo(right.parent),
                $"{left.name} and {right.name} must share a parent for local gap verification.");
            var leftEdge = left.anchoredPosition.x + (left.sizeDelta.x * (1f - left.pivot.x));
            var rightEdge = right.anchoredPosition.x - (right.sizeDelta.x * right.pivot.x);
            Assert.That(rightEdge - leftEdge, Is.GreaterThan(0f),
                $"{left.name} and {right.name} must retain a positive visible gap.");
        }

        private static void AssertRectFullyInsideViewport(RectTransform viewport, RectTransform target)
        {
            Assert.That(viewport, Is.Not.Null);
            Assert.That(target, Is.Not.Null);
            var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
            const float tolerance = 0.5f;
            Assert.That(targetBounds.min.y, Is.GreaterThanOrEqualTo(viewport.rect.yMin - tolerance),
                $"{target.name} must remain fully above the viewport's lower edge.");
            Assert.That(targetBounds.max.y, Is.LessThanOrEqualTo(viewport.rect.yMax + tolerance),
                $"{target.name} must remain fully below the viewport's upper edge.");
        }

        private static void AssertPanelButtons(
            string panelName,
            params (string ButtonName, string TargetTypeName, string MethodName)[] expectedButtons)
        {
            var panel = RequireRectTransform(panelName);
            var buttons = expectedButtons
                .Select(expected => panel.Find(expected.ButtonName)?.GetComponent<Button>())
                .ToArray();

            Assert.That(buttons, Has.None.Null, $"{panelName} must retain all expected buttons.");
            for (var index = 0; index < buttons.Length; index++)
            {
                var button = buttons[index];
                var rect = button.GetComponent<RectTransform>();
                Assert.That(rect.sizeDelta.y, Is.GreaterThanOrEqualTo(44f),
                    $"{button.name} must remain at least 44 pixels high.");
                AssertPersistentAction(
                    button,
                    expectedButtons[index].TargetTypeName,
                    expectedButtons[index].MethodName);
            }

            var ordered = buttons
                .Select(button => button.GetComponent<RectTransform>())
                .OrderByDescending(button => button.anchoredPosition.y)
                .ToArray();
            for (var index = 0; index < ordered.Length - 1; index++)
            {
                var upperBottom = ordered[index].anchoredPosition.y - (ordered[index].sizeDelta.y * 0.5f);
                var lowerTop = ordered[index + 1].anchoredPosition.y + (ordered[index + 1].sizeDelta.y * 0.5f);
                Assert.That(upperBottom - lowerTop, Is.GreaterThan(0f),
                    $"{ordered[index].name} and {ordered[index + 1].name} must have a visible vertical gap.");
            }
        }

        private static void AssertTextTypography(IEnumerable<Text> texts)
        {
            foreach (var text in texts)
            {
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(16),
                    $"{text.name} must use a readable font size.");
                Assert.That(text.resizeTextForBestFit, Is.False,
                    $"{text.name} must not shrink below the typography contract.");
            }
        }

        private static void AssertVirtualViewportMargins(RectTransform rect, Vector2 viewport, float minimumMargin)
        {
            var bounds = CalculateVirtualRect(rect, viewport);
            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(minimumMargin),
                $"{rect.name} must keep a {minimumMargin}-pixel left margin at {viewport.x}x{viewport.y}.");
            Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(minimumMargin),
                $"{rect.name} must keep a {minimumMargin}-pixel bottom margin at {viewport.x}x{viewport.y}.");
            Assert.That(viewport.x - bounds.xMax, Is.GreaterThanOrEqualTo(minimumMargin),
                $"{rect.name} must keep a {minimumMargin}-pixel right margin at {viewport.x}x{viewport.y}.");
            Assert.That(viewport.y - bounds.yMax, Is.GreaterThanOrEqualTo(minimumMargin),
                $"{rect.name} must keep a {minimumMargin}-pixel top margin at {viewport.x}x{viewport.y}.");
        }

        private static void AssertTextContract(Text text, bool requireSingleVisualLine)
        {
            Assert.That(text, Is.Not.Null);
            Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(16), $"{text.name} must remain readable.");
            Assert.That(text.resizeTextForBestFit, Is.False, $"{text.name} must not shrink with Best Fit.");
            Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 0.01f),
                $"{text.name} preferred height must fit its rectangle.");
            if (requireSingleVisualLine)
            {
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 0.01f),
                    $"{text.name} preferred width must fit its one-line rectangle.");
            }
        }

        private static Rect CalculateRectInAncestor(RectTransform ancestor, RectTransform rect)
        {
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(ancestor, rect);
            return Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
        }

        private static Rect CalculateGeneratedGlyphBounds(Text text, RectTransform canvasRect)
        {
            Assert.That(text, Is.Not.Null);
            Assert.That(text.text, Is.Not.Empty, $"{text.name} must have text before generated-glyph measurement.");
            var settings = text.GetGenerationSettings(text.rectTransform.rect.size);
            var generator = text.cachedTextGenerator;
            Assert.That(generator.Populate(text.text, settings), Is.True,
                $"{text.name} must populate its TextGenerator before glyph measurement.");
            var vertices = generator.verts;
            var vertexCount = Mathf.Max(0, generator.vertexCount - 4);
            Assert.That(vertexCount, Is.GreaterThan(0), $"{text.name} must generate visible glyph vertices.");

            var first = ToAncestorPoint(text.rectTransform, canvasRect, vertices[0].position / text.pixelsPerUnit);
            var xMin = first.x;
            var xMax = first.x;
            var yMin = first.y;
            var yMax = first.y;
            for (var index = 1; index < vertexCount; index++)
            {
                var point = ToAncestorPoint(
                    text.rectTransform,
                    canvasRect,
                    vertices[index].position / text.pixelsPerUnit);
                xMin = Mathf.Min(xMin, point.x);
                xMax = Mathf.Max(xMax, point.x);
                yMin = Mathf.Min(yMin, point.y);
                yMax = Mathf.Max(yMax, point.y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Vector3 ToAncestorPoint(RectTransform source, RectTransform ancestor, Vector3 localPoint)
        {
            return ancestor.InverseTransformPoint(source.TransformPoint(localPoint));
        }

        private static void AssertGeneratedGlyphsInsideRect(Text text, RectTransform canvasRect)
        {
            var glyphs = CalculateGeneratedGlyphBounds(text, canvasRect);
            AssertGeneratedGlyphsInsideRect(text, canvasRect, glyphs);
        }

        private static void AssertGeneratedGlyphsInsideRect(Text text, RectTransform canvasRect, Rect glyphs)
        {
            var rect = CalculateRectInAncestor(canvasRect, text.rectTransform);
            const float tolerance = 1f;
            Assert.That(glyphs.xMin, Is.GreaterThanOrEqualTo(rect.xMin - tolerance),
                $"{text.name} generated glyphs must stay inside the left text edge.");
            Assert.That(glyphs.xMax, Is.LessThanOrEqualTo(rect.xMax + tolerance),
                $"{text.name} generated glyphs must stay inside the right text edge.");
            Assert.That(glyphs.yMin, Is.GreaterThanOrEqualTo(rect.yMin - tolerance),
                $"{text.name} generated glyphs must stay inside the lower text edge.");
            Assert.That(glyphs.yMax, Is.LessThanOrEqualTo(rect.yMax + tolerance),
                $"{text.name} generated glyphs must stay inside the upper text edge.");
        }

        private static void AssertVerticalRectOrder(Rect upper, Rect lower, string upperName, string lowerName)
        {
            Assert.That(upper.yMin - lower.yMax, Is.GreaterThanOrEqualTo(0f),
                $"{upperName} and {lowerName} rectangles must not intersect.");
        }

        private static void AssertBattleButtonGeometry(Button attack, Button defend, Button flee, Button potion)
        {
            var buttons = new[] { attack, defend, flee, potion };
            foreach (var button in buttons)
            {
                Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f),
                    $"{button.name} must remain at least 44 pixels high.");
            }

            var attackRect = attack.GetComponent<RectTransform>();
            var defendRect = defend.GetComponent<RectTransform>();
            var fleeRect = flee.GetComponent<RectTransform>();
            var potionRect = potion.GetComponent<RectTransform>();
            Assert.That(attackRect.anchoredPosition.y, Is.EqualTo(defendRect.anchoredPosition.y).Within(0.01f));
            Assert.That(fleeRect.anchoredPosition.y, Is.EqualTo(potionRect.anchoredPosition.y).Within(0.01f));
            Assert.That(attackRect.anchoredPosition.y, Is.GreaterThan(fleeRect.anchoredPosition.y));
            Assert.That(attackRect.anchoredPosition.x, Is.EqualTo(fleeRect.anchoredPosition.x).Within(0.01f));
            Assert.That(defendRect.anchoredPosition.x, Is.EqualTo(potionRect.anchoredPosition.x).Within(0.01f));
            Assert.That(attackRect.anchoredPosition.x, Is.LessThan(defendRect.anchoredPosition.x));
            AssertPositiveHorizontalGap(attackRect, defendRect);
            AssertPositiveHorizontalGap(fleeRect, potionRect);
            var firstRowBottom = attackRect.anchoredPosition.y
                - (attackRect.sizeDelta.y * attackRect.pivot.y);
            var secondRowTop = fleeRect.anchoredPosition.y
                + (fleeRect.sizeDelta.y * (1f - fleeRect.pivot.y));
            Assert.That(firstRowBottom - secondRowTop, Is.GreaterThan(0f),
                "The Battle action rows must retain a positive vertical gap.");
        }

        private static void AssertExplicitNavigation(
            Button button,
            Button up,
            Button down,
            Button left,
            Button right)
        {
            var navigation = button.navigation;
            Assert.That(navigation.mode, Is.EqualTo(Navigation.Mode.Explicit),
                $"{button.name} must use explicit spatial navigation.");
            Assert.That(navigation.selectOnUp, Is.EqualTo(up));
            Assert.That(navigation.selectOnDown, Is.EqualTo(down));
            Assert.That(navigation.selectOnLeft, Is.EqualTo(left));
            Assert.That(navigation.selectOnRight, Is.EqualTo(right));
        }

        private static void MoveSelection(MoveDirection direction)
        {
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            var selected = eventSystem.currentSelectedGameObject;
            Assert.That(selected, Is.Not.Null, $"A selected object is required before moving {direction}.");
            var move = new AxisEventData(eventSystem) { moveDir = direction };
            Assert.That(ExecuteEvents.Execute(selected, move, ExecuteEvents.moveHandler), Is.True,
                $"{selected.name} must handle {direction} navigation.");
        }

        private static Rect CalculateVirtualRect(RectTransform rect, Vector2 canvasSize)
        {
            Assert.That(rect.anchorMin, Is.EqualTo(rect.anchorMax),
                $"{rect.name} must use fixed anchors for deterministic virtual-layout checks.");
            var anchorPosition = Vector2.Scale(rect.anchorMin, canvasSize);
            var lowerLeft = anchorPosition + rect.anchoredPosition - Vector2.Scale(rect.pivot, rect.sizeDelta);
            return new Rect(lowerLeft, rect.sizeDelta);
        }

        private static RectTransform RequireRectTransform(string objectName)
        {
            var rect = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate != null && candidate.name == objectName);
            Assert.That(rect, Is.Not.Null, $"Required RectTransform is missing: {objectName}.");
            return rect;
        }

        private static IEnumerator ReloadSampleScene()
        {
            var operation = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            yield return operation;
            yield return null;
        }

        private IEnumerator AssertHistoricalSaveLoadsAndNormalizes(string original, int expectedLevel, int expectedExperience)
        {
            File.WriteAllText(fixtureSavePath, original);

            yield return ReloadSampleScene();

            var gameManager = RequireComponent(GameManagerTypeName);
            var titleMenu = RequireComponent(TitleMenuControllerTypeName);
            var continueButton = GetPrivateField(titleMenu, "continueButton") as Button;
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;
            var loadedPlayer = GetPrivateField(gameManager, "player");
            var tryGetPrimaryWeapon = loadedPlayer.GetType().GetMethod("TryGetPrimaryWeapon");
            var primaryWeaponArguments = new object[] { null };
            var itemType = loadedPlayer.GetType().Assembly.GetType("ToilRelic.Unity.Core.ItemType");
            var getAmount = loadedPlayer.GetType().GetMethod("GetAmount");

            Assert.That(gameManager.GetType().GetProperty("CurrentSaveLoadStatus").GetValue(gameManager).ToString(), Is.EqualTo("Loaded"));
            Assert.That((bool)gameManager.GetType().GetProperty("HasSavedGame").GetValue(gameManager), Is.True);
            Assert.That(continueButton.interactable, Is.True);
            Assert.That(messageText.text, Is.EqualTo("Save found. Continue or start a new game."));
            Assert.That((int)loadedPlayer.GetType().GetProperty("Hp").GetValue(loadedPlayer), Is.EqualTo(18));
            Assert.That((int)loadedPlayer.GetType().GetProperty("Level").GetValue(loadedPlayer), Is.EqualTo(expectedLevel));
            Assert.That((int)loadedPlayer.GetType().GetProperty("Experience").GetValue(loadedPlayer), Is.EqualTo(expectedExperience));
            Assert.That((int)loadedPlayer.GetType().GetProperty("TreasureCount").GetValue(loadedPlayer), Is.EqualTo(4));
            Assert.That((int)getAmount.Invoke(loadedPlayer, new[] { Enum.Parse(itemType, "Junk") }), Is.EqualTo(2));
            Assert.That((int)getAmount.Invoke(loadedPlayer, new[] { Enum.Parse(itemType, "Treasure") }), Is.EqualTo(4));
            Assert.That((bool)tryGetPrimaryWeapon.Invoke(loadedPlayer, primaryWeaponArguments), Is.True);
            Assert.That(primaryWeaponArguments[0], Is.Not.Null);
            Assert.That(File.ReadAllText(fixtureSavePath), Is.EqualTo(original));

            gameManager.GetType().GetMethod("ContinueGame").Invoke(gameManager, null);
            yield return null;
            Assert.That(GetPrivateField(gameManager, "state").ToString(), Is.EqualTo("Camp"));
            Assert.That(File.ReadAllText(fixtureSavePath), Is.EqualTo(original));
        }

        private void CollectUnreadableWithoutMutationFailures(string caseName, string original, ICollection<string> failures)
        {
            File.WriteAllText(fixtureSavePath, original);

            var loadResult = fixtureSaveServiceType.GetMethod("Load").Invoke(null, null);
            var status = loadResult.GetType().GetProperty("Status").GetValue(loadResult).ToString();
            var player = loadResult.GetType().GetProperty("Player").GetValue(loadResult);

            if (status != "Unreadable") failures.Add($"{caseName} returned {status}");
            if (player != null) failures.Add($"{caseName} exposed player data");
            if (File.ReadAllText(fixtureSavePath) != original) failures.Add($"{caseName} mutated source bytes");
        }

        private static void EnterCampState(Component gameManager)
        {
            var stateType = GetPrivateField(gameManager, "state").GetType();
            var changeState = gameManager.GetType().GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(changeState, Is.Not.Null);
            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Camp") });
        }

        private Component CreateEquipmentPanelController(Component gameManager)
        {
            var controllerType = FindType(EquipmentPanelControllerTypeName);
            Assert.That(controllerType, Is.Not.Null, "EquipmentPanelController must exist for the Camp-local equipment mode.");
            var root = new GameObject("EquipmentPanelControllerFixture", typeof(RectTransform));
            fixtureObjects.Add(root);
            root.SetActive(false);

            var campMenu = new GameObject("CampMenuFixture", typeof(RectTransform));
            campMenu.transform.SetParent(root.transform, false);
            var equipmentPanel = new GameObject("EquipmentPanelFixture", typeof(RectTransform));
            equipmentPanel.transform.SetParent(root.transform, false);
            var entryButton = CreateUiButton(campMenu.transform, "EquipmentEntry", "Equipment");
            var backButton = CreateUiButton(equipmentPanel.transform, "Back", "Back");
            var equipButton = CreateUiButton(equipmentPanel.transform, "Equip", "Equip");
            var unequipButton = CreateUiButton(equipmentPanel.transform, "Unequip", "Unequip");
            var slotRows = new GameObject("SlotRows", typeof(RectTransform)).transform;
            slotRows.SetParent(equipmentPanel.transform, false);
            var candidateRows = new GameObject("CandidateRows", typeof(RectTransform)).transform;
            candidateRows.SetParent(equipmentPanel.transform, false);
            var comparisonText = CreateUiText(equipmentPanel.transform, "ComparisonText");
            var totalsText = CreateUiText(equipmentPanel.transform, "TotalsText");
            var validationText = CreateUiText(equipmentPanel.transform, "ValidationText");

            equipmentPanel.SetActive(false);
            var controller = root.AddComponent(controllerType);
            SetPrivateField(controller, "gameManager", gameManager);
            SetPrivateField(controller, "campMenuPanel", campMenu);
            SetPrivateField(controller, "equipmentPanel", equipmentPanel);
            SetPrivateField(controller, "equipmentEntryButton", entryButton);
            SetPrivateField(controller, "backButton", backButton);
            SetPrivateField(controller, "equipButton", equipButton);
            SetPrivateField(controller, "unequipButton", unequipButton);
            SetPrivateField(controller, "slotRowsContainer", slotRows);
            SetPrivateField(controller, "candidateRowsContainer", candidateRows);
            SetPrivateField(controller, "comparisonText", comparisonText);
            SetPrivateField(controller, "totalsText", totalsText);
            SetPrivateField(controller, "validationText", validationText);
            root.SetActive(true);
            return controller;
        }

        private static Button CreateUiButton(Transform parent, string name, string label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var text = CreateUiText(buttonObject.transform, "Label");
            text.text = label;
            return buttonObject.GetComponent<Button>();
        }

        private static Text CreateUiText(Transform parent, string name)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static string GetButtonLabel(Button button)
        {
            var label = button.GetComponentInChildren<Text>();
            Assert.That(label, Is.Not.Null, $"Button '{button.name}' must expose a uGUI Text label.");
            return label.text;
        }

        private static Type FindType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName, throwOnError: false))
                .FirstOrDefault(type => type != null);
        }

        private static EquipmentComparisonContractFixture LoadEquipmentComparisonFixture()
        {
            var path = Path.Combine(Application.dataPath, "Tests", "Fixtures", "EquipmentComparisonContracts.json");
            Assert.That(File.Exists(path), Is.True, $"Canonical comparison fixture is missing: {path}");
            var fixture = JsonUtility.FromJson<EquipmentComparisonContractFixture>(File.ReadAllText(path));
            Assert.That(fixture, Is.Not.Null, "Canonical comparison fixture could not be parsed.");
            return fixture;
        }

        private static object CreateEquipmentFixturePlayer(
            Type playerType,
            Type slotType,
            IEnumerable<string> ownedIds,
            IEnumerable<EquippedFixtureEntry> equipped)
        {
            var player = Activator.CreateInstance(playerType);
            playerType.GetMethod("InitDefaults").Invoke(player, null);
            var owned = (IEnumerable)GetPublicProperty(player, "OwnedEquipmentIds");
            var ownedSet = new HashSet<string>(owned.Cast<string>(), StringComparer.Ordinal);
            var grant = playerType.GetMethod("GrantEquipment");
            var equip = playerType.GetMethod("Equip");

            foreach (var id in ownedIds)
            {
                if (ownedSet.Add(id))
                {
                    Assert.That(grant.Invoke(player, new object[] { id }), Is.EqualTo(true),
                        $"Fixture equipment should be grantable: {id}");
                }
            }

            foreach (var entry in equipped)
            {
                Assert.That(equip.Invoke(player, new[] { Enum.Parse(slotType, entry.slot), entry.equipmentId }), Is.EqualTo(true),
                    $"Fixture equipment should be equippable: {entry.equipmentId} -> {entry.slot}");
            }

            return player;
        }

        private static object GetPublicProperty(object instance, string name)
        {
            Assert.That(instance, Is.Not.Null, $"Instance is required to read public property '{name}'.");
            var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected public property '{name}' was not found on {instance.GetType().FullName}.");
            return property.GetValue(instance);
        }

        private static string GetEquipmentId(object equipment)
        {
            return equipment == null ? null : (string)GetPublicProperty(equipment, "Id");
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static string FormatDelta(object delta)
        {
            return $"{GetPublicProperty(delta, "Stat")}:{GetPublicProperty(delta, "CurrentValue")}:{GetPublicProperty(delta, "CandidateValue")}:{GetPublicProperty(delta, "Delta")}";
        }

        private static int GetCatalogCount(Type catalogType)
        {
            Assert.That(catalogType, Is.Not.Null, "EquipmentCatalog type is required.");
            var all = catalogType.GetProperty("All", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) as IEnumerable;
            Assert.That(all, Is.Not.Null, "EquipmentCatalog.All must expose read-only enumeration.");
            return all.Cast<object>().Count();
        }

        private static Text RequireSaveStatusText(Component status)
        {
            var saveStatusText = GetPrivateField(status, "saveStatusText") as Text;
            Assert.That(saveStatusText, Is.Not.Null, "The generated scene must wire the auxiliary save row.");
            return saveStatusText;
        }

        private void AssertUnreadableTitleState(string expectedSaveBytes)
        {
            var gameManager = RequireComponent(GameManagerTypeName);
            var titleMenu = RequireComponent(TitleMenuControllerTypeName);
            var continueButton = GetPrivateField(titleMenu, "continueButton") as Button;
            var status = RequireComponent(GameStatusControllerTypeName);
            var messageText = GetPrivateField(status, "messageText") as Text;

            Assert.That((bool)gameManager.GetType().GetProperty("HasSavedGame").GetValue(gameManager), Is.False);
            Assert.That(gameManager.GetType().GetProperty("CurrentSaveLoadStatus").GetValue(gameManager).ToString(), Is.EqualTo("Unreadable"));
            Assert.That(continueButton.interactable, Is.False);
            Assert.That(messageText.text, Is.EqualTo("Save could not be read. Start New Game to replace it."));
            Assert.That(File.ReadAllText(fixtureSavePath), Is.EqualTo(expectedSaveBytes));
        }

        private void CleanupSaveFixtureState()
        {
            foreach (var fixtureObject in fixtureObjects)
            {
                if (fixtureObject != null)
                {
                    UnityEngine.Object.Destroy(fixtureObject);
                }
            }
            fixtureObjects.Clear();

            if (fixtureOverrideInstalled)
            {
                SetPrivateStaticField(fixtureSaveServiceType, "savePathOverride", previousSavePathOverride);
                Assert.That(GetPrivateStaticField(fixtureSaveServiceType, "savePathOverride"), Is.EqualTo(previousSavePathOverride));
                fixtureOverrideInstalled = false;
            }

            if (!string.IsNullOrEmpty(fixtureSaveDirectory) && Directory.Exists(fixtureSaveDirectory))
            {
                Directory.Delete(fixtureSaveDirectory, recursive: true);
            }

            fixtureSaveDirectory = null;
            fixtureSavePath = null;
        }

        private static void RaiseSaveStatus(Type gameEventsType, string status)
        {
            var saveFeedbackType = gameEventsType.Assembly.GetType("ToilRelic.Unity.Core.SaveFeedbackStatus");
            Assert.That(saveFeedbackType, Is.Not.Null, "Semantic save feedback status must exist.");
            var raiseMethod = gameEventsType.GetMethod("RaiseSaveStatusChanged");
            Assert.That(raiseMethod, Is.Not.Null, "GameEvents must publish semantic save feedback.");
            raiseMethod.Invoke(null, new[] { Enum.Parse(saveFeedbackType, status) });
        }

        private static Button RequireVisibleActionButton(string buttonName, string expectedMethodName)
        {
            var button = RequireRectTransform(buttonName).GetComponent<Button>();
            Assert.That(button, Is.Not.Null, $"Visible action button is missing: {buttonName}.");
            Assert.That(button.gameObject.activeInHierarchy, Is.True, $"{buttonName} must be active before pointer dispatch.");
            Assert.That(button.interactable, Is.True, $"{buttonName} must be interactable before pointer dispatch.");
            Assert.That(button.onClick.GetPersistentEventCount(), Is.EqualTo(1),
                $"{buttonName} must retain exactly one persistent action.");

            var target = button.onClick.GetPersistentTarget(0);
            Assert.That(target, Is.Not.Null, $"{buttonName} must retain a persistent GameActionBridge target.");
            Assert.That(target.GetType().FullName, Is.EqualTo(GameActionBridgeTypeName),
                $"{buttonName} must target {GameActionBridgeTypeName}.");
            Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo(expectedMethodName),
                $"{buttonName} must remain bound to {expectedMethodName}.");
            return button;
        }

        private static void AssertTopRaycastReaches(Button button)
        {
            Assert.That(button.gameObject.activeInHierarchy, Is.True);
            Assert.That(button.interactable, Is.True);
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            var canvas = button.GetComponentInParent<Canvas>();
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            Assert.That(raycaster, Is.Not.Null);
            var position = RectTransformUtility.WorldToScreenPoint(
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center));
            var eventData = new PointerEventData(eventSystem) { position = position };
            var results = new List<RaycastResult>();
            raycaster.Raycast(eventData, results);
            var rect = button.GetComponent<RectTransform>();
            Assert.That(
                results,
                Is.Not.Empty,
                $"{button.name} must be reachable by the scene GraphicRaycaster. " +
                $"screen={Screen.width}x{Screen.height}, position={position}, rect={rect.rect}, " +
                $"canvasRect={canvas.GetComponent<RectTransform>().rect}, " +
                $"graphicRaycast={button.targetGraphic != null && button.targetGraphic.raycastTarget}.");
            Assert.That(
                results[0].gameObject == button.gameObject || results[0].gameObject.transform.IsChildOf(button.transform),
                Is.True,
                $"{button.name} must be the top pointer hit, but {results[0].gameObject.name} was above it.");
        }

        private static void ExecutePointerClick(Button button)
        {
            Assert.That(button.gameObject.activeInHierarchy, Is.True, $"{button.name} must be active before pointer dispatch.");
            Assert.That(button.interactable, Is.True, $"{button.name} must be interactable before pointer dispatch.");
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            var canvas = button.GetComponentInParent<Canvas>();
            var rect = button.GetComponent<RectTransform>();
            var eventData = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    rect.TransformPoint(rect.rect.center))
            };
            var handledBy = ExecuteEvents.ExecuteHierarchy(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);
            Assert.That(handledBy, Is.Not.Null, $"{button.name} did not handle the pointer-click event.");
        }

        private static void ClickVisibleActionButton(string buttonName, string expectedMethodName)
        {
            var button = RequireVisibleActionButton(buttonName, expectedMethodName);
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null, $"{buttonName} requires an active EventSystem for pointer dispatch.");
            Assert.That(eventSystem.gameObject.activeInHierarchy, Is.True,
                $"{buttonName} requires the scene EventSystem to be active.");

            var eventData = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left
            };
            var handled = ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);
            Assert.That(handled, Is.True, $"{buttonName} did not handle the pointer-click event.");
        }

        private static ReflectedStringEventRecorder ObserveStringGameEvent(Component gameManager, string eventName)
        {
            var gameEventsType = gameManager.GetType().Assembly.GetType(GameEventsTypeName);
            Assert.That(gameEventsType, Is.Not.Null, "GameEvents must be available for semantic event observation.");
            return new ReflectedStringEventRecorder(gameEventsType, eventName);
        }

        private static void AddItem(object player, string itemName, int amount)
        {
            var playerType = player.GetType();
            var itemType = playerType.Assembly.GetType("ToilRelic.Unity.Core.ItemType");
            var add = playerType.GetMethod("Add");
            Assert.That(itemType, Is.Not.Null, "ItemType must be available for inventory setup.");
            Assert.That(add, Is.Not.Null, "PlayerState.Add must be available for inventory setup.");
            add.Invoke(player, new object[] { Enum.Parse(itemType, itemName), amount });
        }

        private static int GetItemAmount(object player, string itemName)
        {
            var playerType = player.GetType();
            var itemType = playerType.Assembly.GetType("ToilRelic.Unity.Core.ItemType");
            var getAmount = playerType.GetMethod("GetAmount");
            Assert.That(itemType, Is.Not.Null, "ItemType must be available for inventory assertions.");
            Assert.That(getAmount, Is.Not.Null, "PlayerState.GetAmount must be available for inventory assertions.");
            return (int)getAmount.Invoke(player, new[] { Enum.Parse(itemType, itemName) });
        }

        private static RandomStateScope PreserveRandomState()
        {
            return new RandomStateScope();
        }

        private static int FindFirstFailingFleeSeed(Component gameManager, int maximumSeedExclusive = 10000)
        {
            var combatSystemType = gameManager.GetType().Assembly.GetType(CombatSystemTypeName);
            Assert.That(combatSystemType, Is.Not.Null, "CombatSystem must be available for deterministic flee preparation.");
            var tryFlee = combatSystemType.GetMethod("TryFlee", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(tryFlee, Is.Not.Null, "CombatSystem.TryFlee must be available for deterministic flee preparation.");
            var combatSystem = Activator.CreateInstance(combatSystemType);
            using var randomState = PreserveRandomState();

            for (var seed = 0; seed < maximumSeedExclusive; seed++)
            {
                UnityEngine.Random.InitState(seed);
                if (!(bool)tryFlee.Invoke(combatSystem, null))
                {
                    return seed;
                }
            }

            Assert.Fail($"No failing first flee roll was found below seed {maximumSeedExclusive}.");
            return 0;
        }

        private object InstallHarmlessDurableEnemy(Component gameManager, string displayName)
        {
            var enemy = CreateEnemyRuntime(displayName, maxHp: 100, attackMin: 1, attackMax: 1, expReward: 1);
            SetPrivateField(gameManager, "currentEnemy", enemy);
            var publishEnemy = gameManager.GetType().GetMethod("PublishEnemy", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(publishEnemy, Is.Not.Null, "GameManager.PublishEnemy must exist for deterministic enemy setup.");
            publishEnemy.Invoke(gameManager, null);
            return enemy;
        }

        private static IEnumerator CaptureStableScreenshot(string evidenceDirectory, string fileName, int width, int height)
        {
            yield return CaptureScreenshot(evidenceDirectory, fileName, width, height);
            yield return CaptureScreenshot(evidenceDirectory, fileName, width, height);
        }

        private static IEnumerator CaptureScreenshot(string evidenceDirectory, string fileName, int width, int height)
        {
            for (var frame = 0; frame < 10; frame++)
            {
                yield return null;
            }

            var path = Path.Combine(evidenceDirectory, fileName);
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            var originalRenderMode = canvas.renderMode;
            var originalCamera = canvas.worldCamera;
            var cameraObject = new GameObject("LayoutEvidenceCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            var renderTexture = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousActiveTexture = RenderTexture.active;

            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
                camera.targetTexture = renderTexture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(camera.pixelWidth, Is.EqualTo(width), "Capture camera width must match the requested viewport.");
                Assert.That(camera.pixelHeight, Is.EqualTo(height), "Capture camera height must match the requested viewport.");

                camera.Render();
                yield return null;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = originalRenderMode;
                canvas.worldCamera = originalCamera;
                RenderTexture.active = previousActiveTexture;
                camera.targetTexture = null;
                UnityEngine.Object.Destroy(renderTexture);
                UnityEngine.Object.Destroy(texture);
                UnityEngine.Object.Destroy(cameraObject);
            }

            Assert.That(File.Exists(path), Is.True, $"Expected screenshot was not written: {path}");
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(0), $"Screenshot must not be empty: {path}");
        }

        private object CreateEnemyRuntime(int maxHp, int attackMin, int attackMax, int expReward)
        {
            return CreateEnemyRuntime("P0 Lethal Enemy", maxHp, attackMin, attackMax, expReward);
        }

        private object CreateEnemyRuntime(
            string displayName,
            int maxHp,
            int attackMin,
            int attackMax,
            int expReward)
        {
            var assembly = RequireComponent(GameManagerTypeName).GetType().Assembly;
            var enemyDataType = assembly.GetType("ToilRelic.Unity.Data.EnemyData");
            var enemyRuntimeType = assembly.GetType("ToilRelic.Unity.Systems.EnemyRuntime");
            var enemyData = ScriptableObject.CreateInstance(enemyDataType);
            fixtureObjects.Add(enemyData);
            enemyDataType.GetField("displayName").SetValue(enemyData, displayName);
            enemyDataType.GetField("maxHp").SetValue(enemyData, maxHp);
            enemyDataType.GetField("attackMin").SetValue(enemyData, attackMin);
            enemyDataType.GetField("attackMax").SetValue(enemyData, attackMax);
            enemyDataType.GetField("expReward").SetValue(enemyData, expReward);
            return Activator.CreateInstance(enemyRuntimeType, new object[] { enemyData });
        }

        private static Component RequireComponent(string typeName)
        {
            var component = FindComponent(typeName);
            Assert.That(component, Is.Not.Null, $"Required scene component is missing: {typeName}.");
            return component;
        }

        private static Component FindComponent(string typeName)
        {
            return UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(component => component != null && component.GetType().FullName == typeName);
        }

        private static object GetPrivateField(object instance, string name)
        {
            var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{name}' was not found on {instance.GetType().FullName}.");
            return field.GetValue(instance);
        }

        private static void SetPrivateField(object instance, string name, object value)
        {
            var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{name}' was not found on {instance.GetType().FullName}.");
            field.SetValue(instance, value);
        }

        private static object GetPrivateStaticField(Type type, string name)
        {
            var field = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private static field '{name}' was not found on {type.FullName}.");
            return field.GetValue(null);
        }

        private static void SetPrivateStaticField(Type type, string name, object value)
        {
            var field = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private static field '{name}' was not found on {type.FullName}.");
            field.SetValue(null, value);
        }
    }
}
