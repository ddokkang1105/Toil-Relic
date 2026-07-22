using ToilRelic.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ToilRelic.Unity.UI
{
    public sealed class TitleMenuController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button continueButton;

        private void Start()
        {
            if (continueButton != null)
            {
                continueButton.interactable = gameManager != null && gameManager.HasSavedGame;
            }
        }
    }
}
