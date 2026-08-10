using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ToilRelic.PlayModeTests
{
    public sealed class PurposefulHuntActionPlayModeTests
    {
        private const string CategoryName = "PurposefulHuntActionContracts";
        private readonly List<UnityEngine.Object> fixtureObjects = new();
        private Type saveServiceType;
        private object previousSavePathOverride;
        private string saveDirectory;
        private string savePath;
        private UnityEngine.Random.State randomState;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            randomState = UnityEngine.Random.state;
            saveServiceType = FindType("ToilRelic.Unity.Save.SaveService");
            var saveOverride = saveServiceType.GetField("savePathOverride", BindingFlags.Static | BindingFlags.NonPublic);
            previousSavePathOverride = saveOverride.GetValue(null);
            saveDirectory = Path.Combine(Path.GetTempPath(), $"toil-relic-actions-{Guid.NewGuid():N}");
            Directory.CreateDirectory(saveDirectory);
            savePath = Path.Combine(saveDirectory, "save.json");
            saveOverride.SetValue(null, savePath);
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;
            Click("New GameButton");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            EventSystem.current?.SetSelectedGameObject(null);
            UnityEngine.Random.state = randomState;
            saveServiceType.GetField("savePathOverride", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, previousSavePathOverride);
            foreach (var instance in fixtureObjects.Where(item => item != null).Reverse())
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
            fixtureObjects.Clear();
            if (Directory.Exists(saveDirectory)) Directory.Delete(saveDirectory, true);
            yield return null;
        }

        [UnityTest]
        [Category(CategoryName)]
        public IEnumerator SerializedJourney_SelectsSecondWinsReplaysForgesKeepsAndEquips()
        {
            var manager = FindComponent("ToilRelic.Unity.Core.GameManager");
            var huntController = FindComponent("ToilRelic.Unity.UI.HuntContractPanelController");
            var equipmentController = FindComponent("ToilRelic.Unity.UI.EquipmentPanelController");
            Assert.That(manager, Is.Not.Null);

            Click("Hunt ContractButton");
            yield return null;
            Assert.That(GetProperty(huntController, "IsOpen"), Is.EqualTo(true));
            Assert.That(((IEnumerable)GetProperty(huntController, "QuarryButtons")).Cast<object>().Count(), Is.EqualTo(3));
            var rustLabel = FindButton("Quarry_rust-golem").GetComponentInChildren<Text>();
            Assert.That(rustLabel.text, Does.Contain("Rustheart Core"));
            AssertQuarryLabelsFit();
            Click("Quarry_rust-golem");
            Assert.That(((Text)GetField(huntController, "detailsText")).text,
                Does.Contain("35% chance: Rustguard Plate").And.Contain("Guaranteed first-win contribution: Rustheart Core"));
            Click("Confirm HuntButton");
            yield return null;
            Assert.That(GetProperty(manager, "CurrentState").ToString(), Is.EqualTo("Battle"));
            Assert.That(GetProperty(manager, "CurrentQuarryId"), Is.EqualTo("rust-golem"));

            var enemy = GetField(manager, "currentEnemy");
            Invoke(enemy, "TakeDamage", 999);
            Click("AttackButton");
            yield return null;
            Assert.That(GetProperty(manager, "CurrentState").ToString(), Is.EqualTo("Camp"));
            var player = GetProperty(manager, "Player");
            var project = GetProperty(player, "RelicProject");
            Assert.That(Completed(project), Is.EqualTo(new[] { "rustheart-core" }));
            Assert.That(Owned(player), Does.Not.Contain("reward-weapon"));
            Assert.That(GetProperty(saveServiceType.GetMethod("Load").Invoke(null, null), "Status").ToString(), Is.EqualTo("Loaded"));

            Click("Hunt ContractButton");
            yield return null;
            Click("Quarry_rust-golem");
            Assert.That(((Text)GetField(huntController, "detailsText")).text,
                Does.Contain("Replay: no additional project progress"));
            Click("Back to CampButton");
            yield return null;
            Assert.That(GetProperty(huntController, "IsOpen"), Is.EqualTo(false));
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.EqualTo(FindButton("Hunt ContractButton").gameObject));

            Assert.That(Invoke(project, "TryAddContribution", "chitin-shard"), Is.EqualTo(true));
            Assert.That(Invoke(project, "TryAddContribution", "wraith-ash"), Is.EqualTo(true));
            InvokePrivate(manager, "PublishPlayer");
            Click("Hunt ContractButton");
            yield return null;
            var forgeButton = FindButton("Forge RelicButton");
            Assert.That(forgeButton.interactable, Is.True);
            Click("Forge RelicButton");
            yield return null;

            Assert.That(GetProperty(equipmentController, "IsOpen"), Is.EqualTo(true));
            Assert.That(GetProperty(equipmentController, "SelectedSlot").ToString(), Is.EqualTo("Necklace"));
            Assert.That(GetProperty(equipmentController, "SelectedCandidateId"), Is.EqualTo("toilbound-relic"));
            Assert.That(Owned(player), Does.Contain("toilbound-relic"));
            Assert.That(IsEquipped(player, "toilbound-relic"), Is.False);
            Click("BackButton");
            yield return null;
            Assert.That(IsEquipped(player, "toilbound-relic"), Is.False, "Keep must not equip or save another mutation.");

            Click("EquipmentButton");
            yield return null;
            Click("Slot_Necklace");
            Click("Candidate_toilbound-relic");
            Click("EquipButton");
            yield return null;
            Assert.That(IsEquipped(player, "toilbound-relic"), Is.True);
            var loaded = GetProperty(saveServiceType.GetMethod("Load").Invoke(null, null), "Player");
            Assert.That(IsEquipped(loaded, "toilbound-relic"), Is.True);
        }

        [UnityTest]
        [Category(CategoryName)]
        public IEnumerator ThirdVictory_PreservesOutcomeReadyAndLevelUpFacts()
        {
            var manager = FindComponent("ToilRelic.Unity.Core.GameManager");
            var status = FindComponent("ToilRelic.Unity.UI.GameStatusController");
            var player = GetProperty(manager, "Player");
            var project = GetProperty(player, "RelicProject");
            Assert.That(Invoke(project, "TryAddContribution", "chitin-shard"), Is.EqualTo(true));
            Assert.That(Invoke(project, "TryAddContribution", "wraith-ash"), Is.EqualTo(true));
            SetField(player, "experience", 19);
            InvokePrivate(manager, "PublishPlayer");

            Click("Hunt ContractButton");
            yield return null;
            Click("Quarry_rust-golem");
            Click("Confirm HuntButton");
            yield return null;
            var enemy = GetField(manager, "currentEnemy");
            Invoke(enemy, "TakeDamage", 999);
            Click("AttackButton");
            yield return null;

            Assert.That(GetProperty(project, "IsReady"), Is.EqualTo(true));
            Assert.That(GetProperty(player, "Level"), Is.EqualTo(2));
            Assert.That(((Text)GetField(status, "messageText")).text,
                Does.StartWith("Win.")
                    .And.Contain("Rustguard Plate")
                    .And.Contain("Project contribution acquired: Rustheart Core.")
                    .And.Contain("Ready to forge.")
                    .And.Contain("Level up!"));
        }

        [UnityTest]
        [Category(CategoryName)]
        public IEnumerator InvalidContent_RejectsHuntWithoutChangingPlayer()
        {
            var manager = FindComponent("ToilRelic.Unity.Core.GameManager");
            var status = FindComponent("ToilRelic.Unity.UI.GameStatusController");
            var player = GetProperty(manager, "Player");
            var before = JsonUtility.ToJson(player);
            var contract = GetField(manager, "huntContract");
            SetField(manager, "huntContract", null);
            Click("Hunt ContractButton");
            yield return null;
            Assert.That(GetProperty(manager, "CurrentState").ToString(), Is.EqualTo("Camp"));
            Assert.That(JsonUtility.ToJson(player), Is.EqualTo(before));
            Assert.That(((Text)GetField(status, "messageText")).text, Does.Contain("Hunt Contract unavailable"));
            SetField(manager, "huntContract", contract);
        }

        [UnityTest]
        [Category(CategoryName)]
        public IEnumerator VictorySaveFailure_KeepsAppliedContributionAndCanPersistOnRetry()
        {
            var manager = FindComponent("ToilRelic.Unity.Core.GameManager");
            var status = FindComponent("ToilRelic.Unity.UI.GameStatusController");
            var player = GetProperty(manager, "Player");
            var quarry = PurposefulHuntContractFixture.Load().content.quarries
                .Single(item => item.id == "rust-golem");

            Click("Hunt ContractButton");
            yield return null;
            Click($"Quarry_{quarry.id}");
            Click("Confirm HuntButton");
            yield return null;
            var enemy = GetField(manager, "currentEnemy");
            Invoke(enemy, "TakeDamage", 999);

            UseFailingSavePath();
            Click("AttackButton");
            yield return null;
            Assert.That(GetProperty(manager, "CurrentState").ToString(), Is.EqualTo("Camp"));
            Assert.That(Completed(GetProperty(player, "RelicProject")), Is.EqualTo(new[] { quarry.contributionId }));
            AssertSaveFailureStatus(status);

            RestoreSavePath();
            Click("RestButton");
            yield return null;
            var loaded = saveServiceType.GetMethod("Load").Invoke(null, null);
            Assert.That(GetProperty(loaded, "Status").ToString(), Is.EqualTo("Loaded"));
            Assert.That(Completed(GetProperty(GetProperty(loaded, "Player"), "RelicProject")),
                Is.EqualTo(new[] { quarry.contributionId }));
        }

        [UnityTest]
        [Category(CategoryName)]
        public IEnumerator ForgeSaveFailure_KeepsSingleUnequippedRelicAndCanPersistOnRetry()
        {
            var manager = FindComponent("ToilRelic.Unity.Core.GameManager");
            var status = FindComponent("ToilRelic.Unity.UI.GameStatusController");
            var equipmentController = FindComponent("ToilRelic.Unity.UI.EquipmentPanelController");
            var player = GetProperty(manager, "Player");
            var project = GetProperty(player, "RelicProject");
            var content = PurposefulHuntContractFixture.Load().content;
            foreach (var quarry in content.quarries)
            {
                Assert.That(Invoke(project, "TryAddContribution", quarry.contributionId), Is.EqualTo(true));
            }
            InvokePrivate(manager, "PublishPlayer");
            Click("Hunt ContractButton");
            yield return null;
            Assert.That(FindButton("Forge RelicButton").interactable, Is.True);

            UseFailingSavePath();
            Click("Forge RelicButton");
            yield return null;

            Assert.That(GetProperty(project, "IsForged"), Is.EqualTo(true));
            Assert.That(Owned(player).Count(id => id == content.relicEquipmentId), Is.EqualTo(1));
            Assert.That(IsEquipped(player, content.relicEquipmentId), Is.False);
            Assert.That(GetProperty(equipmentController, "IsOpen"), Is.EqualTo(false));
            AssertSaveFailureStatus(status);

            RestoreSavePath();
            Click("RestButton");
            yield return null;
            var loaded = GetProperty(saveServiceType.GetMethod("Load").Invoke(null, null), "Player");
            Assert.That(GetProperty(GetProperty(loaded, "RelicProject"), "IsForged"), Is.EqualTo(true));
            Assert.That(Owned(loaded).Count(id => id == content.relicEquipmentId), Is.EqualTo(1));
            Assert.That(IsEquipped(loaded, content.relicEquipmentId), Is.False);
        }

        [UnityTest]
        [Category(CategoryName)]
        public IEnumerator PurposefulHunt_CaptureLayoutEvidenceWhenRequested()
        {
            var evidenceDirectory = Environment.GetEnvironmentVariable("TOIL_RELIC_LAYOUT_EVIDENCE_DIR");
            if (string.IsNullOrWhiteSpace(evidenceDirectory))
            {
                Assert.Ignore("Set TOIL_RELIC_LAYOUT_EVIDENCE_DIR to capture Purposeful Hunt evidence.");
            }
            Directory.CreateDirectory(evidenceDirectory);
            var manager = FindComponent("ToilRelic.Unity.Core.GameManager");
            var player = GetProperty(manager, "Player");
            var project = GetProperty(player, "RelicProject");

            Click("Hunt ContractButton");
            yield return CapturePair(evidenceDirectory, "hunt-contract-open", assertContractLayout: true);

            Click("Back to CampButton");
            foreach (var id in new[] { "chitin-shard", "rustheart-core", "wraith-ash" }) Invoke(project, "TryAddContribution", id);
            InvokePrivate(manager, "PublishPlayer");
            Click("Hunt ContractButton");
            yield return CapturePair(evidenceDirectory, "hunt-contract-ready", assertContractLayout: true);

            Click("Forge RelicButton");
            yield return null;
            yield return CapturePair(evidenceDirectory, "relic-preview", assertContractLayout: false);
            Click("BackButton");
            Click("Hunt ContractButton");
            yield return CapturePair(evidenceDirectory, "hunt-contract-forged", assertContractLayout: true);

            Click("Back to CampButton");
            var contract = GetField(manager, "huntContract");
            SetField(manager, "huntContract", null);
            Click("Hunt ContractButton");
            yield return CapturePair(evidenceDirectory, "hunt-contract-invalid", assertContractLayout: false);
            SetField(manager, "huntContract", contract);

            UseFailingSavePath();
            Click("RestButton");
            yield return null;
            yield return CapturePair(evidenceDirectory, "hunt-contract-save-failure", assertContractLayout: false);
        }

        private static IEnumerator CapturePair(string directory, string stem, bool assertContractLayout)
        {
            yield return Capture(directory, $"{stem}-1280x720.png", 1280, 720, assertContractLayout);
            yield return Capture(directory, $"{stem}-800x600.png", 800, 600, assertContractLayout);
        }

        private static IEnumerator Capture(string directory, string fileName, int width, int height, bool assertContractLayout)
        {
            for (var frame = 0; frame < 4; frame++) yield return null;
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var cameraObject = new GameObject("PurposefulHuntEvidenceCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            var renderTexture = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
                camera.targetTexture = renderTexture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (assertContractLayout) AssertContractGeometry();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                var pixels = texture.GetPixels32();
                Assert.That(pixels.Any(pixel => !pixel.Equals(pixels[0])), Is.True, $"{fileName} must contain non-uniform pixels.");
                File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                RenderTexture.active = previousActive;
                camera.targetTexture = null;
                UnityEngine.Object.Destroy(renderTexture);
                UnityEngine.Object.Destroy(texture);
                UnityEngine.Object.Destroy(cameraObject);
            }
        }

        private static void AssertContractGeometry()
        {
            var contract = FindRect("HuntContractPanel");
            var hud = FindRect("Hud");
            var status = FindRect("GameStatus");
            foreach (var viewport in new[] { new Vector2(800, 450), new Vector2(800, 600) })
            {
                var bounds = VirtualRect(contract, viewport);
                var protectedBottom = Mathf.Min(VirtualRect(hud, viewport).yMin, VirtualRect(status, viewport).yMin);
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(16f));
                Assert.That(viewport.x - bounds.xMax, Is.GreaterThanOrEqualTo(16f));
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(16f));
                Assert.That(protectedBottom - bounds.yMax, Is.GreaterThanOrEqualTo(16f));
            }
            foreach (var name in new[] { "Back to CampButton", "Confirm HuntButton", "Forge RelicButton" })
            {
                Assert.That(FindRect(name).sizeDelta.y, Is.GreaterThanOrEqualTo(44f));
            }
            AssertQuarryLabelsFit();
        }

        private static void AssertQuarryLabelsFit()
        {
            Canvas.ForceUpdateCanvases();
            var quarryButtons = UnityEngine.Object
                .FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(button => button.name.StartsWith("Quarry_", StringComparison.Ordinal))
                .ToArray();
            Assert.That(quarryButtons, Has.Length.EqualTo(3));
            foreach (var button in quarryButtons)
            {
                var label = button.GetComponentInChildren<Text>();
                Assert.That(label, Is.Not.Null, $"{button.name} must keep its summary label.");
                Assert.That(label.preferredHeight,
                    Is.LessThanOrEqualTo(label.rectTransform.rect.height + 0.01f),
                    $"{button.name} summary must fit without vertical truncation.");
            }
        }

        private static Rect VirtualRect(RectTransform rect, Vector2 canvasSize)
        {
            var anchor = Vector2.Scale(rect.anchorMin, canvasSize);
            var lowerLeft = anchor + rect.anchoredPosition - Vector2.Scale(rect.pivot, rect.sizeDelta);
            return new Rect(lowerLeft, rect.sizeDelta);
        }

        private static RectTransform FindRect(string name) => UnityEngine.Object
            .FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item.name == name);

        private static void Click(string name)
        {
            var button = FindButton(name);
            Assert.That(button.gameObject.activeInHierarchy, Is.True, $"{name} must be visible before dispatch.");
            Assert.That(button.interactable, Is.True, $"{name} must be interactable before dispatch.");
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            Assert.That(ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler), Is.True);
        }

        private static Button FindButton(string name) => UnityEngine.Object
            .FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(button => button.name == name);

        private static Component FindComponent(string fullName) => UnityEngine.Object
            .FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(component => component.GetType().FullName == fullName);

        private static IReadOnlyList<string> Completed(object project) =>
            ((IEnumerable)GetProperty(project, "CompletedContributionIds")).Cast<string>().ToArray();

        private static IReadOnlyList<string> Owned(object player) =>
            ((IEnumerable)GetProperty(player, "OwnedEquipmentIds")).Cast<string>().ToArray();

        private void UseFailingSavePath()
        {
            SetSavePath(Path.Combine(saveDirectory, "missing", "save.json"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Save write failed"));
        }

        private void RestoreSavePath() => SetSavePath(savePath);

        private void SetSavePath(string path) => saveServiceType
            .GetField("savePathOverride", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, path);

        private static void AssertSaveFailureStatus(Component status) =>
            Assert.That(((Text)GetField(status, "messageText")).text,
                Does.EndWith("Save failed. Progress may not be saved."));

        private static bool IsEquipped(object player, string equipmentId) =>
            ((IEnumerable)GetProperty(player, "EquippedEquipment")).Cast<object>()
            .Any(entry => (string)GetField(entry, "equipmentId") == equipmentId);

        private static object Invoke(object instance, string method, params object[] arguments) => instance.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == method && candidate.GetParameters().Length == arguments.Length)
            .Invoke(instance, arguments);

        private static object InvokePrivate(object instance, string method) => instance.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);

        private static object GetProperty(object instance, string name) => instance.GetType()
            .GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);

        private static object GetField(object instance, string name) => instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);

        private static void SetField(object instance, string name, object value) => instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(instance, value);

        private static Type FindType(string fullName) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName)).First(type => type != null);
    }
}
