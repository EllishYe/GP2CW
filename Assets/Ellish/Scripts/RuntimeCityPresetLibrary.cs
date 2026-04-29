using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RuntimeCityPresetLibrary", menuName = "Ellish/City Generation/Runtime City Preset Library")]
public class RuntimeCityPresetLibrary : ScriptableObject
{
    public List<RuntimeCityPreset> presets = new List<RuntimeCityPreset>();

    public void AddOrReplace(RuntimeCityPreset preset)
    {
        if (preset == null || string.IsNullOrWhiteSpace(preset.presetName))
        {
            return;
        }

        for (int i = 0; i < presets.Count; i++)
        {
            RuntimeCityPreset existing = presets[i];
            if (existing != null && existing.presetName == preset.presetName)
            {
                presets[i] = preset;
                return;
            }
        }

        presets.Add(preset);
    }
}

[Serializable]
public class RuntimeCityPreset
{
    public string presetName = "Default";
    public int seed = 12345;

    [Header("Road Network")]
    public int mapSize = 1000;
    public float roadSegmentLength = 100f;
    public int majorRoadCount = 200;
    public int minorRoadCount = 400;
    [Range(0f, 1f)] public float branchProbability = 0.1f;
    [Range(0f, 1f)] public float deletionProbability = 0.2f;
    public float laneWidth = 3.5f;

    [Header("Blocks")]
    [Range(0f, 1f)] public float parkProbability = 0.08f;
    public float parkMinArea = 12000f;
    [Range(0f, 1f)] public float parkIrregularityThreshold = 0.62f;

    [Header("Lots")]
    public BlockTypeAssignmentMode assignmentMode = BlockTypeAssignmentMode.Default;
    public UrbanBlockType defaultBuildingType = UrbanBlockType.Residential;
    [Range(0f, 1f)] public float residentialWeight = 0.45f;
    [Range(0f, 1f)] public float commercialWeight = 0.25f;
    [Range(0f, 1f)] public float industrialWeight = 0.3f;
    [Range(0f, 1f)] public float commercialInnerNormalizedRadius = 0.25f;
    [Range(0f, 1f)] public float residentialMiddleNormalizedRadius = 0.65f;

    [Header("Block Type Profiles")]
    public RuntimeBlockTypePreset residential = new RuntimeBlockTypePreset(6f, 18f, 3f);
    public RuntimeBlockTypePreset commercial = new RuntimeBlockTypePreset(20f, 80f, 1.5f);
    public RuntimeBlockTypePreset industrial = new RuntimeBlockTypePreset(5f, 14f, 2f);

    public static RuntimeCityPreset CreateDefault()
    {
        return new RuntimeCityPreset { presetName = "Default" };
    }

    public static RuntimeCityPreset CreateDenseCity()
    {
        return new RuntimeCityPreset
        {
            presetName = "Dense City",
            seed = 23191,
            roadSegmentLength = 72f,
            majorRoadCount = 320,
            minorRoadCount = 760,
            branchProbability = 0.18f,
            deletionProbability = 0.08f,
            parkProbability = 0.04f,
            assignmentMode = BlockTypeAssignmentMode.DistanceToCenter,
            commercialInnerNormalizedRadius = 0.32f,
            residentialMiddleNormalizedRadius = 0.72f,
            residential = new RuntimeBlockTypePreset(12f, 38f, 2f),
            commercial = new RuntimeBlockTypePreset(35f, 110f, 1f),
            industrial = new RuntimeBlockTypePreset(8f, 22f, 2f)
        };
    }

    public static RuntimeCityPreset CreateSparseGrid()
    {
        return new RuntimeCityPreset
        {
            presetName = "Sparse Grid",
            seed = 12011,
            roadSegmentLength = 140f,
            majorRoadCount = 120,
            minorRoadCount = 220,
            branchProbability = 0.06f,
            deletionProbability = 0.28f,
            parkProbability = 0.12f,
            assignmentMode = BlockTypeAssignmentMode.Default,
            defaultBuildingType = UrbanBlockType.Residential,
            residential = new RuntimeBlockTypePreset(5f, 16f, 4f),
            commercial = new RuntimeBlockTypePreset(12f, 36f, 2f),
            industrial = new RuntimeBlockTypePreset(5f, 12f, 3f)
        };
    }

