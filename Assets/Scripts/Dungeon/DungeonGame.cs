using System.Collections.Generic;
using MultiDungeon.Localization;
using MultiDungeon.UI;
using MultiDungeon.WorldMap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiDungeon.Dungeon
{
    /// <summary>
    /// 던전 탐험 화면. 위쪽은 파티가 방과 복도를 옆으로 걷는 무대, 아래쪽 왼쪽은 인벤토리,
    /// 가운데는 선택한 영웅 정보, 오른쪽은 방과 복도로 된 미니맵.
    /// 고정 UI는 에디터에서 BuildLayout()으로 씬에 배치되고, 미니맵·영웅·기둥은 실행 중에 그린다.
    /// </summary>
    public class DungeonGame : MonoBehaviour
    {
        public const string SceneName = "Dungeon";
        const float StageHeight = 700f;
        const float UnitPx = 260f;        // 복도 한 칸의 화면 거리
        const float MoveSpeed = 2.2f;     // 초당 칸
        const float PillarGap = 320f;
        const int InventorySlots = 16;

        static readonly Color WallColor = new Color(0.12f, 0.1f, 0.1f);
        static readonly Color FloorColor = new Color(0.2f, 0.16f, 0.13f);
        static readonly Color PillarColor = new Color(0.22f, 0.18f, 0.15f);
        static readonly Color DoorColor = new Color(0.3f, 0.22f, 0.14f);
        static readonly Color MiniRoom = new Color(0.55f, 0.47f, 0.38f);
        static readonly Color MiniDim = new Color(0.3f, 0.27f, 0.24f);

        struct Hero { public string Key; public Color Color; }
        static readonly Hero[] Party =
        {
            new Hero { Key = "hero.plague", Color = new Color(0.45f, 0.6f, 0.4f) },
            new Hero { Key = "hero.bone", Color = new Color(0.6f, 0.55f, 0.75f) },
            new Hero { Key = "hero.lantern", Color = new Color(0.9f, 0.75f, 0.45f) },
            new Hero { Key = "hero.chain", Color = new Color(0.65f, 0.4f, 0.35f) },
        };

        [SerializeField] Image wall;
        [SerializeField] RectTransform pillars;
        [SerializeField] GameObject roomDecor;
        [SerializeField] RectTransform partyRoot;
        [SerializeField] Text headerText;
        [SerializeField] Text lightText;
        [SerializeField] Text locationText;
        [SerializeField] Text promptText;
        [SerializeField] Button finishButton;
        [SerializeField] Text heroName;
        [SerializeField] Text heroInfo;
        [SerializeField] RectTransform minimap;

        DungeonLayout layout;
        Room room;              // 방 안에 있을 때
        Corridor corridor;      // 복도에 있을 때
        Room from, to;
        float pos;              // 복도에서 from 쪽부터 걸은 칸 수
        float light = 100f;
        int selectedHero = 3;
        string minimapState;

        void Awake()
        {
            Ui.EnsureEventSystem();
            if (wall == null) BuildLayout(); // 씬에 배치된 UI가 없을 때만 만든다
            for (int i = 0; i < 8; i++)
                Ui.Image(pillars, "Pillar" + i, PillarColor);
            BuildParty();

            var exp = ExpeditionBoard.Active;
            int rooms = exp == null ? 8 : 6 + (int)exp.Length * 2;
            layout = DungeonLayout.Generate(rooms);
            headerText.text = exp != null
                ? Loc.Format("dungeon.header", exp.Region.Name, exp.Title, exp.Goal)
                : Loc.Get("dungeon.free");
            EnterRoom(layout.Start);
            SelectHero(selectedHero);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene(WorldMapScreen.SceneName);
                return;
            }
            if (corridor != null) WalkCorridor();
            lightText.text = Loc.Format("dungeon.light", Mathf.CeilToInt(light));
        }

        // ---------- 이동 ----------

        void EnterRoom(Room target)
        {
            room = target;
            corridor = null;
            room.Visited = true;
            roomDecor.SetActive(true);
            pillars.gameObject.SetActive(false);
            wall.color = WallColor;
            locationText.text = Loc.Format("dungeon.room.title", room.Id, Loc.Get(room.TypeKey));
            promptText.text = Loc.Get("dungeon.prompt.room");
            finishButton.gameObject.SetActive(room == layout.Exit);
            DrawMinimap();
        }

        void StartCorridor(Room target)
        {
            corridor = room.LinkTo(target);
            from = room;
            to = target;
            room = null;
            pos = 0.01f;
            roomDecor.SetActive(false);
            pillars.gameObject.SetActive(true);
            wall.color = WallColor * 0.85f;
            finishButton.gameObject.SetActive(false);
            promptText.text = Loc.Get("dungeon.prompt.corridor");
            DrawMinimap();
        }

        void WalkCorridor()
        {
            float input = Input.GetAxisRaw("Horizontal");
            float before = pos;
            pos += input * MoveSpeed * Time.deltaTime;
            light = Mathf.Max(0f, light - Mathf.Abs(pos - before) * 1.5f);

            if (pos >= corridor.Length) { EnterRoom(to); return; }
            if (pos <= 0f) { EnterRoom(from); return; }

            float offset = (pos * UnitPx) % PillarGap;
            for (int i = 0; i < pillars.childCount; i++)
            {
                var rt = (RectTransform)pillars.GetChild(i);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(i * PillarGap - offset, 120f);
                rt.sizeDelta = new Vector2(60f, StageHeight - 160f);
            }
            locationText.text = Loc.Format("dungeon.corridor", Mathf.FloorToInt(pos) + 1, corridor.Length);
            DrawMinimap();
        }

        public void FinishExpedition()
        {
            ExpeditionBoard.CompleteExpedition();
            SceneManager.LoadScene(WorldMapScreen.SceneName);
        }

        // ---------- 미니맵 (방 = 네모, 복도 = 작은 칸) ----------

        static Vector2 MiniPos(Room r) => new Vector2(
            (r.Cell.x - (DungeonLayout.Cols - 1) / 2f) * 112f,
            (r.Cell.y - (DungeonLayout.Rows - 1) / 2f) * 92f);

        static bool Known(Room r) => r.Visited || r.Links.Exists(c => c.Other(r).Visited);

        void DrawMinimap()
        {
            string state = room != null ? "R" + room.Id : $"C{from.Id}-{to.Id}-{Mathf.FloorToInt(pos)}";
            if (state == minimapState) return;
            minimapState = state;
            for (int i = minimap.childCount - 1; i >= 0; i--) Destroy(minimap.GetChild(i).gameObject);

            foreach (var c in layout.Corridors)
            {
                if (!c.A.Visited && !c.B.Visited) continue;
                for (int s = 0; s < c.Length; s++)
                {
                    float t = (s + 1f) / (c.Length + 1f);
                    var seg = Ui.Image(minimap, "Seg", MiniDim);
                    Center(seg.rectTransform, Vector2.Lerp(MiniPos(c.A), MiniPos(c.B), t), new Vector2(12, 12));
                }
            }

            foreach (var r in layout.Rooms)
            {
                if (!Known(r)) continue;
                var img = Ui.Image(minimap, "Room" + r.Id, r.Visited ? MiniRoom : MiniDim);
                Center(img.rectTransform, MiniPos(r), new Vector2(40, 40));
                if (r == layout.Exit && r.Visited)
                    img.color = new Color(0.35f, 0.6f, 0.5f);
                if (room != null && room.LinkTo(r) != null)
                {
                    var target = r;
                    var outline = img.gameObject.AddComponent<Outline>();
                    outline.effectColor = Ui.Accent;
                    outline.effectDistance = new Vector2(2, 2);
                    img.gameObject.AddComponent<Button>().onClick.AddListener(() => StartCorridor(target));
                }
            }

            Vector2 here = room != null
                ? MiniPos(room)
                : Vector2.Lerp(MiniPos(from), MiniPos(to), (Mathf.Floor(pos) + 1f) / (corridor.Length + 1f));
            var marker = Ui.Image(minimap, "Here", Ui.Accent);
            marker.sprite = Ui.Circle;
            marker.raycastTarget = false;
            Center(marker.rectTransform, here, new Vector2(20, 20));
        }

        static void Center(RectTransform rt, Vector2 p, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = Ui.Center;
            rt.anchoredPosition = p;
            rt.sizeDelta = size;
        }

        // ---------- 파티 ----------

        void BuildParty()
        {
            for (int i = 0; i < Party.Length; i++)
            {
                int index = i;
                var body = Ui.Image(partyRoot, "Hero" + i, Party[i].Color);
                var rt = body.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(i * 170f, 0f);
                rt.sizeDelta = new Vector2(110f, 260f);
                body.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectHero(index));
                var head = Ui.Image(body.transform, "Head", Party[i].Color * 1.1f);
                head.sprite = Ui.Circle;
                head.raycastTarget = false;
                var hrt = head.rectTransform;
                hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 1f);
                hrt.anchoredPosition = new Vector2(0f, 80f);
                hrt.sizeDelta = new Vector2(80f, 80f);
                var label = Ui.Text(body.transform, "Rank", Loc.Format("hero.rank", Party.Length - i), 20, Ui.Bg, FontStyle.Bold);
                Ui.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(110f, 30f));
            }
        }

        void SelectHero(int index)
        {
            selectedHero = index;
            for (int i = 0; i < partyRoot.childCount; i++)
            {
                var outline = partyRoot.GetChild(i).GetComponent<Outline>();
                if (outline == null) outline = partyRoot.GetChild(i).gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(4, 4);
                outline.enabled = i == index;
            }
            heroName.text = Loc.Get(Party[index].Key) + "  ·  " + Loc.Format("hero.rank", Party.Length - index);
            heroInfo.text = Loc.Get("dungeon.hero_info");
        }

        // ---------- 배치 (에디터에서 씬을 만들 때 호출) ----------

        public void BuildLayout()
        {
            var root = Ui.CreateCanvas(transform, "DungeonCanvas");
            Ui.Stretch(Ui.Image(root, "Background", Ui.Bg).rectTransform);

            // 무대
            var stage = Ui.Rect(root, "Stage");
            Ui.Place(stage, Ui.TopLeft, Vector2.zero, new Vector2(1920, StageHeight));
            wall = Ui.Image(stage, "Wall", WallColor);
            Ui.Stretch(wall.rectTransform);
            var floor = Ui.Image(stage, "Floor", FloorColor);
            Ui.Place(floor.rectTransform, Ui.BottomLeft, Vector2.zero, new Vector2(1920, 120));
            pillars = Ui.Rect(stage, "Pillars");
            Ui.Stretch(pillars);

            roomDecor = Ui.Rect(stage, "RoomDecor").gameObject;
            Ui.Stretch((RectTransform)roomDecor.transform);
            var doorL = Ui.Image(roomDecor.transform, "DoorLeft", DoorColor);
            Ui.Place(doorL.rectTransform, Ui.BottomLeft, new Vector2(60, 120), new Vector2(160, 320));
            var doorR = Ui.Image(roomDecor.transform, "DoorRight", DoorColor);
            Ui.Place(doorR.rectTransform, Ui.BottomRight, new Vector2(-60, 120), new Vector2(160, 320));

            partyRoot = Ui.Rect(stage, "Party");
            Ui.Place(partyRoot, Ui.BottomLeft, new Vector2(330, 130), new Vector2(680, 360));

            headerText = Ui.Text(stage, "Header", "", 30, Ui.Gold, FontStyle.Bold, TextAnchor.UpperLeft);
            Ui.Place(headerText.rectTransform, Ui.TopLeft, new Vector2(40, -30), new Vector2(1200, 44));
            lightText = Ui.Text(stage, "Light", "", 30, Ui.Accent, FontStyle.Bold, TextAnchor.UpperRight);
            Ui.Place(lightText.rectTransform, new Vector2(1f, 1f), new Vector2(-40, -30), new Vector2(300, 44));
            var esc = Ui.LText(stage, "EscHint", "dungeon.esc", 22, Ui.Grey, FontStyle.Normal, TextAnchor.UpperRight);
            Ui.Place(esc.rectTransform, new Vector2(1f, 1f), new Vector2(-40, -80), new Vector2(400, 30));
            locationText = Ui.Text(stage, "Location", "", 40, Ui.Gold, FontStyle.Bold);
            Ui.Place(locationText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(900, 56));
            promptText = Ui.Text(stage, "Prompt", "", 26, Ui.Grey, FontStyle.Italic);
            Ui.Place(promptText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(1100, 40));

            Ui.Button(stage, "dungeon.finish", 32, FinishExpedition, out finishButton);
            Ui.Place((RectTransform)finishButton.transform, new Vector2(1f, 0f), new Vector2(-280, 200), new Vector2(300, 80));

            // 아래 패널: 인벤토리 / 영웅 / 미니맵
            var bottom = Ui.Image(root, "BottomPanel", Ui.PanelBg);
            Ui.Place(bottom.rectTransform, Ui.BottomLeft, Vector2.zero, new Vector2(1920, 1080 - StageHeight));

            var inv = Ui.Image(bottom.transform, "Inventory", new Color(0.06f, 0.05f, 0.05f));
            Ui.Place(inv.rectTransform, Ui.TopLeft, new Vector2(30, -20), new Vector2(620, 340));
            var invTitle = Ui.LText(inv.transform, "Title", "dungeon.inventory", 26, Ui.Gold, FontStyle.Bold, TextAnchor.UpperLeft);
            Ui.Place(invTitle.rectTransform, Ui.TopLeft, new Vector2(20, -14), new Vector2(300, 36));
            for (int i = 0; i < InventorySlots; i++)
            {
                var slot = Ui.Image(inv.transform, "Slot" + i, new Color(0.16f, 0.13f, 0.11f));
                Ui.Place(slot.rectTransform, Ui.TopLeft, new Vector2(20 + (i % 8) * 74, -70 - (i / 8) * 130), new Vector2(66, 110));
            }

            var heroPanel = Ui.Image(bottom.transform, "HeroPanel", new Color(0.06f, 0.05f, 0.05f));
            Ui.Place(heroPanel.rectTransform, Ui.TopLeft, new Vector2(670, -20), new Vector2(580, 340));
            heroName = Ui.Text(heroPanel.transform, "HeroName", "", 34, Ui.Gold, FontStyle.Bold, TextAnchor.UpperLeft);
            Ui.Place(heroName.rectTransform, Ui.TopLeft, new Vector2(24, -20), new Vector2(530, 48));
            heroInfo = Ui.Text(heroPanel.transform, "HeroInfo", "", 24, Ui.Grey, FontStyle.Normal, TextAnchor.UpperLeft);
            Ui.Place(heroInfo.rectTransform, Ui.TopLeft, new Vector2(24, -80), new Vector2(530, 200));

            var mapPanel = Ui.Image(bottom.transform, "Minimap", new Color(0.06f, 0.05f, 0.05f));
            Ui.Place(mapPanel.rectTransform, new Vector2(1f, 1f), new Vector2(-30, -20), new Vector2(620, 340));
            var mapTitle = Ui.LText(mapPanel.transform, "Title", "dungeon.minimap", 26, Ui.Gold, FontStyle.Bold, TextAnchor.UpperLeft);
            Ui.Place(mapTitle.rectTransform, Ui.TopLeft, new Vector2(20, -14), new Vector2(300, 36));
            minimap = Ui.Rect(mapPanel.transform, "Rooms");
            Ui.Place(minimap, Ui.Center, new Vector2(0, -18), new Vector2(580, 280));
        }
    }
}
