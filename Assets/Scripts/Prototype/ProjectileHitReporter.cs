using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class ProjectileHitReporter : MonoBehaviour
    {
        public event Action<Collider> Hit;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Projectile>() != null || other.CompareTag("Player"))
            {
                return;
            }

            Hit?.Invoke(other);
        }
    }
}
