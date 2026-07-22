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
        private static readonly Vector2 MenuPanelPosition = new Vector2(0f, -25f);
        private static readonly Vector2 MenuPanelSize = new Vector2(280f, 196f);
        private static readonly Vector2 MenuButtonSize = new Vector2(220f, 44f);
        private static readonly Vector2 BattlePanelPosition = new Vector2(180f, -61f);
        private static readonly Vector2 BattlePanelSize = new Vector2(320f, 316f);
        private static readonly Vector2 BattleButtonSize = new Vector2(220f, 44f);

        public static void ConfigureSampleScene()
        {
            EnsureFolder(DataDirectory);
            var enemyDatabase = CreateEnemyDatabase();
            LoadOrCreate<DropTableData>(DropTablePath);
            AssetDatabase.SaveAssets();
            var dropTable = AssetDatabase.LoadAssetAtPath<DropTableData>(DropTablePath);
            if (dropTable == null)
            {
                throw new InvalidDataException($"Could not load required drop table at {DropTablePath}.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveExistingToilRelicObjects();

            var gameManagerObject = new GameObject("GameManager");
            var gameManager = gameManagerObject.AddComponent<GameManager>();
            var gameManagerProperties = new SerializedObject(gameManager);
            gameManagerProperties.FindProperty("enemyDatabase").objectReferenceValue = enemyDatabase;
            gameManagerProperties.FindProperty("dropTable").objectReferenceValue = AssetDatabase.LoadMainAssetAtPath(DropTablePath);
            gameManagerProperties.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameManager);

            var canvas = CreateCanvas();
            var titlePanel = CreatePanel("TitlePanel", canvas.transform, MenuPanelPosition, MenuPanelSize);
            var campPanel = CreatePanel("CampPanel", canvas.transform, MenuPanelPosition, MenuPanelSize);
            var battlePanel = CreatePanel("BattlePanel", canvas.transform, BattlePanelPosition, BattlePanelSize);
            var hud = CreateHud(canvas.transform);
            var status = CreateStatus(canvas.transform);
            var actions = new GameObject("UIActions");
            var bridge = actions.AddComponent<GameActionBridge>();
            var bridgeProperties = new SerializedObject(bridge);
            bridgeProperties.FindProperty("gameManager").objectReferenceValue = gameManager;
            bridgeProperties.ApplyModifiedPropertiesWithoutUndo();

            var continueButton = CreateButton("Continue", titlePanel.transform, 52f, bridge.ContinueGame, MenuButtonSize);
            CreateButton("New Game", titlePanel.transform, 0f, bridge.StartNewGame, MenuButtonSize);
            CreateButton("Quit", titlePanel.transform, -52f, bridge.Quit, MenuButtonSize);
            CreateButton("Hunt", campPanel.transform, 52f, bridge.StartHunt, MenuButtonSize);
            CreateButton("Rest", campPanel.transform, 0f, bridge.Rest, MenuButtonSize);
            CreateButton("Craft Treasure", campPanel.transform, -52f, bridge.CraftTreasure, MenuButtonSize);
            CreatePanelText("EnemyText", battlePanel.transform, 130f, 280f, 24f);
            CreatePanelText("PhaseText", battlePanel.transform, 102f, 280f, 24f);
            CreatePanelText("BattleLogText", battlePanel.transform, 70f, 280f, 50f);
            var attackButton = CreateButton("Attack", battlePanel.transform, 12f, bridge.Attack, BattleButtonSize);
            var defendButton = CreateButton("Defend", battlePanel.transform, -36f, bridge.Defend, BattleButtonSize);
            var fleeButton = CreateButton("Flee", battlePanel.transform, -84f, bridge.Flee, BattleButtonSize);
            var potionButton = CreateButton("Potion", battlePanel.transform, -132f, bridge.UsePotion, BattleButtonSize);

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

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
            panel.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 0.9f);
            return panel;
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

        private static Button CreateButton(
            string label,
            Transform parent,
            float y,
            UnityEngine.Events.UnityAction action,
            Vector2? size = null)
        {
            var buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
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
            return button;
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
