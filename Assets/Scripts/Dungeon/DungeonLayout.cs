using System.Collections.Generic;
using UnityEngine;

namespace MultiDungeon.Dungeon
{
    public class Room
    {
        public int Id;
        public Vector2Int Cell;      // 미니맵 격자 좌표
        public string TypeKey;       // dungeon.room.*
        public bool Visited;
        public readonly List<Corridor> Links = new List<Corridor>();

        public Corridor LinkTo(Room other) => Links.Find(c => c.Other(this) == other);
    }

    public class Corridor
    {
        public Room A;
        public Room B;
        public int Length = 4;       // 복도 칸 수
        public Room Other(Room r) => r == A ? B : A;
    }

    /// <summary>방과 복도로 이루어진 던전. 격자 위에 방을 퍼뜨리고 이웃한 방끼리 복도로 잇는다.</summary>
    public class DungeonLayout
    {
        public const int Cols = 5;
        public const int Rows = 3;

        public readonly List<Room> Rooms = new List<Room>();
        public readonly List<Corridor> Corridors = new List<Corridor>();
        public Room Start;
        public Room Exit;

        static readonly Vector2Int[] Dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        static readonly string[] MiddleTypes = { "dungeon.room.battle", "dungeon.room.battle", "dungeon.room.treasure", "dungeon.room.empty" };

        public static DungeonLayout Generate(int roomCount)
        {
            var layout = new DungeonLayout();
            var byCell = new Dictionary<Vector2Int, Room>();
            layout.Start = layout.AddRoom(new Vector2Int(0, Rows / 2), byCell);

            int guard = 0;
            while (layout.Rooms.Count < roomCount && guard++ < 1000)
            {
                var from = layout.Rooms[Random.Range(0, layout.Rooms.Count)];
                var cell = from.Cell + Dirs[Random.Range(0, Dirs.Length)];
                if (cell.x < 0 || cell.y < 0 || cell.x >= Cols || cell.y >= Rows || byCell.ContainsKey(cell)) continue;
                layout.Connect(from, layout.AddRoom(cell, byCell));
            }

            // 고리를 한두 개 더해 갈림길을 만든다.
            for (int extra = 0, tries = 0; extra < 2 && tries < 50; tries++)
            {
                var a = layout.Rooms[Random.Range(0, layout.Rooms.Count)];
                if (!byCell.TryGetValue(a.Cell + Dirs[Random.Range(0, Dirs.Length)], out var b) || a.LinkTo(b) != null) continue;
                layout.Connect(a, b);
                extra++;
            }

            layout.Exit = layout.Farthest(layout.Start);
            foreach (var room in layout.Rooms)
                room.TypeKey = room == layout.Start ? "dungeon.room.entrance"
                    : room == layout.Exit ? "dungeon.room.exit"
                    : MiddleTypes[Random.Range(0, MiddleTypes.Length)];
            return layout;
        }

        Room AddRoom(Vector2Int cell, Dictionary<Vector2Int, Room> byCell)
        {
            var room = new Room { Id = Rooms.Count + 1, Cell = cell };
            Rooms.Add(room);
            byCell[cell] = room;
            return room;
        }

        void Connect(Room a, Room b)
        {
            var c = new Corridor { A = a, B = b, Length = Random.Range(3, 6) };
            a.Links.Add(c);
            b.Links.Add(c);
            Corridors.Add(c);
        }

        Room Farthest(Room from)
        {
            var dist = new Dictionary<Room, int> { [from] = 0 };
            var queue = new Queue<Room>();
            queue.Enqueue(from);
            Room last = from;
            while (queue.Count > 0)
            {
                var r = queue.Dequeue();
                last = r;
                foreach (var c in r.Links)
                {
                    var o = c.Other(r);
                    if (dist.ContainsKey(o)) continue;
                    dist[o] = dist[r] + 1;
                    queue.Enqueue(o);
                }
            }
            return last;
        }
    }
}
