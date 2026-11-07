using System.IO;
using System.Linq;
using MultiDungeon.Title;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MultiDungeon.EditorTools
{
    /// <summary>
    /// 처음 프로젝트를 열면 Title 씬을 자동으로 만들고 빌드 설정 첫 번째 씬으로 등록한다.
    /// </summary>
    [InitializeOnLoad]
    public static class TitleSceneCreator
    {
        const string ScenePath = "Assets/Scenes/Title.unity";

        static TitleSceneCreator()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                    Create();
            };
        }

        [MenuItem("멀티던전/타이틀 씬 다시 만들기")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;

            new GameObject("TitleScreen", typeof(TitleScreen));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath))
                .ToArray();
            Debug.Log("[멀티던전] Title 씬을 만들었습니다: " + ScenePath);
        }
    }
}
