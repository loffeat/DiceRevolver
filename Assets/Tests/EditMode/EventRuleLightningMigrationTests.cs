using System;
using System.Collections.Generic;
using System.Linq;
using DiceRevolver.Editor;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class EventRuleLightningMigrationTests
    {
        private const string Root = "Assets/Resources/DiceFacePrototype";
        private readonly List<UnityEngine.Object> owned = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = owned.Count - 1; index >= 0; index--)
            {
                if (owned[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(owned[index]);
                }
            }

            owned.Clear();
        }

        [Test]
        public void LightningEntriesReferencePersistentRuleAssetsAndModules()
        {
            string[] names =
            {
                "LightningOrb",
                "ElectromagneticResonance",
                "Tesla",
                "EchoSynergy",
                "ChainReaction",
                "Finisher"
            };

            foreach (string name in names)
            {
                string rulePath = $"{Root}/EventRules/Lightning/{name}Rule.asset";
                DiceFaceEntry entry = Load<DiceFaceEntry>($"{Root}/DiceFaces/{name}.asset");
                EventRuleDefinition rule = Load<EventRuleDefinition>(rulePath);

                Assert.That(entry.Rule, Is.SameAs(rule), name);
                Assert.That(entry.Effect, Is.Null, name);
                Assert.That(entry.PassiveEffect, Is.Null, name);
                Assert.That(rule.Trigger, Is.Not.Null, name);
                Assert.That(rule.Results, Is.Not.Empty, name);
                Assert.That(AllModules(rule), Is.All.Matches<UnityEngine.Object>(module =>
                    AssetDatabase.GetAssetPath(module) == rulePath), name);
            }
        }

        [Test]
        public void PassiveBaseMigrationNormalizesEntriesAndRulesIdempotently()
        {
            string[] names = { "Tesla", "EchoSynergy", "Finisher" };

            EventRuleMigrationUtility.MigratePassiveBaseEvents();
            AssertPassiveBaseState(names);

            EventRuleMigrationUtility.MigratePassiveBaseEvents();
            AssertPassiveBaseState(names);
        }

        private static void AssertPassiveBaseState(string[] names)
        {
            foreach (string name in names)
            {
                DiceFaceEntry entry = Load<DiceFaceEntry>($"{Root}/DiceFaces/{name}.asset");
                EventRuleDefinition rule = Load<EventRuleDefinition>(
                    $"{Root}/EventRules/Lightning/{name}Rule.asset");
                Assert.That(entry.Rule, Is.SameAs(rule), name);
                if (name == "Tesla")
                {
                    // 特斯拉已迁移为开火时普通词条（增伤其装备面基础事件）。
                    Assert.That(entry.SlotType, Is.EqualTo(DiceFaceSlotType.OnFire), name);
                    Assert.That(entry.IsPassiveBase, Is.False, name);
                    Assert.That(rule.AllowedSlots, Is.EqualTo(DiceFaceSlotMask.OnFire), name);
                }
                else if (name == "Finisher")
                {
                    // 收尾者为普通基础事件（最后抽到 + 收尾者弹），不占被动面。
                    Assert.That(entry.SlotType, Is.EqualTo(DiceFaceSlotType.Base), name);
                    Assert.That(entry.IsPassiveBase, Is.False, name);
                    Assert.That(rule.AllowedSlots, Is.EqualTo(DiceFaceSlotMask.Base), name);
                }
                else
                {
                    // 呼应协同保持被动基础词条。
                    Assert.That(entry.SlotType, Is.EqualTo(DiceFaceSlotType.Base), name);
                    Assert.That(entry.IsPassiveBase, Is.True, name);
                    Assert.That(rule.AllowedSlots, Is.EqualTo(DiceFaceSlotMask.Base), name);
                }
            }
        }

        [Test]
        public void LightningOrbRuleSpawnsTheIdentityMatchedPrimaryDefinition()
        {
            EventRuleDefinition rule = Rule("LightningOrb");
            CapturingServices services = new();
            EventSignal signal = Signal(EventSignalType.Base, DiceFaceSlotType.Base);

            EventRuleInvocationResult result =
                new EventRuleRuntime(rule, 2, DiceFaceSlotType.Base).TryHandle(signal, services);

            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(services.ProjectileRequests, Has.Count.EqualTo(1));
            Assert.That(services.ProjectileRequests[0].Definition, Is.SameAs(
                Load<ProjectileDefinition>($"{Root}/Projectiles/LightningOrb.asset")));
            Assert.That(services.ProjectileRequests[0].IsPrimary, Is.True);
        }

        [Test]
        public void ResonanceRuleRequestsOneConfiguredChainWithAtMostThreeNearbyOrbs()
        {
            EventRuleDefinition rule = Rule("ElectromagneticResonance");
            ProjectileDefinition definition = Load<ProjectileDefinition>(
                $"{Root}/Projectiles/LightningOrb.asset");
            ProjectileRuntimeStats stats = definition.BuildRuntimeStats();
            ProjectileHandle origin = Handle("origin", stats, Vector3.zero);
            CapturingServices services = new()
            {
                OwnedProjectiles = new[]
                {
                    Handle("a", stats, Vector3.right),
                    Handle("b", stats, Vector3.left),
                    Handle("c", stats, Vector3.forward),
                    Handle("d", stats, Vector3.back)
                },
                LightningAccepted = true
            };

            EventRuleInvocationResult result = new EventRuleRuntime(
                rule, 2, DiceFaceSlotType.OnFire).TryHandle(
                    Signal(EventSignalType.OnFire, DiceFaceSlotType.OnFire,
                        projectile: origin, stats: stats),
                    services);

            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(services.FindRadius, Is.EqualTo(6f));
            Assert.That(services.LightningRequests, Has.Count.EqualTo(1));
            Assert.That(services.LightningRequests[0].Targets, Has.Count.EqualTo(3));
            Assert.That(services.LightningRequests[0].Definition, Is.SameAs(
                Load<LightningChainDefinition>($"{Root}/Lightning/LightningChainDefinition.asset")));
        }

        [Test]
        public void TeslaRuleKeepsStacksInItsRuntimeAndResetsAtReloadStart()
        {
            EventRuleDefinition rule = Rule("Tesla");
            ProjectileDefinition definition = Load<ProjectileDefinition>(
                $"{Root}/Projectiles/LightningOrb.asset");
            ProjectileRuntimeStats stats = definition.BuildRuntimeStats().WithDamage(10f);
            RoundProjectileStatistic statistic = new RoundProjectileStatistic();
            statistic.Increment(definition); // 本轮已生成 1 颗雷电球
            CapturingServices services = new() { RoundProjectileStatistic = statistic };
            DiceFaceActivation activation = Activation(2, new DiceEventBudget(32));
            EventRuleRuntime runtime = new(rule, 2, DiceFaceSlotType.OnFire);

            runtime.TryHandle(
                Signal(EventSignalType.OnFire, DiceFaceSlotType.OnFire,
                    sourceFace: 2, activation: activation, stats: stats),
                services);
            Assert.That(activation.DamageMultiplier, Is.EqualTo(1.05f).Within(0.0001f));

            statistic.Reset(); // 换弹重置统计
            DiceFaceActivation resetActivation = Activation(2, new DiceEventBudget(32));
            CapturingServices reset = new() { RoundProjectileStatistic = statistic };
            runtime.TryHandle(
                Signal(EventSignalType.OnFire, DiceFaceSlotType.OnFire,
                    sourceFace: 2, activation: resetActivation, stats: stats),
                reset);
            Assert.That(resetActivation.DamageMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void EchoRulePreservesSourceIdentityAndSharedBudgetAndStopsAfterConsumption()
        {
            EventRuleDefinition rule = Rule("EchoSynergy");
            GameObject target = CreateIgnitedTarget();
            DiceEventBudget budget = new(32);
            DiceFaceActivation activation = Activation(3, budget);
            List<BonusDiceActivationRequest> requests = new();
            EventRuleRuntime runtime = new(rule, 3, DiceFaceSlotType.Base);

            try
            {
                for (int index = 0; index < 5; index++)
                {
                    EventSignal signal = Signal(
                        EventSignalType.EnemyStatusApplied,
                        DiceFaceSlotType.Base,
                        equippedFace: 3,
                        sourceFace: 5,
                        activation: activation,
                        statusTarget: target.GetComponent<EnemyStatusHost>());
                    PassiveEventRuleServices services = new(
                        signal,
                        null,
                        request =>
                        {
                            requests.Add(request);
                            return true;
                        },
                        null,
                        null,
                        null);
                    runtime.TryHandle(signal, services);
                }

                // 面 3 相邻 {1,2,4,6}：每轮最多 4 次触发（maximumTriggers=4），每次触发请求 4 个相邻面。
                Assert.That(requests, Has.Count.EqualTo(4 * 4));
                Assert.That(requests, Is.All.Matches<BonusDiceActivationRequest>(request =>
                    request.EventBudget == budget &&
                    request.SourceActivation == activation &&
                    request.SourceRule == rule &&
                    request.MaximumSpreadAngle == 0f &&
                    request.MinimumSpreadSeparation == 0f));

                runtime.TryHandle(
                    Signal(EventSignalType.FaceConsumed, DiceFaceSlotType.Base,
                        equippedFace: 3, sourceFace: 3),
                    new CapturingServices());
                EventSignal afterConsumption = Signal(
                    EventSignalType.EnemyStatusApplied,
                    DiceFaceSlotType.Base,
                    equippedFace: 3,
                    sourceFace: 5,
                    activation: activation,
                    statusTarget: target.GetComponent<EnemyStatusHost>());
                runtime.TryHandle(afterConsumption, new PassiveEventRuleServices(
                    afterConsumption, null, request =>
                    {
                        requests.Add(request);
                        return true;
                    }, null, null, null));

                Assert.That(requests, Has.Count.EqualTo(4 * 4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static GameObject CreateIgnitedTarget()
        {
            GameObject target = new GameObject("IgnitedTarget");
            target.AddComponent<EnemyHealth>();
            EnemyStatusHost host = target.AddComponent<EnemyStatusHost>();
            EnemyStatusDefinition ignite = ScriptableObject.CreateInstance<EnemyStatusDefinition>();
            SetField(ignite, "statusId", "ignite");
            SetField(ignite, "displayName", "点燃");
            host.ApplyStatus(ignite);
            return target;
        }

        private static void SetField(object target, string name, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{name}");
            field.SetValue(target, value);
        }

        [Test]
        public void EchoResultPassesTheOriginatingRuleIdentityToTheBonusService()
        {
            EventRuleDefinition rule = Rule("EchoSynergy");
            GameObject target = CreateIgnitedTarget();
            DiceFaceActivation activation = Activation(3, new DiceEventBudget(32));
            CapturingServices services = new() { BonusAccepted = true };

            try
            {
                new EventRuleRuntime(rule, 3, DiceFaceSlotType.Base).TryHandle(
                    Signal(
                        EventSignalType.EnemyStatusApplied,
                        DiceFaceSlotType.Base,
                        equippedFace: 3,
                        sourceFace: 5,
                        activation: activation,
                        statusTarget: target.GetComponent<EnemyStatusHost>()),
                    services);

                Assert.That(services.RequestedSourceRule, Is.SameAs(rule));
                Assert.That(services.RequestedBonusFace, Is.EqualTo(6));
                Assert.That(services.RequestedMaximumSpread, Is.EqualTo(0f));
                Assert.That(services.RequestedMinimumSeparation, Is.EqualTo(0f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ChainReactionRuleQueuesOnlyReusableNonEmptyActiveSlots()
        {
            EventRuleDefinition rule = Rule("ChainReaction");
            DiceFaceEntry baseEntry = Entry(DiceFaceSlotType.Base);
            DiceFaceEntry hitEntry = Entry(DiceFaceSlotType.OnHit);
            DiceFaceEntry sourceEntry = Load<DiceFaceEntry>($"{Root}/DiceFaces/ChainReaction.asset");
            DiceFaceConfigurationSnapshot snapshot = new(
                baseEntry, null, hitEntry, sourceEntry);
            DiceFaceActivation activation = Activation(4, new DiceEventBudget(32), snapshot);
            CapturingServices services = new() { OverlayAccepted = true };

            new EventRuleRuntime(rule, 4, DiceFaceSlotType.OnFireEnd).TryHandle(
                Signal(EventSignalType.OnFireEnd, DiceFaceSlotType.OnFireEnd,
                    equippedFace: 4, sourceFace: 4, activation: activation),
                services);

            Assert.That(services.Overlays, Has.Count.EqualTo(1));
            DiceFaceActiveOverlay overlay = services.Overlays[0];
            Assert.That(overlay.IsEmpty, Is.False);
            Assert.That(overlay.BaseEntry, Is.SameAs(baseEntry));
            Assert.That(overlay.OnHitEntry, Is.SameAs(hitEntry));
            Assert.That(overlay.OnFireEndEntry, Is.Null);
        }

        [Test]
        public void FinisherRuleKeepsBoundFaceAtPriorityOneUntilItIsEligible()
        {
            EventRuleMigrationUtility.MigratePassiveBaseEvents();
            DiceFaceEntry finisher = Load<DiceFaceEntry>($"{Root}/DiceFaces/Finisher.asset");
            DiceEventRuleRuntimeSet runtimes = new();
            runtimes.RebuildFace(6, new DiceFaceConfigurationSnapshot(
                finisher, null, null, null));

            DiceDrawConstraintResult before = runtimes.FilterDrawCandidates(
                new[] { 1, 6 }, new[] { 1, 6 }, 6);
            DiceDrawConstraintResult eligible = runtimes.FilterDrawCandidates(
                new[] { 6 }, new[] { 6 }, 6);

            Assert.That(before.Candidates, Is.EqualTo(new[] { 1 }));
            Assert.That(before.ForcedFaceEligible, Is.False);
            Assert.That(eligible.Candidates, Is.EqualTo(new[] { 6 }));
            Assert.That(eligible.ForcedFaceEligible, Is.True);
        }

        [Test]
        public void FinisherRuleSpawnsFinisherBulletOnBaseSignal()
        {
            LightningBuildPrototypeBuilder.Build();
            EventRuleDefinition rule = Rule("Finisher");
            CapturingServices services = new();
            EventSignal signal = Signal(EventSignalType.Base, DiceFaceSlotType.Base);

            EventRuleInvocationResult result =
                new EventRuleRuntime(rule, 5, DiceFaceSlotType.Base).TryHandle(signal, services);

            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(services.ProjectileRequests, Has.Count.EqualTo(1));
            Assert.That(services.ProjectileRequests[0].Definition.name,
                Is.EqualTo("FinisherBullet"));
            Assert.That(services.ProjectileRequests[0].IsPrimary, Is.True);
        }

        [Test]
        public void LightningMigrationIsIdempotentAndDoesNotUseGlobalAssetSaving()
        {
            string[] migrationPaths =
            {
                $"{Root}/EventRules/Lightning/LightningOrbRule.asset",
                $"{Root}/EventRules/Lightning/ElectromagneticResonanceRule.asset",
                $"{Root}/EventRules/Lightning/TeslaRule.asset",
                $"{Root}/EventRules/Lightning/EchoSynergyRule.asset",
                $"{Root}/EventRules/Lightning/ChainReactionRule.asset",
                $"{Root}/EventRules/Lightning/FinisherRule.asset"
            };

            LightningBuildPrototypeBuilder.Build();
            string[] afterFirst = migrationPaths.Select(System.IO.File.ReadAllText).ToArray();
            LightningBuildPrototypeBuilder.Build();

            Assert.That(migrationPaths.Select(System.IO.File.ReadAllText), Is.EqualTo(afterFirst));
            Assert.That(System.IO.File.ReadAllText(
                "Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs"),
                Does.Not.Contain("AssetDatabase.SaveAssets("));
        }

        private EventRuleDefinition Rule(string name) =>
            Load<EventRuleDefinition>($"{Root}/EventRules/Lightning/{name}Rule.asset");

        private static IEnumerable<UnityEngine.Object> AllModules(EventRuleDefinition rule)
        {
            yield return rule.Trigger;
            foreach (EventConditionModule condition in rule.Conditions)
            {
                yield return condition;
            }

            foreach (EventResultEntry entry in rule.Results)
            {
                foreach (EventConditionModule condition in entry.Conditions)
                {
                    yield return condition;
                }

                yield return entry.Result;
            }
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
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

        private DiceFaceEntry Entry(DiceFaceSlotType slot)
        {
            DiceFaceEntry entry = Own(ScriptableObject.CreateInstance<DiceFaceEntry>());
            Set(entry, "slotType", slot);
            return entry;
        }

        private static DiceFaceActivation Activation(
            int face,
            DiceEventBudget budget,
            DiceFaceConfigurationSnapshot configuration = default)
        {
            return new DiceFaceActivation(
                face,
                configuration,
                Vector3.zero,
                Vector3.forward,
                null,
                (Func<ProjectileSpawnRequest, ProjectileHandle>)null,
                null,
                null,
                budget,
                false,
                0);
        }

        private static EventSignal Signal(
            EventSignalType type,
            DiceFaceSlotType slot,
            int equippedFace = 2,
            int sourceFace = 2,
            DiceFaceActivation activation = null,
            ProjectileHandle projectile = default,
            ProjectileRuntimeStats stats = default,
            ProjectileTypeDefinition equippedBaseType = null,
            EnemyStatusHost statusTarget = null)
        {
            return new EventSignal(
                type,
                equippedFace,
                sourceFace,
                slot,
                activation,
                null,
                projectile,
                null,
                Vector3.zero,
                Array.Empty<int>(),
                0,
                stats,
                activation?.EventBudget,
                activation != null && activation.IsBonusActivation,
                default,
                equippedBaseType,
                statusTarget);
        }

        private T Own<T>(T value) where T : UnityEngine.Object
        {
            owned.Add(value);
            return value;
        }

        private static void Set(UnityEngine.Object target, string propertyName, object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            switch (value)
            {
                case DiceFaceSlotType slot:
                    property.enumValueIndex = (int)slot;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported test value: {value}");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct ProjectileRequest
        {
            public ProjectileRequest(ProjectileDefinition definition, bool isPrimary)
            {
                Definition = definition;
                IsPrimary = isPrimary;
            }

            public ProjectileDefinition Definition { get; }
            public bool IsPrimary { get; }
        }

        private readonly struct LightningRequest
        {
            public LightningRequest(
                IReadOnlyList<ProjectileHandle> targets,
                LightningChainDefinition definition)
            {
                Targets = targets;
                Definition = definition;
            }

            public IReadOnlyList<ProjectileHandle> Targets { get; }
            public LightningChainDefinition Definition { get; }
        }

        private sealed class CapturingServices : IEventRuleServices
        {
            public readonly List<ProjectileRequest> ProjectileRequests = new();
            public readonly List<LightningRequest> LightningRequests = new();
            public readonly List<DiceFaceActiveOverlay> Overlays = new();
            public DiceEventBudget EventBudget { get; } = new(32);
            public RoundProjectileStatistic RoundProjectileStatistic { get; set; }
            public IReadOnlyList<ProjectileHandle> OwnedProjectiles { get; set; } =
                Array.Empty<ProjectileHandle>();
            public bool LightningAccepted { get; set; }
            public bool OverlayAccepted { get; set; }
            public bool BonusAccepted { get; set; }
            public float FindRadius { get; private set; } = -1f;
            public float DamageMultiplier { get; private set; } = 1f;
            public EventRuleDefinition RequestedSourceRule { get; private set; }
            public int RequestedBonusFace { get; private set; }
            public float RequestedMaximumSpread { get; private set; }
            public float RequestedMinimumSeparation { get; private set; }

            public bool RequestProjectile(
                ProjectileDefinition definition,
                Vector3 origin,
                Vector3 direction,
                AttackEffectOverride attackEffectOverride,
                bool isPrimary)
            {
                ProjectileRequests.Add(new ProjectileRequest(definition, isPrimary));
                return true;
            }

            public bool Schedule(float delaySeconds, Action callback) => false;

            public bool RequestBonusActivation(
                int face,
                float maximumSpreadAngle,
                float minimumSpreadSeparation,
                EventRuleDefinition sourceRule)
            {
                RequestedBonusFace = face;
                RequestedMaximumSpread = maximumSpreadAngle;
                RequestedMinimumSeparation = minimumSpreadSeparation;
                RequestedSourceRule = sourceRule;
                return BonusAccepted;
            }

            public bool RequestRefillAndForceNextFace(int face) => false;

            public bool RequestLightningChain(
                ProjectileHandle origin,
                IReadOnlyList<ProjectileHandle> targets,
                LightningChainDefinition definition)
            {
                LightningRequests.Add(new LightningRequest(targets, definition));
                return LightningAccepted;
            }

            public bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay)
            {
                Overlays.Add(overlay);
                return OverlayAccepted;
            }

            public IReadOnlyList<ProjectileHandle> FindOwnedProjectiles(
                Vector3 origin,
                float radius,
                ProjectileTagDefinition requiredTag,
                Projectile excludedProjectile)
            {
                FindRadius = radius;
                return OwnedProjectiles;
            }

            public void SetDrawPriority(int priority) { }
            public void RejectDrawCandidate(string reason) { }

            public void MultiplyProjectileDamage(float multiplier)
            {
                DamageMultiplier *= multiplier;
            }

            public void RecordRuleDebug(EventRuleDefinition rule, string stage,
                string description, EventResultStatus status) { }
            public void ReportException(Exception exception, ScriptableObject module) { }
        }
    }
}
