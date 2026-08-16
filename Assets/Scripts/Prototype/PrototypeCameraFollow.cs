using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class PrototypeCameraFollow : MonoBehaviour
    {
        [SerializeField] private TopDownPlayerController target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -1.5f);
        [SerializeField] private float followSharpness = 6f;
        [SerializeField] private float aimLookAhead = 0.45f;
        [SerializeField] private float aimFollowSharpness = 4f;

        private Vector3 smoothedLookAhead;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredLookAhead = target.AimDirection * aimLookAhead;
            smoothedLookAhead = Damp(smoothedLookAhead, desiredLookAhead, aimFollowSharpness);

            Vector3 desiredPosition = target.transform.position + smoothedLookAhead + offset;
            transform.position = Damp(transform.position, desiredPosition, followSharpness);
            transform.rotation = Quaternion.Euler(85f, 0f, 0f);
        }

        private static Vector3 Damp(Vector3 current, Vector3 target, float sharpness)
        {
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, sharpness) * Time.deltaTime);
            return Vector3.Lerp(current, target, t);
        }
    }
}
