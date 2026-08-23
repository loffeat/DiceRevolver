using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageReceiver
    {
        [SerializeField, InspectorName("最大生命")] private int maxHealth = 20;
        [SerializeField, InspectorName("死亡后禁用")] private bool disableOnDeath = true;

        public int MaxHealth
        {
            get => maxHealth;
            set
            {
                maxHealth = Mathf.Max(1, value);
                if (CurrentHealth > maxHealth)
                {
                    CurrentHealth = maxHealth;
                }
            }
        }

        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public event Action<EnemyHealth> Died;
        public event Action<DamageInfo> DamageReceived;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void ReceiveDamage(DamageInfo damage)
        {
            if (IsDead || damage.Amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.CeilToInt(damage.Amount));
            DamageReceived?.Invoke(damage);
            if (CurrentHealth == 0)
            {
                IsDead = true;
                Died?.Invoke(this);
                if (disableOnDeath)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void ResetHealth()
        {
            IsDead = false;
            CurrentHealth = maxHealth;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }
    }
}
