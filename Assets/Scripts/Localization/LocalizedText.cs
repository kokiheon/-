using UnityEngine;
using UnityEngine.UI;

namespace MultiDungeon.Localization
{
    /// <summary>Text에 붙이면 Key에 해당하는 문장을 현재 언어로 보여준다.</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string Key;

        void OnEnable()
        {
            Apply();
            Loc.Changed += Apply;
        }

        void OnDisable() => Loc.Changed -= Apply;

        public void Apply()
        {
            if (!string.IsNullOrEmpty(Key)) GetComponent<Text>().text = Loc.Get(Key);
        }
    }
}
