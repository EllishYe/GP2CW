using Clipper2Lib;
using GraphModel;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Footprint Generator:从Polyline（道路骨架路径）生成Footprint（道路边界）
/// </summary>

public static class RoadFootprintGenerator
{

    public static Paths64 Generate(Paths64 input, float roadWidth)
    {
        ClipperOffset offset = new ClipperOffset();

        offset.AddPaths(
            input,
            JoinType.Round,   // 
            EndType.Butt      // 
        );

        Paths64 solution = new Paths64();

        //offset.Execute(roadWidth * 0.5, solution);
        offset.Execute(roadWidth * 500.0, solution);


        return solution;
    }
}
