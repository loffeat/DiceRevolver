using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Bullet Events/Explosion On Hit")]
    public sealed class ExplosionOnHitEffect : BulletEventEffect
    {
        [SerializeField, InspectorName("爆炸弹丸定义")] private ProjectileDefinition explosionProjectileDefinition;

        public ProjectileDefinition ExplosionProjectileDefinition => explosionProjectileDefinition;

        public override void Trigger(BulletEventContext context)
        {
            if (explosionProjectileDefinition == null)
            {
                Debug.LogWarning($"{nameof(ExplosionOnHitEffect)} skipped because no explosion projectile definition is assigned.", this);
                return;
            }

            Vector3 direction = context.Shot != null
                ? context.Shot.Direction
                : context.Activation?.Direction ?? Vector3.forward;
            context.RequestProjectileAt(
                explosionProjectileDefinition,
                context.HitPosition,
                direction,
                AttackEffectOverride.UseProjectileDefault);
        }
    }
}
