using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Bullet Events/Extra Shot On Fire")]
    public sealed class ExtraShotOnFireEffect : BulletEventEffect
    {
        [SerializeField]
        [Min(0f)]
        [InspectorName("第二发延迟（秒）")]
        [Tooltip("第一发射出后，等待多少游戏时间再生成第二发。暂停游戏时计时也会暂停。")]
        private float delaySeconds = 0.25f;

        [SerializeField, InspectorName("攻击特效判定")]
        private AttackEffectOverride attackEffectOverride = AttackEffectOverride.ForceDisabled;

        public float DelaySeconds => delaySeconds;
        public AttackEffectOverride AttackEffectOverride => attackEffectOverride;

        public override void Trigger(BulletEventContext context)
        {
            if (context.Activation == null)
            {
                return;
            }

            context.Schedule(delaySeconds, delayedContext =>
            {
                ProjectileDefinition definition = delayedContext.Activation.PrimaryProjectileDefinition;
                if (definition != null)
                {
                    delayedContext.RequestProjectile(definition, attackEffectOverride, false);
                }
            });
        }
    }
}
