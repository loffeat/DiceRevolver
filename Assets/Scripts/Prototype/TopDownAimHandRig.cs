using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class TopDownAimHandRig : MonoBehaviour
    {
        private const float MinimumRenderPlaneHeight = 0.01f;

        [Header("References")]
        [SerializeField] private TopDownCharacterController player;
        [SerializeField] private Transform aimRoot;
        [SerializeField] private Transform armVisual;
        [SerializeField] private Transform muzzle;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer armRenderer;

        [Header("Layout")]
        [SerializeField] private float orbitRadius = 0f;
        [SerializeField] private float visualHeight = -0.58f;
        [SerializeField] private float armScaleMultiplier = 1f;
        [SerializeField] private bool bodyFacesRightByDefault;
        [SerializeField] private float facingDeadZone = 0.03f;

        [Header("Aim Debug Gizmos")]
        [SerializeField] private bool showAimGizmos = true;
        [SerializeField] private float pivotGizmoRadius = 0.08f;
        [SerializeField] private float editModePreviewLength = 4f;
        [SerializeField] private Color pivotGizmoColor = new Color(0.1f, 0.8f, 1f, 1f);
        [SerializeField] private Color laserGizmoColor = new Color(1f, 0.15f, 0.05f, 1f);

        public Transform Muzzle => muzzle;
        public Vector3 ShotOrigin { get; private set; }
        public Quaternion ShotRotation { get; private set; } = Quaternion.identity;
        public Vector3 ShotDirection => ShotRotation * Vector3.forward;

        private Vector3 armDefaultLocalScale = Vector3.one;
        private Vector3 muzzleDefaultLocalPosition = Vector3.zero;
        private Quaternion muzzleDefaultLocalRotation = Quaternion.identity;
        private Vector3 muzzlePositionInArmSpace = Vector3.zero;
        private Vector3 muzzleForwardInArmSpace = Vector3.forward;
        private bool isFacingRight;

        private void Awake()
        {
            isFacingRight = bodyFacesRightByDefault;

            if (armVisual != null)
            {
                armDefaultLocalScale = armVisual.localScale;
            }

            if (muzzle != null)
            {
                muzzleDefaultLocalPosition = muzzle.localPosition;
                muzzleDefaultLocalRotation = muzzle.localRotation;
                ShotOrigin = muzzle.position;
                ShotRotation = muzzle.rotation;
            }

            if (aimRoot != null && armVisual != null && muzzle != null)
            {
                Vector3 muzzleWorldPosition = aimRoot.TransformPoint(muzzleDefaultLocalPosition);
                Vector3 muzzleWorldForward = aimRoot.TransformDirection(muzzleDefaultLocalRotation * Vector3.forward);
                muzzlePositionInArmSpace = armVisual.InverseTransformPoint(muzzleWorldPosition);
                muzzleForwardInArmSpace = armVisual.InverseTransformVector(muzzleWorldForward).normalized;
            }
        }

        private void LateUpdate()
        {
            RefreshAimPose();
        }

        public void RefreshAimPose()
        {
            if (player == null || aimRoot == null)
            {
                return;
            }

            Vector3 aimDirection = player.AimDirection;
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude <= 0.0001f)
            {
                aimDirection = Vector3.forward;
            }

            aimDirection.Normalize();
            if (Mathf.Abs(aimDirection.x) > facingDeadZone)
            {
                isFacingRight = aimDirection.x > 0f;
            }

            aimRoot.localPosition = ResolveVisibleAimRootPosition(
                aimDirection * orbitRadius + Vector3.up * visualHeight);

            if (bodyRenderer != null)
            {
                bodyRenderer.flipX = bodyFacesRightByDefault ? !isFacingRight : isFacingRight;
            }

            if (armVisual != null)
            {
                float mirror = isFacingRight ? 1f : -1f;
                armVisual.localScale = new Vector3(
                    Mathf.Abs(armDefaultLocalScale.x) * armScaleMultiplier,
                    Mathf.Abs(armDefaultLocalScale.y) * armScaleMultiplier * mirror,
                    Mathf.Abs(armDefaultLocalScale.z) * armScaleMultiplier);
            }

            ResolveLocalShotPose(out Vector3 localShotPosition, out Quaternion localShotRotation);
            aimRoot.rotation = TopDownAimSolver.ResolveRotation(
                aimRoot.position,
                player.AimWorldPoint,
                localShotPosition,
                localShotRotation,
                aimDirection);
            ShotOrigin = aimRoot.TransformPoint(localShotPosition);
            ShotRotation = aimRoot.rotation * localShotRotation;
        }

        private Vector3 ResolveVisibleAimRootPosition(Vector3 desiredLocalPosition)
        {
            Transform parent = aimRoot.parent;
            Vector3 desiredWorldPosition = parent != null
                ? parent.TransformPoint(desiredLocalPosition)
                : desiredLocalPosition;
            if (desiredWorldPosition.y >= MinimumRenderPlaneHeight)
            {
                return desiredLocalPosition;
            }

            desiredWorldPosition.y = MinimumRenderPlaneHeight;
            return parent != null
                ? parent.InverseTransformPoint(desiredWorldPosition)
                : desiredWorldPosition;
        }

        public bool TryGetShotPose(out Vector3 origin, out Quaternion rotation)
        {
            origin = ShotOrigin;
            rotation = ShotRotation;
            return aimRoot != null && muzzle != null;
        }

        private void ResolveLocalShotPose(out Vector3 localPosition, out Quaternion localRotation)
        {
            localPosition = muzzleDefaultLocalPosition;
            localRotation = muzzleDefaultLocalRotation;
            if (aimRoot == null || armVisual == null || muzzle == null || isFacingRight)
            {
                return;
            }

            Vector3 mirroredWorldPosition = armVisual.TransformPoint(muzzlePositionInArmSpace);
            localPosition = aimRoot.InverseTransformPoint(mirroredWorldPosition);

            Vector3 mirroredWorldForward = armVisual.TransformVector(muzzleForwardInArmSpace);
            Vector3 localForward = aimRoot.InverseTransformVector(mirroredWorldForward);
            localForward.y = 0f;
            if (localForward.sqrMagnitude > 0.0001f)
            {
                localRotation = Quaternion.LookRotation(localForward.normalized, Vector3.up);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showAimGizmos)
            {
                return;
            }

            Vector3 pivot = GetAimPivotPosition();
            Vector3 rayOrigin = GetMuzzlePosition(pivot);
            Vector3 target = GetAimTargetPosition(rayOrigin);

            Gizmos.color = pivotGizmoColor;
            Gizmos.DrawSphere(pivot, pivotGizmoRadius);
            Gizmos.DrawWireSphere(pivot, pivotGizmoRadius * 1.7f);

            Gizmos.color = laserGizmoColor;
            Gizmos.DrawWireSphere(rayOrigin, pivotGizmoRadius * 0.8f);
            Gizmos.DrawLine(rayOrigin, target);
            DrawCross(target, pivotGizmoRadius * 1.4f);
        }

        private Vector3 GetAimPivotPosition()
        {
            if (aimRoot != null)
            {
                return aimRoot.position;
            }

            return transform.position;
        }

        private Vector3 GetMuzzlePosition(Vector3 fallbackPosition)
        {
            if (Application.isPlaying && aimRoot != null && muzzle != null)
            {
                return ShotOrigin;
            }

            if (muzzle != null)
            {
                return muzzle.position;
            }

            return fallbackPosition;
        }

        private Vector3 GetAimTargetPosition(Vector3 rayOrigin)
        {
            if (Application.isPlaying && player != null)
            {
                Vector3 direction = ShotDirection;
                direction.y = 0f;
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    direction = transform.forward;
                    direction.y = 0f;
                }

                Vector3 mouseTarget = player.AimWorldPoint;
                mouseTarget.y = rayOrigin.y;
                float length = Mathf.Max(Vector3.Distance(rayOrigin, mouseTarget), editModePreviewLength);
                return rayOrigin + direction.normalized * length;
            }

            Vector3 previewDirection = Application.isPlaying && aimRoot != null
                ? ShotDirection
                : muzzle != null ? muzzle.forward : transform.forward;
            previewDirection.y = 0f;
            if (previewDirection.sqrMagnitude <= 0.0001f)
            {
                previewDirection = Vector3.forward;
            }

            return rayOrigin + previewDirection.normalized * editModePreviewLength;
        }

        private static void DrawCross(Vector3 center, float size)
        {
            Gizmos.DrawLine(center + Vector3.left * size, center + Vector3.right * size);
            Gizmos.DrawLine(center + Vector3.forward * size, center + Vector3.back * size);
        }
    }
}
