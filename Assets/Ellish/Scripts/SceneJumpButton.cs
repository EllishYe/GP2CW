using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneJumpButton : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string sceneNameOrPath = "Assets/Ding-Sim/S_Test.unity";
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

    public void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(sceneNameOrPath))
        {
            Debug.LogWarning("SceneJumpButton: Target scene name or path is empty.");
            return;
        }

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneNameOrPath);
        if (buildIndex < 0)
        {
            buildIndex = SceneUtility.GetBuildIndexByScenePath(GetSceneNameWithoutExtension(sceneNameOrPath));
        }

        if (buildIndex < 0)
        {
            Debug.LogWarning(
                $"SceneJumpButton: Scene '{sceneNameOrPath}' is not in Build Settings. " +
                "Add it through File > Build Settings > Scenes In Build before using this button in Play Mode.");
            return;
        }

        SceneManager.LoadScene(buildIndex, loadMode);
    }

    public void SetTargetScene(string targetScene)
    {
        sceneNameOrPath = targetScene;
    }

    private static string GetSceneNameWithoutExtension(string scenePath)
    {
        int slashIndex = scenePath.LastIndexOf('/');
        string fileName = slashIndex >= 0 ? scenePath.Substring(slashIndex + 1) : scenePath;

        const string unityExtension = ".unity";
        if (fileName.EndsWith(unityExtension, System.StringComparison.OrdinalIgnoreCase))
        {
            return fileName.Substring(0, fileName.Length - unityExtension.Length);
        }

        return fileName;
    }
}
