using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
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
        private static readonly Vector2 WidescreenVirtualSize = new Vector2(800f, 450f);
        private const float MinimumTopRegionGap = 16f;
        private const string SaveServiceTypeName = "ToilRelic.Unity.Save.SaveService";
        private Type fixtureSaveServiceType;
        private object previousSavePathOverride;
        private string fixtureSaveDirectory;
        private string fixtureSavePath;
        private bool fixtureOverrideInstalled;

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
        public IEnumerator P0_TitleAndCampLayoutsFitBelowTopRegionsAtWidescreenFloor()
        {
            yield return null;
            var hudRect = RequireRectTransform("Hud");
            var statusRect = RequireRectTransform("GameStatus");
            var titleRect = RequireRectTransform("TitlePanel");
            var campRect = RequireRectTransform("CampPanel");
            var topRegionBottom = Mathf.Min(
                CalculateVirtualRect(hudRect, WidescreenVirtualSize).yMin,
                CalculateVirtualRect(statusRect, WidescreenVirtualSize).yMin);

            AssertPanelFitsBelowTopRegion(titleRect, topRegionBottom);
            AssertPanelFitsBelowTopRegion(campRect, topRegionBottom);
            Assert.That(titleRect.sizeDelta, Is.EqualTo(campRect.sizeDelta),
                "TitlePanel and CampPanel must share the same compact three-action geometry.");
        }

        [UnityTest]
        public IEnumerator P0_BattlePanelFitsBelowVisibleStatusAtWidescreenFloor()
        {
            yield return null;
            var statusRect = RequireRectTransform("GameStatus");
            var messageRect = RequireRectTransform("MessageText");
            var battleRect = RequireRectTransform("BattlePanel");
            var statusBounds = CalculateVirtualRect(statusRect, WidescreenVirtualSize);
            var visibleStatusBottom = statusBounds.yMax + messageRect.anchoredPosition.y - messageRect.sizeDelta.y;
            var battleTop = CalculateVirtualRect(battleRect, WidescreenVirtualSize).yMax;

            Assert.That(visibleStatusBottom - battleTop, Is.GreaterThanOrEqualTo(MinimumTopRegionGap),
                "BattlePanel must remain below the visible state and message rows when the save row is hidden.");
        }

        [UnityTest]
        public IEnumerator P0_TitleAndCampButtonsKeepAccessibleGeometry()
        {
            yield return null;
            AssertPanelButtons(
                "TitlePanel",
                ("ContinueButton", "ContinueGame"),
                ("New GameButton", "StartNewGame"),
                ("QuitButton", "Quit"));
            AssertPanelButtons(
                "CampPanel",
                ("HuntButton", "StartHunt"),
                ("RestButton", "Rest"),
                ("Craft TreasureButton", "CraftTreasure"));
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

            changeState.Invoke(gameManager, new[] { Enum.Parse(stateType, "Battle") });
            gameEventsType.GetMethod("RaiseBattleLog").Invoke(null, new object[] { "A wild Mine Vermin appears." });
            yield return CaptureStableScreenshot(evidenceDirectory, "battle-failure-1280x720.png", 1280, 720);
            yield return CaptureStableScreenshot(evidenceDirectory, "battle-failure-800x600.png", 800, 600);
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
            try
            {
                var invalidSavePath = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString(), "toil_relic_save.json");
                SetPrivateStaticField(saveServiceType, "savePathOverride", invalidSavePath);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save write failed"));
                gameManager.GetType().GetMethod("EquipStarterWeapon").Invoke(gameManager, null);
            }
            finally
            {
                SetPrivateStaticField(saveServiceType, "savePathOverride", originalSavePath);
            }
            yield return null;

            Assert.That(messageText.text, Is.EqualTo(
                "Equipped Starter Weapon.\nSave failed. Progress may not be saved."));
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

        private static void AssertPanelButtons(
            string panelName,
            params (string ButtonName, string MethodName)[] expectedButtons)
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
                Assert.That(button.onClick.GetPersistentEventCount(), Is.EqualTo(1),
                    $"{button.name} must retain exactly one persistent action.");
                Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo(expectedButtons[index].MethodName),
                    $"{button.name} must remain bound to {expectedButtons[index].MethodName}.");
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

        private static Type FindType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName, throwOnError: false))
                .FirstOrDefault(type => type != null);
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

        private static object CreateEnemyRuntime(int maxHp, int attackMin, int attackMax, int expReward)
        {
            var assembly = RequireComponent(GameManagerTypeName).GetType().Assembly;
            var enemyDataType = assembly.GetType("ToilRelic.Unity.Data.EnemyData");
            var enemyRuntimeType = assembly.GetType("ToilRelic.Unity.Systems.EnemyRuntime");
            var enemyData = ScriptableObject.CreateInstance(enemyDataType);
            enemyDataType.GetField("displayName").SetValue(enemyData, "P0 Lethal Enemy");
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
