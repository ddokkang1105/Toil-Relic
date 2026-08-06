using ToilRelic.Unity.Core;
using UnityEngine;

namespace ToilRelic.Unity.UI
{
    public sealed class GameActionBridge : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        public void StartHunt() => gameManager.StartHunt();
        public void ContinueGame() => gameManager.ContinueGame();
        public void StartNewGame() => gameManager.StartNewGame();
        public void Quit() => Application.Quit();
        public void Attack() => gameManager.Attack();
        public void Defend() => gameManager.Defend();
        public void Flee() => gameManager.Flee();
        public void UsePotion() => gameManager.UsePotion();
        public void Rest() => gameManager.Rest();
        public void CraftTreasure() => gameManager.CraftTreasure();
        public void ForgeRelic() => gameManager.ForgeRelic();
        public void EquipStarterWeapon() => gameManager.EquipStarterWeapon();
    }
}
