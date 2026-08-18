using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Bullet Events/Spawn Projectile")]
    public sealed class ProjectileSpawnEffect : BulletEventEffect
    {
        [SerializeField, InspectorName("弹丸定义")] private ProjectileDefinition projectileDefinition;
        [SerializeField, Min(0f), InspectorName("生成延迟（秒）")] private float delaySeconds;
        [SerializeField, InspectorName("攻击特效判定")]
        private AttackEffectOverride attackEffectOverride = AttackEffectOverride.UseProjectileDefault;
        [SerializeField, InspectorName("视为主弹")] private bool primaryProjectile = true;

        public ProjectileDefinition ProjectileDefinition => projectileDefinition;
        public float DelaySeconds => delaySeconds;
        public AttackEffectOverride AttackEffectOverride => attackEffectOverride;
        public bool PrimaryProjectile => primaryProjectile;

        public override void Trigger(BulletEventContext context)
        {
            if (projectileDefinition == null)
            {
                Debug.LogWarning($"{nameof(ProjectileSpawnEffect)} skipped because no projectile definition is assigned.", this);
                return;
            }

            context.Schedule(delaySeconds, delayedContext =>
            {
                delayedContext.RequestProjectile(
                    projectileDefinition,
                    attackEffectOverride,
                    primaryProjectile);
            });
        }
    }
}
