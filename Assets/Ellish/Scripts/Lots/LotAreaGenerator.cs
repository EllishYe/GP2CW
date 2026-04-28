using System.Collections.Generic;
using Clipper2Lib;
using UnityEngine;

public class LotAreaGenerator : MonoBehaviour
{
    private const float ClipperScale = 1000f;

    [Header("Block Type Assignment")]
    public BlockTypeAssignmentMode assignmentMode = BlockTypeAssignmentMode.Default;
    public UrbanBlockType defaultBuildingType = UrbanBlockType.Residential;
    public int randomSeed = 12345;

    [Header("Random Weights")]
    [Range(0f, 1f)] public float residentialWeight = 0.45f;
    [Range(0f, 1f)] public float commercialWeight = 0.25f;
    [Range(0f, 1f)] public float industrialWeight = 0.30f;

    [Header("Distance To Center")]
    [Range(0f, 1f)] public float commercialInnerNormalizedRadius = 0.25f;
    [Range(0f, 1f)] public float residentialMiddleNormalizedRadius = 0.65f;

    [Header("Subdivision")]
    public bool enableSubdivision = true;
    [Range(0.2f, 0.8f)] public float minCutRatio = 0.35f;
    [Range(0.2f, 0.8f)] public float maxCutRatio = 0.65f;

    [Header("Height Falloff")]
    public bool applyCenterHeightFalloff = true;
    [Range(0f, 1f)] public float edgeHeightMultiplier = 0.45f;

    [Header("Block Type Preview")]
    public Color residentialBlockPreviewColor = new Color(0.78f, 0.70f, 0.62f, 1f);
    public Color commercialBlockPreviewColor = new Color(0.42f, 0.54f, 0.68f, 1f);
    public Color industrialBlockPreviewColor = new Color(0.56f, 0.56f, 0.52f, 1f);

    [Header("Profiles")]
    public List<UrbanBlockTypeProfile> typeProfiles = new List<UrbanBlockTypeProfile>();

    [Header("Generated Data")]
    public List<LotData> lots = new List<LotData>();

    private readonly Dictionary<UrbanBlockType, Material> blockPreviewMaterials = new Dictionary<UrbanBlockType, Material>();

    void Reset()
    {
        EnsureDefaultProfiles();
    }

    void OnValidate()
    {
        randomSeed = Mathf.Max(0, randomSeed);
        minCutRatio = Mathf.Clamp01(minCutRatio);
        maxCutRatio = Mathf.Clamp(maxCutRatio, minCutRatio, 1f);
        edgeHeightMultiplier = Mathf.Clamp01(edgeHeightMultiplier);
        EnsureDefaultProfiles();
        for (int i = 0; i < typeProfiles.Count; i++)
            typeProfiles[i]?.Sanitize();
    }

    public void Generate(BlockAreaGenerator blockAreaGenerator, RoadNetworkGenerator roadNetworkGenerator)
    {
        Clear();

        if (blockAreaGenerator == null)
        {
            Debug.LogWarning("LotAreaGenerator: BlockAreaGenerator is not assigned.");
            return;
        }

        blockAreaGenerator.ApplyDebugOverridesFromScene();
        EnsureDefaultProfiles();

        AssignBlockTypes(blockAreaGenerator, roadNetworkGenerator);
        ApplyBlockTypeOverridesFromScene(blockAreaGenerator);
        BuildLotsFromAssignedBlockTypes(blockAreaGenerator, roadNetworkGenerator);

        Debug.Log($"LotAreaGenerator: Generated {lots.Count} lot(s) from {blockAreaGenerator.GetBuildingBlocks().Count} building block(s).");
    }

    public void Clear()
    {
        lots.Clear();
    }

