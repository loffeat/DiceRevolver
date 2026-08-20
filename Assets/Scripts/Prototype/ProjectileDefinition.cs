using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Projectiles/Projectile Definition")]
    public sealed class ProjectileDefinition : ScriptableObject
    {
        [Header("显示")]
        [SerializeField, InspectorName("显示名称")] private string displayName = "New Projectile";

        [Header("运行时")]
        [SerializeField, InspectorName("弹丸 Prefab")] private Projectile projectilePrefab;

        [Header("弹幕属性")]
        [SerializeField, InspectorName("弹幕类型")] private string projectileType = "Default";
        [SerializeField, InspectorName("弹幕标签")] private string projectileTag = "Default";
        [SerializeField, InspectorName("弹幕类型资源")] private ProjectileTypeDefinition projectileTypeDefinition;
        [SerializeField, InspectorName("弹幕标签资源")]
        private ProjectileTagDefinition[] projectileTags = Array.Empty<ProjectileTagDefinition>();
        [SerializeField, InspectorName("弹幕伤害")] private float damage = 1f;
        [SerializeField, InspectorName("飞行距离")] private float flightDistance = 18f;
        [SerializeField, InspectorName("飞行速度")] private float flightSpeed = 18f;
        [SerializeField, InspectorName("敌人穿透数量")] private int enemyPierceCount;
        [SerializeField, InspectorName("扩展端口")] private ProjectileExtensionPort[] extensionPorts = Array.Empty<ProjectileExtensionPort>();

        [Header("事件判定")]
        [SerializeField, InspectorName("默认视为攻击特效")] private bool defaultAttackEffect;

        public string DisplayName => displayName;
        public Projectile ProjectilePrefab => projectilePrefab;
        public bool DefaultAttackEffect => defaultAttackEffect;
        public ProjectileTypeDefinition ProjectileTypeDefinition => projectileTypeDefinition;
        public IReadOnlyList<ProjectileTagDefinition> ProjectileTags =>
            projectileTags ?? Array.Empty<ProjectileTagDefinition>();
        public IReadOnlyList<ProjectileExtensionPort> ExtensionPorts =>
            extensionPorts ?? Array.Empty<ProjectileExtensionPort>();

        public ProjectileRuntimeStats BuildRuntimeStats()
        {
            return new ProjectileRuntimeStats(
                projectileType,
                projectileTag,
                projectileTypeDefinition,
                projectileTags,
                damage,
                flightDistance,
                flightSpeed,
                enemyPierceCount);
        }
    }

    [Serializable]
    public struct ProjectileExtensionPort
    {
        [InspectorName("名称")]
        public string Name;

        [InspectorName("数值")]
        public float Value;
    }
}
