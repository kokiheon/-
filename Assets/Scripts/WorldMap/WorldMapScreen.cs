using System.Collections.Generic;
using MultiDungeon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiDungeon.WorldMap
{
    /// <summary>
    /// 원정 선택 지도. 지하왕국 단면 위에 지역 아이콘과 원정 표식(색 = 난이도, 글자 = 길이)을 놓는다.
    /// 씬에 빈 오브젝트로 두면 실행 시 UI 전체를 코드로 만든다.
    /// </summary>
    public class WorldMapScreen : MonoBehaviour
    {
        public const string SceneName = "WorldMap";
        static readonly Vector2 MapSize = new Vector2(1280, 880);
        static readonly Color Dark = new Color(0.08f, 0.07f, 0.06f);

        RectTransform map;
        RectTransform panelContent;
        Text status;
        Button departButton;
        readonly Dictionary<Region, Image> rings = new Dictionary<Region, Image>();
        readonly Dictionary<Region, RectTransform> badgeRows = new Dictionary<Region, RectTransform>();
        Region selectedRegion;
        Expedition selectedExpedition;

        void Awake()
        {
            Ui.EnsureEventSystem();
            ExpeditionBoard.EnsureGenerated();
            Build();
            SelectRegion(ExpeditionBoard.Regions[0]);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) BackToTitle();
        }

        // ---------- 화면 만들기 ----------

        void Build()
        {
            var root = Ui.CreateCanvas(transform, "WorldMapCanvas");
            Ui.Stretch(Ui.Image(root, "Background", Ui.Bg).rectTransform);

            map = Ui.Rect(root, "Map");
            Ui.Place(map, Ui.BottomLeft, new Vector2(600, 150), MapSize);
            Band("지상", 770, 110, 0.16f);
            Band("상층", 610, 160, 0.13f);
            Band("중층", 440, 170, 0.11f);
            Band("하부", 290, 150, 0.09f);
            Band("심층", 160, 130, 0.07f);
            Band("최하층", 0, 160, 0.05f);

            var village = Ui.Image(map, "Village", Ui.Grey);
            village.sprite = Ui.Circle;
            PlaceAt(village.rectTransform, new Vector2(640, 825), new Vector2(70, 70));
            var villageName = Ui.Text(map, "VillageName", "관문 마을 (거점)", 26, Ui.Gold, FontStyle.Bold, TextAnchor.MiddleLeft);
            PlaceLeft(villageName.rectTransform, new Vector2(690, 825), new Vector2(320, 36), Vector2.zero);

            foreach (var region in ExpeditionBoard.Regions) CreateNode(region);

            var panel = Ui.Image(root, "SidePanel", Ui.PanelBg);
            Ui.Place(panel.rectTransform, Ui.TopLeft, new Vector2(40, -40), new Vector2(520, 870));
            panelContent = Ui.Rect(panel.transform, "Content");
            Ui.Stretch(panelContent);
            status = Ui.Text(panel.transform, "Status", "", 22, Ui.Grey, FontStyle.Italic, TextAnchor.LowerLeft);
            Ui.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(460, 130));

            var bar = Ui.Image(root, "BottomBar", Ui.PanelBg);
            Ui.Place(bar.rectTransform, Ui.BottomLeft, new Vector2(40, 30), new Vector2(1840, 100));
            var partyLabel = Ui.Text(bar.transform, "PartyLabel", "내 파티", 30, Ui.Gold, FontStyle.Bold, TextAnchor.MiddleLeft);
            Ui.Place(partyLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(140, 60));
            for (int i = 0; i < 4; i++)
            {
                var slot = Ui.Image(bar.transform, "Slot" + i, new Color(0.2f, 0.17f, 0.15f));
                Ui.Place(slot.rectTransform, new Vector2(0f, 0.5f), new Vector2(180 + i * 84, 0), new Vector2(70, 70));
                var empty = Ui.Text(slot.transform, "Label", "빈 칸", 18, Ui.Grey);
                Ui.Stretch(empty.rectTransform);
            }

            Ui.Button(bar.transform, "원정 출발", 32, Depart, out departButton);
            Ui.Place((RectTransform)departButton.transform, new Vector2(1f, 0.5f), new Vector2(-30, 0), new Vector2(260, 70));
            Ui.Button(bar.transform, "원정 완료 (테스트)", 26, CompleteTest, out var completeButton);
            Ui.Place((RectTransform)completeButton.transform, new Vector2(1f, 0.5f), new Vector2(-310, 0), new Vector2(300, 70));
            Ui.Button(bar.transform, "타이틀로", 26, BackToTitle, out var backButton);
            Ui.Place((RectTransform)backButton.transform, new Vector2(1f, 0.5f), new Vector2(-630, 0), new Vector2(200, 70));
        }

        void Band(string label, float y, float height, float shade)
        {
            var band = Ui.Image(map, "Band_" + label, new Color(shade, shade * 0.9f, shade * 0.85f));
            Ui.Place(band.rectTransform, Ui.BottomLeft, new Vector2(0, y), new Vector2(MapSize.x, height));
            var text = Ui.Text(band.transform, "Label", label, 22, Ui.Grey, FontStyle.Normal, TextAnchor.UpperLeft);
            Ui.Place(text.rectTransform, Ui.TopLeft, new Vector2(16, -10), new Vector2(160, 30));
        }

        void CreateNode(Region region)
        {
            var ring = Ui.Image(map, "Ring_" + region.Id, Ui.Accent);
            ring.sprite = Ui.Circle;
            PlaceAt(ring.rectTransform, region.MapPos, new Vector2(116, 116));
            ring.enabled = false;
            rings[region] = ring;

            var icon = Ui.Image(map, "Icon_" + region.Id, region.Color);
            icon.sprite = Ui.Circle;
            PlaceAt(icon.rectTransform, region.MapPos, new Vector2(90, 90));
            icon.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectRegion(region));
            var letter = Ui.Text(icon.transform, "Letter", region.Name.Substring(0, 1), 40, Dark, FontStyle.Bold);
            Ui.Stretch(letter.rectTransform);

            var name = Ui.Text(map, "Name_" + region.Id, region.Name, 28, Ui.Gold, FontStyle.Bold, TextAnchor.MiddleLeft);
            PlaceLeft(name.rectTransform, region.MapPos, new Vector2(320, 36), new Vector2(60, 20));

            var row = Ui.Rect(map, "Badges_" + region.Id);
            PlaceLeft(row, region.MapPos, new Vector2(200, 44), new Vector2(60, -24));
            badgeRows[region] = row;
        }

        void RebuildBadges(Region region)
        {
            var row = badgeRows[region];
            for (int i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);

            var list = ExpeditionBoard.For(region);
            for (int i = 0; i < list.Count; i++)
            {
                var exp = list[i];
                var badge = Ui.Image(row, "Badge" + i, Expedition.TierColor(exp.Tier));
                var rt = badge.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(i * 52, 0);
                rt.sizeDelta = new Vector2(44, 44);
                badge.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectExpedition(exp));
                if (exp == selectedExpedition)
                {
                    var outline = badge.gameObject.AddComponent<Outline>();
                    outline.effectColor = Color.white;
                    outline.effectDistance = new Vector2(3, 3);
                }
                var len = Ui.Text(badge.transform, "Length", Expedition.LengthName(exp.Length).Substring(0, 1), 22, Dark, FontStyle.Bold);
                Ui.Stretch(len.rectTransform);
            }
        }

        void RefreshPanel()
        {
            for (int i = panelContent.childCount - 1; i >= 0; i--) Destroy(panelContent.GetChild(i).gameObject);
            var region = selectedRegion;
            var list = ExpeditionBoard.For(region);

            AddText(region.Name, 44, Ui.Gold, FontStyle.Bold, -30, 56);
            AddText($"{region.Layer} · 적: {region.Enemies}", 24, Ui.Grey, FontStyle.Normal, -90, 34);
            AddText($"가능한 원정 {list.Count}  (원정이 끝나면 갱신)", 26, Ui.Gold, FontStyle.Bold, -150, 36);

            for (int i = 0; i < list.Count; i++)
            {
                var exp = list[i];
                Ui.Button(panelContent, "", 20, () => SelectExpedition(exp), out var row);
                var rowImage = row.GetComponent<Image>();
                rowImage.color = exp == selectedExpedition ? new Color(0.35f, 0.25f, 0.12f, 0.95f) : Ui.ButtonBg;
                PlaceTopLeft((RectTransform)row.transform, new Vector2(30, -200 - i * 110), new Vector2(460, 96));

                var swatch = Ui.Image(row.transform, "Tier", Expedition.TierColor(exp.Tier));
                swatch.raycastTarget = false;
                PlaceTopLeft(swatch.rectTransform, new Vector2(20, -20), new Vector2(56, 56));
                var title = Ui.Text(row.transform, "Title", exp.Title, 30, Ui.Gold, FontStyle.Bold, TextAnchor.MiddleLeft);
                PlaceTopLeft(title.rectTransform, new Vector2(96, -12), new Vector2(340, 38));
                var goal = Ui.Text(row.transform, "Goal", "목표: " + exp.Goal, 22, Ui.Grey, FontStyle.Normal, TextAnchor.MiddleLeft);
                PlaceTopLeft(goal.rectTransform, new Vector2(96, -54), new Vector2(340, 30));
            }

            if (selectedExpedition != null)
            {
                int tier = (int)selectedExpedition.Tier, length = (int)selectedExpedition.Length;
                AddText("보상 (원정 등급이 정함)", 26, Ui.Gold, FontStyle.Bold, -540, 36);
                AddText($"골드 {500 * (tier + 1) * (length + 1)} · 유물 확률 {10 + tier * 20}%", 24, Ui.Grey, FontStyle.Normal, -580, 34);
            }
            AddText($"완료한 원정: {ExpeditionBoard.CompletedCount}", 22, Ui.Grey, FontStyle.Normal, -640, 30);
        }

        void AddText(string value, int size, Color color, FontStyle style, float y, float height)
        {
            var t = Ui.Text(panelContent, "Text", value, size, color, style, TextAnchor.MiddleLeft);
            PlaceTopLeft(t.rectTransform, new Vector2(30, y), new Vector2(460, height));
        }

        // ---------- 선택과 버튼 ----------

        void SelectRegion(Region region)
        {
            selectedRegion = region;
            var list = ExpeditionBoard.For(region);
            selectedExpedition = list.Count > 0 ? list[0] : null;
            Refresh();
        }

        void SelectExpedition(Expedition exp)
        {
            selectedRegion = exp.Region;
            selectedExpedition = exp;
            Refresh();
        }

        void Refresh()
        {
            foreach (var pair in rings) pair.Value.enabled = pair.Key == selectedRegion;
            foreach (var region in ExpeditionBoard.Regions) RebuildBadges(region);
            RefreshPanel();
            departButton.interactable = selectedExpedition != null;
        }

        void Depart()
        {
            if (selectedExpedition == null) return;
            status.text = $"'{selectedRegion.Name} {selectedExpedition.Title}' 원정 준비 완료. " +
                          "던전 탐험은 다음 단계에서 연결됩니다. '원정 완료 (테스트)'로 목록 갱신을 확인할 수 있어요.";
        }

        void CompleteTest()
        {
            ExpeditionBoard.CompleteExpedition();
            status.text = "원정을 마쳐 지도 전체의 원정 목록이 새로 뽑혔습니다.";
            SelectRegion(selectedRegion ?? ExpeditionBoard.Regions[0]);
        }

        void BackToTitle() => SceneManager.LoadScene(Title.TitleScreen.SceneName);

        // ---------- 배치 도우미 (지도 좌표는 왼쪽 아래 기준) ----------

        static void PlaceAt(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Ui.Center;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void PlaceLeft(RectTransform rt, Vector2 pos, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos + offset;
            rt.sizeDelta = size;
        }

        static void PlaceTopLeft(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = Ui.TopLeft;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
