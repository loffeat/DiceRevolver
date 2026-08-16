using UnityEngine;
using UnityEngine.InputSystem;

namespace DiceRevolver.Prototype
{
    public sealed class RevolverGun : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TopDownPlayerController player;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Collider ownerCollider;

        [Header("Holding")]
        [SerializeField] private float holdDistance = 0.85f;
        [SerializeField] private float holdHeight = 0.72f;

        [Header("Revolver")]
        [SerializeField] private int capacity = 6;
        [SerializeField] private int remainingRounds = 6;
        [SerializeField] private float shotsPerSecond = 5f;
        [SerializeField] private float reloadDuration = 1.8f;
        [SerializeField] private bool automaticReloadWhenEmpty = true;

        [Header("Prototype Reload Animation")]
        [SerializeField] private float reloadSpinDegrees = 540f;
        [SerializeField] private float reloadDropDistance = 0.22f;

        private float nextShotTime;
        private float reloadStartedAt;
        private bool isReloading;
        private Vector3 visualRootDefaultLocalPosition;
        private Quaternion visualRootDefaultLocalRotation;

        public int Capacity => capacity;
        public int RemainingRounds => remainingRounds;
        public float ReloadDuration
        {
            get => reloadDuration;
            set => reloadDuration = Mathf.Max(0.05f, value);
        }

        public bool IsReloading => isReloading;

        private void Awake()
        {
            capacity = Mathf.Max(1, capacity);
            remainingRounds = Mathf.Clamp(remainingRounds, 0, capacity);

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            visualRootDefaultLocalPosition = visualRoot.localPosition;
            visualRootDefaultLocalRotation = visualRoot.localRotation;
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            UpdatePose();
            UpdateReload();
            TryFire();
        }

        private void UpdatePose()
        {
            Vector3 aimDirection = player.AimDirection;
            transform.position = player.transform.position + aimDirection * holdDistance + Vector3.up * holdHeight;
            transform.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
        }

        private void TryFire()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || projectilePrefab == null || muzzle == null)
            {
                return;
            }

            if (!mouse.leftButton.isPressed || isReloading || Time.time < nextShotTime)
            {
                return;
            }

            if (remainingRounds <= 0)
            {
                BeginReload();
                return;
            }

            nextShotTime = Time.time + 1f / shotsPerSecond;
            remainingRounds--;

            Projectile projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
            projectile.Launch(player.AimDirection, ownerCollider);

            if (remainingRounds <= 0 && automaticReloadWhenEmpty)
            {
                BeginReload();
            }
        }

        private void BeginReload()
        {
            if (isReloading)
            {
                return;
            }

            isReloading = true;
            reloadStartedAt = Time.time;
        }

        private void UpdateReload()
        {
            if (!isReloading)
            {
                ResetVisualRoot();
                return;
            }

            float progress = Mathf.Clamp01((Time.time - reloadStartedAt) / ReloadDuration);
            AnimateReload(progress);

            if (progress < 1f)
            {
                return;
            }

            remainingRounds = capacity;
            isReloading = false;
            ResetVisualRoot();
        }

        private void AnimateReload(float progress)
        {
            if (visualRoot == null)
            {
                return;
            }

            float drop = Mathf.Sin(progress * Mathf.PI) * reloadDropDistance;
            float spin = progress * reloadSpinDegrees;
            visualRoot.localPosition = visualRootDefaultLocalPosition + Vector3.down * drop;
            visualRoot.localRotation = visualRootDefaultLocalRotation * Quaternion.Euler(0f, 0f, spin);
        }

        private void ResetVisualRoot()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localPosition = visualRootDefaultLocalPosition;
            visualRoot.localRotation = visualRootDefaultLocalRotation;
        }
    }
}
