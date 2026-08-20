using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public enum DiceRevolverDrawStatus
    {
        Fired,
        CoolingDown,
        Reloading,
        Empty,
        ReloadStarted
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
        private readonly float shotInterval;
        private readonly bool automaticReloadWhenEmpty;
        private readonly bool allowManualReload;

        private float nextShotTime;
        private float reloadStartedAt;
        private int? forcedNextFace;
        private float reloadDuration;

        public DiceRevolverRuntime(float shotsPerSecond, float reloadDuration,
            bool automaticReloadWhenEmpty, bool allowManualReload)
        {
            shotInterval = 1f / Mathf.Max(0.01f, shotsPerSecond);
            this.automaticReloadWhenEmpty = automaticReloadWhenEmpty;
            this.allowManualReload = allowManualReload;
            ReloadDuration = reloadDuration;
            RefillAllFaces();
        }

        public int RemainingRounds => remainingFaces.Count;
        public bool IsReloading { get; private set; }

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

            int face = DrawFace();
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
            if (face < 1 || face > DiceRevolverRules.FaceCount || remainingFaces.Contains(face))
            {
                return false;
            }

            remainingFaces.Add(face);
            remainingFaces.Sort();
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
            return allowManualReload && RemainingRounds < DiceRevolverRules.FaceCount && BeginReload(currentTime);
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

        private int DrawFace()
        {
            if (forcedNextFace.HasValue && remainingFaces.Contains(forcedNextFace.Value))
            {
                int face = forcedNextFace.Value;
                forcedNextFace = null;
                remainingFaces.Remove(face);
                return face;
            }

            forcedNextFace = null;
            int index = Random.Range(0, remainingFaces.Count);
            int drawnFace = remainingFaces[index];
            remainingFaces.RemoveAt(index);
            return drawnFace;
        }

        private void RefillAllFaces()
        {
            remainingFaces.Clear();
            for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
            {
                remainingFaces.Add(face);
            }
        }
    }
}
