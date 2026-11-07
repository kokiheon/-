using UnityEngine;
using UnityEngine.UI;

namespace MultiDungeon.WorldMap
{
    /// <summary>지도 위 지역 아이콘. 씬에 배치되며, 눌리면 지도 화면에 지역 선택을 알린다.</summary>
    public class RegionNode : MonoBehaviour
    {
        public string RegionId;
        public Image Ring;
        public RectTransform Badges;

        public Region Region => ExpeditionBoard.Regions.Find(r => r.Id == RegionId);

        public void OnClick()
        {
            var screen = FindAnyObjectByType<WorldMapScreen>();
            if (screen != null) screen.SelectRegion(Region);
        }
    }
}
