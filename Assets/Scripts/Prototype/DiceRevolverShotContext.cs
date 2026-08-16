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
        {
            Face = face;
            Origin = origin;
            Direction = direction;
            Projectile = projectile;
            Entry = diceFace;
            Stats = stats;
            ProjectilePrefab = projectilePrefab;
        }

        public int Face { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public Projectile Projectile { get; }
        public DiceFaceEntry Entry { get; }
        public DiceFaceEntry DiceFace => Entry;
        public ProjectileRuntimeStats Stats { get; }
        public Projectile ProjectilePrefab { get; }
    }
}
