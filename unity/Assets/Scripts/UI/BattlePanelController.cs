using System.Text;
using TMPro;
using ToilRelic.Unity.Core;
using UnityEngine;

namespace ToilRelic.Unity.UI
{
    public sealed class BattlePanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text enemyText;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private int maxLogLines = 10;

        private readonly StringBuilder logBuffer = new();

        private void OnEnable()
        {
            GameEvents.EnemyChanged += OnEnemyChanged;
            GameEvents.BattleLog += OnBattleLog;
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.EnemyChanged -= OnEnemyChanged;
            GameEvents.BattleLog -= OnBattleLog;
            GameEvents.StateChanged -= OnStateChanged;
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
            AppendLog($"State -> {state}");
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
    }
}
