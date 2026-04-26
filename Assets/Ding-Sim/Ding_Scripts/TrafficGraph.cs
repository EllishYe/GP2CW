using System.Collections.Generic;
using UnityEngine;

public class TrafficGraph : MonoBehaviour
{


    public List<LaneData> allLanes = new List<LaneData>();

    //call this method after pcg
    public void BuildGraphFromScene()
    {
        allLanes.Clear();

        LaneData[] foundLanes = Object.FindObjectsByType<LaneData>(FindObjectsSortMode.None);
        allLanes.AddRange(foundLanes);

        foreach (LaneData currentLane in allLanes)
        {
            currentLane.nextLanes.Clear();

            if (currentLane.pathPoints.Count == 0) continue;

            // get end point of this lane
            Vector3 endPoint = currentLane.pathPoints[currentLane.pathPoints.Count - 1].position;

            foreach (LaneData otherLane in allLanes)
            {
                if (currentLane == otherLane || otherLane.pathPoints.Count == 0) continue;
              
                Vector3 startPoint = otherLane.pathPoints[0].position;

                // if distance< 0.2, they are connected
                if (Vector3.Distance(endPoint, startPoint) < 20f)
                {
                    currentLane.nextLanes.Add(otherLane); 
                }
            }
        }
        Debug.Log($"There is {allLanes.Count} lanes totally");
    }
}
