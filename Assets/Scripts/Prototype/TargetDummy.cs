using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class TargetDummy : MonoBehaviour, IDamageReceiver
    {
        public event Action<DamageInfo> DamageReceived;

        public DamageInfo LastDamage { get; private set; }
        public int HitCount { get; private set; }

        public void ReceiveDamage(DamageInfo damage)
        {
            LastDamage = damage;
            HitCount++;
            DamageReceived?.Invoke(damage);
        }
    }
}
