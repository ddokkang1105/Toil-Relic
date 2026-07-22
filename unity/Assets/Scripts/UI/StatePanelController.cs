using ToilRelic.Unity.Core;
using UnityEngine;

namespace ToilRelic.Unity.UI
{
    public sealed class StatePanelController : MonoBehaviour
    {
        [SerializeField] private GameObject campPanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject titlePanel;

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            var inCamp = state == GameState.Camp;
            var inBattle = state == GameState.Battle;
            var atTitle = state == GameState.Title;

            if (titlePanel != null)
            {
                titlePanel.SetActive(atTitle);
            }

            if (campPanel != null)
            {
                campPanel.SetActive(inCamp);
            }

            if (battlePanel != null)
            {
                battlePanel.SetActive(inBattle);
            }
        }
    }
}
