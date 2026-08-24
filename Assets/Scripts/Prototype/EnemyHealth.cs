using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageReceiver
    {
        [SerializeField, InspectorName("最大生命")] private int maxHealth = 20;
        [SerializeField, InspectorName("最低生命")] private int minimumHealth = 0;
        [SerializeField, InspectorName("死亡后禁用")] private bool disableOnDeath = true;

        public int MaxHealth
        {
            get => maxHealth;
            set
            {
                maxHealth = Mathf.Max(1, value);
                minimumHealth = Mathf.Clamp(minimumHealth, 0, maxHealth);
                if (CurrentHealth > maxHealth)
                {
                    CurrentHealth = maxHealth;
                }
            }
        }

        public int MinimumHealth
        {
            get => minimumHealth;
            set
            {
                minimumHealth = Mathf.Clamp(Mathf.Max(0, value), 0, maxHealth);
                if (CurrentHealth < minimumHealth)
                {
                    CurrentHealth = minimumHealth;
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

            int damageToApply = Mathf.CeilToInt(damage.Amount);
            CurrentHealth = Mathf.Max(minimumHealth, CurrentHealth - damageToApply);
            DamageReceived?.Invoke(damage);
            if (minimumHealth == 0 && CurrentHealth == 0)
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
