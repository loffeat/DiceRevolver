using System;
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
        {
            ProjectileType = string.IsNullOrWhiteSpace(projectileType) ? "Default" : projectileType;
            ProjectileTag = string.IsNullOrWhiteSpace(projectileTag) ? "Default" : projectileTag;
            Damage = damage;
            FlightDistance = Mathf.Max(MinimumPositiveValue, flightDistance);
            FlightSpeed = Mathf.Max(MinimumPositiveValue, flightSpeed);
            EnemyPierceCount = Mathf.Max(0, enemyPierceCount);
        }

        public string ProjectileType { get; }
        public string ProjectileTag { get; }
        public float Damage { get; }
        public float FlightDistance { get; }
        public float FlightSpeed { get; }
        public int EnemyPierceCount { get; }
    }
}
