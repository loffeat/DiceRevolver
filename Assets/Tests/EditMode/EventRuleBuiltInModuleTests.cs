using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class EventRuleBuiltInModuleTests
    {
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
        public void SignalTriggerMatchesOnlySignalsIncludedInItsMask()
        {
            SignalTypeTriggerModule trigger = Own(ScriptableObject.CreateInstance<SignalTypeTriggerModule>());
            Set(trigger, "signals", EventSignalMask.Base | EventSignalMask.OnHit);

            Assert.That(trigger.Matches(Signal(EventSignalType.Base)), Is.True);
            Assert.That(trigger.Matches(Signal(EventSignalType.OnHit)), Is.True);
            Assert.That(trigger.Matches(Signal(EventSignalType.OnFire)), Is.False);
            Assert.That(trigger.Matches(Signal(EventSignalType.ProjectileHit)), Is.False);
        }

        [Test]
        public void ProjectileTypeConditionUsesDefinitionReferenceIdentity()
        {
            ProjectileTypeDefinition expected = Own(ScriptableObject.CreateInstance<ProjectileTypeDefinition>());
            ProjectileTypeDefinition sameName = Own(ScriptableObject.CreateInstance<ProjectileTypeDefinition>());
            expected.name = "Lightning";
            sameName.name = "Lightning";
            ProjectileTypeConditionModule condition = Own(
                ScriptableObject.CreateInstance<ProjectileTypeConditionModule>());
            Set(condition, "projectileType", expected);

            Assert.That(condition.Evaluate(Context(Signal(
                stats: Stats(expected, Array.Empty<ProjectileTagDefinition>())))).Passed, Is.True);
            Assert.That(condition.Evaluate(Context(Signal(
                stats: Stats(sameName, Array.Empty<ProjectileTagDefinition>())))).Passed, Is.False);
        }

        [Test]
        public void ProjectileTagConditionUsesRuntimeStatsTags()
        {
            ProjectileTagDefinition expected = Own(ScriptableObject.CreateInstance<ProjectileTagDefinition>());
            ProjectileTagDefinition other = Own(ScriptableObject.CreateInstance<ProjectileTagDefinition>());
            ProjectileTagConditionModule condition = Own(
                ScriptableObject.CreateInstance<ProjectileTagConditionModule>());
            Set(condition, "projectileTag", expected);

            Assert.That(condition.Evaluate(Context(Signal(
                stats: Stats(null, new[] { expected })))).Passed, Is.True);
            Assert.That(condition.Evaluate(Context(Signal(
                stats: Stats(null, new[] { other })))).Passed, Is.False);
        }

        [Test]
        public void AttackEffectConditionComparesTheShotFlag()
        {
            AttackEffectConditionModule condition = Own(
                ScriptableObject.CreateInstance<AttackEffectConditionModule>());
            Set(condition, "expectedCanTriggerHitEffects", true);

            Assert.That(condition.Evaluate(Context(Signal(
                shot: Shot(canTriggerHitEffects: true)))).Passed, Is.True);
            Assert.That(condition.Evaluate(Context(Signal(
                shot: Shot(canTriggerHitEffects: false)))).Passed, Is.False);
            Assert.That(condition.Evaluate(Context(Signal(shot: null))).Passed, Is.False);
        }

        [Test]
        public void FaceAvailableConditionPassesOnlyWhenRequestedFaceIsAbsent()
        {
            FaceAvailableConditionModule condition = Own(
                ScriptableObject.CreateInstance<FaceAvailableConditionModule>());
            Set(condition, "face", 4);

            Assert.That(condition.Evaluate(Context(Signal(remainingFaces: new[] { 1, 2, 6 }))).Passed,
                Is.True);
            Assert.That(condition.Evaluate(Context(Signal(remainingFaces: new[] { 1, 4, 6 }))).Passed,
                Is.False);
        }

        [Test]
        public void OwnedProjectileCountConditionUsesTheRestrictedSameGunQueryAndAtLeastBoundary()
        {
            ProjectileTagDefinition tag = Own(ScriptableObject.CreateInstance<ProjectileTagDefinition>());
            ProjectileHandle origin = Handle("Origin", Stats(null, new[] { tag }), new Vector3(2f, 0f, 3f));
            ProjectileHandle first = Handle("First", Stats(null, new[] { tag }), Vector3.zero);
            ProjectileHandle second = Handle("Second", Stats(null, new[] { tag }), Vector3.zero);
            FakeServices services = new FakeServices { OwnedProjectiles = new[] { first, second } };
            OwnedProjectileCountConditionModule condition = Own(
                ScriptableObject.CreateInstance<OwnedProjectileCountConditionModule>());
            Set(condition, "projectileTag", tag);
            Set(condition, "searchRadius", 6f);
            Set(condition, "atLeast", 2);

            EventConditionResult result = condition.Evaluate(Context(
                Signal(projectile: origin), services));

            Assert.That(result.Passed, Is.True);
            Assert.That(services.FindOrigin, Is.EqualTo(new Vector3(2f, 0f, 3f)));
            Assert.That(services.FindRadius, Is.EqualTo(6f));
            Assert.That(services.FindTag, Is.SameAs(tag));
            Assert.That(services.FindExcluded, Is.SameAs(origin.Projectile));

            services.OwnedProjectiles = new[] { first };
            Assert.That(condition.Evaluate(Context(Signal(projectile: origin), services)).Passed,
                Is.False);
        }

        [Test]
        public void SpawnProjectileSupportsExplicitDefinitionHitOriginDelayOverrideAndPrimaryFlag()
        {
            ProjectileDefinition definition = Own(ScriptableObject.CreateInstance<ProjectileDefinition>());
            FakeServices services = new FakeServices();
            SpawnProjectileResultModule result = Own(
                ScriptableObject.CreateInstance<SpawnProjectileResultModule>());
            Set(result, "projectileDefinition", definition);
            Set(result, "useHitOrigin", true);
            Set(result, "delaySeconds", 0.25f);
            Set(result, "attackEffectOverride", AttackEffectOverride.ForceDisabled);
            Set(result, "primaryProjectile", true);
            EventSignal signal = Signal(
                shot: Shot(Vector3.right),
                hitPosition: new Vector3(7f, 0f, 9f));

            EventResult execution = result.Execute(ExecutionContext(signal, services));

            Assert.That(execution.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(services.ScheduledDelay, Is.EqualTo(0.25f));
            Assert.That(services.ProjectileRequests, Is.Empty);

            services.ScheduledCallback.Invoke();
            Assert.That(services.ProjectileRequests, Has.Count.EqualTo(1));
            ProjectileRequest request = services.ProjectileRequests[0];
            Assert.That(request.Definition, Is.SameAs(definition));
            Assert.That(request.Origin, Is.EqualTo(new Vector3(7f, 0f, 9f)));
            Assert.That(request.Direction, Is.EqualTo(Vector3.right));
            Assert.That(request.AttackEffectOverride, Is.EqualTo(AttackEffectOverride.ForceDisabled));
            Assert.That(request.IsPrimary, Is.True);
        }

        [Test]
        public void SpawnProjectileCanResolveTheCurrentPrimaryDefinition()
        {
            ProjectileDefinition primary = Own(ScriptableObject.CreateInstance<ProjectileDefinition>());
            DiceFaceActivation activation = Activation(Vector3.forward);
            Assert.That(activation.RequestProjectile(
                primary,
                AttackEffectOverride.UseProjectileDefault,
                true,
                Vector3.zero,
                Vector3.forward), Is.True);
            FakeServices services = new FakeServices();
            SpawnProjectileResultModule result = Own(
                ScriptableObject.CreateInstance<SpawnProjectileResultModule>());
            Set(result, "useCurrentPrimaryDefinition", true);

            EventResult execution = result.Execute(ExecutionContext(
                Signal(activation: activation, shot: Shot(Vector3.forward)), services));

            Assert.That(execution.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(services.ProjectileRequests.Single().Definition, Is.SameAs(primary));
        }

        [Test]
        public void ForceFaceRequestsRefillAndForceExactlyOnce()
        {
            FakeServices services = new FakeServices { RefillAndForceAccepted = true };
            ForceFaceResultModule result = Own(ScriptableObject.CreateInstance<ForceFaceResultModule>());
            Set(result, "face", 4);

            EventResult execution = result.Execute(ExecutionContext(Signal(), services));

            Assert.That(execution.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(services.ForcedFaces, Is.EqualTo(new[] { 4 }));
        }

        [Test]
        public void LightningChainSelectsNoMoreThanConfiguredAliveNearbyProjectiles()
        {
            ProjectileTagDefinition tag = Own(ScriptableObject.CreateInstance<ProjectileTagDefinition>());
            LightningChainDefinition chain = Own(ScriptableObject.CreateInstance<LightningChainDefinition>());
            ProjectileHandle origin = Handle("Origin", Stats(null, new[] { tag }), Vector3.zero);
            ProjectileHandle first = Handle("First", Stats(null, new[] { tag }), Vector3.right);
            ProjectileHandle second = Handle("Second", Stats(null, new[] { tag }), Vector3.left);
            ProjectileHandle destroyed = Handle("Destroyed", Stats(null, new[] { tag }), Vector3.forward);
            UnityEngine.Object.DestroyImmediate(destroyed.Projectile.gameObject);
            FakeServices services = new FakeServices
            {
                OwnedProjectiles = new[] { first, destroyed, second },
                LightningAccepted = true
            };
            CreateLightningChainResultModule result = Own(
                ScriptableObject.CreateInstance<CreateLightningChainResultModule>());
            Set(result, "lightningTag", tag);
            Set(result, "chainDefinition", chain);
            Set(result, "searchRadius", 6f);
            Set(result, "maximumConnections", 1);

            EventResult execution = result.Execute(ExecutionContext(
                Signal(projectile: origin, stats: origin.Stats), services));

            Assert.That(execution.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(services.LightningRequests, Has.Count.EqualTo(1));
            LightningRequest request = services.LightningRequests[0];
            Assert.That(request.Origin.Projectile, Is.SameAs(origin.Projectile));
            Assert.That(request.Definition, Is.SameAs(chain));
            Assert.That(request.Targets.Count, Is.EqualTo(1));
            Assert.That(request.Targets[0].IsAlive, Is.True);
            Assert.That(new[] { first.Projectile, second.Projectile }, Does.Contain(request.Targets[0].Projectile));
        }

        [Test]
        public void QueueOverlayCopiesNonEmptyReusableActiveSlotsAndExcludesSourceOnFireEnd()
        {
            DiceFaceEntry baseEntry = Entry(DiceFaceSlotType.Base);
            DiceFaceEntry onHitEntry = Entry(DiceFaceSlotType.OnHit);
            DiceFaceEntry onFireEndEntry = Entry(DiceFaceSlotType.OnFireEnd);
            DiceFaceConfigurationSnapshot snapshot = new DiceFaceConfigurationSnapshot(
                baseEntry,
                null,
                onHitEntry,
                onFireEndEntry);
            DiceFaceActivation activation = Activation(Vector3.forward, snapshot);
            FakeServices services = new FakeServices { OverlayAccepted = true };
            QueueActiveOverlayResultModule result = Own(
                ScriptableObject.CreateInstance<QueueActiveOverlayResultModule>());

            EventResult execution = result.Execute(ExecutionContext(
                Signal(EventSignalType.OnFireEnd, activation: activation), services));

            Assert.That(execution.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(services.QueuedOverlays, Has.Count.EqualTo(1));
            DiceFaceActiveOverlay overlay = services.QueuedOverlays[0];
            Assert.That(overlay.BaseEntry, Is.SameAs(baseEntry));
            Assert.That(overlay.OnFireEntry, Is.Null);
            Assert.That(overlay.OnHitEntry, Is.SameAs(onHitEntry));
            Assert.That(overlay.OnFireEndEntry, Is.Null);
        }

        [Test]
        public void DelayExecutesNestedEntriesOnlyFromSchedulerWithTheSameStateAndBudget()
        {
            SignalTypeTriggerModule trigger = Own(ScriptableObject.CreateInstance<SignalTypeTriggerModule>());
            Set(trigger, "signals", EventSignalMask.OnFire);
            DelayResultModule delay = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            StateCaptureResult first = Own(ScriptableObject.CreateInstance<StateCaptureResult>());
            StateCaptureResult second = Own(ScriptableObject.CreateInstance<StateCaptureResult>());
            List<int> order = new List<int>();
            first.Identifier = 1;
            first.Order = order;
            first.StateOwner = delay;
            second.Identifier = 2;
            second.Order = order;
            second.StateOwner = delay;
            Set(delay, "delaySeconds", -1f);
            Set(delay, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), first),
                new EventResultEntry(Array.Empty<EventConditionModule>(), second)
            });
            EventRuleDefinition rule = Rule(
                DiceFaceSlotMask.OnFire,
                trigger,
                new EventResultEntry(Array.Empty<EventConditionModule>(), delay));
            DiceEventBudget budget = new DiceEventBudget(5);
            EventSignal signal = Signal(EventSignalType.OnFire, eventBudget: budget);
            FakeServices services = new FakeServices();

            EventRuleInvocationResult invocation = new EventRuleRuntime(
                rule, 2, DiceFaceSlotType.OnFire).TryHandle(signal, services);

            Assert.That(invocation.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(services.ScheduledDelay, Is.Zero);
            Assert.That(order, Is.Empty);
            Assert.That(budget.Remaining, Is.EqualTo(4));

            services.ScheduledCallback.Invoke();
            Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(first.ObservedState, Is.SameAs(second.ObservedState));
            Assert.That(first.ObservedBudget, Is.SameAs(budget));
            Assert.That(second.ObservedBudget, Is.SameAs(budget));
            Assert.That(second.ObservedCounter, Is.EqualTo(2));
            Assert.That(budget.Remaining, Is.EqualTo(4));
        }

        [Test]
        public void PrimaryProjectileLookupWalksOrderedAndNestedResultsWithoutExecutingThem()
        {
            ProjectileDefinition nonPrimary = Own(ScriptableObject.CreateInstance<ProjectileDefinition>());
            ProjectileDefinition nestedPrimary = Own(ScriptableObject.CreateInstance<ProjectileDefinition>());
            ProjectileDefinition laterPrimary = Own(ScriptableObject.CreateInstance<ProjectileDefinition>());
            SpawnProjectileResultModule first = Spawn(nonPrimary, false);
            SpawnProjectileResultModule nested = Spawn(nestedPrimary, true);
            SpawnProjectileResultModule later = Spawn(laterPrimary, true);
            DelayResultModule delay = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            Set(delay, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), nested)
            });
            SignalTypeTriggerModule trigger = Own(ScriptableObject.CreateInstance<SignalTypeTriggerModule>());
            EventRuleDefinition rule = Rule(
                DiceFaceSlotMask.Base,
                trigger,
                new EventResultEntry(Array.Empty<EventConditionModule>(), first),
                new EventResultEntry(Array.Empty<EventConditionModule>(), delay),
                new EventResultEntry(Array.Empty<EventConditionModule>(), later));

            Assert.That(rule.FindPrimaryProjectileDefinition(), Is.SameAs(nestedPrimary));
        }

        [Test]
        public void CyclicDelayResultsAreRejectedBeforeScheduling()
        {
            DelayResultModule self = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            Set(self, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), self)
            });
            DelayResultModule first = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            DelayResultModule second = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            Set(first, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), second)
            });
            Set(second, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), first)
            });
            FakeServices selfServices = new FakeServices();
            FakeServices mutualServices = new FakeServices();

            EventResult selfResult = self.Execute(ExecutionContext(Signal(), selfServices));
            EventResult mutualResult = first.Execute(ExecutionContext(Signal(), mutualServices));
            new EventRuleRuntime(
                OnFireRule(self),
                2,
                DiceFaceSlotType.OnFire).TryHandle(Signal(), selfServices);
            new EventRuleRuntime(
                OnFireRule(first),
                2,
                DiceFaceSlotType.OnFire).TryHandle(Signal(), mutualServices);

            Assert.That(selfResult.Status, Is.Not.EqualTo(EventResultStatus.Success));
            Assert.That(selfResult.Description, Does.Contain("循环"));
            Assert.That(mutualResult.Status, Is.Not.EqualTo(EventResultStatus.Success));
            Assert.That(mutualResult.Description, Does.Contain("循环"));
            Assert.That(selfServices.ScheduleCallCount, Is.Zero);
            Assert.That(mutualServices.ScheduleCallCount, Is.Zero);
        }

        [Test]
        public void CyclicProviderTraversalAndValidationTerminateForSelfAndMutualReferences()
        {
            DelayResultModule self = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            Set(self, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), self)
            });
            DelayResultModule first = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            DelayResultModule second = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            Set(first, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), second)
            });
            Set(second, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), first)
            });
            EventRuleDefinition selfRule = OnFireRule(self);
            EventRuleDefinition mutualRule = OnFireRule(first);

            Assert.That(selfRule.FindPrimaryProjectileDefinition(), Is.Null);
            Assert.That(mutualRule.FindPrimaryProjectileDefinition(), Is.Null);
            Assert.That(selfRule.CollectValidationIssues(DiceFaceSlotType.OnFire).Any(
                issue => issue.Code == "delayed-result-cycle"), Is.True);
            Assert.That(mutualRule.CollectValidationIssues(DiceFaceSlotType.OnFire).Any(
                issue => issue.Code == "delayed-result-cycle"), Is.True);
        }

        [Test]
        public void SharedAcyclicDelaySubgraphRemainsValidAndProvidesItsPrimaryProjectile()
        {
            ProjectileDefinition definition = Own(ScriptableObject.CreateInstance<ProjectileDefinition>());
            DelayResultModule shared = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            Set(shared, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), Spawn(definition, true))
            });
            DelayResultModule first = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            DelayResultModule second = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            Set(first, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), shared)
            });
            Set(second, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), shared)
            });
            DelayResultModule root = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            Set(root, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), first),
                new EventResultEntry(Array.Empty<EventConditionModule>(), second)
            });
            EventRuleDefinition rule = OnFireRule(root);
            FakeServices services = new FakeServices();

            new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire).TryHandle(Signal(), services);

            Assert.That(services.ScheduleCallCount, Is.EqualTo(1));
            Assert.That(rule.FindPrimaryProjectileDefinition(), Is.SameAs(definition));
            Assert.That(rule.CollectValidationIssues(DiceFaceSlotType.OnFire).Any(
                issue => issue.Code == "delayed-result-cycle"), Is.False);
        }

        [Test]
        public void NestedEmptyDelayStillReportsItsMissingResults()
        {
            DelayResultModule empty = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            DelayResultModule root = Own(ScriptableObject.CreateInstance<DelayResultModule>());
            Set(root, "entries", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), empty)
            });

            List<EventRuleValidationIssue> issues =
                OnFireRule(root).CollectValidationIssues(DiceFaceSlotType.OnFire);

            Assert.That(issues.Any(issue => issue.Code == "missing-delayed-results"), Is.True);
        }

        [Test]
        public void BaseRuleWithoutResolvablePrimaryProjectileReportsValidationError()
        {
            SignalTypeTriggerModule trigger = Own(ScriptableObject.CreateInstance<SignalTypeTriggerModule>());
            ForceFaceResultModule force = Own(ScriptableObject.CreateInstance<ForceFaceResultModule>());
            EventRuleDefinition rule = Rule(
                DiceFaceSlotMask.Base,
                trigger,
                new EventResultEntry(Array.Empty<EventConditionModule>(), force));

            List<EventRuleValidationIssue> issues = rule.CollectValidationIssues(DiceFaceSlotType.Base);

            Assert.That(issues.Any(issue =>
                issue.Severity == EventRuleValidationSeverity.Error &&
                issue.Code == "missing-primary-projectile"), Is.True);
        }

        [Test]
        public void ResultsWithMissingReferencesSkipWithDescriptionsInsteadOfThrowing()
        {
            SpawnProjectileResultModule spawn = Own(
                ScriptableObject.CreateInstance<SpawnProjectileResultModule>());
            CreateLightningChainResultModule chain = Own(
                ScriptableObject.CreateInstance<CreateLightningChainResultModule>());
            QueueActiveOverlayResultModule overlay = Own(
                ScriptableObject.CreateInstance<QueueActiveOverlayResultModule>());
            DelayResultModule delay = Own(ScriptableObject.CreateInstance<DelayResultModule>());

            EventResult[] results =
            {
                spawn.Execute(ExecutionContext(Signal(), new FakeServices())),
                chain.Execute(ExecutionContext(Signal(), new FakeServices())),
                overlay.Execute(ExecutionContext(Signal(), new FakeServices())),
                delay.Execute(ExecutionContext(Signal(), new FakeServices()))
            };

            Assert.That(results.Select(result => result.Status),
                Is.All.EqualTo(EventResultStatus.Skipped));
            Assert.That(results.Select(result => result.Description),
                Is.All.Not.Null.And.Not.Empty);
        }

        [Test]
        public void EveryBuiltInModuleIsConcreteDiscoverableAndLabelsItsSerializedFieldsInChinese()
        {
            Type[] moduleTypes =
            {
                typeof(SignalTypeTriggerModule),
                typeof(ProjectileTypeConditionModule),
                typeof(ProjectileTagConditionModule),
                typeof(AttackEffectConditionModule),
                typeof(FaceAvailableConditionModule),
                typeof(OwnedProjectileCountConditionModule),
                typeof(SpawnProjectileResultModule),
                typeof(ForceFaceResultModule),
                typeof(CreateLightningChainResultModule),
                typeof(QueueActiveOverlayResultModule),
                typeof(DelayResultModule)
            };

            foreach (Type moduleType in moduleTypes)
            {
                Assert.That(moduleType.IsAbstract, Is.False, moduleType.Name);
                EventRuleModuleMenuAttribute menu = moduleType.GetCustomAttribute<EventRuleModuleMenuAttribute>();
                Assert.That(menu, Is.Not.Null, moduleType.Name);
                Assert.That(menu.Path.Any(character => character >= '\u4e00' && character <= '\u9fff'),
                    Is.True, moduleType.Name);

                FieldInfo[] serializedFields = moduleType.GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                    .ToArray();
                foreach (FieldInfo field in serializedFields)
                {
                    InspectorNameAttribute label = field.GetCustomAttribute<InspectorNameAttribute>();
                    Assert.That(label, Is.Not.Null, $"{moduleType.Name}.{field.Name}");
                    Assert.That(label.displayName.Any(
                        character => character >= '\u4e00' && character <= '\u9fff'),
                        Is.True, $"{moduleType.Name}.{field.Name}");
                }
            }
        }

        private SpawnProjectileResultModule Spawn(ProjectileDefinition definition, bool primary)
        {
            SpawnProjectileResultModule result = Own(
                ScriptableObject.CreateInstance<SpawnProjectileResultModule>());
            Set(result, "projectileDefinition", definition);
            Set(result, "primaryProjectile", primary);
            return result;
        }

        private EventRuleDefinition Rule(
            DiceFaceSlotMask slots,
            EventTriggerModule trigger,
            params EventResultEntry[] entries)
        {
            EventRuleDefinition rule = Own(ScriptableObject.CreateInstance<EventRuleDefinition>());
            Set(rule, "allowedSlots", slots);
            Set(rule, "trigger", trigger);
            Set(rule, "results", entries.ToList());
            return rule;
        }

        private EventRuleDefinition OnFireRule(EventResultModule result)
        {
            SignalTypeTriggerModule trigger = Own(ScriptableObject.CreateInstance<SignalTypeTriggerModule>());
            Set(trigger, "signals", EventSignalMask.OnFire);
            return Rule(
                DiceFaceSlotMask.OnFire,
                trigger,
                new EventResultEntry(Array.Empty<EventConditionModule>(), result));
        }

        private DiceFaceEntry Entry(DiceFaceSlotType slot)
        {
            DiceFaceEntry entry = Own(ScriptableObject.CreateInstance<DiceFaceEntry>());
            Set(entry, "slotType", slot);
            Set(entry, "effect", Own(ScriptableObject.CreateInstance<NoOpEffect>()));
            return entry;
        }

        private ProjectileHandle Handle(
            string name,
            ProjectileRuntimeStats stats,
            Vector3 position)
        {
            GameObject owner = Own(new GameObject(name));
            owner.transform.position = position;
            return new ProjectileHandle(owner.AddComponent<Projectile>(), stats);
        }

        private DiceFaceActivation Activation(
            Vector3 direction,
            DiceFaceConfigurationSnapshot configuration = default)
        {
            return new DiceFaceActivation(
                2,
                configuration,
                new Vector3(1f, 0f, 2f),
                direction,
                null,
                request => default,
                null,
                null);
        }

        private static DiceRevolverShotContext Shot(
            Vector3? direction = null,
            bool canTriggerHitEffects = false)
        {
            return new DiceRevolverShotContext(
                2,
                new Vector3(1f, 0f, 2f),
                direction ?? Vector3.forward,
                null,
                default,
                default,
                null,
                null,
                null,
                canTriggerHitEffects);
        }

        private static ProjectileRuntimeStats Stats(
            ProjectileTypeDefinition type,
            IReadOnlyList<ProjectileTagDefinition> tags)
        {
            return new ProjectileRuntimeStats(
                "Type", "Tag", type, tags, 1f, 2f, 3f, 0);
        }

        private static EventSignal Signal(
            EventSignalType signalType = EventSignalType.OnFire,
            DiceFaceActivation activation = null,
            DiceRevolverShotContext shot = null,
            ProjectileHandle projectile = default,
            Vector3 hitPosition = default,
            IReadOnlyList<int> remainingFaces = null,
            ProjectileRuntimeStats stats = default,
            DiceEventBudget eventBudget = null)
        {
            return new EventSignal(
                signalType,
                2,
                2,
                signalType == EventSignalType.Base ? DiceFaceSlotType.Base :
                    signalType == EventSignalType.OnHit ? DiceFaceSlotType.OnHit :
                    signalType == EventSignalType.OnFireEnd ? DiceFaceSlotType.OnFireEnd :
                    DiceFaceSlotType.OnFire,
                activation,
                shot,
                projectile,
                null,
                hitPosition,
                remainingFaces ?? Array.Empty<int>(),
                0,
                stats,
                eventBudget,
                false,
                default);
        }

        private static EventEvaluationContext Context(
            EventSignal signal,
            IEventRuleServices services = null)
        {
            return new EventEvaluationContext(signal, new EventRuleStateStore(), services);
        }

        private static EventExecutionContext ExecutionContext(
            EventSignal signal,
            IEventRuleServices services)
        {
            return new EventExecutionContext(signal, new EventRuleStateStore(), services);
        }

        private T Own<T>(T value) where T : UnityEngine.Object
        {
            ownedObjects.Add(value);
            return value;
        }

        private static void Set(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private readonly struct ProjectileRequest
        {
            public ProjectileRequest(
                ProjectileDefinition definition,
                Vector3 origin,
                Vector3 direction,
                AttackEffectOverride attackEffectOverride,
                bool isPrimary)
            {
                Definition = definition;
                Origin = origin;
                Direction = direction;
                AttackEffectOverride = attackEffectOverride;
                IsPrimary = isPrimary;
            }

            public ProjectileDefinition Definition { get; }
            public Vector3 Origin { get; }
            public Vector3 Direction { get; }
            public AttackEffectOverride AttackEffectOverride { get; }
            public bool IsPrimary { get; }
        }

        private readonly struct LightningRequest
        {
            public LightningRequest(
                ProjectileHandle origin,
                IReadOnlyList<ProjectileHandle> targets,
                LightningChainDefinition definition)
            {
                Origin = origin;
                Targets = targets;
                Definition = definition;
            }

            public ProjectileHandle Origin { get; }
            public IReadOnlyList<ProjectileHandle> Targets { get; }
            public LightningChainDefinition Definition { get; }
        }

        private sealed class FakeServices : IEventRuleServices
        {
            public DiceEventBudget EventBudget { get; set; }
            public IReadOnlyList<ProjectileHandle> OwnedProjectiles { get; set; } =
                Array.Empty<ProjectileHandle>();
            public bool RefillAndForceAccepted { get; set; }
            public bool LightningAccepted { get; set; }
            public bool OverlayAccepted { get; set; }
            public float ScheduledDelay { get; private set; } = float.NaN;
            public Action ScheduledCallback { get; private set; }
            public int ScheduleCallCount { get; private set; }
            public List<ProjectileRequest> ProjectileRequests { get; } = new();
            public List<int> ForcedFaces { get; } = new();
            public List<LightningRequest> LightningRequests { get; } = new();
            public List<DiceFaceActiveOverlay> QueuedOverlays { get; } = new();
            public Vector3 FindOrigin { get; private set; }
            public float FindRadius { get; private set; }
            public ProjectileTagDefinition FindTag { get; private set; }
            public Projectile FindExcluded { get; private set; }

            public bool RequestProjectile(
                ProjectileDefinition definition,
                Vector3 origin,
                Vector3 direction,
                AttackEffectOverride attackEffectOverride,
                bool isPrimary)
            {
                ProjectileRequests.Add(new ProjectileRequest(
                    definition, origin, direction, attackEffectOverride, isPrimary));
                return true;
            }

            public bool Schedule(float delaySeconds, Action callback)
            {
                ScheduleCallCount++;
                ScheduledDelay = delaySeconds;
                ScheduledCallback = callback;
                return callback != null;
            }

            public bool RequestBonusActivation(
                int face,
                float maximumSpreadAngle,
                float minimumSpreadSeparation,
                EventRuleDefinition sourceRule) => false;

            public bool RequestRefillAndForceNextFace(int face)
            {
                ForcedFaces.Add(face);
                return RefillAndForceAccepted;
            }

            public bool RequestLightningChain(
                ProjectileHandle origin,
                IReadOnlyList<ProjectileHandle> targets,
                LightningChainDefinition definition)
            {
                LightningRequests.Add(new LightningRequest(origin, targets.ToArray(), definition));
                return LightningAccepted;
            }

            public bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay)
            {
                QueuedOverlays.Add(overlay);
                return OverlayAccepted;
            }

            public IReadOnlyList<ProjectileHandle> FindOwnedProjectiles(
                Vector3 origin,
                float radius,
                ProjectileTagDefinition requiredTag,
                Projectile excludedProjectile)
            {
                FindOrigin = origin;
                FindRadius = radius;
                FindTag = requiredTag;
                FindExcluded = excludedProjectile;
                return OwnedProjectiles;
            }

            public void SetDrawPriority(int priority) { }
            public void RejectDrawCandidate(string reason) { }
            public void MultiplyProjectileDamage(float multiplier) { }
            public void RecordRuleDebug(EventRuleDefinition rule, string stage,
                string description, EventResultStatus status) { }
            public void ReportException(Exception exception, ScriptableObject module) { }
        }

        private sealed class StateCaptureResult : EventResultModule
        {
            public int Identifier { get; set; }
            public List<int> Order { get; set; }
            public ScriptableObject StateOwner { get; set; }
            public EventRuleStateStore ObservedState { get; private set; }
            public DiceEventBudget ObservedBudget { get; private set; }
            public int ObservedCounter { get; private set; }

            public override EventResult Execute(EventExecutionContext context)
            {
                ObservedState = context.State;
                ObservedBudget = context.Signal.EventBudget;
                ObservedCounter = context.State.GetInt(StateOwner, "shared") + 1;
                context.State.SetInt(StateOwner, "shared", ObservedCounter);
                Order.Add(Identifier);
                return new EventResult(EventResultStatus.Success, "captured");
            }
        }

        private sealed class NoOpEffect : BulletEventEffect
        {
            public override void Trigger(BulletEventContext context) { }
        }
    }
}
