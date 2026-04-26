using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadNetworkGenerator))]
public class RoadNetworkGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoadNetworkGenerator generator = (RoadNetworkGenerator)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Road Network"))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Road Network");
            generator.Generate();
            EditorUtility.SetDirty(generator);
        }

        if (GUILayout.Button("Clear Generated Objects"))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear Generated Road Network");
            generator.ClearGeneratedObjects();
            EditorUtility.SetDirty(generator);
        }
    }
}
