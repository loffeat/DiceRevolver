using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceRevolverGun : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField, InspectorName("玩家控制器")] private TopDownCharacterController player;
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
        [SerializeField, InspectorName("每秒射击次数")] private float shotsPerSecond = 5f;
        [SerializeField, InspectorName("换弹时间（秒）")] private float reloadDuration = 1.8f;
        [SerializeField, InspectorName("弹巢耗尽时自动换弹")] private bool automaticReloadWhenEmpty = true;
        [SerializeField, InspectorName("允许手动换弹")] private bool allowManualReload = true;
        [SerializeField, Min(1), InspectorName("单次骰面事件预算")]
        private int eventBudgetPerActivation = DiceFaceActivation.DefaultEventBudget;

        [Header("换弹视觉")]
        [SerializeField, InspectorName("换弹闪烁速度")] private float reloadBlinkSpeed = 8f;
        [SerializeField, InspectorName("换弹暗色")] private Color reloadDimColor =
            new Color(0.35f, 0.35f, 0.35f, 1f);

        private DiceRevolverRuntime runtime;
        private DiceShotPipeline shotPipeline;
        private DicePassiveRuntime passiveRuntime;
        private readonly OwnedProjectileRegistry ownedProjectiles = new OwnedProjectileRegistry();
        private SpriteRenderer reloadBlinkRenderer;
        private Color reloadBlinkDefaultColor = Color.white;
        private TopDownAimHandRig aimRig;

        public event Action<DiceRevolverShotContext> FireStarted;
        public event Action<DiceRevolverHitContext> ProjectileHit;
        public event Action<DiceRevolverShotContext> FireEnded;
        public event Action ReloadStarted;
        public event Action ReloadCompleted;

        public int RemainingRounds => runtime?.RemainingRounds ?? 0;
        public bool IsReloading => runtime?.IsReloading ?? false;
        public OwnedProjectileRegistry OwnedProjectiles => ownedProjectiles;
        public float ReloadDuration
        {
            get => runtime?.ReloadDuration ?? reloadDuration;
            set
            {
                reloadDuration = Mathf.Max(0.05f, value);
                if (runtime != null)
                {
                    runtime.ReloadDuration = reloadDuration;
                }
            }
        }

        private void Awake()
        {
            runtime = new DiceRevolverRuntime(
                shotsPerSecond,
                reloadDuration,
                automaticReloadWhenEmpty,
                allowManualReload);
            shotPipeline = new DiceShotPipeline(
                () => Time.time,
                SpawnActivationProjectile,
                runtime.TryRefillAndForceNextFace,
                message => Debug.LogWarning(message),
                (exception, context) => Debug.LogException(exception, context));
            shotPipeline.ConfigureLightningServices(
                ownedProjectiles,
                ExecuteLightningChain);

            if (loadout == null)
            {
                loadout = GetComponentInParent<DiceFaceLoadout>();
            }

            passiveRuntime?.Dispose();
            passiveRuntime = new DicePassiveRuntime(
                message => Debug.LogWarning(message, this),
                exception => Debug.LogException(exception, this));
            if (loadout != null)
            {
                loadout.SlotChanged -= HandleLoadoutSlotChanged;
                loadout.SlotChanged += HandleLoadoutSlotChanged;
                for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
                {
                    DiceFaceConfigurationSnapshot snapshot = loadout.GetSnapshot(face);
                    passiveRuntime.RebuildFace(
                        face,
                        snapshot.GetPassiveEffect(),
                        GetBaseProjectileType(snapshot));
                }
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
            if (runtime == null)
            {
                return;
            }

            if (player != null)
            {
                UpdatePose();
            }

            DiceRevolverRuntimeUpdate update = runtime.Tick(
                Time.time,
                player != null && player.ReloadPressedThisFrame);
            if (update.ReloadStarted)
            {
                NotifyReloadStarted();
            }

            if (update.ReloadCompleted)
            {
                passiveRuntime?.NotifyReloadCompleted();
                ResetVisualRoot();
                ReloadCompleted?.Invoke();
            }

            if (runtime.IsReloading)
            {
                AnimateReload(runtime.GetReloadProgress(Time.time));
            }
            else
            {
                ResetVisualRoot();
            }
        }

        private void LateUpdate()
        {
            if (player != null)
            {
                aimRig?.RefreshAimPose();
                TryFire();
            }

            shotPipeline?.Tick(Time.time);
        }

        private void OnDestroy()
        {
            if (loadout != null)
            {
                loadout.SlotChanged -= HandleLoadoutSlotChanged;
            }

            passiveRuntime?.Dispose();
            shotPipeline?.Clear();
        }

        private void UpdatePose()
        {
            if (!driveWeaponPose || player == null)
            {
                return;
            }

            Vector3 aimDirection = player.AimDirection;
            transform.position = player.transform.position +
                aimDirection * holdDistance +
                Vector3.up * holdHeight;
            transform.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
        }

        private void TryFire()
        {
            if (player == null || muzzle == null || runtime == null || shotPipeline == null ||
                !player.FireHeld)
            {
                return;
            }

            DiceRevolverDrawResult draw = passiveRuntime != null
                ? runtime.TryBeginShot(Time.time, passiveRuntime.FilterDrawCandidates)
                : runtime.TryBeginShot(Time.time);
            if (draw.Status != DiceRevolverDrawStatus.Fired)
            {
                return;
            }

            if (loadout == null)
            {
                loadout = GetComponentInParent<DiceFaceLoadout>();
            }

            DiceFaceConfigurationSnapshot snapshot = loadout != null
                ? loadout.GetSnapshot(draw.Face)
                : default;
            Vector3 shotOrigin = muzzle.position;
            Quaternion shotRotation = muzzle.rotation;
            if (aimRig != null &&
                aimRig.TryGetShotPose(out Vector3 rigOrigin, out Quaternion rigRotation))
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
            shotPipeline.ExecuteShot(
                draw.Face,
                snapshot,
                shotOrigin,
                shotDirection,
                Mathf.Max(1, eventBudgetPerActivation),
                shot => FireStarted?.Invoke(shot),
                shot => FireEnded?.Invoke(shot));
            passiveRuntime?.NotifyFaceConsumed(draw.Face);

            DiceRevolverRuntimeUpdate completion = runtime.CompleteShot(Time.time);
            if (completion.ReloadStarted)
            {
                NotifyReloadStarted();
            }
        }

        private ProjectileHandle SpawnActivationProjectile(
            DiceFaceActivation activation,
            ProjectileSpawnRequest request)
        {
            ProjectileDefinition definition = request.Definition;
            Projectile prefab = definition != null ? definition.ProjectilePrefab : null;
            if (activation == null || definition == null || prefab == null)
            {
                Debug.LogWarning(
                    "Projectile spawn skipped because its definition or runtime prefab is missing.",
                    definition);
                return default;
            }

            ProjectileRuntimeStats stats = definition.BuildRuntimeStats();
            stats = passiveRuntime != null
                ? passiveRuntime.ModifyProjectileStats(activation.Face, stats)
                : stats;
            Quaternion rotation = GetShotRotation(request.Direction, transform.rotation);
            Vector3 origin = request.Origin;
            origin.y = 0f;
            Projectile projectile = Instantiate(prefab, origin, rotation);
            projectile.Configure(stats);
            projectile.Launch(request.Direction, ownerCollider);
            ProjectileHandle handle = ownedProjectiles.Register(projectile, stats);
            passiveRuntime?.NotifyProjectileSpawned(activation.Face, handle);

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
            projectile.Hit += (hitCollider, hitPosition) =>
                shotPipeline.HandleHit(
                    shot,
                    hitCollider,
                    hitPosition,
                    hit => ProjectileHit?.Invoke(hit));
            return handle;
        }

        private bool ExecuteLightningChain(
            ProjectileHandle origin,
            IReadOnlyList<ProjectileHandle> targets,
            LightningChainDefinition definition)
        {
            if (!origin.IsAlive || definition == null || targets == null || targets.Count == 0)
            {
                return false;
            }

            List<Vector3> nodes = new List<Vector3> { origin.Position };
            for (int index = 0; index < targets.Count; index++)
            {
                if (targets[index].IsAlive)
                {
                    nodes.Add(targets[index].Position);
                }
            }

            if (nodes.Count < 2)
            {
                return false;
            }

            LightningChainExecutor executor;
            if (definition.ExecutorPrefab != null)
            {
                executor = Instantiate(
                    definition.ExecutorPrefab,
                    Vector3.zero,
                    Quaternion.identity);
            }
            else
            {
                executor = new GameObject("Lightning Chain").AddComponent<LightningChainExecutor>();
            }

            Transform owner = ownerCollider != null
                ? ownerCollider.transform
                : player != null ? player.transform : null;
            executor.Execute(nodes, owner, definition);
            return true;
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

        private void NotifyReloadStarted()
        {
            passiveRuntime?.NotifyReloadStarted();
            ReloadStarted?.Invoke();
        }

        private void HandleLoadoutSlotChanged(
            int face,
            DiceFaceSlotType slotType,
            DiceFaceEntry entry)
        {
            if (slotType == DiceFaceSlotType.Base)
            {
                passiveRuntime?.UpdateBaseProjectileType(
                    face,
                    loadout != null
                        ? GetBaseProjectileType(loadout.GetSnapshot(face))
                        : null);
                return;
            }

            if (slotType == DiceFaceSlotType.Passive)
            {
                DiceFaceConfigurationSnapshot snapshot = loadout != null
                    ? loadout.GetSnapshot(face)
                    : default;
                passiveRuntime?.RebuildFace(
                    face,
                    entry != null ? entry.PassiveEffect : null,
                    GetBaseProjectileType(snapshot));
            }
        }

        private static ProjectileTypeDefinition GetBaseProjectileType(
            DiceFaceConfigurationSnapshot snapshot)
        {
            return snapshot.GetEffect(DiceFaceSlotType.Base) is ProjectileSpawnEffect spawnEffect
                ? spawnEffect.ProjectileDefinition?.ProjectileTypeDefinition
                : null;
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
                reloadBlinkRenderer.color =
                    Color.Lerp(reloadDimColor, reloadBlinkDefaultColor, blink);
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
