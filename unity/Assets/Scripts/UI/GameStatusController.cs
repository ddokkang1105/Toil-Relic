using ToilRelic.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ToilRelic.Unity.UI
{
    public sealed class GameStatusController : MonoBehaviour
    {
        [SerializeField] private Text stateText;
        [SerializeField] private Text messageText;
        private bool hasTerminalOutcome;

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.BattleLog += OnBattleLog;
            GameEvents.BattleOutcome += OnBattleOutcome;
            GameEvents.LevelUp += OnLevelUp;
            GameEvents.SaveFailed += OnSaveFailed;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.BattleLog -= OnBattleLog;
            GameEvents.BattleOutcome -= OnBattleOutcome;
            GameEvents.LevelUp -= OnLevelUp;
            GameEvents.SaveFailed -= OnSaveFailed;
        }

        private void OnStateChanged(GameState state)
        {
            if (stateText != null)
            {
                stateText.text = $"State: {state}";
            }
        }

        private void OnBattleLog(string message)
        {
            hasTerminalOutcome = false;
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        private void OnBattleOutcome(string message)
        {
            hasTerminalOutcome = true;
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        private void OnLevelUp(string message)
        {
            if (messageText != null)
            {
                messageText.text = hasTerminalOutcome && !string.IsNullOrEmpty(messageText.text)
                    ? $"{messageText.text}\n{message}"
                    : message;
            }
        }

        private void OnSaveFailed(string message)
        {
            if (messageText != null)
            {
                messageText.text = hasTerminalOutcome && !string.IsNullOrEmpty(messageText.text)
                    ? $"{messageText.text}\n{message}"
                    : message;
            }
        }
    }
}
