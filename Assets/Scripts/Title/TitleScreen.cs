using MultiDungeon.Localization;
using MultiDungeon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiDungeon.Title
{
    /// <summary>
    /// 시작화면. UI는 에디터에서 씬을 만들 때 BuildLayout()으로 배치되어 씬에 저장된다.
    /// 버튼 연결은 각 버튼 인스펙터의 OnClick에서 볼 수 있고, 글자는 LocalizedText의 Key로 정한다.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        public const string SceneName = "Title";
        const string VolumeKey = "settings.masterVolume";

        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject settingsPanel;
        [SerializeField] Text titleText;
        [SerializeField] Text statusText;
        [SerializeField] Text volumeText;
        [SerializeField] Text fullscreenText;
        [SerializeField] Text languageText;
        [SerializeField] Text versionText;

        void Awake()
        {
            AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            Ui.EnsureEventSystem();
            if (titleText == null) BuildLayout(); // 씬에 배치된 UI가 없을 때만 만든다
            ShowSettings(false);
            RefreshLabels();
        }

        void OnEnable() => Loc.Changed += RefreshLabels;
        void OnDisable() => Loc.Changed -= RefreshLabels;

        void Update()
        {
            // 횃불처럼 제목 밝기가 미세하게 흔들린다.
            float n = Mathf.PerlinNoise(Time.time * 2.5f, 0f);
            titleText.color = Color.Lerp(Ui.Gold * 0.8f, Ui.Gold, n);

            if (settingsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                CloseSettings();
        }

        public void BuildLayout()
        {
            var root = Ui.CreateCanvas(transform, "TitleCanvas");
            Ui.Stretch(Ui.Image(root, "Background", Ui.Bg).rectTransform);

            titleText = Ui.LText(root, "Title", "title.name", 120, Ui.Gold, FontStyle.Bold);
            Ui.Place(titleText.rectTransform, Ui.Center, new Vector2(0, 280), new Vector2(1400, 160));
            var sub = Ui.LText(root, "Subtitle", "title.subtitle", 40, Ui.Grey);
            Ui.Place(sub.rectTransform, Ui.Center, new Vector2(0, 180), new Vector2(1000, 60));

            mainPanel = Panel(root, "MainMenu");
            MenuButton(mainPanel.transform, "title.start", 0, OnStart);
            MenuButton(mainPanel.transform, "title.multi", 1, OnMulti);
            MenuButton(mainPanel.transform, "title.settings", 2, OpenSettings);
            MenuButton(mainPanel.transform, "title.quit", 3, OnQuit);

            settingsPanel = Panel(root, "Settings");
            var sTitle = Ui.LText(settingsPanel.transform, "SettingsTitle", "title.settings", 48, Ui.Gold, FontStyle.Bold);
            Ui.Place(sTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 40), new Vector2(420, 70));
            volumeText = MenuButton(settingsPanel.transform, "", 0, CycleVolume);
            fullscreenText = MenuButton(settingsPanel.transform, "", 1, ToggleFullscreen);
            languageText = MenuButton(settingsPanel.transform, "", 2, NextLanguage);
            MenuButton(settingsPanel.transform, "title.back", 3, CloseSettings);

            statusText = Ui.Text(root, "Status", "", 30, Ui.Grey, FontStyle.Italic);
            Ui.Place(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(1400, 50));

            versionText = Ui.Text(root, "Version", "", 24, Ui.Grey * 0.8f, FontStyle.Normal, TextAnchor.LowerRight);
            Ui.Place(versionText.rectTransform, Ui.BottomRight, new Vector2(-40, 40), new Vector2(400, 40));
            RefreshLabels();
        }

        static GameObject Panel(Transform root, string name)
        {
            var rt = Ui.Rect(root, name);
            Ui.Place(rt, Ui.Center, new Vector2(0, -120), new Vector2(420, 380));
            return rt.gameObject;
        }

        static Text MenuButton(Transform parent, string key, int index, UnityEngine.Events.UnityAction onClick)
        {
            var text = Ui.Button(parent, key, 36, onClick, out var button);
            Ui.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0, -50 - index * 95), new Vector2(420, 80));
            return text;
        }

        // ---- 버튼 동작 (인스펙터 OnClick에 연결됨) ----

        public void OnStart() => SceneManager.LoadScene(WorldMap.WorldMapScreen.SceneName);
        public void OnMulti() => statusText.text = Loc.Get("title.multi_status");
        public void OpenSettings() => ShowSettings(true);
        public void CloseSettings() => ShowSettings(false);
        public void NextLanguage() => Loc.Next();

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void CycleVolume()
        {
            float v = Mathf.Round(AudioListener.volume * 5f) / 5f + 0.2f;
            if (v > 1.01f) v = 0f;
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(VolumeKey, v);
            RefreshLabels();
        }

        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            RefreshLabels(!Screen.fullScreen);
        }

        void ShowSettings(bool show)
        {
            settingsPanel.SetActive(show);
            mainPanel.SetActive(!show);
            statusText.text = "";
        }

        void RefreshLabels() => RefreshLabels(null);

        void RefreshLabels(bool? fullscreen)
        {
            volumeText.text = Loc.Format("settings.volume", Mathf.RoundToInt(AudioListener.volume * 100));
            bool fs = fullscreen ?? Screen.fullScreen;
            fullscreenText.text = Loc.Format("settings.fullscreen", Loc.Get(fs ? "settings.on" : "settings.off"));
            languageText.text = Loc.Format("settings.language", Loc.Get("language.name"));
            versionText.text = Loc.Format("title.version", Application.version);
        }
    }
}
