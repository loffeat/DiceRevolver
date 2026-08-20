using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, InspectorName("默认飞行速度")] private float speed = 18f;
        [SerializeField, InspectorName("默认存在时间（秒）")] private float lifetime = 1.6f;

        private Vector3 direction = Vector3.forward;
        private float runtimeSpeed;
        private float runtimeLifetime;
        private string projectileType = "Default";
        private string projectileTag = "Default";
        private float damage = 1f;
        private int enemyPierceCount;
        private int remainingEnemyPierces;
        private float despawnTime;
        private Collider projectileCollider;
        private readonly HashSet<IDamageReceiver> damagedReceivers = new HashSet<IDamageReceiver>();

        public string ProjectileType => projectileType;
        public string ProjectileTag => projectileTag;
        public float Damage => damage;
        public int EnemyPierceCount => enemyPierceCount;
        public Transform OwnerTransform { get; private set; }
        public event Action<Collider, Vector3> Hit;

        private void Awake()
        {
            projectileCollider = GetComponent<Collider>();
            ResetRuntimeDefaults();
        }

        public void Configure(ProjectileRuntimeStats stats)
        {
            projectileType = stats.ProjectileType;
            projectileTag = stats.ProjectileTag;
            damage = stats.Damage;
            runtimeSpeed = stats.FlightSpeed;
            runtimeLifetime = stats.FlightDistance / stats.FlightSpeed;
            enemyPierceCount = stats.EnemyPierceCount;
            remainingEnemyPierces = enemyPierceCount;
            damagedReceivers.Clear();
        }

        public void Launch(Vector3 launchDirection, Collider ownerCollider = null)
        {
            EnsureRuntimeDefaults();
            OwnerTransform = ownerCollider != null ? ownerCollider.transform : null;
            launchDirection.y = 0f;
            if (launchDirection.sqrMagnitude > 0.0001f)
            {
                direction = launchDirection.normalized;
            }

            if (projectileCollider != null && ownerCollider != null)
            {
                Physics.IgnoreCollision(projectileCollider, ownerCollider);
            }

            despawnTime = Time.time + runtimeLifetime;
        }

        private void OnEnable()
        {
            EnsureRuntimeDefaults();
            remainingEnemyPierces = enemyPierceCount;
            damagedReceivers.Clear();
            despawnTime = Time.time + runtimeLifetime;
        }

        private void Update()
        {
            EnsureRuntimeDefaults();
            transform.position += direction * runtimeSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            if (Time.time >= despawnTime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ShouldIgnoreCollision(other))
            {
                return;
            }

            IDamageReceiver receiver = other.GetComponentInParent<IDamageReceiver>();
            if (receiver == null)
            {
                Destroy(gameObject);
                return;
            }

            if (!damagedReceivers.Add(receiver))
            {
                return;
            }

            Vector3 hitPosition = transform.position;
            Hit?.Invoke(other, hitPosition);
            receiver.ReceiveDamage(new DamageInfo(damage, hitPosition, gameObject));

            if (remainingEnemyPierces <= 0)
            {
                Destroy(gameObject);
                return;
            }

            remainingEnemyPierces--;
        }

        public static bool ShouldIgnoreCollision(Collider other)
        {
            return other != null &&
                (other.GetComponentInParent<Projectile>() != null || other.CompareTag("Player"));
        }

        private void ResetRuntimeDefaults()
        {
            runtimeSpeed = Mathf.Max(0.0001f, speed);
            runtimeLifetime = Mathf.Max(0.0001f, lifetime);
        }

        private void EnsureRuntimeDefaults()
        {
            if (runtimeSpeed > 0f && runtimeLifetime > 0f)
            {
                return;
            }

            ResetRuntimeDefaults();
        }
    }
}
