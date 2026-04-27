using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadNetworkGenerator))]
public class RoadNetworkGeneratorEditor : Editor
{
    private const string DefaultProfileFolder = "Assets/Ellish/Data/RoadNetworkProfiles";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "profile");
        serializedObject.ApplyModifiedProperties();

        RoadNetworkGenerator generator = (RoadNetworkGenerator)target;

        EditorGUILayout.Space();
        DrawProfileControls(generator);

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

    private void DrawProfileControls(RoadNetworkGenerator generator)
    {
        EditorGUILayout.LabelField("Road Network Profile", EditorStyles.boldLabel);

        RoadNetworkProfile[] profiles = LoadProfiles();
        string[] labels = BuildProfileLabels(profiles);
        int selectedIndex = GetSelectedProfileIndex(generator.profile, profiles);

        EditorGUI.BeginChangeCheck();
        int nextIndex = EditorGUILayout.Popup("Saved Profiles", selectedIndex, labels);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(generator, "Select Road Network Profile");
            generator.profile = nextIndex <= 0 ? null : profiles[nextIndex - 1];
            EditorUtility.SetDirty(generator);
        }

        using (new EditorGUI.DisabledScope(generator.profile == null))
        {
            if (GUILayout.Button("Apply Selected Profile"))
            {
                Undo.RecordObject(generator, "Apply Road Network Profile");
                generator.ApplyProfile();
                EditorUtility.SetDirty(generator);
            }

            if (GUILayout.Button("Save Current Settings to Selected Profile"))
            {
                Undo.RecordObject(generator.profile, "Save Road Network Profile");
                generator.CaptureCurrentSettingsToProfile();
                EditorUtility.SetDirty(generator.profile);
                AssetDatabase.SaveAssets();
            }
        }

        if (GUILayout.Button("Save Current Settings as New Profile"))
            SaveCurrentSettingsAsNewProfile(generator);
    }

    private static RoadNetworkProfile[] LoadProfiles()
    {
        string[] guids = AssetDatabase.FindAssets("t:RoadNetworkProfile");
        RoadNetworkProfile[] profiles = new RoadNetworkProfile[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            profiles[i] = AssetDatabase.LoadAssetAtPath<RoadNetworkProfile>(path);
        }

        return profiles;
    }

    private static string[] BuildProfileLabels(RoadNetworkProfile[] profiles)
    {
        string[] labels = new string[profiles.Length + 1];
        labels[0] = "None";

        for (int i = 0; i < profiles.Length; i++)
        {
            RoadNetworkProfile profile = profiles[i];
            if (profile == null)
            {
                labels[i + 1] = "Missing Profile";
                continue;
            }

            string path = AssetDatabase.GetAssetPath(profile);
            labels[i + 1] = string.IsNullOrEmpty(path) ? profile.name : $"{profile.name} ({path})";
        }

        return labels;
    }

    private static int GetSelectedProfileIndex(RoadNetworkProfile selectedProfile, RoadNetworkProfile[] profiles)
    {
        if (selectedProfile == null)
            return 0;

        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i] == selectedProfile)
                return i + 1;
        }

        return 0;
    }

    private static void SaveCurrentSettingsAsNewProfile(RoadNetworkGenerator generator)
    {
        EnsureDefaultProfileFolder();

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Road Network Profile",
            "RoadNetworkProfile",
            "asset",
            "Choose where to save the current RoadNetworkGenerator settings.",
            DefaultProfileFolder);

        if (string.IsNullOrEmpty(path))
            return;

        RoadNetworkProfile profile = CreateInstance<RoadNetworkProfile>();
        profile.CaptureFrom(generator);
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Undo.RecordObject(generator, "Assign Road Network Profile");
        generator.profile = profile;
        EditorUtility.SetDirty(generator);
        Selection.activeObject = profile;
    }

    private static void EnsureDefaultProfileFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Ellish/Data"))
            AssetDatabase.CreateFolder("Assets/Ellish", "Data");

        if (!AssetDatabase.IsValidFolder(DefaultProfileFolder))
            AssetDatabase.CreateFolder("Assets/Ellish/Data", "RoadNetworkProfiles");
    }
}
