using System;
using System.Collections.Generic;

namespace DiceRevolver.Prototype
{
    public readonly struct DiceDrawConstraintResult
    {
        public DiceDrawConstraintResult(IReadOnlyList<int> candidates, bool forcedFaceEligible)
        {
            Candidates = candidates ?? Array.Empty<int>();
            ForcedFaceEligible = forcedFaceEligible;
        }

        public IReadOnlyList<int> Candidates { get; }
        public bool ForcedFaceEligible { get; }
    }

    public sealed class DicePassiveRuntime : IDisposable
    {
        private readonly PassiveInstance[] instances =
            new PassiveInstance[DiceRevolverRules.FaceCount];
        private readonly ProjectileTypeDefinition[] baseProjectileTypes =
            new ProjectileTypeDefinition[DiceRevolverRules.FaceCount];
        private readonly Action<string> warningLogger;
        private readonly Action<Exception> exceptionLogger;
        private Func<BonusDiceActivationRequest, bool> bonusActivationRequest;
        private CombatDebugTrace debugTrace;
        private Func<float> debugTime;
        private long nextInstanceId = 1;
        private bool warnedAboutEmptyCandidates;

        public DicePassiveRuntime(
            Action<string> warningLogger = null,
            Action<Exception> exceptionLogger = null)
        {
            this.warningLogger = warningLogger;
            this.exceptionLogger = exceptionLogger;
        }

        public void RebuildFace(int face, PassiveEventEffect effect)
        {
            RebuildFace(face, effect, GetBaseProjectileType(face));
        }

        public void RebuildFace(
            int face,
            PassiveEventEffect effect,
            ProjectileTypeDefinition baseProjectileType)
        {
            if (!IsValidFace(face))
            {
                return;
            }

            int index = face - 1;
            baseProjectileTypes[index] = baseProjectileType;
            DisposeInstance(instances[index]);
            instances[index] = null;
            warnedAboutEmptyCandidates = false;

            if (effect == null)
            {
                return;
            }

            try
            {
                long instanceId = nextInstanceId++;
                IDicePassiveEffectRuntime runtime =
                    effect.CreateRuntime(new PassiveBindingContext(
                        face,
                        instanceId,
                        GetBaseProjectileType,
                        RequestBonusActivation));
                if (runtime != null)
                {
                    instances[index] = new PassiveInstance(
                        face,
                        instanceId,
                        string.IsNullOrWhiteSpace(effect.name) ? effect.GetType().Name : effect.name,
                        runtime);
                }
            }
            catch (Exception exception)
            {
                LogException(exception);
            }
        }

        public DiceDrawConstraintResult FilterDrawCandidates(
            IReadOnlyList<int> remainingFaces,
            int? forcedFace)
        {
            if (remainingFaces == null || remainingFaces.Count == 0)
            {
                return new DiceDrawConstraintResult(Array.Empty<int>(), false);
            }

            int minimumPriority = FindMinimumPriority(remainingFaces);
            for (int index = 0; index < instances.Length; index++)
            {
                PassiveInstance instance = instances[index];
                if (instance != null &&
                    ContainsFace(remainingFaces, instance.Face) &&
                    GetDrawPriority(instance.Face) > minimumPriority)
                {
                    RecordPassive(instance, null, $"骰面 {instance.Face} 保留到最后");
                }
            }

            List<int> candidates = new List<int>(remainingFaces.Count);
            for (int candidateIndex = 0; candidateIndex < remainingFaces.Count; candidateIndex++)
            {
                int candidate = remainingFaces[candidateIndex];
                if (GetDrawPriority(candidate) != minimumPriority ||
                    !AllPassivesAllow(candidate, remainingFaces))
                {
                    continue;
                }

                candidates.Add(candidate);
            }

            if (candidates.Count == 0)
            {
                for (int index = 0; index < remainingFaces.Count; index++)
                {
                    candidates.Add(remainingFaces[index]);
                }

                if (!warnedAboutEmptyCandidates)
                {
                    warnedAboutEmptyCandidates = true;
                    warningLogger?.Invoke(
                        "Passive draw constraints produced no candidates; using the real chamber pool.");
                }
            }
            else
            {
                warnedAboutEmptyCandidates = false;
            }

            return new DiceDrawConstraintResult(
                candidates,
                forcedFace.HasValue && candidates.Contains(forcedFace.Value));
        }

        public void NotifyReloadStarted()
        {
            Notify(runtime => runtime.OnReloadStarted());
        }

        public void NotifyReloadCompleted()
        {
            warnedAboutEmptyCandidates = false;
            Notify(runtime => runtime.OnReloadCompleted());
        }

        public void NotifyFaceConsumed(int face)
        {
            Notify(runtime => runtime.OnFaceConsumed(face));
        }

        public void UpdateBaseProjectileType(
            int face,
            ProjectileTypeDefinition baseProjectileType)
        {
            if (IsValidFace(face))
            {
                baseProjectileTypes[face - 1] = baseProjectileType;
            }
        }

        public ProjectileRuntimeStats ModifyProjectileStats(
            int sourceFace,
            ProjectileRuntimeStats stats)
        {
            ProjectileRuntimeStats modified = stats;
            for (int index = 0; index < instances.Length; index++)
            {
                if (instances[index]?.Runtime is not IDiceProjectileStatsModifier modifier)
                {
                    continue;
                }

                try
                {
                    modified = modifier.ModifyProjectileStats(sourceFace, modified);
                }
                catch (Exception exception)
                {
                    LogException(exception);
                }
            }

            return modified;
        }

        public void NotifyProjectileSpawned(int sourceFace, ProjectileHandle projectile)
        {
            NotifyProjectileSpawned(sourceFace, projectile, null);
        }

