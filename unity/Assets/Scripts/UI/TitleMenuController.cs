using ToilRelic.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ToilRelic.Unity.UI
{
    public sealed class TitleMenuController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button continueButton;

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
        }

        private void Start()
        {
            RefreshContinueAvailability();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Title)
            {
                RefreshContinueAvailability();
            }
        }

        private void RefreshContinueAvailability()
        {
            if (continueButton != null)
            {
                continueButton.interactable = gameManager != null && gameManager.HasSavedGame;
            }
        }
    }
}
