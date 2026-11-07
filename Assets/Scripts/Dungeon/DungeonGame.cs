using MultiDungeon.Localization;
using MultiDungeon.WorldMap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiDungeon.Dungeon
{
    /// <summary>
    /// 개인 플레이용 던전 맵. 씬에 빈 오브젝트로 두면 실행 시 맵, 플레이어, HUD를 코드로 만든다.
    /// '#' 벽, '.' 바닥, 'P' 시작 위치, 'E' 출구.
    /// </summary>
    public class DungeonGame : MonoBehaviour
    {
        public const string SceneName = "Dungeon";
        const float ReturnDelay = 1.5f;

        static readonly string[] Layout =
        {
            "########################################",
            "#......#...............#...............#",
            "#..P...#...............#...............#",
            "#......#.......####....#....#######....#",
            "#..............#..#.........#.....#....#",
            "#......#.......#..#....#....#..E..#....#",
            "###.####.......####....#....#.....#....#",
            "#......#...............#....###.###....#",
            "#......#########.#######...............#",
            "#......#...............#####.###########",
            "#......#...............#...............#",
            "#..........#####.......#...............#",
            "#......#...#...#...........#####.......#",
            "#......#...#...#.......#...#...#.......#",
            "#......#...........##..#...#...#.......#",
            "########################################",
        };

        const float MoveSpeed = 5f;
        const float PlayerHalfSize = 0.35f;

        static readonly Color FloorColor = new Color(0.16f, 0.14f, 0.13f);
        static readonly Color WallColor = new Color(0.33f, 0.26f, 0.2f);
        static readonly Color ExitColor = new Color(0.25f, 0.55f, 0.45f);
        static readonly Color PlayerColor = new Color(0.86f, 0.74f, 0.55f);
        static readonly Color TextColor = new Color(0.86f, 0.74f, 0.55f);

        int width;
        int height;
        bool[,] walls;
        Vector2Int exitTile;
        Transform player;
        Camera cam;
        Text messageText;
        bool cleared;
        float returnTimer;

        void Awake()
        {
            BuildMap();
            SetupCamera();
            BuildHud();
        }

        void Update()
        {
            // ESC: 원정을 포기하고 지도로 돌아간다 (완료 처리 없음).
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene(WorldMapScreen.SceneName);
                return;
            }

            if (cleared)
            {
                returnTimer -= Time.deltaTime;
                if (returnTimer <= 0f)
                    SceneManager.LoadScene(WorldMapScreen.SceneName);
            }
            else
            {
                MovePlayer();
            }

            cam.transform.position = new Vector3(player.position.x, player.position.y, -10f);
        }

        // ---- 맵 ----

        void BuildMap()
        {
            height = Layout.Length;
            width = 0;
            foreach (var row in Layout)
                width = Mathf.Max(width, row.Length);

            walls = new bool[width, height];
            var sprite = CreateSquareSprite();
            var mapRoot = new GameObject("Map").transform;
            mapRoot.SetParent(transform, false);
            Vector2 start = Vector2.zero;

            for (int row = 0; row < height; row++)
            {
                int y = height - 1 - row;
                for (int x = 0; x < width; x++)
                {
                    char c = x < Layout[row].Length ? Layout[row][x] : '#';
                    walls[x, y] = c == '#';
                    var color = c == '#' ? WallColor : c == 'E' ? ExitColor : FloorColor;
                    CreateTile(mapRoot, sprite, x, y, color, 0);
                    if (c == 'P') start = new Vector2(x, y);
                    if (c == 'E') exitTile = new Vector2Int(x, y);
                }
            }

            var playerGo = new GameObject("Player", typeof(SpriteRenderer));
            playerGo.transform.SetParent(transform, false);
            playerGo.transform.position = start;
            playerGo.transform.localScale = Vector3.one * PlayerHalfSize * 2f;
            var sr = playerGo.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = PlayerColor;
            sr.sortingOrder = 10;
            player = playerGo.transform;
        }

        static void CreateTile(Transform parent, Sprite sprite, int x, int y, Color color, int order)
        {
            var go = new GameObject("Tile", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
        }

        static Sprite CreateSquareSprite()
        {
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        bool IsWall(int x, int y) => x < 0 || y < 0 || x >= width || y >= height || walls[x, y];

        bool Blocked(Vector2 p)
        {
            float h = PlayerHalfSize - 0.01f;
            return IsWall(Mathf.RoundToInt(p.x - h), Mathf.RoundToInt(p.y - h))
                || IsWall(Mathf.RoundToInt(p.x + h), Mathf.RoundToInt(p.y - h))
                || IsWall(Mathf.RoundToInt(p.x - h), Mathf.RoundToInt(p.y + h))
                || IsWall(Mathf.RoundToInt(p.x + h), Mathf.RoundToInt(p.y + h));
        }

        // ---- 플레이어 ----

        void MovePlayer()
        {
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f) input.Normalize();
            var delta = input * MoveSpeed * Time.deltaTime;
            Vector2 pos = player.position;

            // 축별로 따로 움직여 벽을 따라 미끄러지게 한다.
            var nx = new Vector2(pos.x + delta.x, pos.y);
            if (!Blocked(nx)) pos = nx;
            var ny = new Vector2(pos.x, pos.y + delta.y);
            if (!Blocked(ny)) pos = ny;
            player.position = pos;

            if (Mathf.RoundToInt(pos.x) == exitTile.x && Mathf.RoundToInt(pos.y) == exitTile.y)
            {
                cleared = true;
                returnTimer = ReturnDelay;
                ExpeditionBoard.CompleteExpedition();
                messageText.text = Loc.Get("dungeon.cleared");
            }
        }

        // ---- 카메라 / HUD ----

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
                cam = camGo.GetComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        void BuildHud()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var exp = ExpeditionBoard.Active;
            string header = exp != null
                ? Loc.Format("dungeon.header", exp.Region.Name, exp.Title, exp.Goal)
                : Loc.Get("dungeon.free");
            CreateText(canvasGo.transform, font, header, 40, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0, -30));
            CreateText(canvasGo.transform, font, Loc.Get("dungeon.help"),
                26, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(30, 30));
            messageText = CreateText(canvasGo.transform, font, "", 56, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0, 200));
        }

        static Text CreateText(Transform parent, Font font, string value, int size, TextAnchor align, Vector2 anchor, Vector2 pos)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1600, 80);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = value;
            t.fontSize = size;
            t.color = TextColor;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }
    }
}
