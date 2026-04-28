using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockGenerationProfile", menuName = "Ellish/City Generation/Block Generation Profile")]
public class BlockGenerationProfile : ScriptableObject
{
    public List<BlockLandUseOverrideEntry> overrides = new List<BlockLandUseOverrideEntry>();
    public List<BlockUrbanBlockTypeOverrideEntry> urbanBlockTypeOverrides = new List<BlockUrbanBlockTypeOverrideEntry>();

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

    public bool TryGetUrbanBlockTypeOverride(string stableKey, out UrbanBlockType urbanBlockType)
    {
        for (int i = 0; i < urbanBlockTypeOverrides.Count; i++)
        {
            BlockUrbanBlockTypeOverrideEntry entry = urbanBlockTypeOverrides[i];
            if (entry != null && entry.stableKey == stableKey)
            {
                urbanBlockType = entry.urbanBlockType;
                return true;
            }
        }

        urbanBlockType = UrbanBlockType.Default;
        return false;
    }

    public void SetUrbanBlockTypeOverride(string stableKey, UrbanBlockType urbanBlockType)
    {
        if (string.IsNullOrWhiteSpace(stableKey))
            return;

        if (urbanBlockType == UrbanBlockType.Default || urbanBlockType == UrbanBlockType.Park)
            return;

        for (int i = 0; i < urbanBlockTypeOverrides.Count; i++)
        {
            BlockUrbanBlockTypeOverrideEntry entry = urbanBlockTypeOverrides[i];
            if (entry != null && entry.stableKey == stableKey)
            {
                entry.urbanBlockType = urbanBlockType;
                return;
            }
        }

        urbanBlockTypeOverrides.Add(new BlockUrbanBlockTypeOverrideEntry
        {
            stableKey = stableKey,
            urbanBlockType = urbanBlockType
        });
    }

    public void RemoveUrbanBlockTypeOverride(string stableKey)
    {
        if (string.IsNullOrWhiteSpace(stableKey))
            return;

        for (int i = urbanBlockTypeOverrides.Count - 1; i >= 0; i--)
        {
            BlockUrbanBlockTypeOverrideEntry entry = urbanBlockTypeOverrides[i];
            if (entry != null && entry.stableKey == stableKey)
                urbanBlockTypeOverrides.RemoveAt(i);
        }
    }

    public void ClearUrbanBlockTypeOverrides()
    {
        urbanBlockTypeOverrides.Clear();
    }
}

[System.Serializable]
public class BlockLandUseOverrideEntry
{
    public string stableKey;
    public BlockLandUse landUse;
}

[System.Serializable]
public class BlockUrbanBlockTypeOverrideEntry
{
    public string stableKey;
    public UrbanBlockType urbanBlockType;
}
