using UnityEngine;

namespace ToilRelic.Unity.Data
{
    [CreateAssetMenu(menuName = "ToilRelic/Enemy", fileName = "EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("Gameplay")]
        public string id = "mine-vermin";
        public string displayName = "Mine Vermin";
        public int maxHp = 12;
        public int attackMin = 2;
        public int attackMax = 5;
        public int expReward = 12;
        [Tooltip("Stable equipment-drop profile ID used by the Hunt Contract.")]
        public string equipmentDropProfileId;

        [Header("Presentation")]
        [Tooltip("Optional battle visual. This can be either a 2D or 3D prefab.")]
        public GameObject battleVisualPrefab;
        [Tooltip("Optional 2D HUD portrait. Leave empty when using a 3D-only presentation.")]
        public Sprite portrait;
    }
}
