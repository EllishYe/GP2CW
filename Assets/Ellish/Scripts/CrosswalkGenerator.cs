using System.Collections.Generic;
using GraphModel;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class CrosswalkData
{
    public int id;
    public int junctionId;
    public RoadKind roadKind;
    public int connectedEdgeId;
    public bool isAtFourWayJunction;

    public Vector3 innerLeft;
    public Vector3 innerRight;
    public Vector3 outerLeft;
    public Vector3 outerRight;
    public Vector3 innerCenter;
    public Vector3 outerCenter;
}

[System.Serializable]
public class FourWayIntersectionArmData
{
    public int armIndex;
    public int axisGroupIndex;
    public int crosswalkId;
    public int connectedEdgeId;
    public RoadKind roadKind;

    public Vector2 planarOutwardDirection;
    public Vector3 outwardDirection;
    public Vector3 innerCenter;
    public Vector3 outerCenter;

    public List<int> incomingLaneIds = new List<int>();
    public List<int> outgoingLaneIds = new List<int>();
}

[System.Serializable]
public class FourWayIntersectionData
{
    public int id;
    public int junctionId;
    public Vector2 planarCenter;
    public Vector3 center;
    public List<FourWayIntersectionArmData> arms = new List<FourWayIntersectionArmData>();
}

public class CrosswalkGenerator : MonoBehaviour
{
    [Header("Crosswalk Area")]
    public bool generateCrosswalkMesh = true;
    public float vehicleStopDistance = 12f;
    public float crosswalkMeshHeight = 0.03f;
    public Material crosswalkMaterial;
    public string fourWayCrosswalkObjectName = "Crosswalk_FourWay_Walkable_Mesh";
    public string nonFourWayCrosswalkObjectName = "Crosswalk_NonFourWay_Visual_Mesh";
    public string walkableLayerName = "Walkable";
    public bool addFourWayMeshCollider = true;
    public bool addNonFourWayMeshCollider = false;
    public bool skipDeadEnds = true;

    private const string LegacyMergedCrosswalkObjectName = "Crosswalk_Mesh";

    [HideInInspector] public GameObject fourWayCrosswalkMeshObject;
    [HideInInspector] public GameObject nonFourWayCrosswalkMeshObject;
    public List<CrosswalkData> crosswalks = new List<CrosswalkData>();
    public List<FourWayIntersectionData> fourWayIntersections = new List<FourWayIntersectionData>();

    void OnValidate()
    {
        vehicleStopDistance = Mathf.Max(0f, vehicleStopDistance);
        crosswalkMeshHeight = Mathf.Max(0f, crosswalkMeshHeight);
    }

    public void Generate(RoadNetworkGenerator roadNetworkGenerator, Transform parent)
    {
        Clear(parent);

        if (!generateCrosswalkMesh)
            return;

        if (roadNetworkGenerator == null || roadNetworkGenerator.Graph == null)
        {
            Debug.LogWarning("CrosswalkGenerator: Road graph is empty. Generate roads before generating crosswalks.");
            return;
        }

        float innerDistance = roadNetworkGenerator.junctionCutDistance;
        float outerDistance = Mathf.Max(vehicleStopDistance, innerDistance + 0.1f);

        CrosswalkMeshBuffers buffers = BuildCrosswalkBuffers(roadNetworkGenerator, innerDistance, outerDistance);
        ApplyVehicleControlPoints(roadNetworkGenerator, innerDistance, outerDistance);
        BuildFourWayIntersectionData(roadNetworkGenerator);

        Mesh fourWayMesh = CreateMesh(buffers.fourWay, "Four Way Crosswalk Mesh");
        Mesh nonFourWayMesh = CreateMesh(buffers.nonFourWay, "Non Four Way Crosswalk Mesh");
        if ((fourWayMesh == null || fourWayMesh.vertexCount == 0) && (nonFourWayMesh == null || nonFourWayMesh.vertexCount == 0))
        {
            Debug.LogWarning("CrosswalkGenerator: No crosswalk mesh was generated.");
            return;
        }

        if (fourWayMesh != null && fourWayMesh.vertexCount > 0)
        {
            fourWayCrosswalkMeshObject = CreateMeshObject(fourWayCrosswalkObjectName, fourWayMesh, parent, addFourWayMeshCollider);
            AssignLayerIfExists(fourWayCrosswalkMeshObject, walkableLayerName);
        }

        if (nonFourWayMesh != null && nonFourWayMesh.vertexCount > 0)
            nonFourWayCrosswalkMeshObject = CreateMeshObject(nonFourWayCrosswalkObjectName, nonFourWayMesh, parent, addNonFourWayMeshCollider);
    }

