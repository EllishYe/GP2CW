using UnityEngine;

public class BlockDebugComponent : MonoBehaviour
{
    public BlockAreaGenerator owner;
    public int blockId;
    public string stableKey;
    public BlockLandUse landUse;
    public UrbanBlockType urbanBlockType = UrbanBlockType.Default;
    public BlockLandUseOverride landUseOverride = BlockLandUseOverride.Auto;
    public UrbanBlockTypeOverride urbanBlockTypeOverride = UrbanBlockTypeOverride.Auto;
    public double area;
    public Vector3 center;

    public BlockLandUse EffectiveLandUse
    {
        get
        {
            if (landUseOverride == BlockLandUseOverride.Building)
                return BlockLandUse.Building;
            if (landUseOverride == BlockLandUseOverride.Park)
                return BlockLandUse.Park;
            return landUse;
        }
    }

    public UrbanBlockType EffectiveUrbanBlockType
    {
        get
        {
            if (EffectiveLandUse == BlockLandUse.Park)
                return UrbanBlockType.Park;

            switch (urbanBlockTypeOverride)
            {
                case UrbanBlockTypeOverride.Residential:
                    return UrbanBlockType.Residential;
                case UrbanBlockTypeOverride.Commercial:
                    return UrbanBlockType.Commercial;
                case UrbanBlockTypeOverride.Industrial:
                    return UrbanBlockType.Industrial;
                default:
                    return urbanBlockType == UrbanBlockType.Park ? UrbanBlockType.Residential : urbanBlockType;
            }
        }
    }
}