    public UrbanBlockTypeProfile GetProfile(UrbanBlockType type)
    {
        for (int i = 0; i < typeProfiles.Count; i++)
        {
            UrbanBlockTypeProfile profile = typeProfiles[i];
            if (profile != null && profile.type == type)
                return profile;
        }

        for (int i = 0; i < typeProfiles.Count; i++)
        {
            UrbanBlockTypeProfile profile = typeProfiles[i];
            if (profile != null && profile.type == UrbanBlockType.Default)
                return profile;
        }

        UrbanBlockTypeProfile fallback = UrbanBlockTypeProfile.CreateDefault(UrbanBlockType.Default);
        typeProfiles.Add(fallback);
        return fallback;
    }

    public void AssignBlockTypes(BlockAreaGenerator blockAreaGenerator, RoadNetworkGenerator roadNetworkGenerator)
    {
        if (blockAreaGenerator == null)
        {
            Debug.LogWarning("LotAreaGenerator: BlockAreaGenerator is not assigned.");
            return;
        }

        blockAreaGenerator.ApplyDebugOverridesFromScene();
        EnsureDefaultProfiles();

        List<BlockData> buildingBlocks = blockAreaGenerator.GetBuildingBlocks();
        System.Random random = new System.Random(randomSeed);
        float mapExtent = roadNetworkGenerator != null ? Mathf.Max(1f, roadNetworkGenerator.mapSize) : 1f;
        AssignBlockTypes(buildingBlocks, blockAreaGenerator.blockGenerationProfile, mapExtent, random);

        Debug.Log($"LotAreaGenerator: Assigned block types for {buildingBlocks.Count} building block(s).");
    }

    public void ApplyBlockTypeOverridesFromScene(BlockAreaGenerator blockAreaGenerator)
    {
        if (blockAreaGenerator == null)
        {
            Debug.LogWarning("LotAreaGenerator: BlockAreaGenerator is not assigned.");
            return;
        }

        BlockDebugComponent[] debugComponents = CollectBlockDebugComponents(blockAreaGenerator);
        if (debugComponents.Length == 0)
        {
            Debug.LogWarning("LotAreaGenerator: No BlockDebugComponent found. Generate blocks before applying block type overrides.");
            return;
        }

        for (int i = 0; i < debugComponents.Length; i++)
        {
            BlockDebugComponent debug = debugComponents[i];
            BlockData block = FindBlockByStableKey(blockAreaGenerator, debug.stableKey);
            if (block == null)
                continue;

            UrbanBlockType effectiveType = debug.EffectiveUrbanBlockType;
            if (block.landUse == BlockLandUse.Park || effectiveType == UrbanBlockType.Park)
                effectiveType = UrbanBlockType.Park;
            else if (effectiveType == UrbanBlockType.Default)
                effectiveType = defaultBuildingType == UrbanBlockType.Park ? UrbanBlockType.Residential : defaultBuildingType;

            block.urbanBlockType = effectiveType;
            debug.urbanBlockType = effectiveType;
            ApplyBlockTypePresentation(block);
        }

        Debug.Log($"LotAreaGenerator: Applied block type overrides from {debugComponents.Length} block debug component(s).");
    }

