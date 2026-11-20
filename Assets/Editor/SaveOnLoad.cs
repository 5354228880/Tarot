using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class SaveOnLoad
{
    static SaveOnLoad()
    {
        EditorApplication.playModeStateChanged += (PlayModeStateChange state) =>
        {
            if(state == PlayModeStateChange.ExitingEditMode)
            {
                Debug.Log("Auto saved");
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveOpenScenes();
            }
        };
    }
}
