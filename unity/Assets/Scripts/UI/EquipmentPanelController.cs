using System;
using System.Collections.Generic;
using System.Linq;
using ToilRelic.Unity.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToilRelic.Unity.UI
{
    public sealed class EquipmentPanelController : MonoBehaviour
    {
        private readonly struct SlotPresentation
        {
            public EquipmentSlot Slot { get; }
            public string Group { get; }
            public string Label { get; }

            public SlotPresentation(EquipmentSlot slot, string group, string label)
            {
                Slot = slot;
                Group = group;
                Label = label;
            }
        }

        private static readonly SlotPresentation[] SlotPresentations =
        {
            new(EquipmentSlot.PrimaryWeapon, "Weapons", "Primary Weapon"),
            new(EquipmentSlot.SecondaryWeapon, "Weapons", "Secondary Weapon"),
            new(EquipmentSlot.Hat, "Armor", "Hat"),
            new(EquipmentSlot.Armor, "Armor", "Armor"),
            new(EquipmentSlot.Gloves, "Armor", "Gloves"),
            new(EquipmentSlot.Shoes, "Armor", "Shoes"),
            new(EquipmentSlot.Necklace, "Accessories", "Necklace"),
            new(EquipmentSlot.Belt, "Accessories", "Belt"),
            new(EquipmentSlot.Ring1, "Accessories", "Ring 1"),
            new(EquipmentSlot.Ring2, "Accessories", "Ring 2"),
            new(EquipmentSlot.Earring1, "Accessories", "Earring 1"),
            new(EquipmentSlot.Earring2, "Accessories", "Earring 2")
        };

        [Header("Command")]
        [SerializeField] private GameManager gameManager;

        [Header("Camp-local panels")]
        [SerializeField] private GameObject campMenuPanel;
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private Button equipmentEntryButton;

        [Header("Fixed actions")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;

        [Header("Runtime rows")]
        [SerializeField] private Transform slotRowsContainer;
        [SerializeField] private Transform candidateRowsContainer;

        [Header("Detail")]
        [SerializeField] private Text comparisonText;
        [SerializeField] private Text totalsText;
        [SerializeField] private Text validationText;

        private readonly List<Button> slotButtons = new();
        private readonly List<Button> candidateButtons = new();
        private readonly List<GameObject> generatedSlotRows = new();
        private readonly List<GameObject> generatedCandidateRows = new();
        private readonly List<EquipmentDefinition> candidates = new();

        private bool isOpen;
        private bool commandInFlight;
        private GameObject observedSelectedControl;
        private EquipmentSlot? selectedSlot;
        private string selectedCandidateId;
        private EquipmentComparisonResult currentComparison;
        private UnequipEligibilityResult currentUnequipEligibility;

        public bool IsOpen => isOpen;
        public EquipmentSlot? SelectedSlot => selectedSlot;
        public string SelectedCandidateId => selectedCandidateId;
        public EquipmentComparisonResult CurrentComparison => currentComparison;
        public UnequipEligibilityResult CurrentUnequipEligibility => currentUnequipEligibility;
        public IReadOnlyList<Button> SlotButtons => slotButtons;
        public IReadOnlyList<Button> CandidateButtons => candidateButtons;

        private void OnEnable()
        {
            GameEvents.PlayerChanged += OnPlayerChanged;
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.EquipmentFocusRequested += OnEquipmentFocusRequested;
            if (!isOpen)
            {
                SetLocalPanelVisibility(showEquipment: false);
            }
        }

        private void OnDisable()
        {
            GameEvents.PlayerChanged -= OnPlayerChanged;
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.EquipmentFocusRequested -= OnEquipmentFocusRequested;
            CloseAndReset(restoreEntryFocus: false);
        }

        private void LateUpdate()
        {
            if (!isOpen || EventSystem.current == null)
            {
                observedSelectedControl = null;
                return;
            }

            var selectedControl = EventSystem.current.currentSelectedGameObject;
            if (selectedControl == observedSelectedControl)
            {
                return;
            }

            observedSelectedControl = selectedControl;
            var selectedSlotButton = slotButtons.FirstOrDefault(button =>
                button != null && button.gameObject == selectedControl);
            KeepSlotButtonVisible(selectedSlotButton);
        }

        public void OpenEquipment()
        {
            if (gameManager == null || gameManager.CurrentState != GameState.Camp)
            {
                CloseAndReset(restoreEntryFocus: false);
                return;
            }

            isOpen = true;
            ClearValidation();
            SetLocalPanelVisibility(showEquipment: true);
            BuildSlotRows();
            selectedSlot = SlotPresentations[0].Slot;
            selectedCandidateId = null;
            RebuildCandidates(preferredCandidateId: null, preferredIndex: -1, allowFallback: false);
            RefreshDetails();
            ConfigureNavigation();
            SelectControl(slotButtons.FirstOrDefault());
        }

        public void OpenAndFocus(EquipmentSlot slot, string equipmentId)
        {
            OpenEquipment();
            if (!isOpen) return;
            SelectSlot(slot);
            SelectCandidate(equipmentId);
        }

        public void BackToCamp()
        {
            CloseAndReset(restoreEntryFocus: true);
        }

        public void SelectSlot(EquipmentSlot slot)
        {
            if (!isOpen || !SlotPresentations.Any(presentation => presentation.Slot == slot))
            {
                return;
            }

            ClearValidation();
            selectedSlot = slot;
            selectedCandidateId = null;
            RebuildCandidates(preferredCandidateId: null, preferredIndex: -1, allowFallback: false);
            RefreshDetails();
            ConfigureNavigation();
            SelectControl(candidateButtons.FirstOrDefault() ?? backButton);
        }

        public void SelectCandidate(string equipmentId)
        {
            if (!isOpen || !selectedSlot.HasValue)
            {
                return;
            }

            ClearValidation();
            var candidate = candidates.FirstOrDefault(item => string.Equals(item.Id, equipmentId, StringComparison.Ordinal));
            selectedCandidateId = candidate?.Id;
            RefreshDetails();
            ConfigureNavigation();
            if (candidate != null)
            {
                SelectControl(FindCandidateButton(candidate.Id));
            }
        }

        public void EquipSelected()
        {
            if (!isOpen || gameManager == null || !selectedSlot.HasValue || string.IsNullOrEmpty(selectedCandidateId))
            {
                return;
            }

            EquipmentCommandOutcome outcome;
            commandInFlight = true;
            try
            {
                outcome = gameManager.EquipEquipment(selectedSlot.Value, selectedCandidateId);
            }
            finally
            {
                commandInFlight = false;
            }

            if (outcome.Applied)
            {
                ClearValidation();
                RefreshAfterMutation();
                return;
            }

            selectedCandidateId = null;
            RefreshAfterRejection();
            ShowValidation(DescribeRejection(outcome));
        }

        public void UnequipSelected()
        {
            if (!isOpen || gameManager == null || !selectedSlot.HasValue)
            {
                return;
            }

            EquipmentCommandOutcome outcome;
            commandInFlight = true;
            try
            {
                outcome = gameManager.UnequipEquipment(selectedSlot.Value);
            }
            finally
            {
                commandInFlight = false;
            }

            if (outcome.Applied)
            {
                ClearValidation();
                RefreshAfterMutation();
                return;
            }

            RefreshAfterRejection();
            ShowValidation(DescribeRejection(outcome));
        }

        public void Refresh()
        {
            if (!isOpen || gameManager == null || gameManager.CurrentState != GameState.Camp)
            {
                return;
            }

            var preferredCandidateId = selectedCandidateId;
            var preferredIndex = FindCandidateIndex(preferredCandidateId);
            EnsureSelectedSlot();
            RebuildCandidates(preferredCandidateId, preferredIndex, allowFallback: true);
            RefreshDetails();
            ConfigureNavigation();
            SelectRefreshTarget();
        }

        private void RefreshAfterRejection()
        {
            EnsureSelectedSlot();
            RebuildCandidates(preferredCandidateId: null, preferredIndex: -1, allowFallback: false);
            RefreshDetails();
            ConfigureNavigation();
            SelectControl(candidateButtons.FirstOrDefault() ?? backButton);
        }

        private void RefreshAfterMutation()
        {
            EnsureSelectedSlot();
            RefreshDetails();
            ConfigureNavigation();
            SelectRefreshTarget();
        }

        private void OnPlayerChanged(PlayerState _)
        {
            if (isOpen && !commandInFlight)
            {
                Refresh();
            }
        }

        private void OnStateChanged(GameState state)
        {
            if (state != GameState.Camp)
            {
                CloseAndReset(restoreEntryFocus: false);
            }
        }

        private void OnEquipmentFocusRequested(EquipmentSlot slot, string equipmentId) =>
            OpenAndFocus(slot, equipmentId);

        private void CloseAndReset(bool restoreEntryFocus)
        {
            ClearEquipmentSelection();
            isOpen = false;
            SetLocalPanelVisibility(showEquipment: false);
            if (restoreEntryFocus && equipmentEntryButton != null && equipmentEntryButton.isActiveAndEnabled)
            {
                SelectControl(equipmentEntryButton);
            }
            else
            {
                ClearEquipmentFocus();
            }
        }

        private void ClearEquipmentSelection()
        {
            observedSelectedControl = null;
            selectedSlot = null;
            selectedCandidateId = null;
            currentComparison = null;
            currentUnequipEligibility = null;
            candidates.Clear();
            DestroyGeneratedRows(generatedSlotRows);
            DestroyGeneratedRows(generatedCandidateRows);
            slotButtons.Clear();
            candidateButtons.Clear();
            SetText(comparisonText, string.Empty);
            SetText(totalsText, string.Empty);
            ClearValidation();
            SetActionState(equipEnabled: false, unequipEnabled: false);
        }

        private void SetLocalPanelVisibility(bool showEquipment)
        {
            if (campMenuPanel != null)
            {
                campMenuPanel.SetActive(!showEquipment);
            }

            if (equipmentPanel != null)
            {
                equipmentPanel.SetActive(showEquipment);
            }
        }

        private void BuildSlotRows()
        {
            DestroyGeneratedRows(generatedSlotRows);
            slotButtons.Clear();
            if (slotRowsContainer == null)
            {
                return;
            }

            string previousGroup = null;
            foreach (var presentation in SlotPresentations)
            {
                if (!string.Equals(previousGroup, presentation.Group, StringComparison.Ordinal))
                {
                    generatedSlotRows.Add(CreateLabel(slotRowsContainer, $"{presentation.Group}Header", presentation.Group, FontStyle.Bold));
                    previousGroup = presentation.Group;
                }

                var slot = presentation.Slot;
                var button = CreateButton(slotRowsContainer, $"Slot_{slot}", presentation.Label);
                button.onClick.AddListener(() => SelectSlot(slot));
                slotButtons.Add(button);
                generatedSlotRows.Add(button.gameObject);
            }

            ResetSlotViewport();
        }

        private void RebuildCandidates(string preferredCandidateId, int preferredIndex, bool allowFallback)
        {
            DestroyGeneratedRows(generatedCandidateRows);
            candidateButtons.Clear();
            candidates.Clear();
            currentComparison = null;

            if (!selectedSlot.HasValue || gameManager?.Player == null)
            {
                selectedCandidateId = null;
                return;
            }

            candidates.AddRange(EquipmentCatalog.All
                .Where(item => EquipmentComparisonEvaluator.IsCandidateAvailable(
                    gameManager.Player,
                    selectedSlot.Value,
                    item.Id)));

            foreach (var candidate in candidates)
            {
                var candidateId = candidate.Id;
                var button = CreateButton(candidateRowsContainer, $"Candidate_{candidate.Id}", candidate.DisplayName);
                button.onClick.AddListener(() => SelectCandidate(candidateId));
                candidateButtons.Add(button);
                generatedCandidateRows.Add(button.gameObject);
            }

            var surviving = candidates.FirstOrDefault(item => string.Equals(item.Id, preferredCandidateId, StringComparison.Ordinal));
            if (surviving != null)
            {
                selectedCandidateId = surviving.Id;
                return;
            }

            if (allowFallback && candidates.Count > 0)
            {
                var fallbackIndex = preferredIndex >= 0 ? Mathf.Clamp(preferredIndex, 0, candidates.Count - 1) : 0;
                selectedCandidateId = candidates[fallbackIndex].Id;
                return;
            }

            selectedCandidateId = null;
        }

        private void RefreshDetails()
        {
            if (!selectedSlot.HasValue || gameManager?.Player == null)
            {
                currentComparison = null;
                currentUnequipEligibility = null;
                SetText(comparisonText, string.Empty);
                SetText(totalsText, string.Empty);
                SetActionState(equipEnabled: false, unequipEnabled: false);
                return;
            }

            currentUnequipEligibility = EquipmentComparisonEvaluator.EvaluateUnequip(gameManager.Player, selectedSlot.Value);
            currentComparison = string.IsNullOrEmpty(selectedCandidateId)
                ? null
                : EquipmentComparisonEvaluator.Compare(gameManager.Player, selectedSlot.Value, selectedCandidateId);

            if (candidates.Count == 0)
            {
                SetText(comparisonText, "No compatible owned equipment is available for this slot.");
            }
            else if (currentComparison == null)
            {
                SetText(comparisonText, "Select compatible equipment to compare.");
            }
            else
            {
                SetText(comparisonText, FormatComparison(currentComparison));
            }

            SetText(totalsText, FormatTotals(gameManager.Player, currentComparison));
            SetActionState(
                equipEnabled: currentComparison?.CanCommit == true,
                unequipEnabled: currentUnequipEligibility.CanCommit);
        }

        private void SetActionState(bool equipEnabled, bool unequipEnabled)
        {
            SetButtonState(backButton, interactable: true);
            SetButtonState(equipButton, equipEnabled);
            SetButtonState(unequipButton, unequipEnabled);
        }

        private void ConfigureNavigation()
        {
            var selectedSlotButton = FindSelectedSlotButton();
            var firstCandidate = candidateButtons.FirstOrDefault(button => button != null && button.interactable);
            var firstAction = equipButton != null && equipButton.interactable
                ? equipButton
                : unequipButton != null && unequipButton.interactable
                    ? unequipButton
                    : backButton;

            for (var index = 0; index < slotButtons.Count; index++)
            {
                var up = slotButtons[(index - 1 + slotButtons.Count) % slotButtons.Count];
                var down = slotButtons[(index + 1) % slotButtons.Count];
                SetExplicitNavigation(slotButtons[index], up, down, backButton, firstCandidate ?? firstAction);
            }

            for (var index = 0; index < candidateButtons.Count; index++)
            {
                var up = candidateButtons[(index - 1 + candidateButtons.Count) % candidateButtons.Count];
                var down = candidateButtons[(index + 1) % candidateButtons.Count];
                SetExplicitNavigation(candidateButtons[index], up, down, selectedSlotButton, firstAction);
            }

            var previousRegion = candidateButtons.LastOrDefault() ?? selectedSlotButton;
            SetExplicitNavigation(equipButton, previousRegion, unequipButton ?? backButton, previousRegion, unequipButton ?? backButton);
            SetExplicitNavigation(unequipButton, equipButton ?? previousRegion, backButton, equipButton ?? previousRegion, backButton);
            SetExplicitNavigation(backButton, unequipButton ?? equipButton ?? previousRegion, slotButtons.FirstOrDefault(), unequipButton ?? equipButton ?? previousRegion, slotButtons.FirstOrDefault());
        }

        private static void SetExplicitNavigation(
            Selectable selectable,
            Selectable up,
            Selectable down,
            Selectable left,
            Selectable right)
        {
            if (selectable == null)
            {
                return;
            }

            selectable.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = up,
                selectOnDown = down,
                selectOnLeft = left,
                selectOnRight = right
            };
        }

        private void SelectRefreshTarget()
        {
            var candidateButton = FindCandidateButton(selectedCandidateId);
            SelectControl(candidateButton ?? candidateButtons.FirstOrDefault() ?? backButton);
        }

        private Button FindSelectedSlotButton()
        {
            if (!selectedSlot.HasValue)
            {
                return slotButtons.FirstOrDefault();
            }

            var index = Array.FindIndex(SlotPresentations, presentation => presentation.Slot == selectedSlot.Value);
            return index >= 0 && index < slotButtons.Count ? slotButtons[index] : slotButtons.FirstOrDefault();
        }

        private Button FindCandidateButton(string equipmentId)
        {
            var index = FindCandidateIndex(equipmentId);
            return index >= 0 && index < candidateButtons.Count ? candidateButtons[index] : null;
        }

        private int FindCandidateIndex(string equipmentId)
        {
            return string.IsNullOrEmpty(equipmentId)
                ? -1
                : candidates.FindIndex(candidate => string.Equals(candidate.Id, equipmentId, StringComparison.Ordinal));
        }

        private void EnsureSelectedSlot()
        {
            if (!selectedSlot.HasValue || !SlotPresentations.Any(presentation => presentation.Slot == selectedSlot.Value))
            {
                selectedSlot = SlotPresentations[0].Slot;
            }

            if (slotButtons.Count != SlotPresentations.Length)
            {
                BuildSlotRows();
            }
        }

        private void ClearValidation()
        {
            SetText(validationText, string.Empty);
        }

        private void ShowValidation(string message)
        {
            SetText(validationText, message);
        }

        private static string DescribeRejection(EquipmentCommandOutcome outcome)
        {
            if (outcome.Status == EquipmentCommandStatus.NotInCamp)
            {
                return "Equipment can only be changed at Camp.";
            }

            if (outcome.Command == EquipmentCommandKind.Equip)
            {
                return outcome.ComparisonReason switch
                {
                    EquipmentComparisonReason.SameItem => "That item is already equipped.",
                    EquipmentComparisonReason.UnknownCandidate => "That equipment is no longer available.",
                    EquipmentComparisonReason.CandidateNotOwned => "That equipment is not owned.",
                    EquipmentComparisonReason.IncompatibleSlot => "That equipment does not fit this slot.",
                    EquipmentComparisonReason.CandidateEquippedElsewhere => "That equipment is already equipped in another slot.",
                    _ => "Equipment could not be equipped."
                };
            }

            return outcome.UnequipStatus switch
            {
                UnequipEligibilityStatus.MandatoryPrimaryWeapon => "Primary weapon cannot be unequipped.",
                UnequipEligibilityStatus.EmptySlot => "No equipment to remove.",
                _ => "Equipment could not be removed."
            };
        }

        private static string FormatComparison(EquipmentComparisonResult comparison)
        {
            var currentName = comparison.Current?.DisplayName ?? "Empty";
            var candidateName = comparison.Candidate?.DisplayName ?? "Unavailable";
            var deltas = comparison.StatDeltas.Count == 0
                ? "No equipment stat change (±0)"
                : string.Join(" | ", comparison.StatDeltas.Select(delta =>
                    $"{StatLabel(delta.Stat)} {delta.CurrentValue} -> {delta.CandidateValue} ({FormatSigned(delta.Delta)})"));
            return $"Slot: {comparison.DestinationSlot}\nCurrent: {currentName}\nCandidate: {candidateName}\n{deltas}";
        }

        private static string FormatTotals(PlayerState player, EquipmentComparisonResult comparison)
        {
            var attack = comparison?.ProjectedAttackBonus ?? player.AttackBonus;
            var defense = comparison?.ProjectedDefenseBonus ?? player.DefenseBonus;
            var reduction = comparison?.ProjectedDamageReductionBonus ?? player.DamageReductionBonus;
            var maxHp = comparison?.ProjectedMaxHp ?? player.MaxHp;
            return $"Result: ATK +{attack} | DEF +{defense} | DR +{reduction} | Max HP {maxHp}";
        }

        private static string StatLabel(EquipmentStat stat) => stat switch
        {
            EquipmentStat.Attack => "ATK",
            EquipmentStat.Defense => "DEF",
            EquipmentStat.DamageReduction => "DR",
            EquipmentStat.MaxHp => "Max HP",
            _ => stat.ToString()
        };

        private static string FormatSigned(int value) => value switch
        {
            > 0 => $"+{value}",
            < 0 => value.ToString(),
            _ => "±0"
        };

        private static Button CreateButton(Transform parent, string name, string label)
        {
            if (parent == null)
            {
                return null;
            }

            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.2f, 0.28f, 1f);
            var button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            var layout = gameObject.GetComponent<LayoutElement>();
            layout.minHeight = 44f;
            layout.preferredHeight = 44f;
            var labelObject = CreateLabel(gameObject.transform, "Label", label, FontStyle.Normal);
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 2f);
            labelRect.offsetMax = new Vector2(-8f, -2f);
            return button;
        }

        private static GameObject CreateLabel(Transform parent, string name, string value, FontStyle fontStyle)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = value;
            text.fontSize = 16;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return gameObject;
        }

        private static void DestroyGeneratedRows(ICollection<GameObject> rows)
        {
            foreach (var row in rows)
            {
                if (row != null)
                {
                    row.SetActive(false);
                    Destroy(row);
                }
            }

            rows.Clear();
        }

        private static void SetText(Text text, string value)
        {
            if (text != null && !string.Equals(text.text, value, StringComparison.Ordinal))
            {
                text.text = value;
            }
        }

        private static void SetButtonState(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            if (!button.gameObject.activeSelf)
            {
                button.gameObject.SetActive(true);
            }

            if (button.interactable != interactable)
            {
                button.interactable = interactable;
            }
        }

        private void ResetSlotViewport()
        {
            var scrollRect = ResolveSlotScrollRect();
            if (scrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
            observedSelectedControl = null;
        }

        private void KeepSlotButtonVisible(Button button)
        {
            var scrollRect = ResolveSlotScrollRect();
            if (button == null || scrollRect?.content == null)
            {
                return;
            }

            var viewport = scrollRect.viewport ?? scrollRect.GetComponent<RectTransform>();
            var target = button.GetComponent<RectTransform>();
            if (viewport == null || target == null)
            {
                return;
            }

            var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
            var viewportBounds = new Bounds(viewport.rect.center, viewport.rect.size);
            var verticalOffset = 0f;
            if (targetBounds.max.y > viewportBounds.max.y)
            {
                verticalOffset = targetBounds.max.y - viewportBounds.max.y;
            }
            else if (targetBounds.min.y < viewportBounds.min.y)
            {
                verticalOffset = targetBounds.min.y - viewportBounds.min.y;
            }

            if (Mathf.Approximately(verticalOffset, 0f))
            {
                return;
            }

            var contentPosition = scrollRect.content.anchoredPosition;
            contentPosition.y -= verticalOffset;
            scrollRect.StopMovement();
            scrollRect.content.anchoredPosition = contentPosition;
        }

        private ScrollRect ResolveSlotScrollRect()
        {
            return slotRowsContainer == null ? null : slotRowsContainer.GetComponentInParent<ScrollRect>();
        }

        private static void SelectControl(Selectable selectable)
        {
            if (selectable == null || !selectable.isActiveAndEnabled || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        private void ClearEquipmentFocus()
        {
            if (equipmentPanel == null || EventSystem.current == null)
            {
                return;
            }

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && (selected == equipmentPanel || selected.transform.IsChildOf(equipmentPanel.transform)))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
}
