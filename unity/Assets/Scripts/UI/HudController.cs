using TMPro;
using ToilRelic.Unity.Core;
using UnityEngine;

namespace ToilRelic.Unity.UI
{
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text invText;

        private void OnEnable()
        {
            GameEvents.PlayerChanged += OnPlayerChanged;
        }

        private void OnDisable()
        {
            GameEvents.PlayerChanged -= OnPlayerChanged;
        }

        private void OnPlayerChanged(PlayerState player)
        {
            if (hpText != null)
            {
                hpText.text = $"HP {player.Hp}/{player.MaxHp}";
            }

            if (levelText != null)
            {
                levelText.text = $"Level {player.LevelProgressValue:0.00} | EXP {player.Experience}/{player.ExperienceToNextLevel}";
            }

            if (invText != null)
            {
                invText.text = $"Junk {player.GetAmount(ItemType.Junk)} | Relic Part {player.GetAmount(ItemType.RelicPart)} | Treasure {player.TreasureCount}";
            }
        }
    }
}
