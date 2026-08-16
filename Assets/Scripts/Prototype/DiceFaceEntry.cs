using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Dice Face Entry")]
    public sealed class DiceFaceEntry : ScriptableObject
    {
        [SerializeField] private string displayName = "New Dice Face";
        [SerializeField] private string description;
        [SerializeField] private Color displayColor = Color.white;
        [SerializeField] private Projectile projectilePrefabOverride;
        [SerializeField] private string projectileType = "Default";
        [SerializeField] private string projectileTag = "Default";
        [SerializeField] private float damage = 1f;
        [SerializeField] private float flightDistance = 18f;
        [SerializeField] private float flightSpeed = 18f;
        [SerializeField] private int enemyPierceCount;
        [SerializeField] private DiceFaceExtensionPort[] extensionPorts = Array.Empty<DiceFaceExtensionPort>();
        [SerializeField] private BulletEventEffect[] onFireEffects = Array.Empty<BulletEventEffect>();
        [SerializeField] private BulletEventEffect[] onHitEffects = Array.Empty<BulletEventEffect>();
        [SerializeField] private BulletEventEffect[] onFireEndEffects = Array.Empty<BulletEventEffect>();

        public string DisplayName => displayName;
        public string Description => description;
        public Color DisplayColor => displayColor;
        public Projectile ProjectilePrefabOverride => projectilePrefabOverride;
        public string ProjectileType => projectileType;
        public string ProjectileTag => projectileTag;
        public float Damage => damage;
        public float FlightDistance => flightDistance;
        public float FlightSpeed => flightSpeed;
        public int EnemyPierceCount => enemyPierceCount;
        public IReadOnlyList<DiceFaceExtensionPort> ExtensionPorts => extensionPorts ?? Array.Empty<DiceFaceExtensionPort>();
        public IReadOnlyList<BulletEventEffect> OnFireEffects => onFireEffects ?? Array.Empty<BulletEventEffect>();
        public IReadOnlyList<BulletEventEffect> OnHitEffects => onHitEffects ?? Array.Empty<BulletEventEffect>();
        public IReadOnlyList<BulletEventEffect> OnFireEndEffects => onFireEndEffects ?? Array.Empty<BulletEventEffect>();
    }

    [Serializable]
    public struct DiceFaceExtensionPort
    {
        public string Name;
        public float Value;
    }
}
