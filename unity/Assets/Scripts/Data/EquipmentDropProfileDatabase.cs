using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToilRelic.Unity.Data
{
    [CreateAssetMenu(menuName = "ToilRelic/Equipment Drop Profile Database", fileName = "EquipmentDropProfileDatabase")]
    public sealed class EquipmentDropProfileDatabase : ScriptableObject
    {
        public List<EquipmentDropProfileData> profiles = new();

        public bool TryGet(string id, out EquipmentDropProfileData profile)
        {
            profile = null;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            foreach (var candidate in profiles)
            {
                if (candidate != null && string.Equals(candidate.id, id, StringComparison.Ordinal))
                {
                    profile = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
