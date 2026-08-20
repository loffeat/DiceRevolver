using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(
        fileName = "LightningChainDefinition",
        menuName = "Dice Revolver/Lightning/Chain Definition")]
    public sealed class LightningChainDefinition : ScriptableObject
    {
        [SerializeField, InspectorName("闪电链执行器 Prefab")]
        private LightningChainExecutor executorPrefab;
        [SerializeField, Min(0f), InspectorName("闪电链伤害")] private float damage = 1f;
        [SerializeField, Min(0.01f), InspectorName("闪电链宽度")] private float chainWidth = 0.25f;
        [SerializeField, Min(0.01f), InspectorName("视觉持续时间（秒）")]
        private float visualDuration = 0.2f;
        [SerializeField, InspectorName("受击图层")] private LayerMask targetLayers = ~0;
        [SerializeField, InspectorName("闪电颜色")]
        private Color chainColor = new Color(0.35f, 0.85f, 1f, 1f);

        public LightningChainExecutor ExecutorPrefab => executorPrefab;
        public float Damage => damage;
        public float ChainWidth => Mathf.Max(0.01f, chainWidth);
        public float VisualDuration => Mathf.Max(0.01f, visualDuration);
        public LayerMask TargetLayers => targetLayers;
        public Color ChainColor => chainColor;
    }
}
