using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MultiDungeon.Title
{
    /// <summary>
    /// 시작화면. 씬에 빈 오브젝트로 두면 실행 시 UI 전체를 코드로 만든다.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        const string VolumeKey = "settings.masterVolume";

        static readonly Color BgColor = new Color(0.04f, 0.035f, 0.05f);
        static readonly Color TitleColor = new Color(0.86f, 0.74f, 0.55f);
        static readonly Color SubColor = new Color(0.6f, 0.57f, 0.55f);
        static readonly Color ButtonColor = new Color(0.13f, 0.1f, 0.1f, 0.92f);

        Font font;
        GameObject mainPanel;
        GameObject settingsPanel;
        Text titleText;
        Text statusText;
        Text volumeText;
        Text fullscreenText;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            EnsureEventSystem();
            Build();
        }

        void Update()
        {
            // 횃불처럼 제목 밝기가 미세하게 흔들린다.
            float n = Mathf.PerlinNoise(Time.time * 2.5f, 0f);
            titleText.color = Color.Lerp(TitleColor * 0.8f, TitleColor, n);

            if (settingsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                ShowSettings(false);
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        void Build()
        {
            var canvasGo = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            var bg = CreateImage(root, "Background", BgColor);
            Stretch(bg.rectTransform);

            titleText = CreateText(root, "Title", "멀티 던전", 120, TitleColor, FontStyle.Bold);
            Place(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 280), new Vector2(1400, 160));

            var sub = CreateText(root, "Subtitle", "지하왕국 원정", 40, SubColor, FontStyle.Normal);
            Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(1000, 60));

            mainPanel = CreatePanel(root, "MainMenu", new Vector2(0, -120));
            CreateButton(mainPanel.transform, "방 만들기", 0, OnHost);
            CreateButton(mainPanel.transform, "참가하기", 1, OnJoin);
            CreateButton(mainPanel.transform, "설정", 2, () => ShowSettings(true));
            CreateButton(mainPanel.transform, "종료", 3, OnQuit);

            settingsPanel = CreatePanel(root, "Settings", new Vector2(0, -120));
            var sTitle = CreateText(settingsPanel.transform, "SettingsTitle", "설정", 48, TitleColor, FontStyle.Bold);
            Place(sTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 40), new Vector2(420, 70));
            volumeText = CreateButton(settingsPanel.transform, "", 0, CycleVolume);
            fullscreenText = CreateButton(settingsPanel.transform, "", 1, ToggleFullscreen);
            CreateButton(settingsPanel.transform, "뒤로", 3, () => ShowSettings(false));
            RefreshSettingsLabels();
            settingsPanel.SetActive(false);

            statusText = CreateText(root, "Status", "", 30, SubColor, FontStyle.Italic);
            Place(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(1400, 50));

            var version = CreateText(root, "Version", "v" + Application.version + " 프로토타입", 24, SubColor * 0.8f, FontStyle.Normal);
            version.alignment = TextAnchor.LowerRight;
            Place(version.rectTransform, new Vector2(1f, 0f), new Vector2(-220, 40), new Vector2(400, 40));
        }

        // ---- 버튼 동작 ----

        void OnHost() => statusText.text = "방 만들기: 네트워크 기능은 다음 단계에서 연결됩니다.";
        void OnJoin() => statusText.text = "참가하기: 네트워크 기능은 다음 단계에서 연결됩니다.";

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
            // 0% → 20% → ... → 100% → 0% 순환
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

        // ---- UI 생성 도우미 ----

        GameObject CreatePanel(Transform parent, string name, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, new Vector2(0.5f, 0.5f), offset, new Vector2(420, 380));
            return go;
        }

        Text CreateButton(Transform parent, string label, int index, UnityEngine.Events.UnityAction onClick)
        {
            var img = CreateImage(parent, "Button_" + index, ButtonColor);
            Place(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -50 - index * 95), new Vector2(420, 80));
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.6f, 1.3f, 1f);
            colors.pressedColor = new Color(0.7f, 0.6f, 0.5f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var text = CreateText(img.transform, "Label", label, 36, TitleColor, FontStyle.Normal);
            Stretch(text.rectTransform);
            return text;
        }

        Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        Text CreateText(Transform parent, string name, string value, int size, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = value;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return t;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
