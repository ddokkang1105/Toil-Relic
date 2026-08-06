using UnityEngine;

namespace ToilRelic.Unity.Data
{
    [CreateAssetMenu(menuName = "ToilRelic/Equipment Drop Profile", fileName = "EquipmentDropProfileData")]
    public sealed class EquipmentDropProfileData : ScriptableObject
    {
        public string id;
        public string equipmentId;
        [Range(0f, 1f)] public float chance = 0.35f;

        public bool HasValidShape =>
            !string.IsNullOrWhiteSpace(id) &&
            !string.IsNullOrWhiteSpace(equipmentId) &&
            chance > 0f &&
            chance <= 1f;
    }
}
