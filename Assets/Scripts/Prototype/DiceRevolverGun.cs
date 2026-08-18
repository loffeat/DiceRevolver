using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiceRevolver.Prototype
{
    public sealed class DiceRevolverGun : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField, InspectorName("玩家控制器")] private TopDownPlayerController player;
        [SerializeField, InspectorName("武器视觉根节点")] private Transform visualRoot;
        [SerializeField, InspectorName("枪口")] private Transform muzzle;
        [SerializeField, InspectorName("默认弹丸 Prefab")] private Projectile projectilePrefab;
        [SerializeField, InspectorName("持有者碰撞体")] private Collider ownerCollider;
        [SerializeField, InspectorName("骰面装备")] private DiceFaceLoadout loadout;

        [Header("持枪设置")]
        [SerializeField, InspectorName("持枪距离")] private float holdDistance = 0.85f;
        [SerializeField, InspectorName("持枪高度")] private float holdHeight = 0.72f;
        [SerializeField, InspectorName("自动驱动武器姿态")] private bool driveWeaponPose = true;

        [Header("骰子左轮")]
        [SerializeField, InspectorName("骰面数量")] private int faceCount = 6;
        [SerializeField, InspectorName("每秒射击次数")] private float shotsPerSecond = 5f;
        [SerializeField, InspectorName("换弹时间（秒）")] private float reloadDuration = 1.8f;
        [SerializeField, InspectorName("弹巢耗尽时自动换弹")] private bool automaticReloadWhenEmpty = true;
        [SerializeField, InspectorName("允许手动换弹")] private bool allowManualReload = true;

        [Header("换弹视觉")]
        [SerializeField, InspectorName("换弹下沉距离")] private float reloadDropDistance = 0.22f;
        [SerializeField, InspectorName("换弹闪烁速度")] private float reloadBlinkSpeed = 8f;
        [SerializeField, InspectorName("换弹暗色")] private Color reloadDimColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private DiceChamber chamber;
        private float nextShotTime;
        private float reloadStartedAt;
        private bool isReloading;
        private SpriteRenderer reloadBlinkRenderer;
        private Color reloadBlinkDefaultColor = Color.white;
        private TopDownAimHandRig aimRig;
        private readonly BulletEventTimeScheduler eventTimeScheduler = new BulletEventTimeScheduler();

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
            if (player != null)
            {
                aimRig?.RefreshAimPose();
                TryFire();
            }

            eventTimeScheduler.Tick(Time.time, Debug.LogException);
        }

        private void OnDestroy()
        {
            eventTimeScheduler.Clear();
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
            if (mouse == null || muzzle == null || chamber == null)
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

            DiceFaceConfigurationSnapshot configuration = loadout != null
                ? loadout.GetSnapshot(face)
                : default;

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
            DiceFaceActivation activation = null;
            activation = new DiceFaceActivation(
                face,
                configuration,
                shotOrigin,
                shotDirection,
                this,
                chamber,
                (delaySeconds, callback) =>
                    eventTimeScheduler.Schedule(Time.time, delaySeconds, callback),
                request => SpawnActivationProjectile(activation, request));

            DiceRevolverShotContext faceTrigger = new DiceRevolverShotContext(
                face,
                shotOrigin,
                shotDirection,
                null,
                configuration,
                default,
                null,
                null,
                activation,
                false);
            BulletEventContext eventContext = CreateEventContext(
                activation,
                faceTrigger,
                null,
                shotOrigin);

            FireStarted?.Invoke(faceTrigger);
            TriggerEffect(configuration.GetEffect(DiceFaceSlotType.Base), eventContext);
            TriggerEffect(configuration.GetEffect(DiceFaceSlotType.OnFire), eventContext);
            FireEnded?.Invoke(faceTrigger);
            TriggerEffect(configuration.GetEffect(DiceFaceSlotType.OnFireEnd), eventContext);

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
            Projectile spawned = SpawnProjectile(
                shot.Origin,
                shot.Direction,
                rotation,
                prefab,
                shot.Stats,
                shot.Configuration.HasAnyEntry);
            if (spawned != null)
            {
                BridgeProjectileHit(spawned, shot, allowTriggeredEffects);
            }

            return spawned;
        }

        private Projectile SpawnActivationProjectile(
            DiceFaceActivation activation,
            ProjectileSpawnRequest request)
        {
            ProjectileDefinition definition = request.Definition;
            Projectile prefab = definition != null ? definition.ProjectilePrefab : null;
            if (activation == null || prefab == null)
            {
                Debug.LogWarning("Projectile spawn skipped because its definition or runtime prefab is missing.", definition);
                return null;
            }

            ProjectileRuntimeStats stats = definition.BuildRuntimeStats();
            Quaternion rotation = GetShotRotation(request.Direction, transform.rotation);
            Projectile projectile = SpawnProjectile(
                request.Origin,
                request.Direction,
                rotation,
                prefab,
                stats,
                true);
            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                activation.Face,
                request.Origin,
                request.Direction,
                projectile,
                activation.Configuration,
                stats,
                prefab,
                definition,
                activation,
                request.CanTriggerHitEffects);
            BridgeProjectileHit(projectile, shot, request.CanTriggerHitEffects);
            return projectile;
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

            origin.y = 0f;
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
                    TriggerEffect(
                        shot.Configuration.GetEffect(DiceFaceSlotType.OnHit),
                        CreateEventContext(shot.Activation, shot, hitCollider, hitPosition));
                }
            };
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

        private BulletEventContext CreateEventContext(
            DiceFaceActivation activation,
            DiceRevolverShotContext shot,
            Collider hitCollider,
            Vector3 hitPosition)
        {
            return new BulletEventContext(
                activation,
                shot,
                hitCollider,
                hitPosition);
        }

        private static void TriggerEffect(BulletEventEffect effect, BulletEventContext context)
        {
            if (effect == null || context.Activation == null || !context.Activation.TryConsumeEventBudget())
            {
                return;
            }

            try
            {
                effect.Trigger(context);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, effect);
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

            float blink = Mathf.PingPong(progress * reloadBlinkSpeed, 1f);

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

            if (reloadBlinkRenderer != null)
            {
                reloadBlinkRenderer.color = reloadBlinkDefaultColor;
            }
        }
    }
}
