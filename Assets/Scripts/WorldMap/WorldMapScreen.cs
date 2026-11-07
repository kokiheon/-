using System.Collections.Generic;
using MultiDungeon.Localization;
using MultiDungeon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiDungeon.WorldMap
{
    /// <summary>
    /// 원정 선택 지도. 지도, 지역 아이콘, 패널, 버튼은 에디터에서 BuildLayout()으로 씬에 배치된다.
    /// 원정 표식과 패널의 원정 목록은 원정이 끝날 때마다 바뀌므로 실행 중에 채운다.
    /// </summary>
    public class WorldMapScreen : MonoBehaviour
    {
        public const string SceneName = "WorldMap";
        static readonly Vector2 MapSize = new Vector2(1280, 880);
        static readonly Color Dark = new Color(0.08f, 0.07f, 0.06f);

        [SerializeField] RectTransform map;
        [SerializeField] RectTransform panelContent;
        [SerializeField] Text status;
        [SerializeField] Button departButton;
        [SerializeField] List<RegionNode> nodes = new List<RegionNode>();

        Region selectedRegion;
        Expedition selectedExpedition;

        void Awake()
        {
            Ui.EnsureEventSystem();
            ExpeditionBoard.EnsureGenerated();
            if (map == null) BuildLayout(); // 씬에 배치된 UI가 없을 때만 만든다
            SelectRegion(ExpeditionBoard.Regions[0]);
        }

        void OnEnable() => Loc.Changed += Refresh;
        void OnDisable() => Loc.Changed -= Refresh;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) BackToTitle();
        }

        // ---------- 배치 (에디터에서 씬을 만들 때 호출) ----------

        public void BuildLayout()
        {
            nodes.Clear();
            var root = Ui.CreateCanvas(transform, "WorldMapCanvas");
            Ui.Stretch(Ui.Image(root, "Background", Ui.Bg).rectTransform);

            map = Ui.Rect(root, "Map");
            Ui.Place(map, Ui.BottomLeft, new Vector2(600, 150), MapSize);
            Band("map.layer.surface", 770, 110, 0.16f);
            Band("map.layer.upper", 610, 160, 0.13f);
            Band("map.layer.middle", 440, 170, 0.11f);
            Band("map.layer.lower", 290, 150, 0.09f);
            Band("map.layer.deep", 160, 130, 0.07f);
            Band("map.layer.abyss", 0, 160, 0.05f);

            var village = Ui.Image(map, "Village", Ui.Grey);
            village.sprite = Ui.Circle;
            PlaceAt(village.rectTransform, new Vector2(640, 825), new Vector2(70, 70));
            var villageName = Ui.LText(map, "VillageName", "map.village", 26, Ui.Gold, FontStyle.Bold, TextAnchor.MiddleLeft);
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
            var partyLabel = Ui.LText(bar.transform, "PartyLabel", "map.party", 30, Ui.Gold, FontStyle.Bold, TextAnchor.MiddleLeft);
            Ui.Place(partyLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(140, 60));
            for (int i = 0; i < 4; i++)
            {
                var slot = Ui.Image(bar.transform, "Slot" + i, new Color(0.2f, 0.17f, 0.15f));
                Ui.Place(slot.rectTransform, new Vector2(0f, 0.5f), new Vector2(180 + i * 84, 0), new Vector2(70, 70));
                Ui.Stretch(Ui.LText(slot.transform, "Label", "map.empty_slot", 18, Ui.Grey).rectTransform);
            }

            Ui.Button(bar.transform, "map.depart", 32, Depart, out departButton);
            Ui.Place((RectTransform)departButton.transform, new Vector2(1f, 0.5f), new Vector2(-30, 0), new Vector2(260, 70));
            Ui.Button(bar.transform, "map.complete_test", 26, CompleteTest, out var completeButton);
            Ui.Place((RectTransform)completeButton.transform, new Vector2(1f, 0.5f), new Vector2(-310, 0), new Vector2(300, 70));
            Ui.Button(bar.transform, "map.back_title", 26, BackToTitle, out var backButton);
            Ui.Place((RectTransform)backButton.transform, new Vector2(1f, 0.5f), new Vector2(-630, 0), new Vector2(200, 70));
        }

        void Band(string key, float y, float height, float shade)
        {
            var band = Ui.Image(map, "Band_" + key, new Color(shade, shade * 0.9f, shade * 0.85f));
            Ui.Place(band.rectTransform, Ui.BottomLeft, new Vector2(0, y), new Vector2(MapSize.x, height));
            var text = Ui.LText(band.transform, "Label", key, 22, Ui.Grey, FontStyle.Normal, TextAnchor.UpperLeft);
            Ui.Place(text.rectTransform, Ui.TopLeft, new Vector2(16, -10), new Vector2(200, 30));
        }

        void CreateNode(Region region)
        {
            var ring = Ui.Image(map, "Ring_" + region.Id, Ui.Accent);
            ring.sprite = Ui.Circle;
            PlaceAt(ring.rectTransform, region.MapPos, new Vector2(116, 116));
            ring.enabled = false;

            var icon = Ui.Image(map, "Region_" + region.Id, region.Color);
            icon.sprite = Ui.Circle;
            PlaceAt(icon.rectTransform, region.MapPos, new Vector2(90, 90));
            var node = icon.gameObject.AddComponent<RegionNode>();
            node.RegionId = region.Id;
            node.Ring = ring;
            Ui.Bind(icon.gameObject.AddComponent<Button>(), node.OnClick);

            var name = Ui.LText(map, "Name_" + region.Id, $"region.{region.Id}.name", 28, Ui.Gold, FontStyle.Bold, TextAnchor.MiddleLeft);
            PlaceLeft(name.rectTransform, region.MapPos, new Vector2(320, 36), new Vector2(60, 20));

            var row = Ui.Rect(map, "Badges_" + region.Id);
            PlaceLeft(row, region.MapPos, new Vector2(200, 44), new Vector2(60, -24));
            node.Badges = row;
            nodes.Add(node);
        }

        // ---------- 실행 중에 채우는 부분 ----------

        void RebuildBadges(RegionNode node)
        {
            var row = node.Badges;
            for (int i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);

            var list = ExpeditionBoard.For(node.Region);
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
                var len = Ui.Text(badge.transform, "Length", Expedition.LengthBadge(exp.Length), 22, Dark, FontStyle.Bold);
                Ui.Stretch(len.rectTransform);
            }
        }

        void RefreshPanel()
        {
            for (int i = panelContent.childCount - 1; i >= 0; i--) Destroy(panelContent.GetChild(i).gameObject);
            var region = selectedRegion;
            var list = ExpeditionBoard.For(region);

            AddText(region.Name, 44, Ui.Gold, FontStyle.Bold, -30, 56);
            AddText(Loc.Format("map.region_info", region.Layer, region.Enemies), 24, Ui.Grey, FontStyle.Normal, -90, 34);
            AddText(Loc.Format("map.available", list.Count), 26, Ui.Gold, FontStyle.Bold, -150, 36);

            for (int i = 0; i < list.Count; i++)
            {
                var exp = list[i];
                Ui.Button(panelContent, "", 20, () => SelectExpedition(exp), out var row);
                row.GetComponent<Image>().color = exp == selectedExpedition ? new Color(0.35f, 0.25f, 0.12f, 0.95f) : Ui.ButtonBg;
                PlaceTopLeft((RectTransform)row.transform, new Vector2(30, -200 - i * 110), new Vector2(460, 96));

                var swatch = Ui.Image(row.transform, "Tier", Expedition.TierColor(exp.Tier));
                swatch.raycastTarget = false;
                PlaceTopLeft(swatch.rectTransform, new Vector2(20, -20), new Vector2(56, 56));
                var title = Ui.Text(row.transform, "Title", exp.Title, 30, Ui.Gold, FontStyle.Bold, TextAnchor.MiddleLeft);
                PlaceTopLeft(title.rectTransform, new Vector2(96, -12), new Vector2(340, 38));
                var goal = Ui.Text(row.transform, "Goal", Loc.Format("map.goal", exp.Goal), 22, Ui.Grey, FontStyle.Normal, TextAnchor.MiddleLeft);
                PlaceTopLeft(goal.rectTransform, new Vector2(96, -54), new Vector2(340, 30));
            }

            if (selectedExpedition != null)
            {
                int tier = (int)selectedExpedition.Tier, length = (int)selectedExpedition.Length;
                AddText(Loc.Get("map.reward_header"), 26, Ui.Gold, FontStyle.Bold, -540, 36);
                AddText(Loc.Format("map.reward", 500 * (tier + 1) * (length + 1), 10 + tier * 20), 24, Ui.Grey, FontStyle.Normal, -580, 34);
            }
            AddText(Loc.Format("map.completed", ExpeditionBoard.CompletedCount), 22, Ui.Grey, FontStyle.Normal, -640, 30);
        }

        void AddText(string value, int size, Color color, FontStyle style, float y, float height)
        {
            var t = Ui.Text(panelContent, "Text", value, size, color, style, TextAnchor.MiddleLeft);
            PlaceTopLeft(t.rectTransform, new Vector2(30, y), new Vector2(460, height));
        }

        // ---------- 선택과 버튼 (인스펙터 OnClick에 연결됨) ----------

        public void SelectRegion(Region region)
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
            if (selectedRegion == null) return;
            foreach (var node in nodes)
            {
                node.Ring.enabled = node.RegionId == selectedRegion.Id;
                RebuildBadges(node);
            }
            RefreshPanel();
            departButton.interactable = selectedExpedition != null;
        }

        public void Depart()
        {
            if (selectedExpedition == null) return;
            ExpeditionBoard.Active = selectedExpedition;
            SceneManager.LoadScene(Dungeon.DungeonGame.SceneName);
        }

        public void CompleteTest()
        {
            ExpeditionBoard.CompleteExpedition();
            SelectRegion(selectedRegion ?? ExpeditionBoard.Regions[0]);
            status.text = Loc.Get("map.regenerated");
        }

        public void BackToTitle() => SceneManager.LoadScene(Title.TitleScreen.SceneName);

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
