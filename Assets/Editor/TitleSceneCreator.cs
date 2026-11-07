using System.IO;
using System.Linq;
using MultiDungeon.Title;
using MultiDungeon.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MultiDungeon.EditorTools
{
    /// <summary>
    /// 처음 프로젝트를 열면 Title, WorldMap 씬을 자동으로 만들고 빌드 설정에 등록한다.
    /// 배치모드: -executeMethod MultiDungeon.EditorTools.TitleSceneCreator.Create
    /// </summary>
    [InitializeOnLoad]
    public static class TitleSceneCreator
    {
        public const string TitlePath = "Assets/Scenes/Title.unity";
        public const string WorldMapPath = "Assets/Scenes/WorldMap.unity";

        static TitleSceneCreator()
        {
            EditorApplication.delayCall += () =>
            {
                if ((!File.Exists(TitlePath) || !File.Exists(WorldMapPath)) && !EditorApplication.isPlayingOrWillChangePlaymode)
                    Create();
            };
        }

        [MenuItem("멀티던전/씬 다시 만들기")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Directory.CreateDirectory("Assets/Scenes");
            CreateScene(WorldMapPath, "WorldMapScreen", typeof(WorldMapScreen));
            CreateScene(TitlePath, "TitleScreen", typeof(TitleScreen));

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TitlePath, true),
                new EditorBuildSettingsScene(WorldMapPath, true),
            };
            Debug.Log("[멀티던전] Title, WorldMap 씬을 만들었습니다.");
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
            new GameObject(objectName, screenType);
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
