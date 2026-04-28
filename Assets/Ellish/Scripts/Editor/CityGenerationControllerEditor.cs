using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CityGenerationController))]
public class CityGenerationControllerEditor : Editor
{
    private const string DefaultRuntimePresetFolder = "Assets/Ellish/Data/RuntimeCityPresets";
    private const string DefaultRuntimePresetPath = DefaultRuntimePresetFolder + "/RuntimeCityPresetLibrary.asset";

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

        DrawSection("Runtime UI Presets");
        DrawButton(controller, "Save Current Settings as Runtime Preset", "Save Runtime Preset", false, () =>
            SaveCurrentSettingsAsRuntimePreset(controller));
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

    private static void SaveCurrentSettingsAsRuntimePreset(CityGenerationController controller)
    {
        if (controller == null)
            return;

        RuntimeCityPresetLibrary library = EnsureRuntimePresetLibrary(controller);
        if (library == null)
            return;

        RuntimeCityPreset preset = RuntimeCityPreset.CreateFromController(controller, controller.runtimePresetName);
        Undo.RecordObject(library, "Save Runtime City Preset");
        library.AddOrReplace(preset);

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"CityGenerationController: Saved runtime preset '{preset.presetName}' to {AssetDatabase.GetAssetPath(library)}.");
    }

    private static RuntimeCityPresetLibrary EnsureRuntimePresetLibrary(CityGenerationController controller)
    {
        if (controller.runtimePresetLibrary != null)
            return controller.runtimePresetLibrary;

        RuntimeCityPresetLibrary library = AssetDatabase.LoadAssetAtPath<RuntimeCityPresetLibrary>(DefaultRuntimePresetPath);
        if (library == null)
        {
            EnsureFolder(DefaultRuntimePresetFolder);
            library = CreateInstance<RuntimeCityPresetLibrary>();
            AssetDatabase.CreateAsset(library, DefaultRuntimePresetPath);
        }

        Undo.RecordObject(controller, "Assign Runtime Preset Library");
        controller.runtimePresetLibrary = library;
        EditorUtility.SetDirty(controller);
        return library;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
