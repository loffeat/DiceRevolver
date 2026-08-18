using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceRevolverShotContext
    {
        public DiceRevolverShotContext(
            int face,
            Vector3 origin,
            Vector3 direction,
            Projectile projectile)
            : this(face, origin, direction, projectile, null, default, null)
        {
        }

        public DiceRevolverShotContext(
            int face,
            Vector3 origin,
            Vector3 direction,
            Projectile projectile,
            DiceFaceEntry diceFace)
            : this(face, origin, direction, projectile, diceFace, default, null)
        {
        }

        public DiceRevolverShotContext(
            int face,
            Vector3 origin,
            Vector3 direction,
            Projectile projectile,
            DiceFaceEntry diceFace,
            ProjectileRuntimeStats stats,
            Projectile projectilePrefab)
            : this(
                face,
                origin,
                direction,
                projectile,
                DiceFaceConfigurationSnapshot.FromEntry(diceFace),
                stats,
                projectilePrefab,
                null,
                null,
                false)
        {
        }

        public DiceRevolverShotContext(
            int face,
            Vector3 origin,
            Vector3 direction,
            Projectile projectile,
            DiceFaceConfigurationSnapshot configuration,
            ProjectileRuntimeStats stats,
            Projectile projectilePrefab,
            ProjectileDefinition projectileDefinition,
            DiceFaceActivation activation,
            bool canTriggerHitEffects)
        {
            Face = face;
            Origin = origin;
            Direction = direction;
            Projectile = projectile;
            Configuration = configuration;
            Stats = stats;
            ProjectilePrefab = projectilePrefab;
            ProjectileDefinition = projectileDefinition;
            Activation = activation;
            CanTriggerHitEffects = canTriggerHitEffects;
        }

        public int Face { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public Projectile Projectile { get; }
        public DiceFaceConfigurationSnapshot Configuration { get; }
        public DiceFaceEntry Entry => Configuration.FirstEntry;
        public DiceFaceEntry DiceFace => Entry;
        public ProjectileRuntimeStats Stats { get; }
        public Projectile ProjectilePrefab { get; }
        public ProjectileDefinition ProjectileDefinition { get; }
        public DiceFaceActivation Activation { get; }
        public bool CanTriggerHitEffects { get; }
    }
}
