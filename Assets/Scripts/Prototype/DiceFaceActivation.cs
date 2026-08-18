using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public readonly struct ProjectileSpawnRequest
    {
        public ProjectileSpawnRequest(
            ProjectileDefinition definition,
            Vector3 origin,
            Vector3 direction,
            bool isPrimary,
            bool canTriggerHitEffects)
        {
            Definition = definition;
            Origin = origin;
            Direction = direction;
            IsPrimary = isPrimary;
            CanTriggerHitEffects = canTriggerHitEffects;
        }

        public ProjectileDefinition Definition { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public bool IsPrimary { get; }
        public bool CanTriggerHitEffects { get; }
    }

    public sealed class DiceFaceActivation
    {
        public const int DefaultEventBudget = 32;

        private readonly Action<float, Action> scheduleAction;
        private readonly Action<ProjectileSpawnRequest> spawnAction;
        private int remainingEventBudget;
        private bool budgetWarningIssued;

        public DiceFaceActivation(
            int face,
            DiceFaceConfigurationSnapshot configuration,
            Vector3 origin,
            Vector3 direction,
            DiceRevolverGun gun,
            DiceChamber chamber,
            Action<float, Action> scheduleAction,
            Action<ProjectileSpawnRequest> spawnAction,
            int eventBudget = DefaultEventBudget)
        {
            Face = face;
            Configuration = configuration;
            Origin = origin;
            Direction = NormalizeDirection(direction);
            Gun = gun;
            Chamber = chamber;
            this.scheduleAction = scheduleAction;
            this.spawnAction = spawnAction;
            remainingEventBudget = Mathf.Max(0, eventBudget);
        }

        public int Face { get; }
        public DiceFaceConfigurationSnapshot Configuration { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public DiceRevolverGun Gun { get; }
        public DiceChamber Chamber { get; }
        public ProjectileDefinition PrimaryProjectileDefinition { get; private set; }
        public int RemainingEventBudget => remainingEventBudget;

        public bool TryConsumeEventBudget()
        {
            if (remainingEventBudget > 0)
            {
                remainingEventBudget--;
                return true;
            }

            if (!budgetWarningIssued)
            {
                budgetWarningIssued = true;
                Debug.LogWarning($"Dice face {Face} stopped because its event budget was exhausted.");
            }

            return false;
        }

        public bool Schedule(float delaySeconds, Action callback)
        {
            if (callback == null || scheduleAction == null)
            {
                return false;
            }

            scheduleAction.Invoke(Mathf.Max(0f, delaySeconds), callback);
            return true;
        }

        public bool RequestProjectile(
            ProjectileDefinition definition,
            AttackEffectOverride attackEffectOverride,
            bool isPrimary,
            Vector3 origin,
            Vector3 direction)
        {
            if (definition == null || spawnAction == null || !TryConsumeEventBudget())
            {
                return false;
            }

            if (isPrimary)
            {
                PrimaryProjectileDefinition = definition;
            }

            bool canTriggerHitEffects = isPrimary || ResolveAttackEffect(definition, attackEffectOverride);
            spawnAction.Invoke(new ProjectileSpawnRequest(
                definition,
                origin,
                NormalizeDirection(direction),
                isPrimary,
                canTriggerHitEffects));
            return true;
        }

        private static bool ResolveAttackEffect(
            ProjectileDefinition definition,
            AttackEffectOverride attackEffectOverride)
        {
            return attackEffectOverride switch
            {
                AttackEffectOverride.ForceEnabled => true,
                AttackEffectOverride.ForceDisabled => false,
                _ => definition.DefaultAttackEffect
            };
        }

        private static Vector3 NormalizeDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }
    }
}
