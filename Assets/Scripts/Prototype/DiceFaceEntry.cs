using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Dice Face Entry")]
    public sealed class DiceFaceEntry : ScriptableObject
    {
        [Header("显示")]
        [SerializeField, InspectorName("显示名称")] private string displayName = "New Dice Face";
        [SerializeField, InspectorName("描述")] private string description;
        [SerializeField, InspectorName("显示颜色")] private Color displayColor = Color.white;

        [Header("弹丸事件")]
        [SerializeField, InspectorName("开火时事件")] private BulletEventEffect[] onFireEffects = System.Array.Empty<BulletEventEffect>();
        [SerializeField, InspectorName("击中时事件")] private BulletEventEffect[] onHitEffects = System.Array.Empty<BulletEventEffect>();
        [SerializeField, InspectorName("结束开火时事件")] private BulletEventEffect[] onFireEndEffects = System.Array.Empty<BulletEventEffect>();

        public string DisplayName => displayName;
        public string Description => description;
        public Color DisplayColor => displayColor;
        public IReadOnlyList<BulletEventEffect> OnFireEffects => onFireEffects ?? System.Array.Empty<BulletEventEffect>();
        public IReadOnlyList<BulletEventEffect> OnHitEffects => onHitEffects ?? System.Array.Empty<BulletEventEffect>();
        public IReadOnlyList<BulletEventEffect> OnFireEndEffects => onFireEndEffects ?? System.Array.Empty<BulletEventEffect>();
    }
}
