using ToilRelic.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ToilRelic.Unity.UI
{
    public sealed class GameStatusController : MonoBehaviour
    {
        private const string SaveFailureWarning = "Save failed. Progress may not be saved.";

        [SerializeField] private Text stateText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text saveStatusText;

        private bool hasTerminalOutcome;
        private bool saveFailureActive;
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
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.BattleLog -= OnBattleLog;
            GameEvents.BattleOutcome -= OnBattleOutcome;
            GameEvents.LevelUp -= OnLevelUp;
            GameEvents.SaveStatusChanged -= OnSaveStatusChanged;
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

        private void UpdateSaveStatusText()
        {
            if (saveStatusText == null)
            {
                return;
            }

            saveStatusText.text = saveStatus switch
            {
                SaveFeedbackStatus.Succeeded => "Save: Saved just now",
                SaveFeedbackStatus.Failed => "Save: Failed",
                _ => string.Empty
            };
            saveStatusText.gameObject.SetActive(currentState == GameState.Camp && saveStatus.HasValue);
        }

        private void RenderMessage()
        {
            if (messageText == null)
            {
                return;
            }

            messageText.text = saveFailureActive
                ? string.IsNullOrEmpty(primaryMessage)
                    ? SaveFailureWarning
                    : $"{primaryMessage}\n{SaveFailureWarning}"
                : primaryMessage;
        }
    }
}
