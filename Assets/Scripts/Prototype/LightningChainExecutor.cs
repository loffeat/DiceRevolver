using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class LightningChainExecutor : MonoBehaviour
    {
        [SerializeField, InspectorName("折线渲染器")] private LineRenderer lineRenderer;

        private float expiresAt;
        private float visualDuration;
        private Color visualColor;

        public int Execute(
            IReadOnlyList<Vector3> nodes,
            Transform owner,
            LightningChainDefinition definition)
        {
            if (definition == null || nodes == null || nodes.Count < 2)
            {
                return 0;
            }

            ConfigureVisual(nodes, definition);
            HashSet<IDamageReceiver> damaged = new HashSet<IDamageReceiver>();
            for (int segment = 0; segment < nodes.Count - 1; segment++)
            {
                DamageSegment(
                    nodes[segment],
                    nodes[segment + 1],
                    owner,
                    definition,
                    damaged);
            }

            return damaged.Count;
        }

        private void Update()
        {
            if (visualDuration <= 0f)
            {
                return;
            }

            float remaining = Mathf.Clamp01((expiresAt - Time.time) / visualDuration);
            if (lineRenderer != null)
            {
                Color color = visualColor;
                color.a *= remaining;
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }

            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void DamageSegment(
            Vector3 start,
            Vector3 end,
            Transform owner,
            LightningChainDefinition definition,
            HashSet<IDamageReceiver> damaged)
        {
            Vector3 segment = end - start;
            float length = segment.magnitude;
            if (length <= 0.0001f)
            {
                return;
            }

            Vector3 midpoint = (start + end) * 0.5f;
            float broadRadius = length * 0.5f + definition.ChainWidth;
            Collider[] colliders = Physics.OverlapSphere(
                midpoint,
                broadRadius,
                definition.TargetLayers,
                QueryTriggerInteraction.Collide);
            for (int index = 0; index < colliders.Length; index++)
            {
                Collider collider = colliders[index];
                IDamageReceiver receiver = collider.GetComponentInParent<IDamageReceiver>();
                if (receiver == null || damaged.Contains(receiver) || IsOwner(receiver, owner))
                {
                    continue;
                }

                Vector3 pointOnSegment = ClosestPointOnSegment(
                    start,
                    end,
                    collider.bounds.center);
                Vector3 pointOnCollider = collider.ClosestPoint(pointOnSegment);
                if ((pointOnCollider - pointOnSegment).sqrMagnitude >
                    definition.ChainWidth * definition.ChainWidth)
                {
                    continue;
                }

                damaged.Add(receiver);
                receiver.ReceiveDamage(new DamageInfo(
                    definition.Damage,
                    pointOnSegment,
                    gameObject));
            }
        }

        private void ConfigureVisual(
            IReadOnlyList<Vector3> nodes,
            LightningChainDefinition definition)
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.positionCount = nodes.Count;
            lineRenderer.widthMultiplier = definition.ChainWidth;
            for (int index = 0; index < nodes.Count; index++)
            {
                lineRenderer.SetPosition(index, nodes[index]);
            }

            visualColor = definition.ChainColor;
            lineRenderer.startColor = visualColor;
            lineRenderer.endColor = visualColor;
            visualDuration = definition.VisualDuration;
            expiresAt = Time.time + visualDuration;
        }

        private static Vector3 ClosestPointOnSegment(
            Vector3 start,
            Vector3 end,
            Vector3 point)
        {
            Vector3 segment = end - start;
            float denominator = segment.sqrMagnitude;
            if (denominator <= 0.0001f)
            {
                return start;
            }

            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / denominator);
            return start + segment * t;
        }

        private static bool IsOwner(IDamageReceiver receiver, Transform owner)
        {
            if (owner == null || receiver is not Component component)
            {
                return false;
            }

            Transform receiverTransform = component.transform;
            return receiverTransform == owner ||
                receiverTransform.IsChildOf(owner) ||
                owner.IsChildOf(receiverTransform);
        }
    }
}
