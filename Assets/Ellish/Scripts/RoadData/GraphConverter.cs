using System.Collections.Generic;
using UnityEngine;
using GraphModel;

public static class GraphConverter
{
    public static RoadNetworkData Convert(Graph graph)
    {
        RoadNetworkData data = new RoadNetworkData();

        // Step 1 : Collect All Nodes from Edges
        HashSet<Node> rawNodes = new HashSet<Node>();

        foreach (var e in graph.MajorEdges)
        {
            rawNodes.Add(e.NodeA);
            rawNodes.Add(e.NodeB);
        }

        foreach (var e in graph.MinorEdges)
        {
            rawNodes.Add(e.NodeA);
            rawNodes.Add(e.NodeB);
        }

        // Step 2 : Remove Duplicated Nodes(if any)
        List<Node> uniqueNodes = new List<Node>();
        float epsilon = 0.01f;

        foreach (var node in rawNodes)
        {
            bool found = false;

            foreach (var existing in uniqueNodes)
            {
                if (Vector2.Distance(
                    new Vector2(node.X, node.Y),
                    new Vector2(existing.X, existing.Y)
                ) < epsilon)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                uniqueNodes.Add(node);
        }

        // Step3£ºNode ¡ú JunctionData + ID 
        Dictionary<Node, int> nodeToId = new Dictionary<Node, int>();

        for (int i = 0; i < uniqueNodes.Count; i++)
        {
            var node = uniqueNodes[i];

            JunctionData jd = new JunctionData();
            jd.id = i;
            jd.position = new Vector2(node.X, node.Y);

            data.nodes.Add(jd);

            nodeToId[node] = i;// bug
        }

        // Step4£ºEdge ¡ú RoadEdgeData 
        int edgeId = 0;

        void AddEdge(Edge e)
        {
            RoadEdgeData ed = new RoadEdgeData();
            ed.id = edgeId++;

            //ed.startNodeId = nodeToId[e.NodeA];
            //ed.endNodeId = nodeToId[e.NodeB];
            ed.startNodeId = FindNodeId(e.NodeA, uniqueNodes, epsilon);
            ed.endNodeId = FindNodeId(e.NodeB, uniqueNodes, epsilon);

            data.edges.Add(ed);
        }

        int FindNodeId(Node node, List<Node> uniqueNodes, float epsilon)
        {
            for (int i = 0; i < uniqueNodes.Count; i++)
            {
                if (Vector2.Distance(
                    new Vector2(node.X, node.Y),
                    new Vector2(uniqueNodes[i].X, uniqueNodes[i].Y)
                ) < epsilon)
                {
                    return i;
                }
            }

            return -1;
        }

        foreach (var e in graph.MajorEdges)
            AddEdge(e);

        foreach (var e in graph.MinorEdges)
            AddEdge(e);

        return data;
    }
}
