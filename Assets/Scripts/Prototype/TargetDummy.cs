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

        private EnemyHealth health;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            if (health == null)
            {
                health = gameObject.AddComponent<EnemyHealth>();
            }

            health.Died -= HandleDied;
            health.Died += HandleDied;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        public void ReceiveDamage(DamageInfo damage)
        {
            LastDamage = damage;
            HitCount++;
            DamageReceived?.Invoke(damage);
            if (health != null)
            {
                health.ReceiveDamage(damage);
            }
        }

        private void HandleDied(EnemyHealth enemyHealth)
        {
            // 测试靶死亡后立即重置，保持可继续被打击。
            health.ResetHealth();
        }
    }
}
