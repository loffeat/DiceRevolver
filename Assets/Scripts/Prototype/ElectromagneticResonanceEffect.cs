using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(
        fileName = "ElectromagneticResonanceEffect",
        menuName = "Dice Revolver/Bullet Events/On Fire/Electromagnetic Resonance")]
    public sealed class ElectromagneticResonanceEffect : BulletEventEffect
    {
        [SerializeField, InspectorName("雷电标签")] private ProjectileTagDefinition lightningTag;
        [SerializeField, InspectorName("闪电链定义")] private LightningChainDefinition chainDefinition;
        [SerializeField, Min(0f), InspectorName("共鸣搜索半径")] private float searchRadius = 6f;
        [SerializeField, Min(1), InspectorName("最大连接数量")] private int maximumConnections = 3;

        public ProjectileTagDefinition LightningTag => lightningTag;
        public LightningChainDefinition ChainDefinition => chainDefinition;
        public float SearchRadius => searchRadius;
        public int MaximumConnections => maximumConnections;

        public override void Trigger(BulletEventContext context)
        {
            ProjectileHandle primary = context.PrimaryProjectile;
            OwnedProjectileRegistry registry = context.Activation?.OwnedProjectiles;
            if (!primary.IsAlive ||
                lightningTag == null ||
                chainDefinition == null ||
                !primary.Stats.HasTag(lightningTag) ||
                registry == null)
            {
                return;
            }

            List<ProjectileHandle> candidates = new List<ProjectileHandle>();
            registry.FindNearby(
                primary.Position,
                Mathf.Max(0f, searchRadius),
                lightningTag,
                primary.Projectile,
                candidates);
            IReadOnlyList<ProjectileHandle> selected = SelectTargets(
                candidates,
                Mathf.Max(1, maximumConnections),
                count => UnityEngine.Random.Range(0, count));
            if (selected.Count > 0)
            {
                context.RequestLightningChain(primary, selected, chainDefinition);
            }
        }

        public static IReadOnlyList<ProjectileHandle> SelectTargets(
            IReadOnlyList<ProjectileHandle> candidates,
            int maximumConnections,
            Func<int, int> randomIndex)
        {
            List<ProjectileHandle> pool = new List<ProjectileHandle>();
            if (candidates != null)
            {
                for (int index = 0; index < candidates.Count; index++)
                {
                    if (candidates[index].IsAlive)
                    {
                        pool.Add(candidates[index]);
                    }
                }
            }

            List<ProjectileHandle> selected = new List<ProjectileHandle>();
            int count = Mathf.Min(Mathf.Max(0, maximumConnections), pool.Count);
            for (int index = 0; index < count; index++)
            {
                int selectedIndex = randomIndex != null
                    ? Mathf.Clamp(randomIndex(pool.Count), 0, pool.Count - 1)
                    : 0;
                selected.Add(pool[selectedIndex]);
                pool.RemoveAt(selectedIndex);
            }

            return selected;
        }
    }
}