    public void Clear(Transform parent)
    {
        crosswalks.Clear();
        fourWayIntersections.Clear();

        fourWayCrosswalkMeshObject = ResolveExisting(parent, fourWayCrosswalkObjectName, fourWayCrosswalkMeshObject);
        nonFourWayCrosswalkMeshObject = ResolveExisting(parent, nonFourWayCrosswalkObjectName, nonFourWayCrosswalkMeshObject);
        GameObject legacyMergedCrosswalkMeshObject = ResolveExisting(parent, LegacyMergedCrosswalkObjectName, null);

        DestroyGeneratedObject(fourWayCrosswalkMeshObject);
        DestroyGeneratedObject(nonFourWayCrosswalkMeshObject);
        DestroyGeneratedObject(legacyMergedCrosswalkMeshObject);

        fourWayCrosswalkMeshObject = null;
        nonFourWayCrosswalkMeshObject = null;
    }

    public List<FourWayIntersectionData> GetFourWayIntersections()
    {
        return fourWayIntersections;
    }

    private CrosswalkMeshBuffers BuildCrosswalkBuffers(RoadNetworkGenerator roadNetworkGenerator, float innerDistance, float outerDistance)
    {
        CrosswalkMeshBuffers buffers = new CrosswalkMeshBuffers();

        foreach (Node node in CollectUniqueNodes(roadNetworkGenerator.Graph))
        {
            int degree = node.Edges.Count;
            if (skipDeadEnds && degree <= 1)
                continue;

            bool isFourWay = degree == 4;
            Vector2 center = new Vector2(node.X, node.Y);

            foreach (Edge edge in node.Edges)
            {
                Node other = edge.NodeA == node ? edge.NodeB : edge.NodeA;
                Vector2 direction = new Vector2(other.X - node.X, other.Y - node.Y).normalized;
                if (direction == Vector2.zero)
                    continue;

                RoadKind roadKind = roadNetworkGenerator.Graph.MajorEdges.Contains(edge) ? RoadKind.Major : RoadKind.Minor;
                float roadWidth = roadKind == RoadKind.Major ? roadNetworkGenerator.majorRoadWidth : roadNetworkGenerator.minorRoadWidth;
                float halfRoadWidth = roadWidth * 0.5f;
                Vector2 normal = new Vector2(-direction.y, direction.x);

                Vector2 innerCenterPlanar = center + direction * innerDistance;
                Vector2 outerCenterPlanar = center + direction * outerDistance;
                Vector2 innerLeftPlanar = innerCenterPlanar + normal * halfRoadWidth;
                Vector2 innerRightPlanar = innerCenterPlanar - normal * halfRoadWidth;
                Vector2 outerLeftPlanar = outerCenterPlanar + normal * halfRoadWidth;
                Vector2 outerRightPlanar = outerCenterPlanar - normal * halfRoadWidth;

                CrosswalkData data = new CrosswalkData
                {
                    id = crosswalks.Count,
                    junctionId = node.GetHashCode(),
                    roadKind = roadKind,
                    connectedEdgeId = edge.GetHashCode(),
                    isAtFourWayJunction = isFourWay,
                    innerLeft = RoadCoordinateUtility.PlanarToWorld(innerLeftPlanar, crosswalkMeshHeight),
                    innerRight = RoadCoordinateUtility.PlanarToWorld(innerRightPlanar, crosswalkMeshHeight),
                    outerLeft = RoadCoordinateUtility.PlanarToWorld(outerLeftPlanar, crosswalkMeshHeight),
                    outerRight = RoadCoordinateUtility.PlanarToWorld(outerRightPlanar, crosswalkMeshHeight),
                    innerCenter = RoadCoordinateUtility.PlanarToWorld(innerCenterPlanar, crosswalkMeshHeight),
                    outerCenter = RoadCoordinateUtility.PlanarToWorld(outerCenterPlanar, crosswalkMeshHeight)
                };

                crosswalks.Add(data);
                GeometryBuffer targetBuffer = isFourWay ? buffers.fourWay : buffers.nonFourWay;
                AddQuad(targetBuffer.vertices, targetBuffer.triangles, targetBuffer.uvs, data);
            }
        }

        return buffers;
    }

