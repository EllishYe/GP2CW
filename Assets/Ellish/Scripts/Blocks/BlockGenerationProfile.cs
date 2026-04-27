using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockGenerationProfile", menuName = "Ellish/City Generation/Block Generation Profile")]
public class BlockGenerationProfile : ScriptableObject
{
    public List<BlockLandUseOverrideEntry> overrides = new List<BlockLandUseOverrideEntry>();

    public bool TryGetOverride(string stableKey, out BlockLandUse landUse)
    {
        for (int i = 0; i < overrides.Count; i++)
        {
            BlockLandUseOverrideEntry entry = overrides[i];
            if (entry != null && entry.stableKey == stableKey)
            {
                landUse = entry.landUse;
                return true;
            }
        }

        landUse = BlockLandUse.Building;
        return false;
    }

    public void SetOverride(string stableKey, BlockLandUse landUse)
    {
        if (string.IsNullOrWhiteSpace(stableKey))
            return;

        for (int i = 0; i < overrides.Count; i++)
        {
            BlockLandUseOverrideEntry entry = overrides[i];
            if (entry != null && entry.stableKey == stableKey)
            {
                entry.landUse = landUse;
                return;
            }
        }

        overrides.Add(new BlockLandUseOverrideEntry
        {
            stableKey = stableKey,
            landUse = landUse
        });
    }

    public void ClearOverrides()
    {
        overrides.Clear();
    }
}

[System.Serializable]
public class BlockLandUseOverrideEntry
{
    public string stableKey;
    public BlockLandUse landUse;
}
