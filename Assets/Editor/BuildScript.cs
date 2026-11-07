using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MultiDungeon.EditorTools
{
    /// <summary>
    /// 메뉴 또는 커맨드라인(-executeMethod MultiDungeon.EditorTools.BuildScript.BuildWindows)으로 실행파일을 만든다.
    /// 결과물: Builds/Windows/MultiDungeon.exe
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("멀티던전/Windows 실행파일 빌드")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Title.unity" },
                locationPathName = "Builds/Windows/MultiDungeon.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[멀티던전] 빌드 결과: " + report.summary.result + " → " + options.locationPathName);
            if (Application.isBatchMode)
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
