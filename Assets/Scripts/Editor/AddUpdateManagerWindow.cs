using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class AddUpdateManagerWindow:  EditorWindow
{
    [MenuItem("Tools/Update Manager/Add Update Manager")]
    public static void Init()
    {
        UpdateManager updateManager = FindObjectOfType<UpdateManager>();
        if (updateManager != null) return;
        GameObject updateManagerObject = new GameObject("UpdateManager");
        updateManagerObject.AddComponent<UpdateManager>();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}