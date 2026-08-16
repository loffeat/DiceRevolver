using UnityEngine;

namespace DiceRevolver.Prototype
{
    public static class TopDownAimSolver
    {
        private const float MinimumStableMargin = 0.05f;

        public static Quaternion ResolveRotation(
            Vector3 pivot,
            Vector3 target,
            Vector3 localMuzzlePosition,
            Quaternion localMuzzleRotation,
            Vector3 fallbackDirection)
        {
            Vector3 pivotToTarget = target - pivot;
            pivotToTarget.y = 0f;
            if (pivotToTarget.sqrMagnitude <= 0.0001f)
            {
                pivotToTarget = FlattenOrFallback(fallbackDirection, Vector3.forward);
            }

            Vector3 targetDirection = pivotToTarget.normalized;
            Vector3 localForward = FlattenOrFallback(localMuzzleRotation * Vector3.forward, Vector3.forward);
            Vector3 localRight = Vector3.Cross(Vector3.up, localForward).normalized;

            float forwardOffset = Vector3.Dot(localMuzzlePosition, localForward);
            float lateralOffset = Vector3.Dot(localMuzzlePosition, localRight);
            float muzzleOrbitRadius = Mathf.Sqrt(
                forwardOffset * forwardOffset + lateralOffset * lateralOffset);
            float stableDistance = Mathf.Max(
                pivotToTarget.magnitude,
                muzzleOrbitRadius + MinimumStableMargin);
            float forwardDistance = Mathf.Sqrt(Mathf.Max(
                stableDistance * stableDistance - lateralOffset * lateralOffset,
                0f));

            Vector3 localTargetDirection =
                localRight * lateralOffset + localForward * forwardDistance;
            localTargetDirection = FlattenOrFallback(localTargetDirection, localForward);

            Quaternion targetFrame = Quaternion.LookRotation(targetDirection, Vector3.up);
            Quaternion localFrame = Quaternion.LookRotation(localTargetDirection, Vector3.up);
            return targetFrame * Quaternion.Inverse(localFrame);
        }

        private static Vector3 FlattenOrFallback(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = fallback;
                direction.y = 0f;
            }

            return direction.normalized;
        }
    }
}
