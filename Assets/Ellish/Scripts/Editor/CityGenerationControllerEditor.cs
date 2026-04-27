using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CityGenerationController))]
public class CityGenerationControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityGenerationController controller = (CityGenerationController)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pipeline", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh Stage References"))
        {
            Undo.RecordObject(controller, "Refresh Stage References");
            controller.FindStageGenerators();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("Ensure Generated Hierarchy"))
        {
            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Ensure Generated Hierarchy");
            controller.EnsureGeneratedHierarchy();
            EditorUtility.SetDirty(controller);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Roads", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Roads"))
        {
            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Generate Roads");
            controller.GenerateRoads();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("Clear Roads"))
        {
            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Clear Roads");
            controller.ClearRoads();
            EditorUtility.SetDirty(controller);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Future Stages", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate Walkable"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Generate Walkable");
                controller.GenerateWalkable();
                EditorUtility.SetDirty(controller);
            }
            if (GUILayout.Button("Clear Walkable"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Clear Walkable");
                controller.ClearWalkable();
                EditorUtility.SetDirty(controller);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate Blocks"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Generate Blocks");
                controller.GenerateBlocks();
                EditorUtility.SetDirty(controller);
            }
            if (GUILayout.Button("Clear Blocks"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Clear Blocks");
                controller.ClearBlocks();
                EditorUtility.SetDirty(controller);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Block Overrides"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Apply Block Overrides");
                controller.ApplyBlockDebugOverrides();
                EditorUtility.SetDirty(controller);
            }
            if (GUILayout.Button("Save Block Overrides"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Save Block Overrides");
                controller.SaveBlockDebugOverrides();
                EditorUtility.SetDirty(controller);
            }
        }

        if (GUILayout.Button("Clear Saved Block Overrides"))
        {
            Undo.RecordObject(controller, "Clear Saved Block Overrides");
            controller.ClearSavedBlockOverrides();
            EditorUtility.SetDirty(controller);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate Lots"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Generate Lots");
                controller.GenerateLots();
                EditorUtility.SetDirty(controller);
            }
            if (GUILayout.Button("Clear Lots"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Clear Lots");
                controller.ClearLots();
                EditorUtility.SetDirty(controller);
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate All"))
        {
            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Generate All City Stages");
            controller.GenerateAll();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("Clear All"))
        {
            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Clear All City Stages");
            controller.ClearAll();
            EditorUtility.SetDirty(controller);
        }
    }
}
