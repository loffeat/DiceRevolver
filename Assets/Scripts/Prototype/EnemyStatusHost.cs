using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class EnemyStatusHost : MonoBehaviour
    {
        private readonly List<ActiveStatus> active = new();
        private EnemyHealth health;

        public event Action<EnemyStatusHost, EnemyStatusDefinition> StatusApplied;
        public static event Action<EnemyStatusHost, EnemyStatusDefinition, DiceFaceActivation>
            StatusAppliedGlobal;

        public void ApplyStatus(
            EnemyStatusDefinition definition,
            DiceFaceActivation sourceActivation = null)
        {
            if (definition == null || string.IsNullOrEmpty(definition.StatusId))
            {
                return;
            }

            ActiveStatus existing = Find(definition.StatusId);
            if (existing != null)
            {
                if (definition.MaxStacks > 1 && existing.Stacks < definition.MaxStacks)
                {
                    existing.Stacks++;
                }

                existing.RemainingSeconds = definition.DurationSeconds;
            }
            else
            {
                active.Add(new ActiveStatus(definition));
            }

            StatusApplied?.Invoke(this, definition);
            StatusAppliedGlobal?.Invoke(this, definition, sourceActivation);
        }

        public bool HasStatus(string statusId)
        {
            return Find(statusId) != null;
        }

        public int GetStacks(string statusId)
        {
            ActiveStatus status = Find(statusId);
            return status != null ? status.Stacks : 0;
        }

        public void ClearAllStatuses()
        {
            active.Clear();
        }

        /// <summary>按增量推进所有状态（DoT 结算与到期）；由 Update 驱动，公开以便确定性测试。</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
                if (health == null)
                {
                    return;
                }
            }

            for (int index = active.Count - 1; index >= 0; index--)
            {
                ActiveStatus status = active[index];
                status.RemainingSeconds -= deltaSeconds;
                if (status.Definition.DamagePerSecond > 0f)
                {
                    health.ReceiveDamage(new DamageInfo(
                        status.Definition.DamagePerSecond * status.Stacks * deltaSeconds,
                        transform.position,
                        null));
                }

                if (status.RemainingSeconds <= 0f)
                {
                    active.RemoveAt(index);
                }
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private ActiveStatus Find(string statusId)
        {
            for (int index = 0; index < active.Count; index++)
            {
                if (string.Equals(
                    active[index].Definition.StatusId,
                    statusId,
                    StringComparison.Ordinal))
                {
                    return active[index];
                }
            }

            return null;
        }

        private sealed class ActiveStatus
        {
            public ActiveStatus(EnemyStatusDefinition definition)
            {
                Definition = definition;
                RemainingSeconds = definition.DurationSeconds;
                Stacks = 1;
            }

            public EnemyStatusDefinition Definition { get; }
            public float RemainingSeconds;
            public int Stacks;
        }
    }
}
