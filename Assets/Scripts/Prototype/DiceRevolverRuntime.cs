using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public enum DiceRevolverDrawStatus
    {
        Fired,
        CoolingDown,
        Reloading,
        Empty
    }

    public readonly struct DiceRevolverDrawResult
    {
        public DiceRevolverDrawResult(DiceRevolverDrawStatus status, int face = 0)
        {
            Status = status;
            Face = face;
        }

        public DiceRevolverDrawStatus Status { get; }
        public int Face { get; }
    }

    public readonly struct DiceRevolverRuntimeUpdate
    {
        public DiceRevolverRuntimeUpdate(bool reloadStarted, bool reloadCompleted)
        {
            ReloadStarted = reloadStarted;
            ReloadCompleted = reloadCompleted;
        }

        public bool ReloadStarted { get; }
        public bool ReloadCompleted { get; }
    }

    public sealed class DiceRevolverRuntime
    {
        private readonly List<int> remainingFaces = new(DiceRevolverRules.FaceCount);
        private readonly HashSet<int> passiveFaces = new();
        private readonly float shotInterval;
        private readonly bool automaticReloadWhenEmpty;
        private readonly bool allowManualReload;
        private readonly Func<int, int> drawIndexSelector;

        private float nextShotTime;
        private float reloadStartedAt;
        private int? forcedNextFace;
        private float reloadDuration;

        public DiceRevolverRuntime(float shotsPerSecond, float reloadDuration,
            bool automaticReloadWhenEmpty, bool allowManualReload,
            Func<int, int> drawIndexSelector = null)
        {
            shotInterval = 1f / Mathf.Max(0.01f, shotsPerSecond);
            this.automaticReloadWhenEmpty = automaticReloadWhenEmpty;
            this.allowManualReload = allowManualReload;
            this.drawIndexSelector = drawIndexSelector;
            ReloadDuration = reloadDuration;
            RefillAllFaces();
        }

        public int RemainingRounds => remainingFaces.Count;
        public int ActiveFaceCount => DiceRevolverRules.FaceCount - passiveFaces.Count;
        public bool IsReloading { get; private set; }

        public void RebuildActiveFaces(IReadOnlyCollection<int> passiveFaceSet)
        {
            passiveFaces.Clear();
            if (passiveFaceSet != null)
            {
                passiveFaces.UnionWith(passiveFaceSet);
            }

            RefillAllFaces();
        }

        public IReadOnlyList<int> CreateRemainingFacesSnapshot()
        {
            return Array.AsReadOnly(remainingFaces.ToArray());
        }

        public float ReloadDuration
        {
            get => reloadDuration;
            set => reloadDuration = Mathf.Max(0.05f, value);
        }

        public DiceRevolverRuntimeUpdate Tick(float currentTime, bool manualReloadRequested)
        {
            bool reloadCompleted = CompleteReloadIfReady(currentTime);
            bool reloadStarted = manualReloadRequested && TryBeginManualReload(currentTime);
            return new DiceRevolverRuntimeUpdate(reloadStarted, reloadCompleted);
        }

        public DiceRevolverDrawResult TryBeginShot(float currentTime)
        {
            return TryBeginShot(currentTime, null);
        }

        public DiceRevolverDrawResult TryBeginShot(
            float currentTime,
            Func<IReadOnlyList<int>, int?, DiceDrawConstraintResult> candidateFilter)
        {
            if (IsReloading)
            {
                return new DiceRevolverDrawResult(DiceRevolverDrawStatus.Reloading);
            }

            if (currentTime < nextShotTime)
            {
                return new DiceRevolverDrawResult(DiceRevolverDrawStatus.CoolingDown);
            }

            if (remainingFaces.Count == 0)
            {
                return new DiceRevolverDrawResult(DiceRevolverDrawStatus.Empty);
            }

            int face = DrawFace(candidateFilter);
            nextShotTime = currentTime + shotInterval;
            return new DiceRevolverDrawResult(DiceRevolverDrawStatus.Fired, face);
        }

        public DiceRevolverRuntimeUpdate CompleteShot(float currentTime)
        {
            bool reloadStarted = automaticReloadWhenEmpty && remainingFaces.Count == 0 && BeginReload(currentTime);
            return new DiceRevolverRuntimeUpdate(reloadStarted, false);
        }

        public bool TryRefillAndForceNextFace(int face)
        {
            if (face < 1 || face > DiceRevolverRules.FaceCount ||
                remainingFaces.Contains(face) || passiveFaces.Contains(face))
            {
                return false;
            }

            remainingFaces.Add(face);
            remainingFaces.Sort();
            forcedNextFace = face;
            return true;
        }

        /// <summary>在换弹完成后设置首抽强制面；拒绝越界、被动面或不在剩余池中的面。</summary>
        public bool SetFirstDrawForce(int face)
        {
            if (face < 1 || face > DiceRevolverRules.FaceCount ||
                passiveFaces.Contains(face) || !remainingFaces.Contains(face))
            {
                return false;
            }

            forcedNextFace = face;
            return true;
        }

        public float GetReloadProgress(float currentTime)
        {
            if (!IsReloading)
            {
                return 0f;
            }

            return Mathf.Clamp01((currentTime - reloadStartedAt) / ReloadDuration);
        }

        private bool TryBeginManualReload(float currentTime)
        {
            return allowManualReload && RemainingRounds < ActiveFaceCount && BeginReload(currentTime);
        }

        private bool BeginReload(float currentTime)
        {
            if (IsReloading)
            {
                return false;
            }

            IsReloading = true;
            reloadStartedAt = currentTime;
            return true;
        }

        private bool CompleteReloadIfReady(float currentTime)
        {
            if (!IsReloading || GetReloadProgress(currentTime) < 1f)
            {
                return false;
            }

            IsReloading = false;
            nextShotTime = currentTime;
            forcedNextFace = null;
            RefillAllFaces();
            return true;
        }

        private int DrawFace(
            Func<IReadOnlyList<int>, int?, DiceDrawConstraintResult> candidateFilter)
        {
            IReadOnlyList<int> candidates = remainingFaces;
            if (candidateFilter != null)
            {
                DiceDrawConstraintResult filtered =
                    candidateFilter(remainingFaces, forcedNextFace);
                if (HasValidCandidates(filtered.Candidates))
                {
                    candidates = filtered.Candidates;
                }
            }

            if (forcedNextFace.HasValue && ContainsFace(candidates, forcedNextFace.Value))
            {
                int face = forcedNextFace.Value;
                forcedNextFace = null;
                remainingFaces.Remove(face);
                return face;
            }

            if (forcedNextFace.HasValue && !remainingFaces.Contains(forcedNextFace.Value))
            {
                forcedNextFace = null;
            }

            int index = drawIndexSelector != null
                ? Mathf.Clamp(drawIndexSelector(candidates.Count), 0, candidates.Count - 1)
                : UnityEngine.Random.Range(0, candidates.Count);
            int drawnFace = candidates[index];
            remainingFaces.Remove(drawnFace);
            return drawnFace;
        }

        private bool HasValidCandidates(IReadOnlyList<int> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                if (!remainingFaces.Contains(candidates[index]))
                {
                    return false;
                }
            }

            return true;
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

        private void RefillAllFaces()
        {
            remainingFaces.Clear();
            for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
            {
                if (!passiveFaces.Contains(face))
                {
                    remainingFaces.Add(face);
                }
            }
        }
    }
}
