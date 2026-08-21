using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceEventRuleRuntimeSet
    {
        private const int SlotCount = 5;
        private readonly EventRuleDefinition[,] definitions =
            new EventRuleDefinition[DiceRevolverRules.FaceCount, SlotCount];
        private readonly EventRuleRuntime[,] runtimes =
            new EventRuleRuntime[DiceRevolverRules.FaceCount, SlotCount];
        private readonly ProjectileTypeDefinition[] baseProjectileTypes =
            new ProjectileTypeDefinition[DiceRevolverRules.FaceCount];
        private OwnedProjectileRegistry ownedProjectiles;
        private Func<BonusDiceActivationRequest, bool> bonusActivation;
        private CombatDebugTrace debugTrace;
        private Func<float> currentTime;
        private Action<string> warningLogger;
        private Action<Exception, UnityEngine.Object> exceptionLogger;
        private bool warnedAboutEmptyCandidates;

        public void ConfigurePassiveServices(
            OwnedProjectileRegistry ownedProjectileRegistry,
            Func<BonusDiceActivationRequest, bool> bonusActivationRequest,
            CombatDebugTrace trace,
            Func<float> timeProvider,
            Action<string> warnings,
            Action<Exception, UnityEngine.Object> exceptions)
        {
            ownedProjectiles = ownedProjectileRegistry;
            bonusActivation = bonusActivationRequest;
            debugTrace = trace;
            currentTime = timeProvider;
            warningLogger = warnings;
            exceptionLogger = exceptions;
        }

        public void RebuildFace(int face, DiceFaceConfigurationSnapshot snapshot)
        {
            if (face < 1 || face > DiceRevolverRules.FaceCount)
            {
                return;
            }

            int faceIndex = face - 1;
            baseProjectileTypes[faceIndex] = ResolveBaseProjectileType(snapshot);
            for (int slotIndex = 0; slotIndex < SlotCount; slotIndex++)
            {
                DiceFaceSlotType slot = (DiceFaceSlotType)slotIndex;
                EventRuleDefinition definition = snapshot.GetRule(slot);
                if (definitions[faceIndex, slotIndex] == definition)
                {
                    continue;
                }

                definitions[faceIndex, slotIndex] = definition;
                runtimes[faceIndex, slotIndex] = definition != null
                    ? new EventRuleRuntime(definition, face, slot)
                    : null;
            }

            warnedAboutEmptyCandidates = false;
        }

        public DiceDrawConstraintResult FilterDrawCandidates(
            IReadOnlyList<int> legacyCandidates,
            IReadOnlyList<int> realChamberPool,
            int? forcedFace)
        {
            if (realChamberPool == null || realChamberPool.Count == 0)
            {
                return new DiceDrawConstraintResult(Array.Empty<int>(), false);
            }

            List<int> allowed = new();
            List<int> priorities = new();
            if (legacyCandidates != null)
            {
                for (int candidateIndex = 0; candidateIndex < legacyCandidates.Count; candidateIndex++)
                {
                    int candidate = legacyCandidates[candidateIndex];
                    EventSignal signal = CreateSignal(
                        EventSignalType.DrawCandidate,
                        candidate,
                        null,
                        null,
                        default,
                        null,
                        Vector3.zero,
                        legacyCandidates,
                        candidate,
                        default);
                    PassiveEventRuleServices services = CreateServices(signal);
                    ExecutePassive(signal, services);
                    if (!services.DrawRejected)
                    {
                        allowed.Add(candidate);
                        priorities.Add(services.HighestDrawPriority);
                    }
                }
            }

            if (allowed.Count > 0)
            {
                int minimumPriority = int.MaxValue;
                for (int index = 0; index < priorities.Count; index++)
                {
                    minimumPriority = Math.Min(minimumPriority, priorities[index]);
                }

                List<int> selected = new();
                for (int index = 0; index < allowed.Count; index++)
                {
                    if (priorities[index] == minimumPriority)
                    {
                        selected.Add(allowed[index]);
                    }
                }

                warnedAboutEmptyCandidates = false;
                return new DiceDrawConstraintResult(
                    selected,
                    forcedFace.HasValue && selected.Contains(forcedFace.Value));
            }

            List<int> fallback = new(realChamberPool.Count);
            for (int index = 0; index < realChamberPool.Count; index++)
            {
                fallback.Add(realChamberPool[index]);
            }

            if (!warnedAboutEmptyCandidates)
            {
                warnedAboutEmptyCandidates = true;
                warningLogger?.Invoke(
                    "Combined legacy and Rule passive draw constraints produced no candidates; using the real chamber pool.");
            }

            return new DiceDrawConstraintResult(
                fallback,
                forcedFace.HasValue && fallback.Contains(forcedFace.Value));
        }

        public ProjectileRuntimeStats ModifyProjectileStats(
            int sourceFace,
            ProjectileRuntimeStats stats,
            DiceFaceActivation activation = null)
        {
            ProjectileRuntimeStats modified = stats;
            for (int faceIndex = 0; faceIndex < DiceRevolverRules.FaceCount; faceIndex++)
            {
                EventRuleRuntime runtime = runtimes[faceIndex, (int)DiceFaceSlotType.Passive];
                if (runtime == null)
                {
                    continue;
                }

                EventSignal signal = CreateSignalForEquippedFace(
                    CreateSignal(
                        EventSignalType.BeforeProjectileStats,
                        sourceFace,
                        activation,
                        null,
                        default,
                        null,
                        Vector3.zero,
                        Array.Empty<int>(),
                        0,
                        modified),
                    faceIndex);
                PassiveEventRuleServices services = CreateServices(signal);
                TryExecute(runtime, signal, services);
                modified = modified.WithDamage(
                    modified.Damage * services.ProjectileDamageMultiplier);
            }

            return modified;
        }

        public void NotifyProjectileSpawned(
            int sourceFace,
            ProjectileHandle projectile,
            DiceFaceActivation sourceActivation)
        {
            EventSignal signal = CreateSignal(
                EventSignalType.ProjectileSpawned,
                sourceFace,
                sourceActivation,
                null,
                projectile,
                null,
                Vector3.zero,
                Array.Empty<int>(),
                0,
                projectile.Stats);
            ExecutePassive(signal);
        }

        public void NotifyProjectileHit(
            DiceRevolverShotContext shot,
            Collider hitCollider,
            Vector3 hitPosition)
        {
            NotifyProjectileHit(shot, default, hitCollider, hitPosition);
        }

        public void NotifyProjectileHit(
            DiceRevolverShotContext shot,
            ProjectileHandle projectile,
            Collider hitCollider,
            Vector3 hitPosition)
        {
            DiceFaceActivation activation = shot?.Activation;
            EventSignal signal = CreateSignal(
                EventSignalType.ProjectileHit,
                shot != null ? shot.Face : 0,
                activation,
                shot,
                projectile,
                hitCollider,
                hitPosition,
                Array.Empty<int>(),
                0,
                shot != null ? shot.Stats : default);
            ExecutePassive(signal);
        }

        public void NotifyReloadStarted()
        {
            ExecutePassive(CreateSignal(EventSignalType.ReloadStarted));
        }

        public void NotifyReloadCompleted()
        {
            warnedAboutEmptyCandidates = false;
            ExecutePassive(CreateSignal(EventSignalType.ReloadCompleted));
        }

        public void NotifyFaceConsumed(int face)
        {
            ExecutePassive(CreateSignal(EventSignalType.FaceConsumed, face));
        }

        public bool ExecuteActive(
            int face,
            DiceFaceSlotType slot,
            EventSignal signal,
            IEventRuleServices services)
        {
            int slotIndex = (int)slot;
            if (!IsValidSlot(face, slotIndex))
            {
                return false;
            }

            return ExecuteActive(
                face,
                slot,
                definitions[face - 1, slotIndex],
                signal,
                services);
        }

        public bool ExecuteActive(
            int face,
            DiceFaceSlotType slot,
            EventRuleDefinition definition,
            EventSignal signal,
            IEventRuleServices services)
        {
            int slotIndex = (int)slot;
            if (!IsValidSlot(face, slotIndex) || definition == null)
            {
                return false;
            }

            EventRuleRuntime runtime = definitions[face - 1, slotIndex] == definition
                ? runtimes[face - 1, slotIndex]
                : new EventRuleRuntime(definition, face, slot);

            runtime.TryHandle(signal, services);
            return true;
        }

        private static bool IsValidSlot(int face, int slotIndex)
        {
            return face >= 1 && face <= DiceRevolverRules.FaceCount &&
                slotIndex >= 0 && slotIndex < SlotCount;
        }

        private void ExecutePassive(EventSignal signal)
        {
            ExecutePassive(signal, null);
        }

        private void ExecutePassive(EventSignal signal, PassiveEventRuleServices sharedServices)
        {
            for (int faceIndex = 0; faceIndex < DiceRevolverRules.FaceCount; faceIndex++)
            {
                EventRuleRuntime runtime = runtimes[faceIndex, (int)DiceFaceSlotType.Passive];
                if (runtime == null)
                {
                    continue;
                }

                EventSignal equippedSignal = CreateSignalForEquippedFace(signal, faceIndex);
                PassiveEventRuleServices services = sharedServices ?? CreateServices(equippedSignal);
                TryExecute(runtime, equippedSignal, services);
            }
        }

        private void TryExecute(
            EventRuleRuntime runtime,
            EventSignal signal,
            PassiveEventRuleServices services)
        {
            try
            {
                runtime.TryHandle(signal, services);
            }
            catch (Exception exception)
            {
                try
                {
                    exceptionLogger?.Invoke(exception, definitions[signal.EquippedFace - 1, (int)DiceFaceSlotType.Passive]);
                }
                catch (Exception)
                {
                    // A logger must not break later passive runtimes.
                }
            }
        }

        private PassiveEventRuleServices CreateServices(EventSignal signal)
        {
            return new PassiveEventRuleServices(
                signal,
                ownedProjectiles,
                bonusActivation,
                debugTrace,
                currentTime,
                exceptionLogger);
        }

        private EventSignal CreateSignal(
            EventSignalType signalType,
            int sourceFace = 0,
            DiceFaceActivation activation = null,
            DiceRevolverShotContext shot = null,
            ProjectileHandle projectile = default,
            Collider hitCollider = null,
            Vector3 hitPosition = default,
            IReadOnlyList<int> remainingFaces = null,
            int drawCandidate = 0,
            ProjectileRuntimeStats currentStats = default)
        {
            return new EventSignal(
                signalType,
                0,
                sourceFace,
                DiceFaceSlotType.Passive,
                activation,
                shot,
                projectile,
                hitCollider,
                hitPosition,
                remainingFaces ?? Array.Empty<int>(),
                drawCandidate,
                currentStats,
                activation?.EventBudget,
                activation != null && activation.IsBonusActivation,
                activation != null ? activation.DebugScope : default);
        }

        private EventSignal CreateSignalForEquippedFace(EventSignal source, int faceIndex)
        {
            return new EventSignal(
                source.SignalType,
                faceIndex + 1,
                source.SourceFace,
                DiceFaceSlotType.Passive,
                source.Activation,
                source.Shot,
                source.Projectile,
                source.HitCollider,
                source.HitPosition,
                source.RemainingFaces,
                source.DrawCandidate,
                source.CurrentStats,
                source.EventBudget,
                source.IsBonusActivation,
                source.DebugScope,
                baseProjectileTypes[faceIndex]);
        }

        private static ProjectileTypeDefinition ResolveBaseProjectileType(
            DiceFaceConfigurationSnapshot snapshot)
        {
            if (snapshot.GetEffect(DiceFaceSlotType.Base) is ProjectileSpawnEffect legacy)
            {
                return legacy.ProjectileDefinition?.ProjectileTypeDefinition;
            }

            return snapshot.GetRule(DiceFaceSlotType.Base)
                ?.FindPrimaryProjectileDefinition()
                ?.ProjectileTypeDefinition;
        }
    }
}
