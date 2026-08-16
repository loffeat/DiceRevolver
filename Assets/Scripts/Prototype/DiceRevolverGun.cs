using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiceRevolver.Prototype
{
    public sealed class DiceRevolverGun : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TopDownPlayerController player;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Collider ownerCollider;
        [SerializeField] private DiceFaceLoadout loadout;

        [Header("Holding")]
        [SerializeField] private float holdDistance = 0.85f;
        [SerializeField] private float holdHeight = 0.72f;
        [SerializeField] private bool driveWeaponPose = true;

        [Header("Dice Revolver")]
        [SerializeField] private int faceCount = 6;
        [SerializeField] private float shotsPerSecond = 5f;
        [SerializeField] private float reloadDuration = 1.8f;
        [SerializeField] private bool automaticReloadWhenEmpty = true;
        [SerializeField] private bool allowManualReload = true;

        [Header("Prototype Reload Animation")]
        [SerializeField] private float reloadDropDistance = 0.22f;
        [SerializeField] private float reloadBlinkSpeed = 8f;
        [SerializeField] private Color reloadDimColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private DiceChamber chamber;
        private float nextShotTime;
        private float reloadStartedAt;
        private bool isReloading;
        private Vector3 visualRootDefaultLocalPosition;
        private Quaternion visualRootDefaultLocalRotation;
        private SpriteRenderer reloadBlinkRenderer;
        private Color reloadBlinkDefaultColor = Color.white;
        private TopDownAimHandRig aimRig;

        public event Action<DiceRevolverShotContext> FireStarted;
        public event Action<DiceRevolverHitContext> ProjectileHit;
        public event Action<DiceRevolverShotContext> FireEnded;
        public event Action ReloadStarted;
        public event Action ReloadCompleted;

        public int RemainingRounds => chamber?.RemainingCount ?? 0;
        public bool IsReloading => isReloading;
        public float ReloadDuration
        {
            get => reloadDuration;
            set => reloadDuration = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            faceCount = Mathf.Max(1, faceCount);
            chamber = new DiceChamber(faceCount);
            if (loadout == null)
            {
                loadout = GetComponentInParent<DiceFaceLoadout>();
            }

            if (player != null)
            {
                aimRig = player.GetComponentInChildren<TopDownAimHandRig>();
            }

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            visualRootDefaultLocalPosition = visualRoot.localPosition;
            visualRootDefaultLocalRotation = visualRoot.localRotation;
            reloadBlinkRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>();
            if (reloadBlinkRenderer != null)
            {
                reloadBlinkDefaultColor = reloadBlinkRenderer.color;
            }
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            UpdatePose();
            UpdateReload();
            TryManualReload();
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                return;
            }

            aimRig?.RefreshAimPose();
            TryFire();
        }

        private void UpdatePose()
        {
            if (!driveWeaponPose)
            {
                return;
            }

            Vector3 aimDirection = player.AimDirection;
            transform.position = player.transform.position + aimDirection * holdDistance + Vector3.up * holdHeight;
            transform.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
        }

        private void TryManualReload()
        {
            if (!allowManualReload || isReloading || chamber == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame && chamber.RemainingCount < faceCount)
            {
                BeginReload();
            }
        }

        private void TryFire()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || projectilePrefab == null || muzzle == null || chamber == null)
            {
                return;
            }

            if (!mouse.leftButton.isPressed || isReloading || Time.time < nextShotTime)
            {
                return;
            }

            if (!chamber.TryDrawFace(out int face))
            {
                BeginReload();
                return;
            }

            nextShotTime = Time.time + 1f / shotsPerSecond;
            if (loadout == null)
            {
                loadout = GetComponentInParent<DiceFaceLoadout>();
            }

            DiceFaceEntry entry = loadout != null ? loadout.GetEntry(face) : null;
            ProjectileRuntimeStats stats = BuildStats(entry);
            Projectile selectedProjectilePrefab = ResolveProjectilePrefab(entry);

            Vector3 shotOrigin = muzzle.position;
            Quaternion shotRotation = muzzle.rotation;
            if (aimRig != null && aimRig.TryGetShotPose(out Vector3 rigOrigin, out Quaternion rigRotation))
            {
                shotOrigin = rigOrigin;
                shotRotation = rigRotation;
            }

            Vector3 shotDirection = shotRotation * Vector3.forward;
            shotDirection.y = 0f;
            if (shotDirection.sqrMagnitude <= 0.0001f)
            {
                shotDirection = player.AimDirection;
            }

            shotDirection.Normalize();
            Projectile projectile = SpawnProjectile(
                shotOrigin,
                shotDirection,
                shotRotation,
                selectedProjectilePrefab,
                stats,
                entry != null);

            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                face,
                shotOrigin,
                shotDirection,
                projectile,
                entry,
                stats,
                selectedProjectilePrefab);
            BridgeProjectileHit(projectile, shot, true);

            FireStarted?.Invoke(shot);
            TriggerEffects(entry?.OnFireEffects, CreateEventContext(shot, null, shotOrigin, true));
            FireEnded?.Invoke(shot);
            TriggerEffects(entry?.OnFireEndEffects, CreateEventContext(shot, null, shotOrigin, false));

            if (chamber.IsEmpty && automaticReloadWhenEmpty)
            {
                BeginReload();
            }
        }

        public Projectile SpawnConfiguredProjectile(DiceRevolverShotContext shot, bool allowTriggeredEffects)
        {
            if (shot == null)
            {
                return null;
            }

            Projectile prefab = shot.ProjectilePrefab != null ? shot.ProjectilePrefab : projectilePrefab;
            Quaternion rotation = GetShotRotation(shot.Direction, transform.rotation);
            Projectile spawned = SpawnProjectile(shot.Origin, shot.Direction, rotation, prefab, shot.Stats, shot.Entry != null);
            if (spawned != null)
            {
                BridgeProjectileHit(spawned, shot, allowTriggeredEffects);
            }

            return spawned;
        }

        private Projectile SpawnProjectile(
            Vector3 origin,
            Vector3 direction,
            Quaternion rotation,
            Projectile prefab,
            ProjectileRuntimeStats stats,
            bool applyStats)
        {
            if (prefab == null)
            {
                return null;
            }

            Projectile projectile = Instantiate(prefab, origin, rotation);
            if (applyStats)
            {
                projectile.Configure(stats);
            }

            projectile.Launch(direction, ownerCollider);
            return projectile;
        }

        private void BridgeProjectileHit(Projectile projectile, DiceRevolverShotContext shot, bool allowTriggeredEffects)
        {
            if (projectile == null)
            {
                return;
            }

            ProjectileHitReporter reporter = projectile.GetComponent<ProjectileHitReporter>();
            if (reporter == null)
            {
                reporter = projectile.gameObject.AddComponent<ProjectileHitReporter>();
            }

            reporter.Hit += hitCollider =>
            {
                Vector3 hitPosition = projectile.transform.position;
                DiceRevolverHitContext hit = new DiceRevolverHitContext(shot, hitCollider, hitPosition);
                ProjectileHit?.Invoke(hit);
                if (allowTriggeredEffects)
                {
                    TriggerEffects(shot.Entry?.OnHitEffects, CreateEventContext(shot, hitCollider, hitPosition, false));
                }
            };
        }

        private Projectile ResolveProjectilePrefab(DiceFaceEntry entry)
        {
            if (entry != null && entry.ProjectilePrefabOverride != null)
            {
                return entry.ProjectilePrefabOverride;
            }

            return projectilePrefab;
        }

        private static Quaternion GetShotRotation(Vector3 direction, Quaternion fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return fallback;
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static ProjectileRuntimeStats BuildStats(DiceFaceEntry entry)
        {
            if (entry == null)
            {
                return default;
            }

            return new ProjectileRuntimeStats(
                entry.ProjectileType,
                entry.ProjectileTag,
                entry.Damage,
                entry.FlightDistance,
                entry.FlightSpeed,
                entry.EnemyPierceCount);
        }

        private BulletEventContext CreateEventContext(
            DiceRevolverShotContext shot,
            Collider hitCollider,
            Vector3 hitPosition,
            bool canTriggerAdditionalShots)
        {
            return new BulletEventContext(
                this,
                chamber,
                shot,
                hitCollider,
                hitPosition,
                canTriggerAdditionalShots,
                requestedShot => SpawnConfiguredProjectile(requestedShot, false));
        }

        private static void TriggerEffects(System.Collections.Generic.IReadOnlyList<BulletEventEffect> effects, BulletEventContext context)
        {
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                effects[i]?.Trigger(context);
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
            ReloadStarted?.Invoke();
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

            chamber.Reset();
            isReloading = false;
            ResetVisualRoot();
            ReloadCompleted?.Invoke();
        }

        private void AnimateReload(float progress)
        {
            if (visualRoot == null)
            {
                return;
            }

            float drop = Mathf.Sin(progress * Mathf.PI) * reloadDropDistance;
            float blink = Mathf.PingPong(progress * reloadBlinkSpeed, 1f);
            visualRoot.localPosition = visualRootDefaultLocalPosition + Vector3.down * drop;
            visualRoot.localRotation = visualRootDefaultLocalRotation;

            if (reloadBlinkRenderer != null)
            {
                reloadBlinkRenderer.color = Color.Lerp(reloadDimColor, reloadBlinkDefaultColor, blink);
            }
        }

        private void ResetVisualRoot()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localPosition = visualRootDefaultLocalPosition;
            visualRoot.localRotation = visualRootDefaultLocalRotation;

            if (reloadBlinkRenderer != null)
            {
                reloadBlinkRenderer.color = reloadBlinkDefaultColor;
            }
        }
    }
}
