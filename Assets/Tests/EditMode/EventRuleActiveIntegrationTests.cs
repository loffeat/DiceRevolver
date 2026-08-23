using System;
using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DiceRevolver.Tests
{
    public sealed class EventRuleActiveIntegrationTests
    {
        private const string PlayerPrefabPath = "Assets/Prefab/Player.prefab";
        private readonly List<UnityEngine.Object> ownedObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = ownedObjects.Count - 1; index >= 0; index--)
            {
                if (ownedObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(ownedObjects[index]);
                }
            }

            ownedObjects.Clear();
        }

        [Test]
        public void RuleBackedEntryAndSnapshotSuppressTheirSerializedLegacyEffect()
        {
            EventRuleDefinition rule = CreateRule(Own(ScriptableObject.CreateInstance<CountingResult>()));
            RecordingEffect legacy = Own(ScriptableObject.CreateInstance<RecordingEffect>());
            DiceFaceEntry entry = CreateEntry(DiceFaceSlotType.OnFire, rule, legacy);
            DiceFaceConfiguration configuration = new DiceFaceConfiguration();

            Assert.That(configuration.Equip(entry), Is.True);
            DiceFaceConfigurationSnapshot snapshot = configuration.CreateSnapshot();

            Assert.That(entry.Rule, Is.SameAs(rule));
            Assert.That(entry.Effect, Is.Null, "a new Rule must suppress the legacy Effect");
            Assert.That(snapshot.GetRule(DiceFaceSlotType.OnFire), Is.SameAs(rule));
            Assert.That(snapshot.GetEffect(DiceFaceSlotType.OnFire), Is.Null);
        }

        [Test]
        public void BaseRuleSuppressesTheSeparateLegacyBaseEffectFallback()
        {
            EventRuleDefinition rule = CreateRule(Own(ScriptableObject.CreateInstance<CountingResult>()));
            RecordingEffect legacyBase = CreateEffect(null);
            DiceFaceEntry baseEntry = CreateEntry(DiceFaceSlotType.Base, rule);
            DiceFaceConfigurationSnapshot snapshot = new DiceFaceConfigurationSnapshot(
                baseEntry,
                null,
                null,
                null,
                legacyBase);

            Assert.That(snapshot.GetRule(DiceFaceSlotType.Base), Is.SameAs(rule));
            Assert.That(snapshot.GetEffect(DiceFaceSlotType.Base), Is.Null);
        }

        [Test]
        public void PipelineExecutesRuleOrLegacyExactlyOnceAcrossTwoShots()
        {
            int ruleExecutions = 0;
            int malformedLegacyExecutions = 0;
            int legacyOnlyExecutions = 0;
            EventRuleDefinition rule = CreateRule(Own(ScriptableObject.CreateInstance<CountingResult>()));
            RecordingEffect malformedLegacy = CreateEffect(() => malformedLegacyExecutions++);
            RecordingEffect legacyOnly = CreateEffect(() => legacyOnlyExecutions++);
            DiceFaceEntry ruleEntry = CreateEntry(DiceFaceSlotType.OnFire, rule, malformedLegacy);
            DiceFaceEntry legacyEntry = CreateEntry(DiceFaceSlotType.OnFire, null, legacyOnly);
            DiceShotPipeline pipeline = CreatePipeline();
            pipeline.ConfigureRuleExecution((face, slot, dispatchedRule, context) =>
            {
                Assert.That(face, Is.EqualTo(1));
                Assert.That(slot, Is.EqualTo(DiceFaceSlotType.OnFire));
                Assert.That(dispatchedRule, Is.SameAs(rule));
                ruleExecutions++;
                return true;
            });

            pipeline.ExecuteShot(1, Snapshot(ruleEntry), Vector3.zero, Vector3.forward, 32, null, null);
            pipeline.ExecuteShot(2, Snapshot(legacyEntry), Vector3.zero, Vector3.forward, 32, null, null);

            Assert.That(ruleExecutions, Is.EqualTo(1));
            Assert.That(malformedLegacyExecutions, Is.Zero);
            Assert.That(legacyOnlyExecutions, Is.EqualTo(1));
        }

        [Test]
        public void PipelineOverlayRuleExecutesWhenTheFaceSlotHasNoPersistentRule()
        {
            CountingResult overlayResult = Own(ScriptableObject.CreateInstance<CountingResult>());
            EventRuleDefinition overlayRule = CreateRule(overlayResult);
            DiceFaceConfigurationSnapshot overlaySnapshot = Snapshot(CreateEntry(
                DiceFaceSlotType.OnFire,
                overlayRule));
            DiceEventRuleRuntimeSet runtimes = new DiceEventRuleRuntimeSet();
            runtimes.RebuildFace(1, default);
            DiceShotPipeline pipeline = CreatePipeline();
            pipeline.ConfigureRuleExecution((face, slot, dispatchedRule, context) =>
                runtimes.ExecuteActive(
                    face,
                    slot,
                    dispatchedRule,
                    Signal(face, slot, context),
                    new BulletEventRuleServices(context, null)));

            DiceFaceActivation activation = pipeline.ExecuteShot(
                1,
                overlaySnapshot,
                Vector3.zero,
                Vector3.forward,
                4,
                null,
                null);

            Assert.That(overlayResult.ExecutionCount, Is.EqualTo(1));
            Assert.That(activation.RemainingEventBudget, Is.EqualTo(3));
        }

        [Test]
        public void SnapshotRuleMismatchUsesTransientStateWithoutReplacingPersistentRuntime()
        {
            StatefulResult persistentResult = Own(ScriptableObject.CreateInstance<StatefulResult>());
            StatefulResult snapshotResult = Own(ScriptableObject.CreateInstance<StatefulResult>());
            EventRuleDefinition persistentRule = CreateRule(persistentResult);
            EventRuleDefinition snapshotRule = CreateRule(snapshotResult);
            DiceFaceConfigurationSnapshot persistentSnapshot = Snapshot(CreateEntry(
                DiceFaceSlotType.OnFire,
                persistentRule));
            DiceFaceConfigurationSnapshot mismatchedSnapshot = Snapshot(CreateEntry(
                DiceFaceSlotType.OnFire,
                snapshotRule));
            DiceEventRuleRuntimeSet runtimes = new DiceEventRuleRuntimeSet();
            runtimes.RebuildFace(2, persistentSnapshot);
            DiceShotPipeline pipeline = CreatePipeline();
            pipeline.ConfigureRuleExecution((face, slot, dispatchedRule, context) =>
                runtimes.ExecuteActive(
                    face,
                    slot,
                    dispatchedRule,
                    Signal(face, slot, context),
                    new BulletEventRuleServices(context, null)));

            pipeline.ExecuteShot(
                2, mismatchedSnapshot, Vector3.zero, Vector3.forward, 4, null, null);
            pipeline.ExecuteShot(
                2, mismatchedSnapshot, Vector3.zero, Vector3.forward, 4, null, null);
            pipeline.ExecuteShot(
                2, persistentSnapshot, Vector3.zero, Vector3.forward, 4, null, null);

            Assert.That(snapshotResult.ObservedCounts, Is.EqualTo(new[] { 1, 1 }));
            Assert.That(persistentResult.ObservedCounts, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void GunExecutesRuleBackedOnFireOnceWithoutItsSerializedLegacyEffect()
        {
            GameObject playerInstance = UnityEngine.Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath));
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            DiceFaceLoadout loadout = playerInstance.GetComponent<DiceFaceLoadout>();
            CountingResult result = Own(ScriptableObject.CreateInstance<CountingResult>());
            EventRuleDefinition rule = CreateRule(result);
            int legacyCalls = 0;
            RecordingEffect legacy = CreateEffect(() => legacyCalls++);
            DiceFaceEntry entry = CreateEntry(DiceFaceSlotType.OnFire, rule, legacy);
            Mouse mouse = null;

            try
            {
                for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
                {
                    loadout.Equip(face, entry);
                }

                TopDownPlayerController player = playerInstance.GetComponent<TopDownPlayerController>();
                TopDownAimHandRig aimRig = playerInstance.GetComponentInChildren<TopDownAimHandRig>();
                InvokePrivate(player, "Awake");
                InvokePrivate(aimRig, "Awake");
                InvokePrivate(gun, "Awake");
                mouse = HoldLeftMouse();
                InvokePrivate(gun, "LateUpdate");

                Assert.That(result.ExecutionCount, Is.EqualTo(1));
                Assert.That(legacyCalls, Is.Zero);
            }
            finally
            {
                RemoveDevice(mouse);
                DestroyAllSceneProjectiles();
                UnityEngine.Object.DestroyImmediate(playerInstance);
            }
        }

        [Test]
        public void GunActiveSignalUsesRuntimeRemainingFacesForConditionBranches()
        {
            GameObject owner = Own(new GameObject("Rule Gun"));
            DiceRevolverGun gun = owner.AddComponent<DiceRevolverGun>();
            CountingResult result = Own(ScriptableObject.CreateInstance<CountingResult>());
            FaceAvailableConditionModule condition = Own(
                ScriptableObject.CreateInstance<FaceAvailableConditionModule>());
            Set(condition, "face", 4);
            EventRuleDefinition rule = CreateRule(result);
            Set(rule, "conditions", new List<EventConditionModule> { condition });
            DiceFaceConfigurationSnapshot snapshot = Snapshot(CreateEntry(
                DiceFaceSlotType.OnFire,
                rule));
            InvokePrivate(gun, "Awake");
            DiceRevolverRuntime runtime = new DiceRevolverRuntime(
                100f,
                2f,
                false,
                true,
                _ => 3);
            Set(gun, "runtime", runtime);
            GetPrivate<DiceEventRuleRuntimeSet>(gun, "eventRuleRuntimes").RebuildFace(1, snapshot);
            DiceFaceActivation activation = new DiceFaceActivation(
                1,
                snapshot,
                Vector3.zero,
                Vector3.forward,
                null,
                (Action<ProjectileSpawnRequest>)null,
                null,
                null);
            BulletEventContext context = new BulletEventContext(activation, null, null, Vector3.zero);

            Assert.That(InvokePrivate<bool>(
                gun,
                "ExecuteActiveRule",
                1,
                DiceFaceSlotType.OnFire,
                rule,
                context), Is.True);
            Assert.That(result.ExecutionCount, Is.Zero,
                "Face 4 is still in the real chamber and must stop later Results.");

            DiceRevolverDrawResult consumed = runtime.TryBeginShot(0f);
            Assert.That(consumed.Face, Is.EqualTo(4));
            Assert.That(InvokePrivate<bool>(
                gun,
                "ExecuteActiveRule",
                1,
                DiceFaceSlotType.OnFire,
                rule,
                context), Is.True);
            Assert.That(result.ExecutionCount, Is.EqualTo(1),
                "After face 4 is consumed the condition must allow the later Result.");
        }

        [Test]
        public void RebuildFacePreservesSameDefinitionsAndDoesNotResetAnotherFace()
        {
            StatefulResult faceOneResult = Own(ScriptableObject.CreateInstance<StatefulResult>());
            StatefulResult faceOneReplacementResult = Own(ScriptableObject.CreateInstance<StatefulResult>());
            StatefulResult faceOneStableResult = Own(ScriptableObject.CreateInstance<StatefulResult>());
            StatefulResult faceTwoResult = Own(ScriptableObject.CreateInstance<StatefulResult>());
            DiceFaceEntry faceOneStableEntry = CreateEntry(
                DiceFaceSlotType.OnHit,
                CreateRule(faceOneStableResult));
            DiceFaceConfigurationSnapshot faceOne = new DiceFaceConfigurationSnapshot(
                null,
                CreateEntry(
                    DiceFaceSlotType.OnFire,
                    CreateRule(faceOneResult)),
                faceOneStableEntry,
                null);
            DiceFaceConfigurationSnapshot faceOneReplacement = new DiceFaceConfigurationSnapshot(
                null,
                CreateEntry(
                    DiceFaceSlotType.OnFire,
                    CreateRule(faceOneReplacementResult)),
                faceOneStableEntry,
                null);
            DiceFaceConfigurationSnapshot faceTwo = Snapshot(CreateEntry(
                DiceFaceSlotType.OnFire,
                CreateRule(faceTwoResult)));
            DiceEventRuleRuntimeSet runtimes = new DiceEventRuleRuntimeSet();
            FakeRuleServices services = new FakeRuleServices();

            runtimes.RebuildFace(1, faceOne);
            runtimes.RebuildFace(2, faceTwo);
            Assert.That(runtimes.ExecuteActive(1, DiceFaceSlotType.OnFire, Signal(1), services), Is.True);
            Assert.That(runtimes.ExecuteActive(
                1, DiceFaceSlotType.OnHit, Signal(1, DiceFaceSlotType.OnHit), services), Is.True);
            Assert.That(runtimes.ExecuteActive(2, DiceFaceSlotType.OnFire, Signal(2), services), Is.True);

            runtimes.RebuildFace(1, faceOne);
            Assert.That(runtimes.ExecuteActive(1, DiceFaceSlotType.OnFire, Signal(1), services), Is.True);

            runtimes.RebuildFace(1, faceOneReplacement);
            Assert.That(runtimes.ExecuteActive(1, DiceFaceSlotType.OnFire, Signal(1), services), Is.True);
            Assert.That(runtimes.ExecuteActive(
                1, DiceFaceSlotType.OnHit, Signal(1, DiceFaceSlotType.OnHit), services), Is.True);
            Assert.That(runtimes.ExecuteActive(2, DiceFaceSlotType.OnFire, Signal(2), services), Is.True);
            Assert.That(runtimes.ExecuteActive(3, DiceFaceSlotType.OnFire, Signal(3), services), Is.False);

            Assert.That(faceOneResult.ObservedCounts, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(faceOneReplacementResult.ObservedCounts, Is.EqualTo(new[] { 1 }));
            Assert.That(faceOneStableResult.ObservedCounts, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(faceTwoResult.ObservedCounts, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void BulletServicesDelegateThroughTheOriginatingActivationPorts()
        {
            float scheduledDelay = -1f;
            Action scheduledCallback = null;
            ProjectileSpawnRequest spawn = default;
            int forcedFace = 0;
            bool chainRequested = false;
            bool overlayQueued = false;
            List<(Exception Exception, UnityEngine.Object Context)> exceptions = new();
            DiceEventBudget budget = new DiceEventBudget(12);
            DiceFaceActivation activation = new DiceFaceActivation(
                4,
                default,
                Vector3.zero,
                Vector3.forward,
                (delay, callback) =>
                {
                    scheduledDelay = delay;
                    scheduledCallback = callback;
                },
                request =>
                {
                    spawn = request;
                    return default;
                },
                face =>
                {
                    forcedFace = face;
                    return true;
                },
                null,
                budget,
                false,
                0);
            OwnedProjectileRegistry registry = new OwnedProjectileRegistry();
            GameObject projectileOwner = Own(new GameObject("Owned Projectile"));
            Projectile projectile = projectileOwner.AddComponent<Projectile>();
            ProjectileTagDefinition tag = Own(ScriptableObject.CreateInstance<ProjectileTagDefinition>());
            ProjectileRuntimeStats stats = new ProjectileRuntimeStats(
                "Type", "Tag", null, new[] { tag }, 1f, 2f, 3f, 0);
            ProjectileHandle handle = registry.Register(projectile, stats);
            LightningChainDefinition chain = Own(ScriptableObject.CreateInstance<LightningChainDefinition>());
            activation.ConfigureLightningServices(registry, (origin, targets, definition) =>
            {
                chainRequested = origin.Projectile == projectile &&
                    targets.Count == 1 &&
                    definition == chain;
                return chainRequested;
            });
            activation.ConfigureOverlayService(_ => overlayQueued = true);
            CombatDebugTrace trace = new CombatDebugTrace();
            activation.ConfigureDebugScope(
                trace,
                trace.BeginActivation(4, false, default, 3f),
                () => 3f);
            BulletEventContext context = new BulletEventContext(
                activation,
                null,
                null,
                new Vector3(8f, 0f, 9f));
            BulletEventRuleServices services = new BulletEventRuleServices(
                context,
                (exception, exceptionContext) => exceptions.Add((exception, exceptionContext)));
            ProjectileDefinition definition = Own(ScriptableObject.CreateInstance<ProjectileDefinition>());
            DiceFaceEntry overlayEntry = CreateEntry(DiceFaceSlotType.OnFire, null, CreateEffect(null));
            bool callbackRan = false;

            Assert.That(services.EventBudget, Is.SameAs(budget));
            Assert.That(services.RequestProjectile(
                definition,
                new Vector3(2f, 0f, 3f),
                Vector3.right,
                AttackEffectOverride.ForceDisabled,
                true), Is.True);
            Assert.That(spawn.Definition, Is.SameAs(definition));
            Assert.That(spawn.Origin, Is.EqualTo(new Vector3(2f, 0f, 3f)));
            Assert.That(spawn.Direction, Is.EqualTo(Vector3.right));
            Assert.That(budget.Remaining, Is.EqualTo(11));
            Assert.That(services.Schedule(0.25f, () => callbackRan = true), Is.True);
            Assert.That(scheduledDelay, Is.EqualTo(0.25f));
            scheduledCallback.Invoke();
            Assert.That(callbackRan, Is.True);
            Assert.That(services.RequestRefillAndForceNextFace(6), Is.True);
            Assert.That(forcedFace, Is.EqualTo(6));
            Assert.That(services.RequestLightningChain(handle, new[] { handle }, chain), Is.True);
            Assert.That(chainRequested, Is.True);
            Assert.That(services.QueueNextShotOverlay(
                new DiceFaceActiveOverlay(null, overlayEntry, null, null)), Is.True);
            Assert.That(overlayQueued, Is.True);
            Assert.That(services.FindOwnedProjectiles(
                Vector3.zero, 1f, tag, null), Has.Count.EqualTo(1));
            Assert.That(services.RequestBonusActivation(1, 8f, 2f, null), Is.False);

            EventRuleDefinition debugRule = CreateRule(Own(ScriptableObject.CreateInstance<CountingResult>()));
            int recordsBeforeRuleDebug = trace.Records.Count;
            services.RecordRuleDebug(
                debugRule, "result", "recorded", EventResultStatus.Success);
            Assert.That(trace.Records, Has.Count.EqualTo(recordsBeforeRuleDebug + 1));
            Assert.That(
                trace.Records[trace.Records.Count - 1].Detail,
                Is.EqualTo("result: Success - recorded"));

            InvalidOperationException expected = new InvalidOperationException("expected");
            services.ReportException(expected, debugRule.Trigger);
            Assert.That(exceptions, Has.Count.EqualTo(1));
            Assert.That(exceptions[0].Exception, Is.SameAs(expected));
            Assert.That(exceptions[0].Context, Is.SameAs(debugRule.Trigger));

            Assert.DoesNotThrow(() =>
            {
                services.SetDrawPriority(9);
                services.RejectDrawCandidate("active adapter");
                services.MultiplyProjectileDamage(4f);
            });
        }

        private DiceShotPipeline CreatePipeline()
        {
            return new DiceShotPipeline(() => 0f, (Action<DiceFaceActivation, ProjectileSpawnRequest>)null,
                null, null, null);
        }

        private DiceFaceConfigurationSnapshot Snapshot(DiceFaceEntry onFireEntry)
        {
            return new DiceFaceConfigurationSnapshot(null, onFireEntry, null, null);
        }

        private DiceFaceEntry CreateEntry(
            DiceFaceSlotType slot,
            EventRuleDefinition rule,
            BulletEventEffect legacyEffect = null)
        {
            DiceFaceEntry entry = Own(ScriptableObject.CreateInstance<DiceFaceEntry>());
            Set(entry, "slotType", slot);
            Set(entry, "rule", rule);
            Set(entry, "effect", legacyEffect);
            return entry;
        }

        private EventRuleDefinition CreateRule(EventResultModule result)
        {
            EventRuleDefinition rule = Own(ScriptableObject.CreateInstance<EventRuleDefinition>());
            MatchingTrigger trigger = Own(ScriptableObject.CreateInstance<MatchingTrigger>());
            Set(rule, "allowedSlots", DiceFaceSlotMask.OnFire);
            Set(rule, "trigger", trigger);
            Set(rule, "results", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), result)
            });
            return rule;
        }

        private RecordingEffect CreateEffect(Action action)
        {
            RecordingEffect effect = Own(ScriptableObject.CreateInstance<RecordingEffect>());
            effect.Action = action;
            return effect;
        }

        private static EventSignal Signal(
            int face,
            DiceFaceSlotType slot = DiceFaceSlotType.OnFire)
        {
            return new EventSignal(
                slot == DiceFaceSlotType.OnHit ? EventSignalType.OnHit : EventSignalType.OnFire,
                face,
                face,
                slot,
                null,
                null,
                default,
                null,
                default,
                Array.Empty<int>(),
                0,
                default,
                null,
                false,
                default);
        }

        private static EventSignal Signal(
            int face,
            DiceFaceSlotType slot,
            BulletEventContext context)
        {
            DiceFaceActivation activation = context.Activation;
            DiceRevolverShotContext shot = context.Shot;
            return new EventSignal(
                slot == DiceFaceSlotType.OnHit ? EventSignalType.OnHit : EventSignalType.OnFire,
                face,
                shot != null ? shot.Face : face,
                slot,
                activation,
                shot,
                context.PrimaryProjectile,
                context.HitCollider,
                context.HitPosition,
                Array.Empty<int>(),
                0,
                shot != null ? shot.Stats : default,
                activation?.EventBudget,
                activation != null && activation.IsBonusActivation,
                activation != null ? activation.DebugScope : default);
        }

        private T Own<T>(T value) where T : UnityEngine.Object
        {
            ownedObjects.Add(value);
            return value;
        }

        private static Mouse HoldLeftMouse()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(
                mouse,
                new MouseState().WithButton(MouseButton.Left));
            InputSystem.Update();
            return mouse;
        }

        private static void RemoveDevice(InputDevice device)
        {
            if (device != null && device.added)
            {
                InputSystem.RemoveDevice(device);
            }
        }

        private static void DestroyAllSceneProjectiles()
        {
            Projectile[] projectiles = UnityEngine.Object.FindObjectsByType<Projectile>(
                FindObjectsSortMode.None);
            for (int index = 0; index < projectiles.Length; index++)
            {
                if (projectiles[index] != null && projectiles[index].gameObject.scene.IsValid())
                {
                    UnityEngine.Object.DestroyImmediate(projectiles[index].gameObject);
                }
            }
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {target.GetType().Name}.{methodName}");
            method.Invoke(target, null);
        }

        private static T InvokePrivate<T>(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {target.GetType().Name}.{methodName}");
            return (T)method.Invoke(target, arguments);
        }

        private static T GetPrivate<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void Set(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private sealed class MatchingTrigger : EventTriggerModule
        {
            public override bool Matches(EventSignal signal) => true;
        }

        private sealed class CountingResult : EventResultModule
        {
            public int ExecutionCount { get; private set; }

            public override EventResult Execute(EventExecutionContext context)
            {
                ExecutionCount++;
                return new EventResult(EventResultStatus.Success, "counted");
            }
        }

        private sealed class StatefulResult : EventResultModule
        {
            public List<int> ObservedCounts { get; } = new();

            public override EventResult Execute(EventExecutionContext context)
            {
                int next = context.State.GetInt(this, "count") + 1;
                context.State.SetInt(this, "count", next);
                ObservedCounts.Add(next);
                return new EventResult(EventResultStatus.Success, "stateful");
            }
        }

        private sealed class RecordingEffect : BulletEventEffect
        {
            public Action Action { get; set; }

            public override void Trigger(BulletEventContext context)
            {
                Action?.Invoke();
            }
        }

        private sealed class FakeRuleServices : IEventRuleServices
        {
            public DiceEventBudget EventBudget => null;
            public RoundProjectileStatistic RoundProjectileStatistic => null;
            public bool RequestProjectile(ProjectileDefinition definition, Vector3 origin,
                Vector3 direction, AttackEffectOverride attackEffectOverride, bool isPrimary) => false;
            public bool Schedule(float delaySeconds, Action callback) => false;
            public bool RequestBonusActivation(int face, float maximumSpreadAngle,
                float minimumSpreadSeparation, EventRuleDefinition sourceRule) => false;
            public bool RequestRefillAndForceNextFace(int face) => false;
            public bool RequestLightningChain(ProjectileHandle origin,
                IReadOnlyList<ProjectileHandle> targets, LightningChainDefinition definition) => false;
            public bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay) => false;
            public IReadOnlyList<ProjectileHandle> FindOwnedProjectiles(Vector3 origin, float radius,
                ProjectileTagDefinition requiredTag, Projectile excludedProjectile) =>
                Array.Empty<ProjectileHandle>();
            public void SetDrawPriority(int priority) { }
            public void RejectDrawCandidate(string reason) { }
            public void MultiplyProjectileDamage(float multiplier) { }
            public void RecordRuleDebug(EventRuleDefinition rule, string stage,
                string description, EventResultStatus status) { }
            public void ReportException(Exception exception, ScriptableObject module) { }
        }
    }
}
