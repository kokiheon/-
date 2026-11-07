using System.IO;
using MultiDungeon.Dungeon;
using MultiDungeon.Title;
using MultiDungeon.UI;
using MultiDungeon.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MultiDungeon.EditorTools
{
    /// <summary>
    /// 처음 프로젝트를 열면 Title, WorldMap, Dungeon 씬을 자동으로 만들고 빌드 설정에 등록한다.
    /// 배치모드: -executeMethod MultiDungeon.EditorTools.TitleSceneCreator.Create
    /// </summary>
    [InitializeOnLoad]
    public static class TitleSceneCreator
    {
        public const string TitlePath = "Assets/Scenes/Title.unity";
        public const string WorldMapPath = "Assets/Scenes/WorldMap.unity";
        public const string DungeonPath = "Assets/Scenes/Dungeon.unity";

        static TitleSceneCreator()
        {
            EditorApplication.delayCall += () =>
            {
                if ((!File.Exists(TitlePath) || !File.Exists(WorldMapPath) || !File.Exists(DungeonPath))
                    && !EditorApplication.isPlayingOrWillChangePlaymode)
                    Create();
            };
        }

        [MenuItem("멀티던전/씬 다시 만들기")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Directory.CreateDirectory("Assets/Scenes");
            CreateScene(DungeonPath, "DungeonGame", typeof(DungeonGame));
            CreateScene(WorldMapPath, "WorldMapScreen", typeof(WorldMapScreen));
            CreateScene(TitlePath, "TitleScreen", typeof(TitleScreen));

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TitlePath, true),
                new EditorBuildSettingsScene(WorldMapPath, true),
                new EditorBuildSettingsScene(DungeonPath, true),
            };
            Debug.Log("[멀티던전] Title, WorldMap, Dungeon 씬을 만들었습니다.");
        }

        static void CreateScene(string path, string objectName, System.Type screenType)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            var screen = new GameObject(objectName).AddComponent(screenType);
            // UI를 씬에 실제 오브젝트로 배치해 저장한다 (Hierarchy에서 보고 고칠 수 있음).
            if (screen is TitleScreen title) { Ui.EnsureEventSystem(); title.BuildLayout(); }
            if (screen is WorldMapScreen worldMap) { Ui.EnsureEventSystem(); worldMap.BuildLayout(); }
            if (screen is DungeonGame dungeon) { Ui.EnsureEventSystem(); dungeon.BuildLayout(); }
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
