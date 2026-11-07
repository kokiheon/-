using System.Collections.Generic;
using MultiDungeon.Localization;
using UnityEngine;

namespace MultiDungeon.WorldMap
{
    public enum Tier { Novice, Veteran, Champion }
    public enum Length { Short, Medium, Long }

    public class Region
    {
        public string Id;
        public string LayerKey;
        public string Name => Loc.Get($"region.{Id}.name");
        public string Enemies => Loc.Get($"region.{Id}.enemies");
        public string Layer => Loc.Get(LayerKey);
        public Vector2 MapPos;   // 지도 영역(1280x900) 안의 좌표, 왼쪽 아래 기준
        public Color Color;
    }

    public class Expedition
    {
        public Region Region;
        public Tier Tier;
        public Length Length;
        public string GoalKey;
        public string Goal => Loc.Get(GoalKey);

        public static string TierName(Tier t) => Loc.Get("tier." + (int)t);
        public static string LengthName(Length l) => Loc.Get("length." + (int)l);
        public static string LengthBadge(Length l) => Loc.Get("length.badge." + (int)l);
        public static Color TierColor(Tier t) => t == Tier.Novice ? new Color(0.45f, 0.7f, 0.4f)
            : t == Tier.Veteran ? new Color(0.9f, 0.65f, 0.25f) : new Color(0.85f, 0.3f, 0.28f);
        public string Title => $"{TierName(Tier)} · {LengthName(Length)}";
    }

    /// <summary>
    /// 지도에 걸린 원정 목록. 지역마다 1~3개, 원정이 끝날 때마다 새로 뽑는다.
    /// 지역은 모델링과 적 타입만 다르고, 난이도·보상은 원정 등급이 정한다.
    /// </summary>
    public static class ExpeditionBoard
    {
        public static readonly List<Region> Regions = new List<Region>
        {
            new Region { Id = "roots", LayerKey = "map.layer.upper", MapPos = new Vector2(330, 690), Color = new Color(0.45f, 0.55f, 0.4f) },
            new Region { Id = "crystal", LayerKey = "map.layer.upper", MapPos = new Vector2(930, 690), Color = new Color(0.45f, 0.6f, 0.8f) },
            new Region { Id = "capital", LayerKey = "map.layer.middle", MapPos = new Vector2(400, 520), Color = new Color(0.3f, 0.5f, 0.6f) },
            new Region { Id = "spore", LayerKey = "map.layer.middle", MapPos = new Vector2(950, 520), Color = new Color(0.6f, 0.5f, 0.7f) },
            new Region { Id = "forge", LayerKey = "map.layer.lower", MapPos = new Vector2(650, 365), Color = new Color(0.8f, 0.4f, 0.25f) },
            new Region { Id = "crypt", LayerKey = "map.layer.deep", MapPos = new Vector2(400, 220), Color = new Color(0.55f, 0.5f, 0.35f) },
            new Region { Id = "throne", LayerKey = "map.layer.abyss", MapPos = new Vector2(880, 110), Color = new Color(0.5f, 0.25f, 0.4f) },
        };

        static readonly string[] ShortGoals = { "goal.boss", "goal.relic" };
        static readonly string[] MediumGoals = { "goal.clear", "goal.rescue", "goal.boss" };
        static readonly string[] LongGoals = { "goal.explore", "goal.clear", "goal.boss2" };

        public static readonly List<Expedition> Current = new List<Expedition>();
        public static int CompletedCount { get; private set; }

        /// <summary>지도에서 '원정 출발'로 고른, 지금 진행 중인 원정. 없으면 null.</summary>
        public static Expedition Active;

        public static void EnsureGenerated()
        {
            if (Current.Count == 0) Regenerate();
        }

        public static void Regenerate()
        {
            Current.Clear();
            foreach (var region in Regions)
            {
                int count = Random.Range(1, 4);
                for (int i = 0; i < count; i++)
                {
                    var length = (Length)Random.Range(0, 3);
                    var goals = length == Length.Short ? ShortGoals : length == Length.Medium ? MediumGoals : LongGoals;
                    Current.Add(new Expedition
                    {
                        Region = region,
                        Tier = (Tier)Random.Range(0, 3),
                        Length = length,
                        GoalKey = goals[Random.Range(0, goals.Length)],
                    });
                }
            }
        }

        /// <summary>원정을 마치면 호출. 지도 전체의 원정 목록이 새로 뽑힌다.</summary>
        public static void CompleteExpedition()
        {
            CompletedCount++;
            Active = null;
            Regenerate();
        }

        public static List<Expedition> For(Region region) => Current.FindAll(e => e.Region == region);
    }
}
