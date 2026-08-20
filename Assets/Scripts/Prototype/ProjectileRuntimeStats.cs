using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [Serializable]
    public readonly struct ProjectileRuntimeStats
    {
        private const float MinimumPositiveValue = 0.0001f;

        public ProjectileRuntimeStats(
            string projectileType,
            string projectileTag,
            float damage,
            float flightDistance,
            float flightSpeed,
            int enemyPierceCount)
            : this(
                projectileType,
                projectileTag,
                null,
                Array.Empty<ProjectileTagDefinition>(),
                damage,
                flightDistance,
                flightSpeed,
                enemyPierceCount)
        {
        }

        public ProjectileRuntimeStats(
            string projectileType,
            string projectileTag,
            ProjectileTypeDefinition projectileTypeDefinition,
            IReadOnlyList<ProjectileTagDefinition> projectileTags,
            float damage,
            float flightDistance,
            float flightSpeed,
            int enemyPierceCount)
        {
            ProjectileType = string.IsNullOrWhiteSpace(projectileType) ? "Default" : projectileType;
            ProjectileTag = string.IsNullOrWhiteSpace(projectileTag) ? "Default" : projectileTag;
            ProjectileTypeDefinition = projectileTypeDefinition;
            Tags = CopyTags(projectileTags);
            Damage = damage;
            FlightDistance = Mathf.Max(MinimumPositiveValue, flightDistance);
            FlightSpeed = Mathf.Max(MinimumPositiveValue, flightSpeed);
            EnemyPierceCount = Mathf.Max(0, enemyPierceCount);
        }

        public string ProjectileType { get; }
        public string ProjectileTag { get; }
        public ProjectileTypeDefinition ProjectileTypeDefinition { get; }
        public IReadOnlyList<ProjectileTagDefinition> Tags { get; }
        public float Damage { get; }
        public float FlightDistance { get; }
        public float FlightSpeed { get; }
        public int EnemyPierceCount { get; }

        public bool HasTag(ProjectileTagDefinition tag)
        {
            if (tag == null || Tags == null)
            {
                return false;
            }

            for (int i = 0; i < Tags.Count; i++)
            {
                if (Tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }

        private static ProjectileTagDefinition[] CopyTags(
            IReadOnlyList<ProjectileTagDefinition> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<ProjectileTagDefinition>();
            }

            ProjectileTagDefinition[] copy = new ProjectileTagDefinition[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }
    }
}
