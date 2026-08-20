using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Debug/Combat Debug Settings")]
    public sealed class CombatDebugSettings : ScriptableObject
    {
        [SerializeField, InspectorName("启用战斗事件 Debug")] private bool debugEnabled = true;
        [SerializeField, Min(1), InspectorName("最大显示行数")] private int maximumLines = 14;
        [SerializeField, Min(0f), InspectorName("文本停留时间（秒）")] private float lineLifetime = 10f;
        [SerializeField, Min(8), InspectorName("字体大小")] private int fontSize = 16;
        [SerializeField, Min(100f), InspectorName("面板宽度")] private float panelWidth = 620f;
        [SerializeField, Min(100f), InspectorName("面板高度")] private float panelHeight = 420f;

        public bool DebugEnabled => debugEnabled;
        public int MaximumLines => maximumLines;
        public float LineLifetime => lineLifetime;
        public int FontSize => fontSize;
        public float PanelWidth => panelWidth;
        public float PanelHeight => panelHeight;
    }
}
