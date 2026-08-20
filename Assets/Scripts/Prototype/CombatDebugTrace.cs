using System;
using System.Collections.Generic;

namespace DiceRevolver.Prototype
{
    public enum CombatDebugEventType
    {
        ShotStarted,
        BonusShotStarted,
        EffectTriggered,
        Result,
        Hit,
        PassiveTriggered,
        ShotEnded,
        ReloadStarted,
        ReloadCompleted
    }

    public readonly struct CombatDebugScope
    {
        public CombatDebugScope(long chainId, long activationId, long parentActivationId, int depth, int face)
        {
            ChainId = chainId;
            ActivationId = activationId;
            ParentActivationId = parentActivationId;
            Depth = depth;
            Face = face;
        }

        public long ChainId { get; }
        public long ActivationId { get; }
        public long ParentActivationId { get; }
        public int Depth { get; }
        public int Face { get; }
        public bool IsValid => ActivationId > 0;
    }

    public readonly struct CombatDebugRecord
    {
        public CombatDebugRecord(
            long sequence,
            long chainId,
            long activationId,
            long parentActivationId,
            int depth,
            int face,
            CombatDebugEventType eventType,
            string phase,
            string name,
            string detail,
            float timestamp)
        {
            Sequence = sequence;
            ChainId = chainId;
            ActivationId = activationId;
            ParentActivationId = parentActivationId;
            Depth = depth;
            Face = face;
            EventType = eventType;
            Phase = phase ?? string.Empty;
            Name = name ?? string.Empty;
            Detail = detail ?? string.Empty;
            Timestamp = timestamp;
        }

        public long Sequence { get; }
        public long ChainId { get; }
        public long ActivationId { get; }
        public long ParentActivationId { get; }
        public int Depth { get; }
        public int Face { get; }
        public CombatDebugEventType EventType { get; }
        public string Phase { get; }
        public string Name { get; }
        public string Detail { get; }
        public float Timestamp { get; }
    }

    public sealed class CombatDebugTrace
    {
        private readonly List<CombatDebugRecord> records;
        private readonly int capacity;
        private long nextSequence = 1;
        private long nextActivationId = 1;

        public CombatDebugTrace(int capacity = 128)
        {
            this.capacity = Math.Max(1, capacity);
            records = new List<CombatDebugRecord>(this.capacity);
        }

        public event Action<CombatDebugRecord> RecordAdded;

        public IReadOnlyList<CombatDebugRecord> Records => records;

        public CombatDebugScope BeginActivation(int face, bool isBonusActivation, CombatDebugScope parent, float timestamp)
        {
            long activationId = nextActivationId++;
            return new CombatDebugScope(
                parent.IsValid ? parent.ChainId : activationId,
                activationId,
                parent.IsValid ? parent.ActivationId : 0,
                parent.IsValid ? parent.Depth + 1 : 0,
                face);
        }

        public CombatDebugRecord Record(
            CombatDebugScope scope,
            CombatDebugEventType eventType,
            string phase,
            string name,
            string detail,
            int additionalDepth,
            float timestamp)
        {
            CombatDebugRecord record = new CombatDebugRecord(
                nextSequence++,
                scope.ChainId,
                scope.ActivationId,
                scope.ParentActivationId,
                Math.Max(0, scope.Depth + additionalDepth),
                scope.Face,
                eventType,
                phase,
                name,
                detail,
                timestamp);
            records.Add(record);
            if (records.Count > capacity)
            {
                records.RemoveAt(0);
            }

            RecordAdded?.Invoke(record);
            return record;
        }

        public CombatDebugRecord RecordStandalone(
            CombatDebugEventType eventType,
            string phase,
            string name,
            string detail,
            float timestamp)
        {
            CombatDebugScope scope = BeginActivation(0, false, default, timestamp);
            return Record(scope, eventType, phase, name, detail, 0, timestamp);
        }
    }
}
