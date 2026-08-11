using ToilRelic.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ToilRelic.Unity.UI
{
    public sealed class GameStatusController : MonoBehaviour
    {
        private const string RecoveryNotice = "Recovered a previous valid save. Recent progress may be missing.";
        private const string SaveFailureWarning = "Save failed. Progress may not be saved.";
        private const float DefaultStatusHeight = 120f;
        private const float RecoveryStatusHeight = 164f;
        private const float DefaultSaveStatusHeight = 22f;
        private const float RecoverySaveStatusHeight = 66f;

        [SerializeField] private Text stateText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text saveStatusText;

        private bool hasTerminalOutcome;
        private bool saveFailureActive;
        private bool recoveryNoticePending;
        private string primaryMessage;
        private GameState currentState = GameState.Title;
        private SaveFeedbackStatus? saveStatus;

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.BattleLog += OnBattleLog;
            GameEvents.BattleOutcome += OnBattleOutcome;
            GameEvents.LevelUp += OnLevelUp;
            GameEvents.SaveStatusChanged += OnSaveStatusChanged;
            GameEvents.RecoveryNoticeChanged += OnRecoveryNoticeChanged;
            GameEvents.RelicProjectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.BattleLog -= OnBattleLog;
            GameEvents.BattleOutcome -= OnBattleOutcome;
            GameEvents.LevelUp -= OnLevelUp;
            GameEvents.SaveStatusChanged -= OnSaveStatusChanged;
            GameEvents.RecoveryNoticeChanged -= OnRecoveryNoticeChanged;
            GameEvents.RelicProjectChanged -= OnProjectChanged;
        }

        private void OnStateChanged(GameState state)
        {
            if (currentState == GameState.Camp && state != GameState.Camp && saveStatus == SaveFeedbackStatus.Succeeded)
            {
                saveStatus = null;
            }

            currentState = state;
            if (stateText != null)
            {
                stateText.text = $"State: {state}";
            }

            UpdateSaveStatusText();
        }

        private void OnBattleLog(string message)
        {
            hasTerminalOutcome = false;
            primaryMessage = message;
            RenderMessage();
        }

        private void OnBattleOutcome(string message)
        {
            hasTerminalOutcome = true;
            primaryMessage = message;
            RenderMessage();
        }

        private void OnLevelUp(string message)
        {
            primaryMessage = hasTerminalOutcome && !string.IsNullOrEmpty(primaryMessage)
                ? $"{primaryMessage}\n{message}"
                : message;
            RenderMessage();
        }

        private void OnSaveStatusChanged(SaveFeedbackStatus status)
        {
            saveStatus = status;
            saveFailureActive = status == SaveFeedbackStatus.Failed;
            UpdateSaveStatusText();
            RenderMessage();
        }

        private void OnRecoveryNoticeChanged(bool pending)
        {
            recoveryNoticePending = pending;
            UpdateSaveStatusText();
            RenderMessage();
        }

        private void OnProjectChanged(RelicProjectSnapshot project)
        {
            if (!project.Ready && !project.Forged) return;
            primaryMessage = project.Forged
                ? "First Relic Project complete: Toilbound Relic forged."
                : "First Relic Project ready: Forge the Toilbound Relic at Camp.";
            RenderMessage();
        }

        private void UpdateSaveStatusText()
        {
            if (saveStatusText == null)
            {
                return;
            }

            var saveFeedback = saveStatus switch
            {
                SaveFeedbackStatus.Succeeded => "Save: Saved just now",
                SaveFeedbackStatus.Failed => "Save: Failed",
                _ => string.Empty
            };
            saveStatusText.text = currentState == GameState.Camp && recoveryNoticePending
                ? string.IsNullOrEmpty(saveFeedback)
                    ? RecoveryNotice
                    : $"{RecoveryNotice}\n{saveFeedback}"
                : saveFeedback;

            var showAtTitle = currentState == GameState.Title && recoveryNoticePending && saveStatus.HasValue;
            var showAtCamp = currentState == GameState.Camp && (recoveryNoticePending || saveStatus.HasValue);
            saveStatusText.gameObject.SetActive(showAtTitle || showAtCamp);
            UpdateStatusGeometry(currentState == GameState.Camp && recoveryNoticePending);
        }

        private void RenderMessage()
        {
            if (messageText == null)
            {
                return;
            }

            if (currentState == GameState.Title && recoveryNoticePending)
            {
                messageText.text = RecoveryNotice;
                return;
            }

            var failureBelongsInPrimary = saveFailureActive &&
                !(currentState == GameState.Camp && recoveryNoticePending);
            messageText.text = failureBelongsInPrimary
                ? string.IsNullOrEmpty(primaryMessage)
                    ? SaveFailureWarning
                    : $"{primaryMessage}\n{SaveFailureWarning}"
                : primaryMessage;
        }

        private void UpdateStatusGeometry(bool recoveryAtCamp)
        {
            var statusRect = transform as RectTransform;
            if (statusRect != null)
            {
                var size = statusRect.sizeDelta;
                size.y = recoveryAtCamp ? RecoveryStatusHeight : DefaultStatusHeight;
                statusRect.sizeDelta = size;
            }

            var saveRect = saveStatusText.rectTransform;
            var saveSize = saveRect.sizeDelta;
            saveSize.y = recoveryAtCamp ? RecoverySaveStatusHeight : DefaultSaveStatusHeight;
            saveRect.sizeDelta = saveSize;
        }
    }
}
