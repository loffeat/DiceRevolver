using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public readonly struct ProjectileHandle
    {
        public ProjectileHandle(Projectile projectile, ProjectileRuntimeStats stats)
        {
            Projectile = projectile;
            Stats = stats;
        }

        public Projectile Projectile { get; }
        public ProjectileRuntimeStats Stats { get; }
        public bool IsAlive => Projectile != null;
        public Vector3 Position => Projectile != null ? Projectile.transform.position : Vector3.zero;
    }

    public sealed class OwnedProjectileRegistry
    {
        private readonly List<ProjectileHandle> projectiles = new List<ProjectileHandle>();

        public int Count
        {
            get
            {
                RemoveDestroyedReferences();
                return projectiles.Count;
            }
        }

        public ProjectileHandle Register(Projectile projectile, ProjectileRuntimeStats stats)
        {
            if (projectile == null)
            {
                return default;
            }

            ProjectileHandle handle = new ProjectileHandle(projectile, stats);
            projectiles.Add(handle);
            return handle;
        }

        public void FindNearby(
            Vector3 center,
            float radius,
            ProjectileTagDefinition tag,
            Projectile exclude,
            List<ProjectileHandle> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            float clampedRadius = Mathf.Max(0f, radius);
            float radiusSquared = clampedRadius * clampedRadius;

            for (int index = projectiles.Count - 1; index >= 0; index--)
            {
                ProjectileHandle handle = projectiles[index];
                if (!handle.IsAlive)
                {
                    projectiles.RemoveAt(index);
                    continue;
                }

                if (handle.Projectile == exclude || !handle.Stats.HasTag(tag))
                {
                    continue;
                }

                if ((handle.Position - center).sqrMagnitude <= radiusSquared)
                {
                    results.Add(handle);
                }
            }

            results.Reverse();
        }

        private void RemoveDestroyedReferences()
        {
            for (int index = projectiles.Count - 1; index >= 0; index--)
            {
                if (!projectiles[index].IsAlive)
                {
                    projectiles.RemoveAt(index);
                }
            }
        }
    }
}
