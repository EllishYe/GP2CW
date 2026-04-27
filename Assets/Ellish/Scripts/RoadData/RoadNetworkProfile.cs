using UnityEngine;

[CreateAssetMenu(fileName = "RoadNetworkProfile", menuName = "Ellish/City Generation/Road Network Profile")]
public class RoadNetworkProfile : ScriptableObject
{
    [Header("Map Settings")]
    public int mapSize = 1000;
    public int randomSeed = 12345;
    public float roadSegmentLength = 100f;

    [Header("Major Roads")]
    public int majorRoadCount = 200;
    public int maxLeanAngle = 20;
    public float branchProbability = 0.1f;

    [Header("Minor Roads")]
    public int minorRoadCount = 400;
    public float deletionProbability = 0.2f;

    [Header("Road Dimensions")]
    public float laneWidth = 3.5f;
    public int majorLanesPerDirection = 2;
    public int minorLanesPerDirection = 1;
    public float junctionCutDistance = 8f;
    public bool deriveRoadWidthFromLaneWidth = true;
    public float majorRoadWidth = 14f;
    public float minorRoadWidth = 7f;
    public bool skipLanesTooShortForJunctionCut = true;

    [Header("Agent Interface")]
    public float agentLaneHeight = 0f;

    [Header("Generated Road Mesh")]
    public bool generateRoadMesh = true;
    public float roadMeshHeight = 0.01f;
    public Material roadMaterial;
    public string roadMeshObjectName = "Road_Surface_Mesh";
    public bool addRoadMeshCollider = true;
    public string roadLayerName = "Road";

    void OnValidate()
    {
        Sanitize();
    }

    public void CaptureFrom(RoadNetworkGenerator generator)
    {
        if (generator == null) return;

        mapSize = generator.mapSize;
        randomSeed = generator.randomSeed;
        roadSegmentLength = generator.roadSegmentLength;

        majorRoadCount = generator.majorRoadCount;
        maxLeanAngle = generator.maxLeanAngle;
        branchProbability = generator.branchProbability;

        minorRoadCount = generator.minorRoadCount;
        deletionProbability = generator.deletionProbability;

        laneWidth = generator.laneWidth;
        majorLanesPerDirection = generator.majorLanesPerDirection;
        minorLanesPerDirection = generator.minorLanesPerDirection;
        junctionCutDistance = generator.junctionCutDistance;
        deriveRoadWidthFromLaneWidth = generator.deriveRoadWidthFromLaneWidth;
        majorRoadWidth = generator.majorRoadWidth;
        minorRoadWidth = generator.minorRoadWidth;
        skipLanesTooShortForJunctionCut = generator.skipLanesTooShortForJunctionCut;

        agentLaneHeight = generator.agentLaneHeight;

        generateRoadMesh = generator.generateRoadMesh;
        roadMeshHeight = generator.roadMeshHeight;
        roadMaterial = generator.roadMaterial;
        roadMeshObjectName = generator.roadMeshObjectName;
        addRoadMeshCollider = generator.addRoadMeshCollider;
        roadLayerName = generator.roadLayerName;

        Sanitize();
    }

    public void ApplyTo(RoadNetworkGenerator generator)
    {
        if (generator == null) return;

        Sanitize();

        generator.mapSize = mapSize;
        generator.randomSeed = randomSeed;
        generator.roadSegmentLength = roadSegmentLength;

        generator.majorRoadCount = majorRoadCount;
        generator.maxLeanAngle = maxLeanAngle;
        generator.branchProbability = branchProbability;

        generator.minorRoadCount = minorRoadCount;
        generator.deletionProbability = deletionProbability;

        generator.laneWidth = laneWidth;
        generator.majorLanesPerDirection = majorLanesPerDirection;
        generator.minorLanesPerDirection = minorLanesPerDirection;
        generator.junctionCutDistance = junctionCutDistance;
        generator.deriveRoadWidthFromLaneWidth = deriveRoadWidthFromLaneWidth;
        generator.majorRoadWidth = majorRoadWidth;
        generator.minorRoadWidth = minorRoadWidth;
        generator.skipLanesTooShortForJunctionCut = skipLanesTooShortForJunctionCut;

        generator.agentLaneHeight = agentLaneHeight;

        generator.generateRoadMesh = generateRoadMesh;
        generator.roadMeshHeight = roadMeshHeight;
        generator.roadMaterial = roadMaterial;
        generator.roadMeshObjectName = roadMeshObjectName;
        generator.addRoadMeshCollider = addRoadMeshCollider;
        generator.roadLayerName = roadLayerName;
        generator.SanitizeSettings();
    }

    public void Sanitize()
    {
        mapSize = Mathf.Max(1, mapSize);
        roadSegmentLength = Mathf.Max(1f, roadSegmentLength);
        laneWidth = Mathf.Max(0.1f, laneWidth);
        majorLanesPerDirection = Mathf.Max(1, majorLanesPerDirection);
        minorLanesPerDirection = Mathf.Max(1, minorLanesPerDirection);
        junctionCutDistance = Mathf.Max(0f, junctionCutDistance);
        majorRoadWidth = Mathf.Max(0.1f, majorRoadWidth);
        minorRoadWidth = Mathf.Max(0.1f, minorRoadWidth);
        agentLaneHeight = Mathf.Max(0f, agentLaneHeight);
        roadMeshHeight = Mathf.Max(0f, roadMeshHeight);
    }
}
