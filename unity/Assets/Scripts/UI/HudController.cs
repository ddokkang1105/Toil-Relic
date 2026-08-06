using ToilRelic.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ToilRelic.Unity.UI
{
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private Text hpText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text invText;
        [SerializeField] private Text equipmentText;
        [SerializeField] private Text projectText;

        private void OnEnable()
        {
            GameEvents.PlayerChanged += OnPlayerChanged;
            GameEvents.RelicProjectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            GameEvents.PlayerChanged -= OnPlayerChanged;
            GameEvents.RelicProjectChanged -= OnProjectChanged;
        }

        private void OnProjectChanged(RelicProjectSnapshot project)
        {
            if (projectText == null) return;
            projectText.text = project.Forged
                ? "Relic: Forged"
                : project.Ready
                    ? "Relic: 3/3 Ready"
                    : $"Relic: {project.Completed}/{project.Required}";
        }

        private void OnPlayerChanged(PlayerState player)
        {
            if (hpText != null)
            {
                hpText.text = $"HP {player.Hp}/{player.MaxHp}";
            }

            if (levelText != null)
            {
                levelText.text = $"Lv {player.LevelProgressValue:0.00}";
            }

            if (invText != null)
            {
                invText.text = $"Junk {player.GetAmount(ItemType.Junk)} | Part {player.GetAmount(ItemType.RelicPart)} | Potion {player.GetAmount(ItemType.HealingPotion)} | Treasure {player.TreasureCount}";
            }

            if (equipmentText != null)
            {
                equipmentText.text = player.TryGetPrimaryWeapon(out var weapon)
                    ? $"Wpn {weapon.DisplayName} | ATK +{player.AttackBonus} | DEF +{player.DefenseBonus}"
                    : "Wpn unavailable";
            }
        }
    }
}
