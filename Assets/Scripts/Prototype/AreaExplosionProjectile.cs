using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Projectile))]
    public sealed class AreaExplosionProjectile : MonoBehaviour
    {
        [Header("范围伤害")]
        [SerializeField, InspectorName("爆炸半径")] private float radius = 2.5f;
        [SerializeField, InspectorName("受击图层")] private LayerMask targetLayers = ~0;

        [Header("圆环表现")]
        [SerializeField, InspectorName("圆环渲染器")] private LineRenderer ringRenderer;
        [SerializeField, InspectorName("视觉持续时间（秒）")] private float visualDuration = 0.35f;
        [SerializeField, InspectorName("圆环颜色")] private Color ringColor = new Color(1f, 0.24f, 0.05f, 1f);
        [SerializeField, InspectorName("圆环宽度")] private float ringWidth = 0.18f;
        [SerializeField, InspectorName("圆环分段数")] private int ringSegments = 64;

        private readonly HashSet<IDamageReceiver> damagedReceivers = new HashSet<IDamageReceiver>();
        private Projectile projectile;
        private bool detonated;
        private float detonatedAt;

        public float Radius => radius;
        public float VisualDuration => visualDuration;

        private void Awake()
        {
            projectile = GetComponent<Projectile>();
            if (ringRenderer == null)
            {
                ringRenderer = GetComponent<LineRenderer>();
            }

            ConfigureRing();
        }

        private void Start()
        {
            Detonate();
        }

        private void Update()
        {
            if (!detonated)
            {
                return;
            }

            float duration = Mathf.Max(0.01f, visualDuration);
            float progress = Mathf.Clamp01((Time.time - detonatedAt) / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            UpdateRing(radius * easedProgress, 1f - progress);

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        public void Detonate()
        {
            if (detonated)
            {
                return;
            }

            detonated = true;
            detonatedAt = Time.time;
            projectile ??= GetComponent<Projectile>();
            ApplyAreaDamage();
            UpdateRing(0f, 1f);
        }

        private void ApplyAreaDamage()
        {
            float effectiveRadius = Mathf.Max(0f, radius);
            if (effectiveRadius <= 0f || projectile == null)
            {
                return;
            }

            damagedReceivers.Clear();
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                effectiveRadius,
                targetLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits.Length; i++)
            {
                IDamageReceiver receiver = hits[i].GetComponentInParent<IDamageReceiver>();
                if (receiver == null || damagedReceivers.Contains(receiver) || IsProjectileOwner(receiver))
                {
                    continue;
                }

                damagedReceivers.Add(receiver);
                receiver.ReceiveDamage(new DamageInfo(
                    projectile.Damage,
                    transform.position,
                    gameObject));
            }
        }

        private bool IsProjectileOwner(IDamageReceiver receiver)
        {
            if (projectile == null || projectile.OwnerTransform == null || receiver is not Component component)
            {
                return false;
            }

            Transform ownerTransform = projectile.OwnerTransform;
            Transform receiverTransform = component.transform;
            return receiverTransform == ownerTransform ||
                receiverTransform.IsChildOf(ownerTransform) ||
                ownerTransform.IsChildOf(receiverTransform);
        }

        private void ConfigureRing()
        {
            if (ringRenderer == null)
            {
                return;
            }

            ringRenderer.useWorldSpace = false;
            ringRenderer.loop = true;
            ringRenderer.widthMultiplier = Mathf.Max(0.01f, ringWidth);
            ringRenderer.positionCount = Mathf.Max(12, ringSegments);
            UpdateRing(0f, 1f);
        }

        private void UpdateRing(float currentRadius, float alpha)
        {
            if (ringRenderer == null)
            {
                return;
            }

            int segments = Mathf.Max(12, ringSegments);
            if (ringRenderer.positionCount != segments)
            {
                ringRenderer.positionCount = segments;
            }

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                ringRenderer.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    0f,
                    Mathf.Sin(angle) * currentRadius));
            }

            Color color = ringColor;
            color.a *= Mathf.Clamp01(alpha);
            ringRenderer.startColor = color;
            ringRenderer.endColor = color;
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0f, radius);
            visualDuration = Mathf.Max(0.01f, visualDuration);
            ringWidth = Mathf.Max(0.01f, ringWidth);
            ringSegments = Mathf.Max(12, ringSegments);
            ConfigureRing();
        }
    }
}