    public void SaveBlockTypeOverridesToProfile(BlockAreaGenerator blockAreaGenerator)
    {
        if (blockAreaGenerator == null)
        {
            Debug.LogWarning("LotAreaGenerator: BlockAreaGenerator is not assigned.");
            return;
        }

        BlockGenerationProfile profile = blockAreaGenerator.blockGenerationProfile;
        if (profile == null)
        {
            Debug.LogWarning("LotAreaGenerator: BlockGenerationProfile is not assigned on BlockAreaGenerator.");
            return;
        }

        ApplyBlockTypeOverridesFromScene(blockAreaGenerator);

        int savedCount = 0;
        BlockDebugComponent[] debugComponents = CollectBlockDebugComponents(blockAreaGenerator);
        for (int i = 0; i < debugComponents.Length; i++)
        {
            BlockDebugComponent debug = debugComponents[i];
            UrbanBlockType type = debug.EffectiveUrbanBlockType;
            if (debug.EffectiveLandUse == BlockLandUse.Park || debug.urbanBlockTypeOverride == UrbanBlockTypeOverride.Auto)
            {
                profile.RemoveUrbanBlockTypeOverride(debug.stableKey);
                continue;
            }

            if (type == UrbanBlockType.Park || type == UrbanBlockType.Default)
                continue;

            profile.SetUrbanBlockTypeOverride(debug.stableKey, type);
            savedCount++;
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(profile);
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log($"LotAreaGenerator: Saved {savedCount} block type override(s) to {profile.name}.");
    }

    public void ClearSavedBlockTypeOverrides(BlockAreaGenerator blockAreaGenerator)
    {
        if (blockAreaGenerator == null || blockAreaGenerator.blockGenerationProfile == null)
            return;

        blockAreaGenerator.blockGenerationProfile.ClearUrbanBlockTypeOverrides();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(blockAreaGenerator.blockGenerationProfile);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    public void BuildLotsFromAssignedBlockTypes(BlockAreaGenerator blockAreaGenerator, RoadNetworkGenerator roadNetworkGenerator)
    {
        Clear();

        if (blockAreaGenerator == null)
        {
            Debug.LogWarning("LotAreaGenerator: BlockAreaGenerator is not assigned.");
            return;
        }

        List<BlockData> buildingBlocks = blockAreaGenerator.GetBuildingBlocks();
        System.Random random = new System.Random(randomSeed);
        float mapExtent = roadNetworkGenerator != null ? Mathf.Max(1f, roadNetworkGenerator.mapSize) : 1f;
        BuildLots(buildingBlocks, random, mapExtent);
    }

    private void AssignBlockTypes(List<BlockData> buildingBlocks, BlockGenerationProfile profile, float mapExtent, System.Random random)
    {
        for (int i = 0; i < buildingBlocks.Count; i++)
        {
            BlockData block = buildingBlocks[i];
            UrbanBlockType generatedType = ResolveBlockType(block, random, mapExtent);
            bool hasSavedOverride = TryResolveSavedBlockType(profile, block.stableKey, out UrbanBlockType savedType);
            block.urbanBlockType = hasSavedOverride ? savedType : generatedType;
            SyncDebugUrbanBlockType(block, hasSavedOverride ? ToOverride(block.urbanBlockType) : UrbanBlockTypeOverride.Auto);
            ApplyBlockTypePresentation(block);
        }
    }

    private bool TryResolveSavedBlockType(BlockGenerationProfile profile, string stableKey, out UrbanBlockType savedType)
    {
        if (profile != null && profile.TryGetUrbanBlockTypeOverride(stableKey, out savedType))
            return true;

        savedType = UrbanBlockType.Default;
        return false;
    }

    private UrbanBlockType ResolveBlockType(BlockData block, System.Random random, float mapExtent)
    {
        if (block.landUse == BlockLandUse.Park)
            return UrbanBlockType.Park;

        switch (assignmentMode)
        {
            case BlockTypeAssignmentMode.RandomWeighted:
                return ResolveRandomWeightedType(random);
            case BlockTypeAssignmentMode.DistanceToCenter:
                return ResolveDistanceToCenterType(block.planarCenter, mapExtent);
            default:
                return defaultBuildingType == UrbanBlockType.Park ? UrbanBlockType.Residential : defaultBuildingType;
        }
    }

    private UrbanBlockType ResolveRandomWeightedType(System.Random random)
    {
        float totalWeight = Mathf.Max(0.001f, residentialWeight + commercialWeight + industrialWeight);
        double roll = random.NextDouble() * totalWeight;

        if (roll < residentialWeight)
            return UrbanBlockType.Residential;

        roll -= residentialWeight;
        if (roll < commercialWeight)
            return UrbanBlockType.Commercial;

        return UrbanBlockType.Industrial;
    }

    private UrbanBlockType ResolveDistanceToCenterType(Vector2 planarCenter, float mapExtent)
    {
        float normalizedDistance = Mathf.Clamp01(planarCenter.magnitude / mapExtent);
        if (normalizedDistance <= commercialInnerNormalizedRadius)
            return UrbanBlockType.Commercial;
        if (normalizedDistance <= residentialMiddleNormalizedRadius)
            return UrbanBlockType.Residential;
        return UrbanBlockType.Industrial;
    }

    private void BuildLots(List<BlockData> buildingBlocks, System.Random random, float mapExtent)
    {
        for (int i = 0; i < buildingBlocks.Count; i++)
        {
            BlockData block = buildingBlocks[i];
            UrbanBlockTypeProfile profile = GetProfile(block.urbanBlockType);
            List<Path64> lotPolygons = new List<Path64>();

            if (enableSubdivision)
                SubdividePolygon(block.polygon, profile, random, 0, lotPolygons);
            else
                lotPolygons.Add(block.polygon);

            for (int lotIndex = 0; lotIndex < lotPolygons.Count; lotIndex++)
                AddLot(block, profile, lotPolygons[lotIndex], random, mapExtent);
        }
    }

    private void SubdividePolygon(Path64 polygon, UrbanBlockTypeProfile profile, System.Random random, int depth, List<Path64> output)
    {
        if (polygon == null || polygon.Count < 3)
            return;

        if (depth >= profile.maxSubdivisionDepth)
        {
            output.Add(polygon);
            return;
        }

        if (!TrySplitPolygon(polygon, random, out Path64 first, out Path64 second))
        {
            output.Add(polygon);
            return;
        }

        if (!IsLotValid(first, profile) || !IsLotValid(second, profile))
        {
            output.Add(polygon);
            return;
        }

        SubdividePolygon(first, profile, random, depth + 1, output);
        SubdividePolygon(second, profile, random, depth + 1, output);
    }

    private void AddLot(BlockData block, UrbanBlockTypeProfile profile, Path64 polygon, System.Random random, float mapExtent)
    {
        double area = GetScaledArea(polygon);
        Vector2 center = CalculateCentroid(polygon);
        float height = Mathf.Lerp(profile.minHeight, profile.maxHeight, (float)random.NextDouble());
        if (applyCenterHeightFalloff)
            height *= CalculateCenterHeightMultiplier(center, mapExtent);

        lots.Add(new LotData
        {
            id = lots.Count,
            sourceBlockId = block.id,
            sourceBlockStableKey = block.stableKey,
            urbanBlockType = block.urbanBlockType,
            polygon = polygon,
            buildingFootprint = BuildInsetFootprint(polygon, profile.buildingSetback),
            area = area,
            planarCenter = center,
            center = RoadCoordinateUtility.PlanarToWorld(center),
            buildingHeight = height
        });
    }

    private bool TrySplitPolygon(Path64 polygon, System.Random random, out Path64 first, out Path64 second)
    {
        first = null;
        second = null;

        OrientedBounds2D bounds = CalculateOrientedBounds(polygon);
        if (!bounds.IsValid || bounds.Width <= 0.01f || bounds.Height <= 0.01f)
            return false;

        bool splitAlongPrimary = bounds.Width >= bounds.Height;
        float ratio = Mathf.Lerp(minCutRatio, maxCutRatio, (float)random.NextDouble());
        float cut = splitAlongPrimary
            ? Mathf.Lerp(bounds.MinPrimary, bounds.MaxPrimary, ratio)
            : Mathf.Lerp(bounds.MinSecondary, bounds.MaxSecondary, ratio);

        Paths64 polygonPaths = new Paths64 { polygon };
        Paths64 firstClip = new Paths64 { BuildOrientedClipPolygon(bounds, splitAlongPrimary, true, cut) };
        Paths64 secondClip = new Paths64 { BuildOrientedClipPolygon(bounds, splitAlongPrimary, false, cut) };

        Paths64 firstResult = Clipper.Intersect(polygonPaths, firstClip, FillRule.NonZero);
        Paths64 secondResult = Clipper.Intersect(polygonPaths, secondClip, FillRule.NonZero);

        first = FindLargestPositivePath(firstResult);
        second = FindLargestPositivePath(secondResult);
        return first != null && second != null;
    }

    private bool IsLotValid(Path64 polygon, UrbanBlockTypeProfile profile)
    {
        if (polygon == null || polygon.Count < 3)
            return false;

        double area = GetScaledArea(polygon);
        if (area < profile.minLotArea)
            return false;

        OrientedBounds2D bounds = CalculateOrientedBounds(polygon);
        if (!bounds.IsValid || bounds.Width <= 0.01f || bounds.Height <= 0.01f)
            return false;

        float aspectRatio = Mathf.Max(bounds.Width, bounds.Height) / Mathf.Max(0.01f, Mathf.Min(bounds.Width, bounds.Height));
        return aspectRatio <= profile.maxAspectRatio;
    }

    private float CalculateCenterHeightMultiplier(Vector2 center, float mapExtent)
    {
        float normalizedDistance = Mathf.Clamp01(center.magnitude / Mathf.Max(1f, mapExtent));
        return Mathf.Lerp(1f, edgeHeightMultiplier, normalizedDistance);
    }

    private static Path64 BuildInsetFootprint(Path64 source, float insetDistance)
    {
        if (source == null || source.Count < 3)
            return null;

        if (insetDistance <= 0f)
            return source;

        ClipperOffset offset = new ClipperOffset();
        offset.AddPath(source, JoinType.Round, EndType.Polygon);

        Paths64 insetPaths = new Paths64();
        offset.Execute(-insetDistance * ClipperScale, insetPaths);
        return FindLargestPositivePath(insetPaths);
    }

    private static Path64 FindLargestPositivePath(Paths64 paths)
    {
        if (paths == null || paths.Count == 0)
            return null;

        Path64 bestPath = null;
        double bestArea = 0d;
        for (int i = 0; i < paths.Count; i++)
        {
            Path64 path = paths[i];
            if (path == null || path.Count < 3)
                continue;

            double area = Clipper.Area(path);
            if (area > bestArea)
            {
                bestArea = area;
                bestPath = path;
            }
        }

        return bestPath;
    }

    private static double GetScaledArea(Path64 polygon)
    {
        return Mathf.Abs((float)Clipper.Area(polygon)) / (ClipperScale * ClipperScale);
    }

    private static OrientedBounds2D CalculateOrientedBounds(Path64 polygon)
    {
        OrientedBounds2D bestBounds = new OrientedBounds2D();
        if (polygon == null || polygon.Count == 0)
            return bestBounds;

        float bestArea = float.MaxValue;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 current = ToVector2(polygon[i]);
            Vector2 next = ToVector2(polygon[(i + 1) % polygon.Count]);
            Vector2 primaryAxis = next - current;
            if (primaryAxis.sqrMagnitude <= 0.0001f)
                continue;

            primaryAxis.Normalize();
            Vector2 secondaryAxis = new Vector2(-primaryAxis.y, primaryAxis.x);
            OrientedBounds2D candidate = CalculateOrientedBoundsForAxis(polygon, primaryAxis, secondaryAxis);
            if (!candidate.IsValid)
                continue;

            float area = candidate.Width * candidate.Height;
            if (area < bestArea)
            {
                bestArea = area;
                bestBounds = candidate;
            }
        }

        return bestBounds;
    }

    private static OrientedBounds2D CalculateOrientedBoundsForAxis(Path64 polygon, Vector2 primaryAxis, Vector2 secondaryAxis)
    {
        OrientedBounds2D bounds = new OrientedBounds2D
        {
            PrimaryAxis = primaryAxis,
            SecondaryAxis = secondaryAxis,
            IsValid = true
        };

        Vector2 firstPoint = ToVector2(polygon[0]);
        bounds.MinPrimary = bounds.MaxPrimary = Vector2.Dot(firstPoint, primaryAxis);
        bounds.MinSecondary = bounds.MaxSecondary = Vector2.Dot(firstPoint, secondaryAxis);

        for (int i = 1; i < polygon.Count; i++)
        {
            Vector2 point = ToVector2(polygon[i]);
            float primary = Vector2.Dot(point, primaryAxis);
            float secondary = Vector2.Dot(point, secondaryAxis);
            bounds.MinPrimary = Mathf.Min(bounds.MinPrimary, primary);
            bounds.MaxPrimary = Mathf.Max(bounds.MaxPrimary, primary);
            bounds.MinSecondary = Mathf.Min(bounds.MinSecondary, secondary);
            bounds.MaxSecondary = Mathf.Max(bounds.MaxSecondary, secondary);
        }

        return bounds;
    }

    private static Path64 BuildOrientedClipPolygon(OrientedBounds2D bounds, bool splitAlongPrimary, bool firstSide, float cut)
    {
        float margin = Mathf.Max(bounds.Width, bounds.Height) + 10f;
        float minPrimary = bounds.MinPrimary - margin;
        float maxPrimary = bounds.MaxPrimary + margin;
        float minSecondary = bounds.MinSecondary - margin;
        float maxSecondary = bounds.MaxSecondary + margin;

        if (splitAlongPrimary)
        {
            if (firstSide)
                maxPrimary = cut;
            else
                minPrimary = cut;
        }
        else
        {
            if (firstSide)
                maxSecondary = cut;
            else
                minSecondary = cut;
        }

        return new Path64
        {
            ToPoint64(FromOriented(bounds, minPrimary, minSecondary)),
            ToPoint64(FromOriented(bounds, maxPrimary, minSecondary)),
            ToPoint64(FromOriented(bounds, maxPrimary, maxSecondary)),
            ToPoint64(FromOriented(bounds, minPrimary, maxSecondary))
        };
    }

    private static Vector2 FromOriented(OrientedBounds2D bounds, float primary, float secondary)
    {
        return bounds.PrimaryAxis * primary + bounds.SecondaryAxis * secondary;
    }

    private static Vector2 ToVector2(Point64 point)
    {
        return new Vector2(point.X / ClipperScale, point.Y / ClipperScale);
    }

    private static Point64 ToPoint64(float x, float y)
    {
        return new Point64(Mathf.RoundToInt(x * ClipperScale), Mathf.RoundToInt(y * ClipperScale));
    }

    private static Point64 ToPoint64(Vector2 point)
    {
        return ToPoint64(point.x, point.y);
    }

    private static Vector2 CalculateCentroid(Path64 polygon)
    {
        if (polygon == null || polygon.Count == 0)
            return Vector2.zero;

        double signedArea = 0d;
        double centroidX = 0d;
        double centroidY = 0d;

        for (int i = 0; i < polygon.Count; i++)
        {
            Point64 current = polygon[i];
            Point64 next = polygon[(i + 1) % polygon.Count];
            double cross = current.X * (double)next.Y - next.X * (double)current.Y;
            signedArea += cross;
            centroidX += (current.X + next.X) * cross;
            centroidY += (current.Y + next.Y) * cross;
        }

        signedArea *= 0.5d;
        if (System.Math.Abs(signedArea) < double.Epsilon)
        {
            double averageX = 0d;
            double averageY = 0d;
            for (int i = 0; i < polygon.Count; i++)
            {
                averageX += polygon[i].X;
                averageY += polygon[i].Y;
            }

            return new Vector2((float)(averageX / polygon.Count / ClipperScale), (float)(averageY / polygon.Count / ClipperScale));
        }

        centroidX /= 6d * signedArea;
        centroidY /= 6d * signedArea;
        return new Vector2((float)(centroidX / ClipperScale), (float)(centroidY / ClipperScale));
    }

    private void SyncDebugUrbanBlockType(BlockData block, UrbanBlockTypeOverride overrideState = UrbanBlockTypeOverride.Auto)
    {
        if (block.meshObject == null)
            return;

        BlockDebugComponent debug = block.meshObject.GetComponent<BlockDebugComponent>();
        if (debug != null)
        {
            debug.urbanBlockType = block.urbanBlockType;
            debug.urbanBlockTypeOverride = overrideState;
        }
    }

    private static UrbanBlockTypeOverride ToOverride(UrbanBlockType type)
    {
        switch (type)
        {
            case UrbanBlockType.Commercial:
                return UrbanBlockTypeOverride.Commercial;
            case UrbanBlockType.Industrial:
                return UrbanBlockTypeOverride.Industrial;
            case UrbanBlockType.Residential:
                return UrbanBlockTypeOverride.Residential;
            default:
                return UrbanBlockTypeOverride.Auto;
        }
    }

    private void ApplyBlockTypePresentation(BlockData block)
    {
        if (block == null || block.meshObject == null)
            return;

        MeshRenderer meshRenderer = block.meshObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            return;

        UrbanBlockTypeProfile profile = GetProfile(block.urbanBlockType);
        if (profile != null && profile.blockMaterial != null)
        {
            meshRenderer.sharedMaterial = profile.blockMaterial;
            return;
        }

        if (block.urbanBlockType == UrbanBlockType.Residential ||
            block.urbanBlockType == UrbanBlockType.Commercial ||
            block.urbanBlockType == UrbanBlockType.Industrial)
        {
            meshRenderer.sharedMaterial = GetFallbackBlockPreviewMaterial(block.urbanBlockType);
        }
    }

    private Material GetFallbackBlockPreviewMaterial(UrbanBlockType type)
    {
        if (blockPreviewMaterials.TryGetValue(type, out Material material) && material != null)
            return material;

        material = CreateFallbackMaterial($"Default {type} Block Preview Material", GetFallbackBlockPreviewColor(type));
        blockPreviewMaterials[type] = material;
        return material;
    }

    private Color GetFallbackBlockPreviewColor(UrbanBlockType type)
    {
        switch (type)
        {
            case UrbanBlockType.Commercial:
                return commercialBlockPreviewColor;
            case UrbanBlockType.Industrial:
                return industrialBlockPreviewColor;
            default:
                return residentialBlockPreviewColor;
        }
    }

    private static Material CreateFallbackMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.name = materialName;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        return material;
    }

