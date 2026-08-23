using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Enemy Status")]
    public sealed class EnemyStatusDefinition : ScriptableObject
    {
        [SerializeField, InspectorName("状态 ID")] private string statusId;
        [SerializeField, InspectorName("显示名称")] private string displayName;
        [SerializeField, InspectorName("描述")] private string description;
        [SerializeField, Min(0f), InspectorName("持续时间（秒）")] private float durationSeconds = 3f;
        [SerializeField, Min(0f), InspectorName("每秒伤害")] private float damagePerSecond;
        [SerializeField, Min(1), InspectorName("最大叠层")] private int maxStacks = 1;
        [SerializeField, InspectorName("视觉提示色")] private Color visualColor = Color.red;

        public string StatusId => statusId;
        public string DisplayName => displayName;
        public string Description => description;
        public float DurationSeconds => durationSeconds;
        public float DamagePerSecond => damagePerSecond;
        public int MaxStacks => maxStacks;
        public Color VisualColor => visualColor;
    }
}
