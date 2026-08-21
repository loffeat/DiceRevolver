using UnityEngine;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("触发器/信号类型")]
    public sealed class SignalTypeTriggerModule : EventTriggerModule
    {
        [SerializeField, InspectorName("信号类型")]
        private EventSignalMask signals;

        public EventSignalMask Signals => signals;

        public override bool Matches(EventSignal signal)
        {
            EventSignalMask signalMask = signal.SignalType switch
            {
                EventSignalType.Base => EventSignalMask.Base,
                EventSignalType.OnFire => EventSignalMask.OnFire,
                EventSignalType.OnHit => EventSignalMask.OnHit,
                EventSignalType.OnFireEnd => EventSignalMask.OnFireEnd,
                EventSignalType.ProjectileSpawned => EventSignalMask.ProjectileSpawned,
                EventSignalType.ProjectileHit => EventSignalMask.ProjectileHit,
                EventSignalType.ReloadStarted => EventSignalMask.ReloadStarted,
                EventSignalType.ReloadCompleted => EventSignalMask.ReloadCompleted,
                EventSignalType.FaceConsumed => EventSignalMask.FaceConsumed,
                EventSignalType.DrawCandidate => EventSignalMask.DrawCandidate,
                EventSignalType.BeforeProjectileStats => EventSignalMask.BeforeProjectileStats,
                _ => EventSignalMask.None
            };

            return (signals & signalMask) != 0;
        }
    }
}
