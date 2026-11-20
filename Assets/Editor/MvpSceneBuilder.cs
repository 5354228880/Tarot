using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tarot.Game;

public static class MvpSceneBuilder
{
    const string MenuScene = "Assets/_Scenes/Menu.unity";
    const string LayerScene = "Assets/_Scenes/Layer.unity";
    const string CombatScene = "Assets/_Scenes/Combat.unity";

    [MenuItem("Tarot/Build MVP Scenes")]
    public static void Build()
    {
        CreateScene(LayerScene, "LayerBootstrap", typeof(LayerBootstrap));
        CreateScene(CombatScene, "CombatBootstrap", typeof(CombatBootstrap));

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScene, true),
            new EditorBuildSettingsScene(LayerScene, true),
            new EditorBuildSettingsScene(CombatScene, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("MVP scenes built: Menu, Layer, Combat.");
    }

    static void CreateScene(string path, string name, Type componentType)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent(componentType);
        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
    }
}
