using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(
        fileName = "TeslaPassiveEffect",
        menuName = "Dice Revolver/Bullet Events/Passive/Tesla")]
    public sealed class TeslaPassiveEffect : PassiveEventEffect
    {
        [SerializeField, InspectorName("雷电标签")] private ProjectileTagDefinition lightningTag;
        [SerializeField, Min(0f), InspectorName("每层伤害提升比例")]
        private float damagePerStack = 0.05f;

        public ProjectileTagDefinition LightningTag => lightningTag;
        public float DamagePerStack => damagePerStack;

        public override IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context)
        {
            return new TeslaRuntime(context, lightningTag, Mathf.Max(0f, damagePerStack));
        }

        private sealed class TeslaRuntime :
            IDicePassiveEffectRuntime,
            IDiceProjectileStatsModifier,
            IDiceProjectileSpawnObserver
        {
            private readonly PassiveBindingContext context;
            private readonly ProjectileTagDefinition lightningTag;
            private readonly float damagePerStack;
            private int stackCount;

            public TeslaRuntime(
                PassiveBindingContext context,
                ProjectileTagDefinition lightningTag,
                float damagePerStack)
            {
                this.context = context;
                this.lightningTag = lightningTag;
                this.damagePerStack = damagePerStack;
            }

            public ProjectileRuntimeStats ModifyProjectileStats(
                int sourceFace,
                ProjectileRuntimeStats stats)
            {
                ProjectileTypeDefinition baseType = context.BaseProjectileType;
                if (sourceFace != context.Face ||
                    baseType == null ||
                    stats.ProjectileTypeDefinition != baseType)
                {
                    return stats;
                }

                return stats.WithDamage(stats.Damage * (1f + stackCount * damagePerStack));
            }

            public bool OnProjectileSpawned(int sourceFace, ProjectileHandle projectile)
            {
                if (lightningTag != null && projectile.Stats.HasTag(lightningTag))
                {
                    stackCount++;
                    return true;
                }

                return false;
            }

            public bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces)
            {
                return true;
            }

            public void OnReloadStarted()
            {
                stackCount = 0;
            }

            public void OnReloadCompleted()
            {
            }

            public void OnFaceConsumed(int face)
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
