using System.Text;
using ToilRelic.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ToilRelic.Unity.UI
{
    public sealed class BattlePanelController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Text enemyText;
        [SerializeField] private Text phaseText;
        [SerializeField] private Text logText;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private Button fleeButton;
        [SerializeField] private Button potionButton;
        [SerializeField] private int maxLogLines = 10;

        private readonly StringBuilder logBuffer = new();

        private void OnEnable()
        {
            GameEvents.EnemyChanged += OnEnemyChanged;
            GameEvents.BattleLog += OnBattleLog;
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.BattlePhaseChanged += OnBattlePhaseChanged;
            UpdateActionAvailability();
        }

        private void OnDisable()
        {
            GameEvents.EnemyChanged -= OnEnemyChanged;
            GameEvents.BattleLog -= OnBattleLog;
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.BattlePhaseChanged -= OnBattlePhaseChanged;
            ClearBattleSurface();
        }

        private void OnEnemyChanged(string name, int hp, int maxHp)
        {
            if (enemyText != null)
            {
                enemyText.text = $"Enemy: {name} ({hp}/{maxHp})";
            }
        }

        private void OnBattleLog(string message)
        {
            AppendLog(message);
        }

        private void OnStateChanged(GameState state)
        {
            if (state != GameState.Battle)
            {
                ClearBattleSurface();
                return;
            }

            UpdateActionAvailability();
        }

        private void OnBattlePhaseChanged(BattlePhase phase)
        {
            if (phaseText != null)
            {
                phaseText.text = phase switch
                {
                    BattlePhase.PlayerAction => "Your turn — choose an action.",
                    BattlePhase.EnemyAction => "Enemy turn — resolving attack.",
                    BattlePhase.Resolving => "Resolving battle result...",
                    _ => string.Empty
                };
            }

            UpdateActionAvailability();
        }

        private void AppendLog(string line)
        {
            var current = logBuffer.ToString().Split('\n');
            logBuffer.Clear();

            var start = Mathf.Max(0, current.Length - maxLogLines + 1);
            for (var i = start; i < current.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(current[i]))
                {
                    continue;
                }

                logBuffer.AppendLine(current[i]);
            }

            logBuffer.AppendLine(line);
            if (logText != null)
            {
                logText.text = logBuffer.ToString();
            }
        }

        private void UpdateActionAvailability()
        {
            var canAct = gameManager != null
                && gameManager.CurrentState == GameState.Battle
                && gameManager.CurrentBattlePhase == BattlePhase.PlayerAction;
            SetInteractable(attackButton, canAct);
            SetInteractable(defendButton, canAct);
            SetInteractable(fleeButton, canAct);
            SetInteractable(potionButton, canAct);
        }

        private void ClearBattleSurface()
        {
            if (enemyText != null)
            {
                enemyText.text = "Enemy: -";
            }

            if (phaseText != null)
            {
                phaseText.text = string.Empty;
            }

            logBuffer.Clear();
            if (logText != null)
            {
                logText.text = string.Empty;
            }

            SetInteractable(attackButton, false);
            SetInteractable(defendButton, false);
            SetInteractable(fleeButton, false);
            SetInteractable(potionButton, false);
        }

        private static void SetInteractable(Button button, bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }
    }
}
