using UnityEngine;

public class BlockDebugComponent : MonoBehaviour
{
    public BlockAreaGenerator owner;
    public int blockId;
    public string stableKey;
    public BlockLandUse landUse;
    public UrbanBlockType urbanBlockType = UrbanBlockType.Default;
    public BlockLandUseOverride landUseOverride = BlockLandUseOverride.Auto;
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
}
