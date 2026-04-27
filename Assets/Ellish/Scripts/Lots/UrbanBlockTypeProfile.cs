using UnityEngine;

[System.Serializable]
public class UrbanBlockTypeProfile
{
    public UrbanBlockType type = UrbanBlockType.Default;

    [Header("Subdivision")]
    public int maxSubdivisionDepth = 4;
    public float minLotArea = 600f;
    public float maxAspectRatio = 4f;

    [Header("Building Footprint")]
    public float buildingSetback = 3f;

    [Header("Height")]
    public float minHeight = 6f;
    public float maxHeight = 18f;

    [Header("Visual")]
    public Material blockMaterial;
    public Material buildingMaterial;

    public void Sanitize()
    {
        maxSubdivisionDepth = Mathf.Max(0, maxSubdivisionDepth);
        minLotArea = Mathf.Max(1f, minLotArea);
        maxAspectRatio = Mathf.Max(1f, maxAspectRatio);
        buildingSetback = Mathf.Max(0f, buildingSetback);
        minHeight = Mathf.Max(0f, minHeight);
        maxHeight = Mathf.Max(minHeight + 0.01f, maxHeight);
    }

    public static UrbanBlockTypeProfile CreateDefault(UrbanBlockType type)
    {
        UrbanBlockTypeProfile profile = new UrbanBlockTypeProfile
        {
            type = type
        };

        switch (type)
        {
            case UrbanBlockType.Commercial:
                profile.maxSubdivisionDepth = 5;
                profile.minLotArea = 350f;
                profile.maxAspectRatio = 5f;
                profile.buildingSetback = 1.5f;
                profile.minHeight = 20f;
                profile.maxHeight = 80f;
                break;
            case UrbanBlockType.Industrial:
                profile.maxSubdivisionDepth = 2;
                profile.minLotArea = 1800f;
                profile.maxAspectRatio = 4.5f;
                profile.buildingSetback = 2f;
                profile.minHeight = 5f;
                profile.maxHeight = 14f;
                break;
            case UrbanBlockType.Park:
                profile.maxSubdivisionDepth = 0;
                profile.minLotArea = 1000f;
                profile.maxAspectRatio = 8f;
                profile.buildingSetback = 0f;
                profile.minHeight = 0.02f;
                profile.maxHeight = 0.2f;
                break;
            case UrbanBlockType.Default:
                profile.maxSubdivisionDepth = 4;
                profile.minLotArea = 600f;
                profile.maxAspectRatio = 4f;
                profile.buildingSetback = 3f;
                profile.minHeight = 8f;
                profile.maxHeight = 24f;
                break;
            default:
                profile.maxSubdivisionDepth = 4;
                profile.minLotArea = 650f;
                profile.maxAspectRatio = 4f;
                profile.buildingSetback = 3f;
                profile.minHeight = 6f;
                profile.maxHeight = 18f;
                break;
        }

        return profile;
    }
}
