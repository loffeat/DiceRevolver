using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Bullet Events/Explosion On Hit")]
    public sealed class ExplosionOnHitEffect : BulletEventEffect
    {
        [SerializeField] private Projectile explosionProjectilePrefab;

        public Projectile ExplosionProjectilePrefab => explosionProjectilePrefab;

        public override void Trigger(BulletEventContext context)
        {
            if (explosionProjectilePrefab == null)
            {
                Debug.LogWarning($"{nameof(ExplosionOnHitEffect)} skipped because no explosion projectile prefab is assigned.", this);
                return;
            }

            Object.Instantiate(explosionProjectilePrefab, context.HitPosition, Quaternion.identity);
        }
    }
}
