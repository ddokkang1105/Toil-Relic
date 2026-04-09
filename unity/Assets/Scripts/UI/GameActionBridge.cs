using ToilRelic.Unity.Core;
using UnityEngine;

namespace ToilRelic.Unity.UI
{
    public sealed class GameActionBridge : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        public void StartHunt() => gameManager.StartHunt();
        public void Attack() => gameManager.Attack();
        public void Defend() => gameManager.Defend();
        public void Flee() => gameManager.Flee();
        public void Rest() => gameManager.Rest();
        public void CraftTreasure() => gameManager.CraftTreasure();
    }
}