    public static RuntimeCityPreset CreateOrganic()
    {
        return new RuntimeCityPreset
        {
            presetName = "Organic",
            seed = 8027,
            roadSegmentLength = 86f,
            majorRoadCount = 230,
            minorRoadCount = 520,
            branchProbability = 0.28f,
            deletionProbability = 0.18f,
            parkProbability = 0.16f,
            parkIrregularityThreshold = 0.5f,
            assignmentMode = BlockTypeAssignmentMode.RandomWeighted,
            residentialWeight = 0.58f,
            commercialWeight = 0.24f,
            industrialWeight = 0.18f
        };
    }

    public static RuntimeCityPreset CreateIndustrial()
    {
        return new RuntimeCityPreset
        {
            presetName = "Industrial",
            seed = 55102,
            roadSegmentLength = 125f,
            majorRoadCount = 160,
            minorRoadCount = 300,
            branchProbability = 0.09f,
            deletionProbability = 0.22f,
            laneWidth = 4f,
            parkProbability = 0.03f,
            assignmentMode = BlockTypeAssignmentMode.RandomWeighted,
            residentialWeight = 0.18f,
            commercialWeight = 0.22f,
            industrialWeight = 0.6f,
            residential = new RuntimeBlockTypePreset(5f, 14f, 3f),
            commercial = new RuntimeBlockTypePreset(10f, 34f, 2f),
            industrial = new RuntimeBlockTypePreset(6f, 20f, 4f)
        };
    }

    public static RuntimeCityPreset CreateFromController(CityGenerationController controller, string presetName)
    {
        RuntimeCityPreset preset = new RuntimeCityPreset
        {
            presetName = string.IsNullOrWhiteSpace(presetName) ? "Runtime Preset" : presetName
        };

        if (controller == null)
        {
            return preset;
        }

        controller.FindStageGenerators();

        RoadNetworkGenerator roads = controller.roadNetworkGenerator;
        if (roads != null)
        {
            preset.seed = roads.randomSeed;
            preset.mapSize = roads.mapSize;
            preset.roadSegmentLength = roads.roadSegmentLength;
            preset.majorRoadCount = roads.majorRoadCount;
            preset.minorRoadCount = roads.minorRoadCount;
            preset.branchProbability = roads.branchProbability;
            preset.deletionProbability = roads.deletionProbability;
            preset.laneWidth = roads.laneWidth;
        }

        BlockAreaGenerator blocks = controller.blockAreaGenerator;
        if (blocks != null)
        {
            preset.parkProbability = blocks.parkProbability;
            preset.parkMinArea = blocks.parkMinArea;
            preset.parkIrregularityThreshold = blocks.parkIrregularityThreshold;
        }

        LotAreaGenerator lots = controller.lotAreaGenerator;
        if (lots != null)
        {
            preset.assignmentMode = lots.assignmentMode;
            preset.defaultBuildingType = lots.defaultBuildingType;
            preset.residentialWeight = lots.residentialWeight;
            preset.commercialWeight = lots.commercialWeight;
            preset.industrialWeight = lots.industrialWeight;
            preset.commercialInnerNormalizedRadius = lots.commercialInnerNormalizedRadius;
            preset.residentialMiddleNormalizedRadius = lots.residentialMiddleNormalizedRadius;
            preset.residential = RuntimeBlockTypePreset.CreateFromProfile(lots.GetProfile(UrbanBlockType.Residential));
            preset.commercial = RuntimeBlockTypePreset.CreateFromProfile(lots.GetProfile(UrbanBlockType.Commercial));
            preset.industrial = RuntimeBlockTypePreset.CreateFromProfile(lots.GetProfile(UrbanBlockType.Industrial));
        }

        return preset;
    }
}

[Serializable]
public class RuntimeBlockTypePreset
{
    public float minHeight = 6f;
    public float maxHeight = 18f;
    public float buildingSetback = 3f;

    public RuntimeBlockTypePreset(float minHeight, float maxHeight, float buildingSetback)
    {
        this.minHeight = minHeight;
        this.maxHeight = maxHeight;
        this.buildingSetback = buildingSetback;
    }

    public static RuntimeBlockTypePreset CreateFromProfile(UrbanBlockTypeProfile profile)
    {
        if (profile == null)
        {
            return new RuntimeBlockTypePreset(6f, 18f, 3f);
        }

        return new RuntimeBlockTypePreset(profile.minHeight, profile.maxHeight, profile.buildingSetback);
    }
}
