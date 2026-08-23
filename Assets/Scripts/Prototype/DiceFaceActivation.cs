using System;
using System.Collections.Generic;
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
        private readonly Func<ProjectileSpawnRequest, ProjectileHandle> spawnAction;
        private readonly Func<int, bool> refillAndForceNextFaceAction;
        private readonly Action<string> warningAction;
        private Func<ProjectileHandle, IReadOnlyList<ProjectileHandle>, LightningChainDefinition, bool>
            lightningChainAction;
        private Action<DiceFaceActiveOverlay> queueNextShotOverlayAction;
        private CombatDebugTrace debugTrace;
        private Func<float> debugTime;

        public DiceFaceActivation(
            int face,
            DiceFaceConfigurationSnapshot configuration,
            Vector3 origin,
            Vector3 direction,
            Action<float, Action> scheduleAction,
            Action<ProjectileSpawnRequest> spawnAction,
            Func<int, bool> refillAndForceNextFaceAction,
            Action<string> warningAction,
            int eventBudget = DefaultEventBudget)
            : this(
                face,
                configuration,
                origin,
                direction,
                scheduleAction,
                spawnAction == null
                    ? null
                    : request =>
                    {
                        spawnAction.Invoke(request);
                        return default;
                    },
                refillAndForceNextFaceAction,
                warningAction,
                eventBudget)
        {
        }

        public DiceFaceActivation(
            int face,
            DiceFaceConfigurationSnapshot configuration,
            Vector3 origin,
            Vector3 direction,
            Action<float, Action> scheduleAction,
            Func<ProjectileSpawnRequest, ProjectileHandle> spawnAction,
            Func<int, bool> refillAndForceNextFaceAction,
            Action<string> warningAction,
            int eventBudget = DefaultEventBudget)
            : this(
                face,
                configuration,
                origin,
                direction,
                scheduleAction,
                spawnAction,
                refillAndForceNextFaceAction,
                warningAction,
                new DiceEventBudget(eventBudget),
                false,
                0)
        {
        }

        public DiceFaceActivation(
            int face,
            DiceFaceConfigurationSnapshot configuration,
            Vector3 origin,
            Vector3 direction,
            Action<float, Action> scheduleAction,
            Func<ProjectileSpawnRequest, ProjectileHandle> spawnAction,
            Func<int, bool> refillAndForceNextFaceAction,
            Action<string> warningAction,
            DiceEventBudget eventBudget,
            bool isBonusActivation,
            long suppressedPassiveInstanceId)
        {
            Face = face;
            Configuration = configuration;
            Origin = origin;
            Direction = NormalizeDirection(direction);
            this.scheduleAction = scheduleAction;
            this.spawnAction = spawnAction;
            this.refillAndForceNextFaceAction = refillAndForceNextFaceAction;
            this.warningAction = warningAction;
            EventBudget = eventBudget ?? new DiceEventBudget(DefaultEventBudget);
            IsBonusActivation = isBonusActivation;
            SuppressedPassiveInstanceId = suppressedPassiveInstanceId;
        }

        public int Face { get; }
        public DiceFaceConfigurationSnapshot Configuration { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public ProjectileDefinition PrimaryProjectileDefinition { get; private set; }
        public ProjectileHandle PrimaryProjectile { get; private set; }
        public OwnedProjectileRegistry OwnedProjectiles { get; private set; }
        public DiceEventBudget EventBudget { get; }
        public bool IsBonusActivation { get; }
        public long SuppressedPassiveInstanceId { get; }
        public CombatDebugScope DebugScope { get; private set; }
        public int RemainingEventBudget => EventBudget.Remaining;
        public float DamageMultiplier { get; set; } = 1f;

        public void ConfigureDebugScope(
            CombatDebugTrace trace,
            CombatDebugScope scope,
            Func<float> currentTime)
        {
            debugTrace = trace;
            DebugScope = scope;
            debugTime = currentTime;
        }

        public bool TryConsumeEventBudget()
        {
            return EventBudget.TryConsume(() => warningAction?.Invoke(
                $"Dice face {Face} stopped because its event budget was exhausted."));
        }

        public bool RequestRefillAndForceNextFace(int face)
        {
            bool applied = refillAndForceNextFaceAction != null && refillAndForceNextFaceAction.Invoke(face);
            if (applied)
            {
                RecordDebug(CombatDebugEventType.Result, "结果", "检索骰面", $"骰面 {face}", 2);
            }

            return applied;
        }

        public void ConfigureLightningServices(
            OwnedProjectileRegistry ownedProjectiles,
            Func<ProjectileHandle, IReadOnlyList<ProjectileHandle>, LightningChainDefinition, bool>
                requestLightningChain)
        {
            OwnedProjectiles = ownedProjectiles;
            lightningChainAction = requestLightningChain;
        }

        public bool RequestLightningChain(
            ProjectileHandle origin,
            IReadOnlyList<ProjectileHandle> targets,
            LightningChainDefinition definition)
        {
            bool created = lightningChainAction != null &&
                origin.IsAlive &&
                definition != null &&
                targets != null &&
                targets.Count > 0 &&
                lightningChainAction.Invoke(origin, targets, definition);
            if (created)
            {
                RecordDebug(
                    CombatDebugEventType.Result,
                    "结果",
                    "生成闪电链",
                    $"连接 {targets.Count} 个雷电球",
                    2);
            }

            return created;
        }

        public void ConfigureOverlayService(Action<DiceFaceActiveOverlay> queueOverlay)
        {
            queueNextShotOverlayAction = queueOverlay;
        }

        public bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay)
        {
            if (overlay.IsEmpty || queueNextShotOverlayAction == null)
            {
                return false;
            }

            queueNextShotOverlayAction.Invoke(overlay);
            RecordDebug(CombatDebugEventType.Result, "结果", "覆盖下一骰面", null, 2);
            return true;
        }

        public bool Schedule(float delaySeconds, Action callback)
        {
            if (callback == null || scheduleAction == null)
            {
                return false;
            }

            float delay = Mathf.Max(0f, delaySeconds);
            RecordDebug(
                CombatDebugEventType.Result,
                "延迟",
                "安排延迟事件",
                $"{delay:0.##} 秒后",
                2);
            scheduleAction.Invoke(delay, () =>
            {
                RecordDebug(CombatDebugEventType.Result, "延迟", "执行延迟事件", null, 2);
                callback.Invoke();
            });
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
            ProjectileHandle handle = spawnAction.Invoke(new ProjectileSpawnRequest(
                definition,
                origin,
                NormalizeDirection(direction),
                isPrimary,
                canTriggerHitEffects));
            if (isPrimary)
            {
                PrimaryProjectile = handle;
            }

            RecordDebug(
                CombatDebugEventType.Result,
                "结果",
                "生成弹丸",
                definition.name,
                2);

            return true;
        }

        public void RecordDebug(
            CombatDebugEventType eventType,
            string phase,
            string name,
            string detail,
            int additionalDepth,
            bool verbose = false)
        {
            if (debugTrace == null || !DebugScope.IsValid)
            {
                return;
            }

            debugTrace.Record(
                DebugScope,
                eventType,
                phase,
                name,
                detail,
                additionalDepth,
                debugTime != null ? debugTime.Invoke() : 0f,
                verbose);
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
