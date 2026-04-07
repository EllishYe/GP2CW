using System.Collections.Generic;
using UnityEngine;

public class TrafficGraph : MonoBehaviour
{


    public List<LaneData> allLanes = new List<LaneData>();


    // 2. 全城普查
    // 队友的生成代码跑完后，需要调用一次这个方法
    public void BuildGraphFromScene()
    {
        allLanes.Clear();

        // 抓取场景里所有的 LaneData
        LaneData[] foundLanes = Object.FindObjectsByType<LaneData>(FindObjectsSortMode.None);
        allLanes.AddRange(foundLanes);

        // 核心魔法：空间邻近自动缝合！
        foreach (LaneData currentLane in allLanes)
        {
            currentLane.nextLanes.Clear();

            if (currentLane.pathPoints.Count == 0) continue;

            // 获取当前车道的终点
            Vector3 endPoint = currentLane.pathPoints[currentLane.pathPoints.Count - 1].position;

            foreach (LaneData otherLane in allLanes)
            {
                if (currentLane == otherLane || otherLane.pathPoints.Count == 0) continue;

                // 获取其他车道的起点
                Vector3 startPoint = otherLane.pathPoints[0].position;

                // 如果两者的距离小于 0.2 米，说明它们是首尾相接的！
                if (Vector3.Distance(endPoint, startPoint) < 20f)
                {
                    currentLane.nextLanes.Add(otherLane); // 自动接上！
                }
            }
        }
        Debug.Log($"地图缝合完毕！共找到 {allLanes.Count} 条车道。");
    }
}
