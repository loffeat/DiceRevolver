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

        [Header("槽位事件")]
        [SerializeField, InspectorName("槽位类型")] private DiceFaceSlotType slotType;
        [SerializeField, InspectorName("事件规则")] private EventRuleDefinition rule;
        [SerializeField, InspectorName("事件效果")] private BulletEventEffect effect;
        [SerializeField, InspectorName("被动效果")] private PassiveEventEffect passiveEffect;
        [SerializeField, InspectorName("被动型基础")] private bool isPassiveBase;

        [SerializeField, HideInInspector] private BulletEventEffect[] onFireEffects = System.Array.Empty<BulletEventEffect>();
        [SerializeField, HideInInspector] private BulletEventEffect[] onHitEffects = System.Array.Empty<BulletEventEffect>();
        [SerializeField, HideInInspector] private BulletEventEffect[] onFireEndEffects = System.Array.Empty<BulletEventEffect>();

        // 事件内容（名称/描述/颜色）以绑定的规则为准；无规则（legacy 效果词条）回退到词条自身字段。
        // 这样事件规则编辑器的修改会直接反映到构筑页。
        public string DisplayName => rule != null ? rule.DisplayName : displayName;
        public string Description => rule != null ? rule.Description : description;
        public Color DisplayColor => rule != null ? rule.DisplayColor : displayColor;
        public bool IsPassiveBase => isPassiveBase;
        public DiceFaceSlotType SlotType =>
            rule != null || effect != null || passiveEffect != null ? slotType : ResolveLegacySlotType();
        public EventRuleDefinition Rule => rule;
        public BulletEventEffect Effect => rule != null
            ? null
            : effect != null ? effect : ResolveLegacyEffect();
        public PassiveEventEffect PassiveEffect => rule == null ? passiveEffect : null;

        private DiceFaceSlotType ResolveLegacySlotType()
        {
            if (FirstEffect(onFireEffects) != null)
            {
                return DiceFaceSlotType.OnFire;
            }

            if (FirstEffect(onHitEffects) != null)
            {
                return DiceFaceSlotType.OnHit;
            }

            if (FirstEffect(onFireEndEffects) != null)
            {
                return DiceFaceSlotType.OnFireEnd;
            }

            return slotType;
        }

        private BulletEventEffect ResolveLegacyEffect()
        {
            return FirstEffect(onFireEffects) ?? FirstEffect(onHitEffects) ?? FirstEffect(onFireEndEffects);
        }

        private static BulletEventEffect FirstEffect(BulletEventEffect[] effects)
        {
            if (effects == null)
            {
                return null;
            }

            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != null)
                {
                    return effects[i];
                }
            }

            return null;
        }
    }
}
