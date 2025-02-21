using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UpdateManagerEditor : EditorWindow
{
    private GameObject _selected;
    private string _lastSaved;
    private string file;
    private string path;
    [MenuItem("Tools/Update Manager/Editor Updates")]
    public static void ShowWindow()
    {
        var window = GetWindow(typeof(UpdateManagerEditor));
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Select the object you want to add the new UpdateManager");
        EditorGUILayout.Separator();
        _selected = EditorGUILayout.ObjectField("Game Object to Modify: ",_selected, typeof(GameObject), true) as GameObject;
        if (_selected != null) 
            DetectUpdate();
        if(_lastSaved != String.Empty && file != String.Empty && path != String.Empty)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Undo"))
            {
                file = _lastSaved;
                File.WriteAllText(path, file);
                AssetDatabase.Refresh();
                _lastSaved = String.Empty;
                Debug.Log("Undo Done");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }

    private void DetectUpdate()
    {
        EditorGUILayout.Separator();
        if(_selected.GetComponent<BaseMonoBehaviour>())
        {
            EditorGUILayout.HelpBox("The object has the BaseMonoBehaviour script attached", MessageType.Info, true);
            return;
        }
        if (_selected.GetComponent<MonoBehaviour>())
        {
            List<Component> temp = new List<Component>();
            _selected.GetComponents(temp);
            foreach (var item in temp)
            {
                if (item.GetType().BaseType == typeof(MonoBehaviour))
                {
                    var t = item.GetType().FullName + ".cs";
                    path = SearchFileByScriptName(t);
                    file = File.ReadAllText(path);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (file.Contains("MonoBehaviour") && GUILayout.Button("Modify Script"))
                    {
                        _lastSaved = file;
                        file = file.Replace("MonoBehaviour", "BaseMonoBehaviour");
                        if (file.Contains("void Start()"))
                            file = file.Replace("void Start()" + Environment.NewLine + "    {",
                                "protected override void Start()\n\t{\n\t\tbase.Start();");
                        if (file.Contains("void Update"))
                            file = file.Replace("void Update", "public override void OnUpdate");
                        if (file.Contains("void FixedUpdate"))
                            file = file.Replace("private void FixedUpdate", "public override void OnFixedUpdate");
                        if (file.Contains("void LateUpdate"))
                            file = file.Replace("private void LateUpdate", "public override void OnLateUpdate");

                        File.WriteAllText(path, file);
                        AssetDatabase.Refresh();
                        Debug.Log("saved");
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
        }
        else
            EditorGUILayout.HelpBox("The object has no MonoBehaviour", MessageType.Error, true);
    }
    
    private string SearchFileByScriptName(string scriptName)
    {
        var files = Directory.GetFiles(Application.dataPath, scriptName, SearchOption.AllDirectories);
        return files.Length > 0 ? files[0] : "Not Found";
    }
}
