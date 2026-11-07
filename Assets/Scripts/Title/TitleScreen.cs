using MultiDungeon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiDungeon.Title
{
    /// <summary>시작화면. 씬에 빈 오브젝트로 두면 실행 시 UI 전체를 코드로 만든다.</summary>
    public class TitleScreen : MonoBehaviour
    {
        public const string SceneName = "Title";
        const string VolumeKey = "settings.masterVolume";

        GameObject mainPanel;
        GameObject settingsPanel;
        Text titleText;
        Text statusText;
        Text volumeText;
        Text fullscreenText;

        void Awake()
        {
            AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            Ui.EnsureEventSystem();
            Build();
        }

        void Update()
        {
            // 횃불처럼 제목 밝기가 미세하게 흔들린다.
            float n = Mathf.PerlinNoise(Time.time * 2.5f, 0f);
            titleText.color = Color.Lerp(Ui.Gold * 0.8f, Ui.Gold, n);

            if (settingsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                ShowSettings(false);
        }

        void Build()
        {
            var root = Ui.CreateCanvas(transform, "TitleCanvas");
            Ui.Stretch(Ui.Image(root, "Background", Ui.Bg).rectTransform);

            titleText = Ui.Text(root, "Title", "멀티 던전", 120, Ui.Gold, FontStyle.Bold);
            Ui.Place(titleText.rectTransform, Ui.Center, new Vector2(0, 280), new Vector2(1400, 160));
            var sub = Ui.Text(root, "Subtitle", "지하왕국 원정", 40, Ui.Grey);
            Ui.Place(sub.rectTransform, Ui.Center, new Vector2(0, 180), new Vector2(1000, 60));

            mainPanel = Panel(root, "MainMenu");
            MenuButton(mainPanel.transform, "게임 시작", 0, OnStart);
            MenuButton(mainPanel.transform, "멀티플레이 (준비 중)", 1, OnMulti);
            MenuButton(mainPanel.transform, "설정", 2, () => ShowSettings(true));
            MenuButton(mainPanel.transform, "종료", 3, OnQuit);

            settingsPanel = Panel(root, "Settings");
            var sTitle = Ui.Text(settingsPanel.transform, "SettingsTitle", "설정", 48, Ui.Gold, FontStyle.Bold);
            Ui.Place(sTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 40), new Vector2(420, 70));
            volumeText = MenuButton(settingsPanel.transform, "", 0, CycleVolume);
            fullscreenText = MenuButton(settingsPanel.transform, "", 1, ToggleFullscreen);
            MenuButton(settingsPanel.transform, "뒤로", 3, () => ShowSettings(false));
            RefreshSettingsLabels();
            settingsPanel.SetActive(false);

            statusText = Ui.Text(root, "Status", "", 30, Ui.Grey, FontStyle.Italic);
            Ui.Place(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(1400, 50));

            var version = Ui.Text(root, "Version", "v" + Application.version + " 프로토타입", 24, Ui.Grey * 0.8f,
                FontStyle.Normal, TextAnchor.LowerRight);
            Ui.Place(version.rectTransform, Ui.BottomRight, new Vector2(-40, 40), new Vector2(400, 40));
        }

        static GameObject Panel(Transform root, string name)
        {
            var rt = Ui.Rect(root, name);
            Ui.Place(rt, Ui.Center, new Vector2(0, -120), new Vector2(420, 380));
            return rt.gameObject;
        }

        static Text MenuButton(Transform parent, string label, int index, UnityEngine.Events.UnityAction onClick)
        {
            var text = Ui.Button(parent, label, 36, onClick, out var button);
            Ui.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0, -50 - index * 95), new Vector2(420, 80));
            return text;
        }

        void OnStart() => SceneManager.LoadScene(WorldMap.WorldMapScreen.SceneName);
        void OnMulti() => statusText.text = "멀티플레이는 개인 플레이 버전 이후에 추가됩니다.";

        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ShowSettings(bool show)
        {
            settingsPanel.SetActive(show);
            mainPanel.SetActive(!show);
            statusText.text = "";
        }

        void CycleVolume()
        {
            float v = Mathf.Round(AudioListener.volume * 5f) / 5f + 0.2f;
            if (v > 1.01f) v = 0f;
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(VolumeKey, v);
            RefreshSettingsLabels();
        }

        void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            RefreshSettingsLabels(!Screen.fullScreen);
        }

        void RefreshSettingsLabels(bool? fullscreen = null)
        {
            volumeText.text = $"전체 볼륨: {Mathf.RoundToInt(AudioListener.volume * 100)}%";
            fullscreenText.text = "전체 화면: " + ((fullscreen ?? Screen.fullScreen) ? "켬" : "끔");
        }
    }
}
