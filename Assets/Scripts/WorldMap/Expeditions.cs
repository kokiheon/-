using System.Collections.Generic;
using UnityEngine;

namespace MultiDungeon.WorldMap
{
    public enum Tier { Novice, Veteran, Champion }
    public enum Length { Short, Medium, Long }

    public class Region
    {
        public string Id;
        public string Name;
        public string Layer;
        public string Enemies;
        public Vector2 MapPos;   // 지도 영역(1280x900) 안의 좌표, 왼쪽 아래 기준
        public Color Color;
    }

    public class Expedition
    {
        public Region Region;
        public Tier Tier;
        public Length Length;
        public string Goal;

        public static string TierName(Tier t) => t == Tier.Novice ? "초급" : t == Tier.Veteran ? "베테랑" : "챔피언";
        public static string LengthName(Length l) => l == Length.Short ? "짧음" : l == Length.Medium ? "중간" : "김";
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
            new Region { Id = "roots", Name = "뿌리 묘지", Layer = "상층", Enemies = "언데드 · 무덤지기", MapPos = new Vector2(330, 690), Color = new Color(0.45f, 0.55f, 0.4f) },
            new Region { Id = "crystal", Name = "울림 수정 광맥", Layer = "상층", Enemies = "수정 골렘 · 광부의 유령", MapPos = new Vector2(930, 690), Color = new Color(0.45f, 0.6f, 0.8f) },
            new Region { Id = "capital", Name = "침수된 왕도", Layer = "중층", Enemies = "익사자 · 수중 괴물", MapPos = new Vector2(400, 520), Color = new Color(0.3f, 0.5f, 0.6f) },
            new Region { Id = "spore", Name = "포자 숲", Layer = "중층", Enemies = "버섯 인간 · 기생 생물", MapPos = new Vector2(950, 520), Color = new Color(0.6f, 0.5f, 0.7f) },
            new Region { Id = "forge", Name = "용암 대장간", Layer = "하부", Enemies = "대장장이 골렘 · 불꽃 정령", MapPos = new Vector2(650, 365), Color = new Color(0.8f, 0.4f, 0.25f) },
            new Region { Id = "crypt", Name = "왕가의 납골궁", Layer = "심층", Enemies = "저주받은 근위대 · 망령", MapPos = new Vector2(400, 220), Color = new Color(0.55f, 0.5f, 0.35f) },
            new Region { Id = "throne", Name = "심연의 옥좌", Layer = "최하층", Enemies = "심연의 권속", MapPos = new Vector2(880, 110), Color = new Color(0.5f, 0.25f, 0.4f) },
        };

        static readonly string[] ShortGoals = { "보스 처치", "유물 회수" };
        static readonly string[] MediumGoals = { "전투방 모두 정리", "생존자 구출", "보스 처치" };
        static readonly string[] LongGoals = { "탐험 90%", "전투방 모두 정리", "보스 2체 처치" };

        public static readonly List<Expedition> Current = new List<Expedition>();
        public static int CompletedCount { get; private set; }

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
                        Goal = goals[Random.Range(0, goals.Length)],
                    });
                }
            }
        }

        /// <summary>원정을 마치면 호출. 지도 전체의 원정 목록이 새로 뽑힌다.</summary>
        public static void CompleteExpedition()
        {
            CompletedCount++;
            Regenerate();
        }

        public static List<Expedition> For(Region region) => Current.FindAll(e => e.Region == region);
    }
}
