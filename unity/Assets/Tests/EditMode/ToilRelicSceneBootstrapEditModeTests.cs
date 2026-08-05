using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ToilRelic.EditModeTests
{
    public sealed class ToilRelicSceneBootstrapEditModeTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string EquipmentPanelControllerTypeName = "ToilRelic.Unity.UI.EquipmentPanelController";
        private const string BattlePanelControllerTypeName = "ToilRelic.Unity.UI.BattlePanelController";
        private static readonly HashSet<string> BootstrapRootNames = new(StringComparer.Ordinal)
        {
            "GameManager",
            "Canvas",
            "UIActions",
            "EventSystem"
        };
        private static readonly string[] EquipmentReferenceFields =
        {
            "gameManager",
            "campMenuPanel",
            "equipmentPanel",
            "equipmentEntryButton",
            "backButton",
            "equipButton",
            "unequipButton",
            "slotRowsContainer",
            "candidateRowsContainer",
            "comparisonText",
            "totalsText",
            "validationText"
        };
        private static readonly string[] BattleReferenceFields =
        {
            "gameManager",
            "enemyText",
            "phaseText",
            "logText",
            "attackButton",
            "defendButton",
            "fleeButton",
            "potionButton"
        };
        private static readonly string[] GeometryPaths =
        {
            "Canvas/CampPanel/EquipmentPanel",
            "Canvas/CampPanel/EquipmentPanel/SlotScrollView",
            "Canvas/CampPanel/EquipmentPanel/SlotScrollView/Viewport",
            "Canvas/CampPanel/EquipmentPanel/SlotScrollView/Viewport/SlotRowsContainer",
            "Canvas/CampPanel/EquipmentPanel/CandidateScrollView",
            "Canvas/CampPanel/EquipmentPanel/CandidateScrollView/Viewport",
            "Canvas/CampPanel/EquipmentPanel/CandidateScrollView/Viewport/CandidateRowsContainer",
            "Canvas/CampPanel/EquipmentPanel/EquipmentDetailPanel",
            "Canvas/CampPanel/EquipmentPanel/BackButton",
            "Canvas/CampPanel/EquipmentPanel/EquipButton",
            "Canvas/CampPanel/EquipmentPanel/UnequipButton",
            "Canvas/BattlePanel",
            "Canvas/BattlePanel/EnemyText",
            "Canvas/BattlePanel/PhaseText",
            "Canvas/BattlePanel/BattleLogText",
            "Canvas/BattlePanel/AttackButton",
            "Canvas/BattlePanel/DefendButton",
            "Canvas/BattlePanel/FleeButton",
            "Canvas/BattlePanel/PotionButton"
        };
        private static readonly (string Path, string Method)[] PersistentActions =
        {
            ("Canvas/CampPanel/CampActionMenu/EquipmentButton", "OpenEquipment"),
            ("Canvas/CampPanel/EquipmentPanel/BackButton", "BackToCamp"),
            ("Canvas/CampPanel/EquipmentPanel/EquipButton", "EquipSelected"),
            ("Canvas/CampPanel/EquipmentPanel/UnequipButton", "UnequipSelected"),
            ("Canvas/BattlePanel/AttackButton", "Attack"),
            ("Canvas/BattlePanel/DefendButton", "Defend"),
            ("Canvas/BattlePanel/FleeButton", "Flee"),
            ("Canvas/BattlePanel/PotionButton", "UsePotion")
        };
        private static readonly string[] BattleButtonPaths =
        {
            "Canvas/BattlePanel/AttackButton",
            "Canvas/BattlePanel/DefendButton",
            "Canvas/BattlePanel/FleeButton",
            "Canvas/BattlePanel/PotionButton"
        };

        [Test]
        public void BootstrapRegeneratesCommittedEquipmentContractInDisposableScene()
        {
            var originalActiveScene = SceneManager.GetActiveScene();
            var originalSceneHandles = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(index => SceneManager.GetSceneAt(index).handle)
                .ToHashSet();
            var sceneFile = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ScenePath);
            var committedBytes = File.ReadAllBytes(sceneFile);
            var temporaryScenePath = $"Assets/Scenes/EquipmentBootstrapContract_{Guid.NewGuid():N}.unity";

            try
            {
                var committedScene = SceneManager.GetSceneByPath(ScenePath);
                if (!committedScene.IsValid() || !committedScene.isLoaded)
                {
                    committedScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                }

                var committed = CaptureContract(committedScene);

                var disposableScene = string.IsNullOrEmpty(originalActiveScene.path)
                    ? originalActiveScene
                    : Enumerable.Range(0, SceneManager.sceneCount)
                        .Select(index => SceneManager.GetSceneAt(index))
                        .FirstOrDefault(scene => scene.isLoaded && string.IsNullOrEmpty(scene.path));
                if (!disposableScene.IsValid())
                {
                    disposableScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                }

                if (SceneManager.GetActiveScene().handle != disposableScene.handle)
                {
                    Assert.That(SceneManager.SetActiveScene(disposableScene), Is.True);
                }
                Assert.That(disposableScene.path, Is.Empty,
                    "The integration contract must generate into an unsaved disposable scene.");
                var originalDisposableRoots = disposableScene.GetRootGameObjects()
                    .Select(root => root.GetInstanceID())
                    .OrderBy(id => id)
                    .ToArray();
                var originalDisposableDirtyState = disposableScene.isDirty;

                var bootstrapType = FindType("ToilRelic.Unity.Editor.ToilRelicSceneBootstrap");
                Assert.That(bootstrapType, Is.Not.Null);
                var regenerateSceneAtPath = bootstrapType.GetMethod(
                    "RegenerateSceneAtPath",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(regenerateSceneAtPath, Is.Not.Null,
                    "The public bootstrap and integration test must share one target-path regeneration pipeline.");

                regenerateSceneAtPath.Invoke(null, new object[] { temporaryScenePath });
                Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(disposableScene.handle),
                    "Bootstrap regeneration must restore the original active scene.");
                Assert.That(disposableScene.GetRootGameObjects()
                        .Select(root => root.GetInstanceID())
                        .OrderBy(id => id),
                    Is.EqualTo(originalDisposableRoots),
                    "Bootstrap regeneration must preserve every object in the open pathless scene.");
                Assert.That(disposableScene.isDirty, Is.EqualTo(originalDisposableDirtyState),
                    "Bootstrap regeneration must preserve the pathless scene dirty state.");
                Assert.That(originalSceneHandles.All(handle =>
                        Enumerable.Range(0, SceneManager.sceneCount)
                            .Select(index => SceneManager.GetSceneAt(index))
                            .Any(scene => scene.handle == handle && scene.isLoaded)),
                    Is.True,
                    "Bootstrap regeneration must not unload scenes that were open before the test.");

                var generatedScene = EditorSceneManager.OpenScene(temporaryScenePath, OpenSceneMode.Additive);
                var generated = CaptureContract(generatedScene);

                Assert.That(generated.Hierarchy, Is.EqualTo(committed.Hierarchy));
                Assert.That(generated.SerializedReferences, Is.EquivalentTo(committed.SerializedReferences));
                Assert.That(generated.Actions, Is.EquivalentTo(committed.Actions));
                Assert.That(generated.Geometry, Is.EquivalentTo(committed.Geometry));
                Assert.That(generated.Navigation, Is.EquivalentTo(committed.Navigation));
                Assert.That(generated.BattleMaxLogLines, Is.EqualTo(committed.BattleMaxLogLines));
                Assert.That(committed.BattleMaxLogLines, Is.EqualTo(2),
                    "BattlePanelController must serialize a two-log-line surface contract.");
                AssertBattleNavigationContract(committed.Navigation);
                Assert.That(File.ReadAllBytes(sceneFile), Is.EqualTo(committedBytes),
                    "The disposable bootstrap contract must not rewrite SampleScene.unity.");
            }
            finally
            {
                for (var index = SceneManager.sceneCount - 1; index >= 0; index--)
                {
                    var scene = SceneManager.GetSceneAt(index);
                    if (!originalSceneHandles.Contains(scene.handle))
                    {
                        EditorSceneManager.CloseScene(scene, removeScene: true);
                    }
                }

                if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(originalActiveScene);
                }

                AssetDatabase.DeleteAsset(temporaryScenePath);
            }
        }

        private static SceneContract CaptureContract(Scene scene)
        {
            var hierarchy = scene.GetRootGameObjects()
                .Where(root => BootstrapRootNames.Contains(root.name))
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(GetHierarchyPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var controllerType = FindType(EquipmentPanelControllerTypeName);
            Assert.That(controllerType, Is.Not.Null);
            var controller = Resources.FindObjectsOfTypeAll(controllerType)
                .OfType<Component>()
                .Single(component => component.gameObject.scene == scene);
            var serializedController = new SerializedObject(controller);
            var serializedReferences = EquipmentReferenceFields.ToDictionary(
                field => field,
                field => DescribeReference(serializedController.FindProperty(field)?.objectReferenceValue),
                StringComparer.Ordinal);
            var battleControllerType = FindType(BattlePanelControllerTypeName);
            Assert.That(battleControllerType, Is.Not.Null);
            var battleController = Resources.FindObjectsOfTypeAll(battleControllerType)
                .OfType<Component>()
                .Single(component => component.gameObject.scene == scene);
            var serializedBattleController = new SerializedObject(battleController);
            foreach (var field in BattleReferenceFields)
            {
                serializedReferences[$"Battle.{field}"] = DescribeReference(
                    serializedBattleController.FindProperty(field)?.objectReferenceValue);
            }
            var maxLogLines = serializedBattleController.FindProperty("maxLogLines");
            Assert.That(maxLogLines, Is.Not.Null);
            var actions = PersistentActions.ToDictionary(
                action => action.Path,
                action => DescribeAction(scene, action.Path, action.Method),
                StringComparer.Ordinal);
            var geometry = GeometryPaths.ToDictionary(
                path => path,
                path => DescribeGeometry(FindTransform(scene, path)),
                StringComparer.Ordinal);
            var grid = FindTransform(
                scene,
                "Canvas/CampPanel/EquipmentPanel/SlotScrollView/Viewport/SlotRowsContainer")
                .GetComponent<GridLayoutGroup>();
            Assert.That(grid, Is.Not.Null);
            geometry["SlotRowsContainer.Grid"] = string.Join("|",
                DescribeVector(grid.cellSize),
                DescribeVector(grid.spacing),
                grid.constraint,
                grid.constraintCount,
                grid.padding.left,
                grid.padding.right,
                grid.padding.top,
                grid.padding.bottom);
            var navigation = BattleButtonPaths.ToDictionary(
                path => path,
                path => DescribeNavigation(scene, path),
                StringComparer.Ordinal);
            return new SceneContract(
                hierarchy,
                serializedReferences,
                actions,
                geometry,
                navigation,
                maxLogLines.intValue);
        }

        private static void AssertBattleNavigationContract(IReadOnlyDictionary<string, string> navigation)
        {
            Assert.That(navigation["Canvas/BattlePanel/AttackButton"], Is.EqualTo(
                "Explicit|Canvas/BattlePanel/FleeButton|Canvas/BattlePanel/FleeButton|Canvas/BattlePanel/DefendButton|Canvas/BattlePanel/DefendButton"));
            Assert.That(navigation["Canvas/BattlePanel/DefendButton"], Is.EqualTo(
                "Explicit|Canvas/BattlePanel/PotionButton|Canvas/BattlePanel/PotionButton|Canvas/BattlePanel/AttackButton|Canvas/BattlePanel/AttackButton"));
            Assert.That(navigation["Canvas/BattlePanel/FleeButton"], Is.EqualTo(
                "Explicit|Canvas/BattlePanel/AttackButton|Canvas/BattlePanel/AttackButton|Canvas/BattlePanel/PotionButton|Canvas/BattlePanel/PotionButton"));
            Assert.That(navigation["Canvas/BattlePanel/PotionButton"], Is.EqualTo(
                "Explicit|Canvas/BattlePanel/DefendButton|Canvas/BattlePanel/DefendButton|Canvas/BattlePanel/FleeButton|Canvas/BattlePanel/FleeButton"));
        }

        private static string DescribeNavigation(Scene scene, string path)
        {
            var button = FindTransform(scene, path).GetComponent<Button>();
            Assert.That(button, Is.Not.Null, $"{path} must contain a Button.");
            var navigation = button.navigation;
            return string.Join("|",
                navigation.mode,
                DescribeNavigationTarget(scene, navigation.selectOnUp),
                DescribeNavigationTarget(scene, navigation.selectOnDown),
                DescribeNavigationTarget(scene, navigation.selectOnLeft),
                DescribeNavigationTarget(scene, navigation.selectOnRight));
        }

        private static string DescribeNavigationTarget(Scene scene, Selectable target)
        {
            Assert.That(target, Is.Not.Null, "Explicit Battle navigation targets must be assigned.");
            Assert.That(target.gameObject.scene, Is.EqualTo(scene));
            return GetHierarchyPath(target.transform);
        }

        private static string DescribeAction(Scene scene, string path, string expectedMethod)
        {
            var button = FindTransform(scene, path).GetComponent<Button>();
            Assert.That(button, Is.Not.Null, $"{path} must contain a Button.");
            Assert.That(button.onClick.GetPersistentEventCount(), Is.EqualTo(1));
            Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo(expectedMethod));
            var target = button.onClick.GetPersistentTarget(0);
            Assert.That(target, Is.Not.Null);
            return $"{target.GetType().FullName}|{button.onClick.GetPersistentMethodName(0)}";
        }

        private static string DescribeReference(UnityEngine.Object reference)
        {
            Assert.That(reference, Is.Not.Null);
            return reference switch
            {
                Component component => $"{GetHierarchyPath(component.transform)}#{component.GetType().FullName}",
                GameObject gameObject => GetHierarchyPath(gameObject.transform),
                _ => $"{AssetDatabase.GetAssetPath(reference)}#{reference.GetType().FullName}"
            };
        }

        private static string DescribeGeometry(Transform transform)
        {
            var rect = transform as RectTransform;
            Assert.That(rect, Is.Not.Null, $"{GetHierarchyPath(transform)} must use RectTransform geometry.");
            return string.Join("|",
                DescribeVector(rect.anchorMin),
                DescribeVector(rect.anchorMax),
                DescribeVector(rect.pivot),
                DescribeVector(rect.anchoredPosition),
                DescribeVector(rect.sizeDelta),
                DescribeVector(rect.offsetMin),
                DescribeVector(rect.offsetMax));
        }

        private static string DescribeVector(Vector2 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:R},{1:R}", value.x, value.y);
        }

        private static Transform FindTransform(Scene scene, string path)
        {
            var segments = path.Split('/');
            var current = scene.GetRootGameObjects()
                .Single(root => string.Equals(root.name, segments[0], StringComparison.Ordinal))
                .transform;
            for (var index = 1; index < segments.Length; index++)
            {
                current = current.Find(segments[index]);
                Assert.That(current, Is.Not.Null, $"Scene hierarchy is missing {path}.");
            }

            return current;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            while (transform != null)
            {
                names.Push(transform.name);
                transform = transform.parent;
            }

            return string.Join("/", names);
        }

        private static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, throwOnError: false))
                .FirstOrDefault(type => type != null);
        }

        private sealed class SceneContract
        {
            public string[] Hierarchy { get; }
            public IReadOnlyDictionary<string, string> SerializedReferences { get; }
            public IReadOnlyDictionary<string, string> Actions { get; }
            public IReadOnlyDictionary<string, string> Geometry { get; }
            public IReadOnlyDictionary<string, string> Navigation { get; }
            public int BattleMaxLogLines { get; }

            public SceneContract(
                string[] hierarchy,
                IReadOnlyDictionary<string, string> serializedReferences,
                IReadOnlyDictionary<string, string> actions,
                IReadOnlyDictionary<string, string> geometry,
                IReadOnlyDictionary<string, string> navigation,
                int battleMaxLogLines)
            {
                Hierarchy = hierarchy;
                SerializedReferences = serializedReferences;
                Actions = actions;
                Geometry = geometry;
                Navigation = navigation;
                BattleMaxLogLines = battleMaxLogLines;
            }
        }
    }
}
