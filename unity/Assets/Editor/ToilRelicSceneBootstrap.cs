using System.IO;
using ToilRelic.Unity.Core;
using ToilRelic.Unity.Data;
using ToilRelic.Unity.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ToilRelic.Unity.Editor
{
    public static class ToilRelicSceneBootstrap
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string DataDirectory = "Assets/ScriptableObjects";
        private const string EnemyDatabasePath = DataDirectory + "/EnemyDatabase_Main.asset";
        private const string DropTablePath = DataDirectory + "/DropTable_Default.asset";
        private const float ScreenMargin = 16f;
        private const float HudWidth = 344f;
        private const float HudHeight = 94f;
        private const float StatusWidth = 400f;
        private const float StatusHeight = 120f;
        private const float TopTextHeight = 22f;
        private const float StatusMessageHeight = 72f;
        private const float TopTextStep = 24f;
        private const float SaveStatusPositionY = -98f;
        private const int TopTextFontSize = 16;
        private static readonly Vector2 TitleMenuPanelPosition = new Vector2(0f, -25f);
        private static readonly Vector2 TitleMenuPanelSize = new Vector2(280f, 196f);
        private static readonly Vector2 CampMenuPanelPosition = new Vector2(0f, -42f);
        private static readonly Vector2 CampMenuPanelSize = new Vector2(280f, 224f);
        private static readonly Vector2 EquipmentPanelPosition = new Vector2(0f, -68f);
        private static readonly Vector2 EquipmentPanelSize = new Vector2(768f, 282f);
        private static readonly Vector2 MenuButtonSize = new Vector2(220f, 44f);
        private static readonly Vector2 EquipmentActionButtonSize = new Vector2(156f, 44f);
        private static readonly Vector2 BattlePanelPosition = new Vector2(180f, -61f);
        private static readonly Vector2 BattlePanelSize = new Vector2(320f, 316f);

        [MenuItem("Tools/Toil Relic/Regenerate Sample Scene")]
        public static void ConfigureSampleScene()
        {
            RegenerateSceneAtPath(ScenePath);
        }

        private static void RegenerateSceneAtPath(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath) ||
                !scenePath.StartsWith("Assets/", System.StringComparison.Ordinal) ||
                !string.Equals(Path.GetExtension(scenePath), ".unity", System.StringComparison.OrdinalIgnoreCase))
            {
                throw new System.ArgumentException(
                    "Scene regeneration requires a project-relative Assets path ending in .unity.",
                    nameof(scenePath));
            }

            var sceneDirectory = Path.GetDirectoryName(scenePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(sceneDirectory))
            {
                throw new InvalidDataException($"Could not resolve the scene directory for {scenePath}.");
            }

            EnsureFolder(sceneDirectory);
            EnsureFolder(DataDirectory);
            var enemyDatabase = CreateEnemyDatabase();
            var dropTable = LoadOrCreate<DropTableData>(DropTablePath);
            AssetDatabase.SaveAssets();
            if (dropTable == null)
            {
                throw new InvalidDataException($"Could not load required drop table at {DropTablePath}.");
            }

            var originalActiveScene = SceneManager.GetActiveScene();
            var targetScene = SceneManager.GetSceneByPath(scenePath);
            var closeTargetScene = false;
            var seededTargetScene = false;

            try
            {
                if (!targetScene.IsValid() || !targetScene.isLoaded)
                {
                    seededTargetScene = EnsureSceneAssetExists(scenePath);
                    targetScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    closeTargetScene = true;
                }

                if (seededTargetScene)
                {
                    foreach (var root in targetScene.GetRootGameObjects())
                    {
                        Object.DestroyImmediate(root);
                    }
                }

                if (!SceneManager.SetActiveScene(targetScene))
                {
                    throw new InvalidDataException($"Could not activate scene {scenePath} for regeneration.");
                }

                ConfigureActiveScene(enemyDatabase, dropTable);
                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, scenePath))
                {
                    throw new IOException($"Could not save regenerated scene at {scenePath}.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(originalActiveScene);
                }

                if (closeTargetScene && targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, removeScene: true);
                }
            }
        }

        private static bool EnsureSceneAssetExists(string scenePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidDataException("Could not resolve the Unity project root.");
            var absoluteScenePath = Path.Combine(
                projectRoot,
                scenePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absoluteScenePath))
            {
                return false;
            }

            if (string.Equals(scenePath, ScenePath, System.StringComparison.Ordinal) ||
                !AssetDatabase.CopyAsset(ScenePath, scenePath))
            {
                throw new IOException($"Could not create temporary scene asset at {scenePath}.");
            }

            AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceSynchronousImport);
            return true;
        }

        private static void ConfigureActiveScene(EnemyDatabase enemyDatabase, DropTableData dropTable)
        {
            if (enemyDatabase == null)
            {
                throw new System.ArgumentNullException(nameof(enemyDatabase));
            }

            if (dropTable == null)
            {
                throw new System.ArgumentNullException(nameof(dropTable));
            }

            RemoveExistingToilRelicObjects();

            var gameManagerObject = new GameObject("GameManager");
            var gameManager = gameManagerObject.AddComponent<GameManager>();
            var gameManagerProperties = new SerializedObject(gameManager);
            gameManagerProperties.FindProperty("enemyDatabase").objectReferenceValue = enemyDatabase;
            gameManagerProperties.FindProperty("dropTable").objectReferenceValue = dropTable;
            gameManagerProperties.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameManager);

            var canvas = CreateCanvas();
            var titlePanel = CreatePanel("TitlePanel", canvas.transform, TitleMenuPanelPosition, TitleMenuPanelSize);
            var campPanel = CreateStretchRoot("CampPanel", canvas.transform);
            var campActionMenu = CreatePanel("CampActionMenu", campPanel.transform, CampMenuPanelPosition, CampMenuPanelSize);
            var equipmentPanel = CreatePanel("EquipmentPanel", campPanel.transform, EquipmentPanelPosition, EquipmentPanelSize);
            var battlePanel = CreatePanel("BattlePanel", canvas.transform, BattlePanelPosition, BattlePanelSize);
            var hud = CreateHud(canvas.transform);
            var status = CreateStatus(canvas.transform);
            var actions = new GameObject("UIActions");
            var bridge = actions.AddComponent<GameActionBridge>();
            var bridgeProperties = new SerializedObject(bridge);
            bridgeProperties.FindProperty("gameManager").objectReferenceValue = gameManager;
            bridgeProperties.ApplyModifiedPropertiesWithoutUndo();

            var equipmentController = campPanel.AddComponent<EquipmentPanelController>();
            var continueButton = CreateButton("Continue", titlePanel.transform, 52f, bridge.ContinueGame, MenuButtonSize);
            CreateButton("New Game", titlePanel.transform, 0f, bridge.StartNewGame, MenuButtonSize);
            CreateButton("Quit", titlePanel.transform, -52f, bridge.Quit, MenuButtonSize);
            var huntButton = CreateButton("Hunt", campActionMenu.transform, 78f, bridge.StartHunt, MenuButtonSize);
            var restButton = CreateButton("Rest", campActionMenu.transform, 26f, bridge.Rest, MenuButtonSize);
            var craftButton = CreateButton("Craft Treasure", campActionMenu.transform, -26f, bridge.CraftTreasure, MenuButtonSize);
            var equipmentEntryButton = CreateButton(
                "Equipment", campActionMenu.transform, -78f, equipmentController.OpenEquipment, MenuButtonSize);
            SetExplicitVerticalNavigation(huntButton, restButton, craftButton, equipmentEntryButton);

            var equipmentTitle = CreatePanelText(
                "EquipmentTitleText", equipmentPanel.transform, 124f, EquipmentPanelSize.x - 24f, 24f);
            equipmentTitle.text = "Equipment";
            equipmentTitle.fontStyle = FontStyle.Bold;
            equipmentTitle.fontSize = 20;
            var slotRowsContainer = CreateScrollArea(
                "SlotScrollView", "SlotRowsContainer", equipmentPanel.transform,
                new Vector2(-222f, 12f), new Vector2(304f, 192f), useTwoColumnGrid: true);
            var candidateRowsContainer = CreateScrollArea(
                "CandidateScrollView", "CandidateRowsContainer", equipmentPanel.transform,
                new Vector2(28f, 12f), new Vector2(188f, 192f), useTwoColumnGrid: false);
            var detailPanel = CreatePanel(
                "EquipmentDetailPanel", equipmentPanel.transform, new Vector2(252f, 12f), new Vector2(240f, 192f));
            var comparisonText = CreateDetailText(
                "ComparisonText", detailPanel.transform, new Vector2(0f, 34f), new Vector2(224f, 112f));
            comparisonText.lineSpacing = 0.78f;
            var totalsText = CreateDetailText(
                "TotalsText", detailPanel.transform, new Vector2(0f, -42f), new Vector2(224f, 38f));
            var validationText = CreateDetailText(
                "ValidationText", detailPanel.transform, new Vector2(0f, -79f), new Vector2(224f, 34f));
            validationText.lineSpacing = 0.8f;
            validationText.color = new Color(1f, 0.76f, 0.42f, 1f);
            var backButton = CreateButton(
                "Back", equipmentPanel.transform, new Vector2(-174f, -113f),
                equipmentController.BackToCamp, EquipmentActionButtonSize);
            var equipButton = CreateButton(
                "Equip", equipmentPanel.transform, new Vector2(0f, -113f),
                equipmentController.EquipSelected, EquipmentActionButtonSize);
            var unequipButton = CreateButton(
                "Unequip", equipmentPanel.transform, new Vector2(174f, -113f),
                equipmentController.UnequipSelected, EquipmentActionButtonSize);
            SetExplicitVerticalNavigation(backButton, equipButton, unequipButton);

            var equipmentProperties = new SerializedObject(equipmentController);
            equipmentProperties.FindProperty("gameManager").objectReferenceValue = gameManager;
            equipmentProperties.FindProperty("campMenuPanel").objectReferenceValue = campActionMenu;
            equipmentProperties.FindProperty("equipmentPanel").objectReferenceValue = equipmentPanel;
            equipmentProperties.FindProperty("equipmentEntryButton").objectReferenceValue = equipmentEntryButton;
            equipmentProperties.FindProperty("backButton").objectReferenceValue = backButton;
            equipmentProperties.FindProperty("equipButton").objectReferenceValue = equipButton;
            equipmentProperties.FindProperty("unequipButton").objectReferenceValue = unequipButton;
            equipmentProperties.FindProperty("slotRowsContainer").objectReferenceValue = slotRowsContainer;
            equipmentProperties.FindProperty("candidateRowsContainer").objectReferenceValue = candidateRowsContainer;
            equipmentProperties.FindProperty("comparisonText").objectReferenceValue = comparisonText;
            equipmentProperties.FindProperty("totalsText").objectReferenceValue = totalsText;
            equipmentProperties.FindProperty("validationText").objectReferenceValue = validationText;
            equipmentProperties.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(equipmentController);
            equipmentPanel.SetActive(false);
            CreatePanelText("EnemyText", battlePanel.transform, 130f, 280f, 24f);
            CreatePanelText("PhaseText", battlePanel.transform, 102f, 280f, 24f);
            CreatePanelText("BattleLogText", battlePanel.transform, 70f, 280f, 50f);
            var attackButton = CreateButton("Attack", battlePanel.transform, 12f, bridge.Attack, MenuButtonSize);
            var defendButton = CreateButton("Defend", battlePanel.transform, -36f, bridge.Defend, MenuButtonSize);
            var fleeButton = CreateButton("Flee", battlePanel.transform, -84f, bridge.Flee, MenuButtonSize);
            var potionButton = CreateButton("Potion", battlePanel.transform, -132f, bridge.UsePotion, MenuButtonSize);

            var stateController = canvas.gameObject.AddComponent<StatePanelController>();
            var stateProperties = new SerializedObject(stateController);
            stateProperties.FindProperty("campPanel").objectReferenceValue = campPanel;
            stateProperties.FindProperty("battlePanel").objectReferenceValue = battlePanel;
            stateProperties.FindProperty("titlePanel").objectReferenceValue = titlePanel;
            stateProperties.ApplyModifiedPropertiesWithoutUndo();

            var titleMenuController = titlePanel.AddComponent<TitleMenuController>();
            var titleMenuProperties = new SerializedObject(titleMenuController);
            titleMenuProperties.FindProperty("gameManager").objectReferenceValue = gameManager;
            titleMenuProperties.FindProperty("continueButton").objectReferenceValue = continueButton;
            titleMenuProperties.ApplyModifiedPropertiesWithoutUndo();

            var hudController = hud.AddComponent<HudController>();
            var hudProperties = new SerializedObject(hudController);
            hudProperties.FindProperty("hpText").objectReferenceValue = hud.transform.Find("HpText").GetComponent<Text>();
            hudProperties.FindProperty("levelText").objectReferenceValue = hud.transform.Find("LevelText").GetComponent<Text>();
            hudProperties.FindProperty("invText").objectReferenceValue = hud.transform.Find("InventoryText").GetComponent<Text>();
            hudProperties.FindProperty("equipmentText").objectReferenceValue = hud.transform.Find("EquipmentText").GetComponent<Text>();
            hudProperties.ApplyModifiedPropertiesWithoutUndo();

            var statusController = status.AddComponent<GameStatusController>();
            var statusProperties = new SerializedObject(statusController);
            statusProperties.FindProperty("stateText").objectReferenceValue = status.transform.Find("StateText").GetComponent<Text>();
            statusProperties.FindProperty("messageText").objectReferenceValue = status.transform.Find("MessageText").GetComponent<Text>();
            statusProperties.FindProperty("saveStatusText").objectReferenceValue = status.transform.Find("SaveStatusText").GetComponent<Text>();
            statusProperties.ApplyModifiedPropertiesWithoutUndo();

            var battleController = battlePanel.AddComponent<BattlePanelController>();
            var battleProperties = new SerializedObject(battleController);
            battleProperties.FindProperty("gameManager").objectReferenceValue = gameManager;
            battleProperties.FindProperty("enemyText").objectReferenceValue = battlePanel.transform.Find("EnemyText").GetComponent<Text>();
            battleProperties.FindProperty("phaseText").objectReferenceValue = battlePanel.transform.Find("PhaseText").GetComponent<Text>();
            battleProperties.FindProperty("logText").objectReferenceValue = battlePanel.transform.Find("BattleLogText").GetComponent<Text>();
            battleProperties.FindProperty("attackButton").objectReferenceValue = attackButton;
            battleProperties.FindProperty("defendButton").objectReferenceValue = defendButton;
            battleProperties.FindProperty("fleeButton").objectReferenceValue = fleeButton;
            battleProperties.FindProperty("potionButton").objectReferenceValue = potionButton;
            battleProperties.ApplyModifiedPropertiesWithoutUndo();

            if (!ActiveSceneContainsEventSystem())
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static bool ActiveSceneContainsEventSystem()
        {
            var activeScene = SceneManager.GetActiveScene();
            foreach (var root in activeScene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<EventSystem>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static EnemyDatabase CreateEnemyDatabase()
        {
            var database = LoadOrCreate<EnemyDatabase>(EnemyDatabasePath);
            var enemies = new[]
            {
                CreateEnemy("MineVermin", "Mine Vermin", 10, 2, 4, 10),
                CreateEnemy("RustGolem", "Rust Golem", 14, 3, 5, 14),
                CreateEnemy("RuinWraith", "Ruin Wraith", 18, 4, 6, 20)
            };
            var properties = new SerializedObject(database);
            var list = properties.FindProperty("enemies");
            list.arraySize = enemies.Length;
            for (var index = 0; index < enemies.Length; index++)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = enemies[index];
            }

            properties.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            return database;
        }

        private static EnemyData CreateEnemy(string fileName, string displayName, int maxHp, int attackMin, int attackMax, int expReward)
        {
            var path = DataDirectory + "/" + fileName + ".asset";
            var enemy = LoadOrCreate<EnemyData>(path);
            enemy.displayName = displayName;
            enemy.maxHp = maxHp;
            enemy.attackMin = attackMin;
            enemy.attackMax = attackMax;
            enemy.expReward = expReward;
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.matchWidthOrHeight = 0f;
            return canvas;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = panel.GetComponent<Image>();
            image.color = new Color(0.08f, 0.12f, 0.18f, 0.94f);
            image.raycastTarget = false;
            return panel;
        }

        private static GameObject CreateStretchRoot(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return root;
        }

        private static Transform CreateScrollArea(
            string name,
            string contentName,
            Transform parent,
            Vector2 position,
            Vector2 size,
            bool useTwoColumnGrid)
        {
            var scrollObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(parent, false);
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = position;
            scrollRectTransform.sizeDelta = size;
            var scrollBackground = scrollObject.GetComponent<Image>();
            scrollBackground.color = new Color(0.04f, 0.07f, 0.11f, 0.96f);
            scrollBackground.raycastTarget = false;

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(4f, 4f);
            viewport.offsetMax = new Vector2(-20f, -4f);
            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.025f);
            viewportImage.raycastTarget = true;
            viewportObject.GetComponent<Mask>().showMaskGraphic = true;

            var contentObject = new GameObject(contentName, typeof(RectTransform), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (useTwoColumnGrid)
            {
                var grid = contentObject.AddComponent<GridLayoutGroup>();
                grid.padding = new RectOffset(4, 4, 4, 4);
                grid.spacing = new Vector2(6f, 4f);
                grid.cellSize = new Vector2(133f, 44f);
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
            }
            else
            {
                var vertical = contentObject.AddComponent<VerticalLayoutGroup>();
                vertical.padding = new RectOffset(4, 4, 4, 4);
                vertical.spacing = 4f;
                vertical.childAlignment = TextAnchor.UpperLeft;
                vertical.childControlWidth = true;
                vertical.childControlHeight = true;
                vertical.childForceExpandWidth = true;
                vertical.childForceExpandHeight = false;
            }

            var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(scrollObject.transform, false);
            var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-16f, 4f);
            scrollbarRect.offsetMax = new Vector2(-4f, -4f);
            scrollbarObject.GetComponent<Image>().color = new Color(0.1f, 0.14f, 0.2f, 1f);

            var slidingAreaObject = new GameObject("Sliding Area", typeof(RectTransform));
            slidingAreaObject.transform.SetParent(scrollbarObject.transform, false);
            var slidingArea = slidingAreaObject.GetComponent<RectTransform>();
            slidingArea.anchorMin = Vector2.zero;
            slidingArea.anchorMax = Vector2.one;
            slidingArea.offsetMin = new Vector2(2f, 2f);
            slidingArea.offsetMax = new Vector2(-2f, -2f);

            var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(slidingAreaObject.transform, false);
            var handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            var handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(0.36f, 0.55f, 0.72f, 1f);

            var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = 4f;
            return content;
        }

        private static GameObject CreateHud(Transform parent)
        {
            var hud = new GameObject("Hud", typeof(RectTransform));
            hud.transform.SetParent(parent, false);
            var rect = hud.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(ScreenMargin, -ScreenMargin);
            rect.sizeDelta = new Vector2(HudWidth, HudHeight);

            CreateHudText("HpText", hud.transform, 0f);
            CreateHudText("LevelText", hud.transform, -TopTextStep);
            CreateHudText("InventoryText", hud.transform, -TopTextStep * 2f);
            CreateHudText("EquipmentText", hud.transform, -TopTextStep * 3f);
            return hud;
        }

        private static GameObject CreateStatus(Transform parent)
        {
            var status = new GameObject("GameStatus", typeof(RectTransform));
            status.transform.SetParent(parent, false);
            var rect = status.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-ScreenMargin, -ScreenMargin);
            rect.sizeDelta = new Vector2(StatusWidth, StatusHeight);

            CreateStatusText("StateText", status.transform, 0f, TopTextHeight);
            CreateStatusText("MessageText", status.transform, -TopTextStep, StatusMessageHeight);
            CreateStatusText("SaveStatusText", status.transform, SaveStatusPositionY, TopTextHeight);
            return status;
        }

        private static Text CreateHudText(string name, Transform parent, float y)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(HudWidth, TopTextHeight);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = TopTextFontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        private static Text CreateStatusText(string name, Transform parent, float y, float height)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(StatusWidth, height);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = TopTextFontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleRight;
            return text;
        }

        private static Text CreatePanelText(string name, Transform parent, float y, float width, float height)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(width, height);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        private static Text CreateDetailText(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button CreateButton(
            string label,
            Transform parent,
            float y,
            UnityEngine.Events.UnityAction action,
            Vector2? size = null)
        {
            return CreateButton(label, parent, new Vector2(0f, y), action, size);
        }

        private static Button CreateButton(
            string label,
            Transform parent,
            Vector2 position,
            UnityEngine.Events.UnityAction action,
            Vector2? size = null)
        {
            var buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size ?? new Vector2(220f, 48f);
            buttonObject.GetComponent<Image>().color = new Color(0.22f, 0.42f, 0.62f, 1f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            UnityEventTools.AddPersistentListener(button.onClick, action);

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.raycastTarget = false;
            return button;
        }

        private static void SetExplicitVerticalNavigation(params Button[] buttons)
        {
            for (var index = 0; index < buttons.Length; index++)
            {
                var button = buttons[index];
                var previous = buttons[(index - 1 + buttons.Length) % buttons.Length];
                var next = buttons[(index + 1) % buttons.Length];
                button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = previous,
                    selectOnDown = next,
                    selectOnLeft = previous,
                    selectOnRight = next
                };
            }
        }

        private static void RemoveExistingToilRelicObjects()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name != "Main Camera")
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
