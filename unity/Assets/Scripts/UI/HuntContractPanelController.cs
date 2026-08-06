using System.Collections.Generic;
using System.Linq;
using ToilRelic.Unity.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToilRelic.Unity.UI
{
    public sealed class HuntContractPanelController : MonoBehaviour
    {
        [Header("Command")]
        [SerializeField] private GameManager gameManager;

        [Header("Camp-local panels")]
        [SerializeField] private GameObject campMenuPanel;
        [SerializeField] private GameObject contractPanel;
        [SerializeField] private Button huntEntryButton;

        [Header("Contract controls")]
        [SerializeField] private Transform quarryRowsContainer;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Text projectText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button forgeButton;

        private readonly List<GameObject> generatedRows = new();
        private readonly List<Button> quarryButtons = new();
        private HuntContractSnapshot snapshot;
        private string selectedQuarryId;
        private bool isOpen;

        public bool IsOpen => isOpen;
        public string SelectedQuarryId => selectedQuarryId;
        public HuntContractSnapshot Snapshot => snapshot;
        public IReadOnlyList<Button> QuarryButtons => quarryButtons;

        private void OnEnable()
        {
            GameEvents.HuntContractPresented += OnContractPresented;
            GameEvents.HuntContractClosed += OnContractClosed;
            GameEvents.RelicProjectChanged += OnProjectChanged;
            GameEvents.StateChanged += OnStateChanged;
            SetPanelVisibility(false);
        }

        private void OnDisable()
        {
            GameEvents.HuntContractPresented -= OnContractPresented;
            GameEvents.HuntContractClosed -= OnContractClosed;
            GameEvents.RelicProjectChanged -= OnProjectChanged;
            GameEvents.StateChanged -= OnStateChanged;
            Close(restoreFocus: false);
        }

        public void OpenContract()
        {
            if (gameManager == null || gameManager.CurrentState != GameState.Camp) return;
            gameManager.StartHunt();
        }

        public void SelectQuarry(string quarryId)
        {
            if (!isOpen || snapshot == null) return;
            var quarry = snapshot.Quarries.FirstOrDefault(item => item.Id == quarryId);
            if (quarry == null) return;
            selectedQuarryId = quarry.Id;
            if (detailsText != null)
            {
                detailsText.text =
                    $"{quarry.EnemyName} | Danger {quarry.Danger}\n" +
                    $"{quarry.ProfileChancePercent}% chance: {quarry.ProfileEquipmentName}\n" +
                    (quarry.Completed
                        ? $"Completed: {quarry.ContributionName} | Replay: no additional project progress"
                        : $"Guaranteed first-win contribution: {quarry.ContributionName}");
            }
            if (confirmButton != null) confirmButton.interactable = true;
            SelectControl(confirmButton);
        }

        public void ConfirmSelection()
        {
            if (!isOpen || snapshot == null || string.IsNullOrEmpty(selectedQuarryId) || gameManager == null) return;
            if (!gameManager.ConfirmHunt(selectedQuarryId, snapshot.Revision))
            {
                selectedQuarryId = null;
                if (confirmButton != null) confirmButton.interactable = false;
                return;
            }

            Close(restoreFocus: false);
        }

        public void Cancel()
        {
            if (gameManager != null) gameManager.CancelHunt();
            Close(restoreFocus: true);
        }

        public void Forge() => gameManager?.ForgeRelic();

        private void OnContractPresented(HuntContractSnapshot presented)
        {
            snapshot = presented;
            selectedQuarryId = null;
            isOpen = true;
            SetPanelVisibility(true);
            if (titleText != null) titleText.text = presented.DisplayName;
            if (detailsText != null) detailsText.text = "Select a quarry to review its locked battle rewards.";
            if (confirmButton != null) confirmButton.interactable = false;
            BuildRows();
            SelectControl(quarryButtons.FirstOrDefault() ?? cancelButton);
        }

        private void OnContractClosed() => Close(restoreFocus: false);

        private void OnProjectChanged(RelicProjectSnapshot project)
        {
            if (projectText != null)
            {
                projectText.text = project.Forged
                    ? "First Relic Project: forged"
                    : project.Ready
                        ? "First Relic Project: 3/3 — Ready to forge"
                        : $"First Relic Project: {project.Completed}/{project.Required}";
            }
            if (forgeButton != null) forgeButton.interactable = project.Ready && !project.Forged;
        }

        private void OnStateChanged(GameState next)
        {
            if (next != GameState.Camp) Close(restoreFocus: false);
        }

        private void BuildRows()
        {
            DestroyRows();
            if (quarryRowsContainer == null || snapshot == null) return;
            foreach (var quarry in snapshot.Quarries)
            {
                var row = new GameObject($"Quarry_{quarry.Id}", typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(quarryRowsContainer, false);
                var image = row.GetComponent<Image>();
                image.color = new Color(0.18f, 0.20f, 0.24f, 0.96f);
                var button = row.GetComponent<Button>();
                var capturedId = quarry.Id;
                button.onClick.AddListener(() => SelectQuarry(capturedId));

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(row.transform, false);
                var labelRect = (RectTransform)labelObject.transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(12f, 5f);
                labelRect.offsetMax = new Vector2(-12f, -5f);
                var label = labelObject.GetComponent<Text>();
                label.font = detailsText != null ? detailsText.font : null;
                label.fontSize = 16;
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.white;
                label.text = $"{quarry.EnemyName} · Danger {quarry.Danger}\n" +
                    $"{quarry.ProfileChancePercent}% {quarry.ProfileEquipmentName} · " +
                    (quarry.Completed ? "Completed — replay only" : $"Guaranteed {quarry.ContributionName}");

                var layout = row.AddComponent<LayoutElement>();
                layout.preferredHeight = 58f;
                generatedRows.Add(row);
                quarryButtons.Add(button);
            }
        }

        private void Close(bool restoreFocus)
        {
            var wasOpen = isOpen;
            snapshot = null;
            selectedQuarryId = null;
            isOpen = false;
            DestroyRows();
            SetPanelVisibility(false);
            if (restoreFocus && wasOpen) SelectControl(huntEntryButton);
        }

        private void DestroyRows()
        {
            foreach (var row in generatedRows)
            {
                if (row != null) Destroy(row);
            }
            generatedRows.Clear();
            quarryButtons.Clear();
        }

        private void SetPanelVisibility(bool showContract)
        {
            if (campMenuPanel != null) campMenuPanel.SetActive(!showContract);
            if (contractPanel != null) contractPanel.SetActive(showContract);
        }

        private static void SelectControl(Selectable selectable)
        {
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(selectable != null ? selectable.gameObject : null);
        }
    }
}