    private void ApplyVehicleControlPoints(RoadNetworkGenerator roadNetworkGenerator, float innerDistance, float outerDistance)
    {
        List<AgentLaneData> agentLanes = roadNetworkGenerator.GetAgentLanes();
        if (agentLanes == null || agentLanes.Count == 0 || crosswalks.Count == 0)
            return;

        float matchTolerance = Mathf.Max(0.25f, roadNetworkGenerator.laneWidth * 0.25f);
        float controlOffsetDistance = outerDistance - innerDistance;

        foreach (AgentLaneData lane in agentLanes)
        {
            Vector2 planarVehicleStart = TryOffsetLanePointOutsideCrosswalk(lane.planarStart, controlOffsetDistance, matchTolerance, out Vector2 startPoint)
                ? startPoint
                : lane.planarStart;
            Vector2 planarVehicleStop = TryOffsetLanePointOutsideCrosswalk(lane.planarEnd, controlOffsetDistance, matchTolerance, out Vector2 stopPoint)
                ? stopPoint
                : lane.planarEnd;

            lane.SetVehicleControlPoints(planarVehicleStart, planarVehicleStop, lane.startPoint.y);
        }
    }

    private bool TryOffsetLanePointOutsideCrosswalk(Vector2 lanePoint, float offsetDistance, float matchTolerance, out Vector2 offsetPoint)
    {
        CrosswalkData bestCrosswalk = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < crosswalks.Count; i++)
        {
            CrosswalkData crosswalk = crosswalks[i];
            float distance = DistancePointToSegment(lanePoint, RoadCoordinateUtility.WorldToPlanar(crosswalk.innerLeft), RoadCoordinateUtility.WorldToPlanar(crosswalk.innerRight));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCrosswalk = crosswalk;
            }
        }

        if (bestCrosswalk == null || bestDistance > matchTolerance)
        {
            offsetPoint = lanePoint;
            return false;
        }

        Vector2 offsetDirection = (RoadCoordinateUtility.WorldToPlanar(bestCrosswalk.outerCenter) - RoadCoordinateUtility.WorldToPlanar(bestCrosswalk.innerCenter)).normalized;
        offsetPoint = lanePoint + offsetDirection * offsetDistance;
        return true;
    }

    private void BuildFourWayIntersectionData(RoadNetworkGenerator roadNetworkGenerator)
    {
        List<AgentLaneData> agentLanes = roadNetworkGenerator.GetAgentLanes();
        float matchTolerance = Mathf.Max(0.25f, roadNetworkGenerator.laneWidth * 0.25f);

        foreach (Node node in CollectUniqueNodes(roadNetworkGenerator.Graph))
        {
            if (node.Edges.Count != 4)
                continue;

            int junctionId = node.GetHashCode();
            FourWayIntersectionData intersection = new FourWayIntersectionData
            {
                id = fourWayIntersections.Count,
                junctionId = junctionId,
                planarCenter = new Vector2(node.X, node.Y),
                center = RoadCoordinateUtility.PlanarToWorld(node.X, node.Y, crosswalkMeshHeight)
            };

            for (int i = 0; i < crosswalks.Count; i++)
            {
                CrosswalkData crosswalk = crosswalks[i];
                if (!crosswalk.isAtFourWayJunction || crosswalk.junctionId != junctionId)
                    continue;

                Vector2 innerCenterPlanar = RoadCoordinateUtility.WorldToPlanar(crosswalk.innerCenter);
                Vector2 outerCenterPlanar = RoadCoordinateUtility.WorldToPlanar(crosswalk.outerCenter);
                Vector2 outwardDirectionPlanar = (outerCenterPlanar - innerCenterPlanar).normalized;

                FourWayIntersectionArmData arm = new FourWayIntersectionArmData
                {
                    crosswalkId = crosswalk.id,
                    connectedEdgeId = crosswalk.connectedEdgeId,
                    roadKind = crosswalk.roadKind,
                    planarOutwardDirection = outwardDirectionPlanar,
                    outwardDirection = new Vector3(outwardDirectionPlanar.x, 0f, outwardDirectionPlanar.y),
                    innerCenter = crosswalk.innerCenter,
                    outerCenter = crosswalk.outerCenter
                };

                AddLaneIdsForCrosswalkArm(arm, crosswalk, agentLanes, matchTolerance);
                intersection.arms.Add(arm);
            }

            SortAndAssignFourWayArmGroups(intersection.arms);
            if (intersection.arms.Count == 4)
                fourWayIntersections.Add(intersection);
        }
    }

    private static void AddLaneIdsForCrosswalkArm(FourWayIntersectionArmData arm, CrosswalkData crosswalk, List<AgentLaneData> agentLanes, float matchTolerance)
    {
        if (agentLanes == null) return;

        Vector2 innerLeft = RoadCoordinateUtility.WorldToPlanar(crosswalk.innerLeft);
        Vector2 innerRight = RoadCoordinateUtility.WorldToPlanar(crosswalk.innerRight);

        for (int i = 0; i < agentLanes.Count; i++)
        {
            AgentLaneData lane = agentLanes[i];
            if (DistancePointToSegment(lane.planarEnd, innerLeft, innerRight) <= matchTolerance)
                arm.incomingLaneIds.Add(lane.id);

            if (DistancePointToSegment(lane.planarStart, innerLeft, innerRight) <= matchTolerance)
                arm.outgoingLaneIds.Add(lane.id);
        }
    }

    private static void SortAndAssignFourWayArmGroups(List<FourWayIntersectionArmData> arms)
    {
        arms.Sort((a, b) =>
        {
            float angleA = Mathf.Atan2(a.planarOutwardDirection.y, a.planarOutwardDirection.x);
            float angleB = Mathf.Atan2(b.planarOutwardDirection.y, b.planarOutwardDirection.x);
            return angleA.CompareTo(angleB);
        });

        for (int i = 0; i < arms.Count; i++)
        {
            arms[i].armIndex = i;
            arms[i].axisGroupIndex = i % 2;
        }
    }

    private GameObject CreateMeshObject(string objectName, Mesh mesh, Transform parent, bool addMeshCollider)
    {
        GameObject meshObject = new GameObject(objectName);
        meshObject.transform.SetParent(parent != null ? parent : transform, false);

        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        if (crosswalkMaterial != null)
            meshRenderer.sharedMaterial = crosswalkMaterial;

        if (addMeshCollider)
        {
            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        return meshObject;
    }

    private static Mesh CreateMesh(GeometryBuffer buffer, string meshName)
    {
        if (buffer.vertices.Count == 0)
            return null;

        Mesh mesh = new Mesh();
        mesh.name = meshName;
        if (buffer.vertices.Count > 65535)
            mesh.indexFormat = IndexFormat.UInt32;

        mesh.SetVertices(buffer.vertices);
        mesh.SetUVs(0, buffer.uvs);
        mesh.SetTriangles(buffer.triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static List<Node> CollectUniqueNodes(Graph graph)
    {
        List<Node> nodes = new List<Node>();
        AddUniqueNodes(nodes, graph.MajorNodes);
        AddUniqueNodes(nodes, graph.MinorNodes);
        return nodes;
    }

    private static void AddUniqueNodes(List<Node> target, List<Node> source)
    {
        if (source == null) return;
        foreach (Node node in source)
        {
            if (!target.Contains(node))
                target.Add(node);
        }
    }

    private static void AddQuad(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, CrosswalkData data)
    {
        int startIndex = vertices.Count;
        vertices.Add(data.innerLeft);
        vertices.Add(data.outerLeft);
        vertices.Add(data.outerRight);
        vertices.Add(data.innerRight);

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(0f, 1f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(1f, 0f));

        triangles.Add(startIndex);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= Mathf.Epsilon)
            return Vector2.Distance(point, segmentStart);

        float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / lengthSquared);
        Vector2 projection = segmentStart + segment * t;
        return Vector2.Distance(point, projection);
    }

    private static GameObject ResolveExisting(Transform parent, string objectName, GameObject current)
    {
        if (current != null || parent == null)
            return current;

        Transform existing = parent.Find(objectName);
        return existing != null ? existing.gameObject : null;
    }

    private static void DestroyGeneratedObject(GameObject target)
    {
        if (target == null) return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private static void AssignLayerIfExists(GameObject target, string layerName)
    {
        if (target == null || string.IsNullOrWhiteSpace(layerName)) return;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"CrosswalkGenerator: Layer '{layerName}' does not exist. Crosswalk object stays on its current layer.");
            return;
        }

        SetLayerRecursively(target, layer);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        for (int i = 0; i < target.transform.childCount; i++)
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
    }

    private class GeometryBuffer
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> triangles = new List<int>();
        public readonly List<Vector2> uvs = new List<Vector2>();
    }

    private class CrosswalkMeshBuffers
    {
        public readonly GeometryBuffer fourWay = new GeometryBuffer();
        public readonly GeometryBuffer nonFourWay = new GeometryBuffer();
    }
}