        public void NotifyProjectileSpawned(
            int sourceFace,
            ProjectileHandle projectile,
            DiceFaceActivation sourceActivation)
        {
            for (int index = 0; index < instances.Length; index++)
            {
                PassiveInstance instance = instances[index];
                if (instance?.Runtime is not IDiceProjectileSpawnObserver observer)
                {
                    continue;
                }

                try
                {
                    if (observer.OnProjectileSpawned(sourceFace, projectile))
                    {
                        RecordPassive(instance, sourceActivation, "雷电弹丸使本轮层数增加 1");
                    }
                }
                catch (Exception exception)
                {
                    LogException(exception);
                }
            }
        }

        public void ConfigureBonusActivation(
            Func<BonusDiceActivationRequest, bool> request)
        {
            bonusActivationRequest = request;
        }

        public void ConfigureDebugTrace(CombatDebugTrace trace, Func<float> currentTime)
        {
            debugTrace = trace;
            debugTime = currentTime;
        }

        public void NotifyProjectileHit(
            DiceRevolverShotContext shot,
            UnityEngine.Collider hitCollider,
            UnityEngine.Vector3 hitPosition,
            long suppressedPassiveInstanceId = 0)
        {
            if (shot == null)
            {
                return;
            }

            for (int index = 0; index < instances.Length; index++)
            {
                PassiveInstance instance = instances[index];
                if (instance == null ||
                    instance.InstanceId == suppressedPassiveInstanceId ||
                    instance.Runtime is not IDiceProjectileHitObserver observer)
                {
                    continue;
                }

                try
                {
                    observer.OnProjectileHit(shot, hitCollider, hitPosition);
                }
                catch (Exception exception)
                {
                    LogException(exception);
                }
            }
        }

        public void Dispose()
        {
            for (int index = 0; index < instances.Length; index++)
            {
                DisposeInstance(instances[index]);
                instances[index] = null;
            }
        }

        private int FindMinimumPriority(IReadOnlyList<int> remainingFaces)
        {
            int minimum = int.MaxValue;
            for (int index = 0; index < remainingFaces.Count; index++)
            {
                minimum = Math.Min(minimum, GetDrawPriority(remainingFaces[index]));
            }

            return minimum == int.MaxValue ? 0 : minimum;
        }

        private int GetDrawPriority(int face)
        {
            PassiveInstance instance = GetInstance(face);
            if (instance?.Runtime is not IDiceDrawPriorityProvider provider)
            {
                return 0;
            }

            try
            {
                return provider.DrawPriority;
            }
            catch (Exception exception)
            {
                LogException(exception);
                return 0;
            }
        }

        private bool AllPassivesAllow(int candidate, IReadOnlyList<int> remainingFaces)
        {
            bool allowed = true;
            for (int index = 0; index < instances.Length; index++)
            {
                IDicePassiveEffectRuntime runtime = instances[index]?.Runtime;
                if (runtime == null)
                {
                    continue;
                }

                try
                {
                    if (!runtime.AllowsDraw(candidate, remainingFaces))
                    {
                        allowed = false;
                    }
                }
                catch (Exception exception)
                {
                    LogException(exception);
                }
            }

            return allowed;
        }

        private void Notify(Action<IDicePassiveEffectRuntime> notification)
        {
            for (int index = 0; index < instances.Length; index++)
            {
                IDicePassiveEffectRuntime runtime = instances[index]?.Runtime;
                if (runtime == null)
                {
                    continue;
                }

                try
                {
                    notification(runtime);
                }
                catch (Exception exception)
                {
                    LogException(exception);
                }
            }
        }

        private void DisposeInstance(PassiveInstance instance)
        {
            if (instance?.Runtime == null)
            {
                return;
            }

            try
            {
                instance.Runtime.Dispose();
            }
            catch (Exception exception)
            {
                LogException(exception);
            }
        }

        private PassiveInstance GetInstance(int face)
        {
            return IsValidFace(face) ? instances[face - 1] : null;
        }

        private ProjectileTypeDefinition GetBaseProjectileType(int face)
        {
            return IsValidFace(face) ? baseProjectileTypes[face - 1] : null;
        }

        private bool RequestBonusActivation(BonusDiceActivationRequest request)
        {
            return bonusActivationRequest != null && bonusActivationRequest.Invoke(request);
        }

        private void LogException(Exception exception)
        {
            exceptionLogger?.Invoke(exception);
        }

        private void RecordPassive(
            PassiveInstance instance,
            DiceFaceActivation sourceActivation,
            string detail)
        {
            if (debugTrace == null || instance == null)
            {
                return;
            }

            float time = debugTime != null ? debugTime.Invoke() : 0f;
            CombatDebugScope scope = sourceActivation != null && sourceActivation.DebugScope.IsValid
                ? sourceActivation.DebugScope
                : debugTrace.BeginActivation(instance.Face, false, default, time);
            debugTrace.Record(
                scope,
                CombatDebugEventType.PassiveTriggered,
                "被动",
                instance.EffectName,
                detail,
                sourceActivation != null ? 1 : 0,
                time);
        }

        private static bool IsValidFace(int face)
        {
            return face >= 1 && face <= DiceRevolverRules.FaceCount;
        }

        private static bool ContainsFace(IReadOnlyList<int> faces, int face)
        {
            for (int index = 0; index < faces.Count; index++)
            {
                if (faces[index] == face)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class PassiveInstance
        {
            public PassiveInstance(
                int face,
                long instanceId,
                string effectName,
                IDicePassiveEffectRuntime runtime)
            {
                Face = face;
                InstanceId = instanceId;
                EffectName = effectName;
                Runtime = runtime;
            }

            public int Face { get; }
            public long InstanceId { get; }
            public string EffectName { get; }
            public IDicePassiveEffectRuntime Runtime { get; }
        }
    }
}
