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
        [SerializeField, InspectorName("事件效果")] private BulletEventEffect effect;

        [SerializeField, HideInInspector] private BulletEventEffect[] onFireEffects = System.Array.Empty<BulletEventEffect>();
        [SerializeField, HideInInspector] private BulletEventEffect[] onHitEffects = System.Array.Empty<BulletEventEffect>();
        [SerializeField, HideInInspector] private BulletEventEffect[] onFireEndEffects = System.Array.Empty<BulletEventEffect>();

        public string DisplayName => displayName;
        public string Description => description;
        public Color DisplayColor => displayColor;
        public DiceFaceSlotType SlotType => effect != null ? slotType : ResolveLegacySlotType();
        public BulletEventEffect Effect => effect != null ? effect : ResolveLegacyEffect();

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