    private static BlockDebugComponent[] CollectBlockDebugComponents(BlockAreaGenerator blockAreaGenerator)
    {
        if (blockAreaGenerator.generatedBlockRoot != null)
            return blockAreaGenerator.generatedBlockRoot.GetComponentsInChildren<BlockDebugComponent>(true);

        List<BlockDebugComponent> collected = new List<BlockDebugComponent>();
        for (int i = 0; i < blockAreaGenerator.blocks.Count; i++)
        {
            GameObject meshObject = blockAreaGenerator.blocks[i].meshObject;
            if (meshObject == null)
                continue;

            BlockDebugComponent debug = meshObject.GetComponent<BlockDebugComponent>();
            if (debug != null)
                collected.Add(debug);
        }

        return collected.ToArray();
    }

    private static BlockData FindBlockByStableKey(BlockAreaGenerator blockAreaGenerator, string stableKey)
    {
        for (int i = 0; i < blockAreaGenerator.blocks.Count; i++)
        {
            if (blockAreaGenerator.blocks[i].stableKey == stableKey)
                return blockAreaGenerator.blocks[i];
        }

        return null;
    }

    private void EnsureDefaultProfiles()
    {
        EnsureProfile(UrbanBlockType.Default);
        EnsureProfile(UrbanBlockType.Residential);
        EnsureProfile(UrbanBlockType.Commercial);
        EnsureProfile(UrbanBlockType.Industrial);
        EnsureProfile(UrbanBlockType.Park);
    }

    private void EnsureProfile(UrbanBlockType type)
    {
        for (int i = 0; i < typeProfiles.Count; i++)
        {
            UrbanBlockTypeProfile profile = typeProfiles[i];
            if (profile != null && profile.type == type)
                return;
        }

        typeProfiles.Add(UrbanBlockTypeProfile.CreateDefault(type));
    }

    private struct OrientedBounds2D
    {
        public bool IsValid;
        public Vector2 PrimaryAxis;
        public Vector2 SecondaryAxis;
        public float MinPrimary;
        public float MaxPrimary;
        public float MinSecondary;
        public float MaxSecondary;
        public float Width => MaxPrimary - MinPrimary;
        public float Height => MaxSecondary - MinSecondary;
    }
}
