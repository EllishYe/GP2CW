using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CityGenerationController))]
public class CityGenerationControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityGenerationController controller = (CityGenerationController)target;

        DrawSection("Setup");
        DrawButton(controller, "Refresh Stage References", "Refresh Stage References", false, () =>
            controller.FindStageGenerators());
        DrawButton(controller, "Ensure Generated Hierarchy", "Ensure Generated Hierarchy", true, () =>
            controller.EnsureGeneratedHierarchy());

        DrawSection("Road Network");
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawButton(controller, "Generate Roads", "Generate Roads", true, () =>
                controller.GenerateRoads());
            DrawButton(controller, "Clear Roads", "Clear Roads", true, () =>
                controller.ClearRoads());
        }

        DrawSection("Walkable Areas");
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawButton(controller, "Generate Walkable", "Generate Walkable", true, () =>
                controller.GenerateWalkable());
            DrawButton(controller, "Clear Walkable", "Clear Walkable", true, () =>
                controller.ClearWalkable());
        }

        DrawSection("Blocks");
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawButton(controller, "Generate Blocks", "Generate Blocks", true, () =>
                controller.GenerateBlocks());
            DrawButton(controller, "Clear Blocks", "Clear Blocks", true, () =>
                controller.ClearBlocks());
        }

        DrawSection("Block Land Use Overrides");
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawButton(controller, "Apply Land Use Overrides", "Apply Block Overrides", true, () =>
                controller.ApplyBlockDebugOverrides());
            DrawButton(controller, "Save Land Use Overrides", "Save Block Overrides", true, () =>
                controller.SaveBlockDebugOverrides());
        }
        DrawButton(controller, "Clear Saved Land Use Overrides", "Clear Saved Block Overrides", false, () =>
            controller.ClearSavedBlockOverrides());

        DrawSection("Block Type Assignment");
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawButton(controller, "Assign Block Types", "Assign Block Types", true, () =>
                controller.AssignBlockTypes());
            DrawButton(controller, "Apply Type Overrides", "Apply Block Type Overrides", true, () =>
                controller.ApplyBlockTypeOverrides());
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawButton(controller, "Save Type Overrides", "Save Block Type Overrides", true, () =>
                controller.SaveBlockTypeOverrides());
            DrawButton(controller, "Clear Saved Type Overrides", "Clear Saved Block Type Overrides", false, () =>
                controller.ClearSavedBlockTypeOverrides());
        }

        DrawSection("Lots & Buildings");
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawButton(controller, "Generate Lots & Buildings", "Generate Lots", true, () =>
                controller.GenerateLots());
            DrawButton(controller, "Clear Lots & Buildings", "Clear Lots", true, () =>
                controller.ClearLots());
        }

        DrawSection("Full Pipeline");
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawButton(controller, "Generate All", "Generate All City Stages", true, () =>
                controller.GenerateAll());
            DrawButton(controller, "Clear All", "Clear All City Stages", true, () =>
                controller.ClearAll());
        }
    }

    private static void DrawSection(string label)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    private static void DrawButton(CityGenerationController controller, string label, string undoName, bool fullHierarchyUndo, System.Action action)
    {
        if (!GUILayout.Button(label))
            return;

        if (fullHierarchyUndo)
            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, undoName);
        else
            Undo.RecordObject(controller, undoName);

        action?.Invoke();
        EditorUtility.SetDirty(controller);
    }
}
