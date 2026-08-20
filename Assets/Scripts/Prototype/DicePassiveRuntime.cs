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
        private readonly Action<string> warningLogger;
        private readonly Action<Exception> exceptionLogger;
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
            if (!IsValidFace(face))
            {
                return;
            }

            int index = face - 1;
            DisposeInstance(instances[index]);
            instances[index] = null;
            warnedAboutEmptyCandidates = false;

            if (effect == null)
            {
                return;
            }

            try
            {
                IDicePassiveEffectRuntime runtime =
                    effect.CreateRuntime(new PassiveBindingContext(face));
                if (runtime != null)
                {
                    instances[index] = new PassiveInstance(face, runtime);
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

        private void LogException(Exception exception)
        {
            exceptionLogger?.Invoke(exception);
        }

        private static bool IsValidFace(int face)
        {
            return face >= 1 && face <= DiceRevolverRules.FaceCount;
        }

        private sealed class PassiveInstance
        {
            public PassiveInstance(int face, IDicePassiveEffectRuntime runtime)
            {
                Face = face;
                Runtime = runtime;
            }

            public int Face { get; }
            public IDicePassiveEffectRuntime Runtime { get; }
        }
    }
}
