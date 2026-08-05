using System.Collections.Generic;
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
        [SerializeField] private int maxLogLines = 2;

        private readonly StringBuilder logBuffer = new();
        private readonly Queue<string> logicalLogLines = new();

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

        private void AppendLog(string message)
        {
            var lineLimit = Mathf.Max(0, maxLogLines);
            if (lineLimit == 0)
            {
                ClearLog();
                return;
            }

            if (!EnqueueLogicalLines(message, lineLimit))
            {
                return;
            }

            logBuffer.Clear();
            foreach (var logicalLine in logicalLogLines)
            {
                if (logBuffer.Length > 0)
                {
                    logBuffer.Append('\n');
                }

                logBuffer.Append(logicalLine);
            }

            if (logText != null)
            {
                logText.text = logBuffer.ToString();
            }
        }

        private bool EnqueueLogicalLines(string content, int lineLimit)
        {
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            var acceptedLine = false;
            var lineStart = 0;
            for (var index = 0; index <= content.Length; index++)
            {
                if (index < content.Length && content[index] != '\n')
                {
                    continue;
                }

                var logicalLine = content.Substring(lineStart, index - lineStart).Trim();
                lineStart = index + 1;
                if (logicalLine.Length == 0)
                {
                    continue;
                }

                logicalLogLines.Enqueue(logicalLine);
                while (logicalLogLines.Count > lineLimit)
                {
                    logicalLogLines.Dequeue();
                }

                acceptedLine = true;
            }

            return acceptedLine;
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

            ClearLog();

            SetInteractable(attackButton, false);
            SetInteractable(defendButton, false);
            SetInteractable(fleeButton, false);
            SetInteractable(potionButton, false);
        }

        private void ClearLog()
        {
            logicalLogLines.Clear();
            logBuffer.Clear();
            if (logText != null)
            {
                logText.text = string.Empty;
            }
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
