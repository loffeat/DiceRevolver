using System;
using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class EventRuleRuntimeTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void ExecutesRuleConditionsThenLocalConditionsThenResultsInOrder()
        {
            List<string> order = new();
            EventRuleDefinition rule = CreateRule(
                CreateRecordingTrigger(order, true),
                new[] { CreateRecordingCondition(order, "rule", true) },
                Entry(CreateRecordingCondition(order, "local-1", true), CreateRecordingResult(order, "result-1")),
                Entry(CreateRecordingCondition(order, "local-2", false), CreateRecordingResult(order, "result-2")),
                Entry(null, CreateRecordingResult(order, "result-3")));

            EventRuleInvocationResult result = new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire)
                .TryHandle(Signal(EventSignalType.OnFire), new FakeRuleServices());

            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(order, Is.EqualTo(new[]
            {
                "trigger", "rule", "local-1", "result-1", "local-2", "result-3"
            }));
        }

        [Test]
        public void TriggerMismatchDoesNotConsumeTheOriginatingBudget()
        {
            DiceEventBudget budget = new DiceEventBudget(3);
            EventRuleDefinition rule = CreateRule(CreateRecordingTrigger(new List<string>(), false));

            EventRuleInvocationResult result = new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire)
                .TryHandle(Signal(EventSignalType.OnFire, budget), new FakeRuleServices());

            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Skipped));
            Assert.That(budget.Remaining, Is.EqualTo(3));
        }

        [Test]
        public void TriggerMismatchProducesASkippedDebugRecord()
        {
            EventRuleDefinition rule = CreateRule(CreateRecordingTrigger(new List<string>(), false));
            FakeRuleServices services = new FakeRuleServices();

            new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire)
                .TryHandle(Signal(EventSignalType.OnFire), services);

            Assert.That(services.DebugRecords, Is.EqualTo(new[]
            {
                new RuleDebugRecord(rule, "trigger", EventResultStatus.Skipped)
            }));
        }

        [Test]
        public void RuleConditionsUseAndSemanticsAndStopAtFirstFailure()
        {
            List<string> order = new();
            EventRuleDefinition rule = CreateRule(
                CreateRecordingTrigger(order, true),
                new[]
                {
                    CreateRecordingCondition(order, "first", false),
                    CreateRecordingCondition(order, "second", true)
                },
                Entry(null, CreateRecordingResult(order, "result")));

            EventRuleInvocationResult result = new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire)
                .TryHandle(Signal(EventSignalType.OnFire), new FakeRuleServices());

            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Skipped));
            Assert.That(order, Is.EqualTo(new[] { "trigger", "first" }));
        }

        [Test]
        public void FailedResultStopsLaterResults()
        {
            List<string> order = new();
            EventRuleDefinition rule = CreateRule(
                CreateRecordingTrigger(order, true),
                null,
                Entry(null, CreateRecordingResult(order, "first", EventResultStatus.Failed)),
                Entry(null, CreateRecordingResult(order, "second")));

            EventRuleInvocationResult result = new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire)
                .TryHandle(Signal(EventSignalType.OnFire), new FakeRuleServices());

            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Failed));
            Assert.That(order, Is.EqualTo(new[] { "trigger", "first" }));
        }

        [Test]
        public void ConditionAndResultFailuresProduceDebugRecords()
        {
            EventRuleDefinition conditionRule = CreateRule(
                CreateRecordingTrigger(new List<string>(), true),
                new[] { CreateRecordingCondition(new List<string>(), "condition", false) });
            EventRuleDefinition resultRule = CreateRule(
                CreateRecordingTrigger(new List<string>(), true),
                null,
                Entry(null, CreateRecordingResult(new List<string>(), "result", EventResultStatus.Failed)));
            FakeRuleServices services = new FakeRuleServices();

            new EventRuleRuntime(conditionRule, 2, DiceFaceSlotType.OnFire)
                .TryHandle(Signal(EventSignalType.OnFire), services);
            new EventRuleRuntime(resultRule, 2, DiceFaceSlotType.OnFire)
                .TryHandle(Signal(EventSignalType.OnFire), services);

            Assert.That(services.DebugRecords, Does.Contain(
                new RuleDebugRecord(conditionRule, "rule-condition", EventResultStatus.Skipped)));
            Assert.That(services.DebugRecords, Does.Contain(
                new RuleDebugRecord(resultRule, "result", EventResultStatus.Failed)));
        }

        [Test]
        public void DenyReentryBlocksTheSameRuntimeWhileExecuting()
        {
            EventRuleDefinition rule = CreateRule(CreateRecordingTrigger(new List<string>(), true));
            Set(rule, "recursionPolicy", EventRuleRecursionPolicy.DenyReentry);
            EventRuleRuntime runtime = new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire);
            ReenteringResult resultModule = Track(ScriptableObject.CreateInstance<ReenteringResult>());
            resultModule.Initialize(runtime);
            Set(rule, "results", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), resultModule)
            });

            EventRuleInvocationResult result = runtime.TryHandle(Signal(EventSignalType.OnFire), new FakeRuleServices());

            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(resultModule.NestedStatus, Is.EqualTo(EventResultStatus.Skipped));
        }

        [Test]
        public void IgnoreBonusActivationSkipsOnlyRulesWithThatPolicy()
        {
            List<string> ignoredOrder = new();
            EventRuleDefinition ignoredRule = CreateRule(CreateRecordingTrigger(ignoredOrder, true));
            Set(ignoredRule, "recursionPolicy", EventRuleRecursionPolicy.IgnoreBonusActivation);
            List<string> acceptedOrder = new();
            EventRuleDefinition acceptedRule = CreateRule(CreateRecordingTrigger(acceptedOrder, true));

            EventRuleInvocationResult ignored = new EventRuleRuntime(ignoredRule, 2, DiceFaceSlotType.OnFire)
                .TryHandle(Signal(EventSignalType.OnFire, isBonusActivation: true), new FakeRuleServices());
            EventRuleInvocationResult accepted = new EventRuleRuntime(acceptedRule, 2, DiceFaceSlotType.OnFire)
                .TryHandle(Signal(EventSignalType.OnFire, isBonusActivation: true), new FakeRuleServices());

            Assert.That(ignored.Status, Is.EqualTo(EventResultStatus.Skipped));
            Assert.That(ignoredOrder, Is.Empty);
            Assert.That(accepted.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(acceptedOrder, Is.EqualTo(new[] { "trigger" }));
        }

        [Test]
        public void RuntimesFromTheSameRuleKeepModuleStateIndependent()
        {
            StatefulResult statefulResult = Track(ScriptableObject.CreateInstance<StatefulResult>());
            EventRuleDefinition rule = CreateRule(
                CreateRecordingTrigger(new List<string>(), true),
                null,
                Entry(null, statefulResult));
            EventRuleRuntime first = new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire);
            EventRuleRuntime second = new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire);

            first.TryHandle(Signal(EventSignalType.OnFire), new FakeRuleServices());
            second.TryHandle(Signal(EventSignalType.OnFire), new FakeRuleServices());

            Assert.That(statefulResult.ObservedCounts, Is.EqualTo(new[] { 1, 1 }));
        }

        [TestCase(ModuleFailureStage.Trigger)]
        [TestCase(ModuleFailureStage.Condition)]
        [TestCase(ModuleFailureStage.Result)]
        public void ModuleExceptionsAreReportedOnceAndDoNotEscape(ModuleFailureStage stage)
        {
            FakeRuleServices services = new FakeRuleServices();
            EventRuleDefinition rule = CreateExceptionRule(stage);

            EventRuleInvocationResult result = default;
            Assert.That(
                () => result = new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire)
                    .TryHandle(Signal(EventSignalType.OnFire), services),
                Throws.Nothing);
            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Failed));
            Assert.That(services.Exceptions, Has.Count.EqualTo(1));
            Assert.That(services.DebugRecords, Does.Contain(
                new RuleDebugRecord(rule, stage.ToString().ToLowerInvariant(), EventResultStatus.Failed)));
        }

        [Test]
        public void ThrowingDebugReporterDoesNotEscapeTheRuleBoundary()
        {
            EventRuleDefinition rule = CreateRule(CreateRecordingTrigger(new List<string>(), false));
            FakeRuleServices services = new FakeRuleServices { ThrowOnRecordRuleDebug = true };

            Assert.That(
                () => new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire)
                    .TryHandle(Signal(EventSignalType.OnFire), services),
                Throws.Nothing);
        }

        [Test]
        public void BudgetExhaustionStopsTheRuleBeforeResultsWithoutPartialConsumption()
        {
            List<string> order = new();
            DiceEventBudget budget = new DiceEventBudget(1);
            EventRuleDefinition rule = CreateRule(
                CreateRecordingTrigger(order, true),
                null,
                Entry(null, CreateRecordingResult(order, "result")));
            Set(rule, "eventBudgetCost", 2);

            EventRuleInvocationResult result = new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire)
                .TryHandle(Signal(EventSignalType.OnFire, budget), new FakeRuleServices());

            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Skipped));
            Assert.That(order, Is.EqualTo(new[] { "trigger" }));
            Assert.That(budget.Remaining, Is.EqualTo(1));
        }

        [Test]
        public void ExecutionContextRejectsScheduledEntriesWithoutRuntimeDelegate()
        {
            EventExecutionContext context = new EventExecutionContext(
                Signal(EventSignalType.OnFire), new EventRuleStateStore(), new FakeRuleServices());

            Assert.That(context.ScheduleEntries(0.1f, Array.Empty<EventResultEntry>()), Is.False);
        }

        private EventRuleDefinition CreateExceptionRule(ModuleFailureStage stage)
        {
            return stage switch
            {
                ModuleFailureStage.Trigger => CreateRule(Track(ScriptableObject.CreateInstance<ThrowingTrigger>())),
                ModuleFailureStage.Condition => CreateRule(
                    CreateRecordingTrigger(new List<string>(), true),
                    new[] { Track(ScriptableObject.CreateInstance<ThrowingCondition>()) }),
                _ => CreateRule(
                    CreateRecordingTrigger(new List<string>(), true),
                    null,
                    Entry(null, Track(ScriptableObject.CreateInstance<ThrowingResult>())))
            };
        }

        private EventRuleDefinition CreateRule(
            EventTriggerModule trigger,
            IReadOnlyList<EventConditionModule> conditions = null,
            params EventResultEntry[] results)
        {
            EventRuleDefinition rule = Track(ScriptableObject.CreateInstance<EventRuleDefinition>());
            Set(rule, "trigger", trigger);
            Set(rule, "conditions", conditions == null
                ? new List<EventConditionModule>()
                : new List<EventConditionModule>(conditions));
            Set(rule, "results", new List<EventResultEntry>(results ?? Array.Empty<EventResultEntry>()));
            return rule;
        }

        private RecordingTrigger CreateRecordingTrigger(List<string> order, bool matches)
        {
            RecordingTrigger trigger = Track(ScriptableObject.CreateInstance<RecordingTrigger>());
            trigger.Initialize(order, matches);
            return trigger;
        }

        private RecordingCondition CreateRecordingCondition(List<string> order, string label, bool passes)
        {
            RecordingCondition condition = Track(ScriptableObject.CreateInstance<RecordingCondition>());
            condition.Initialize(order, label, passes);
            return condition;
        }

        private RecordingResult CreateRecordingResult(
            List<string> order,
            string label,
            EventResultStatus status = EventResultStatus.Success)
        {
            RecordingResult result = Track(ScriptableObject.CreateInstance<RecordingResult>());
            result.Initialize(order, label, status);
            return result;
        }

        private static EventResultEntry Entry(EventConditionModule condition, EventResultModule result)
        {
            return new EventResultEntry(
                condition == null ? Array.Empty<EventConditionModule>() : new[] { condition },
                result);
        }

        private static EventSignal Signal(
            EventSignalType signalType,
            DiceEventBudget budget = null,
            bool isBonusActivation = false)
        {
            return new EventSignal(signalType, 2, 2, DiceFaceSlotType.OnFire, null, null,
                default, null, default, Array.Empty<int>(), 0, default, budget,
                isBonusActivation, default);
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            createdObjects.Add(value);
            return value;
        }

        private static void Set(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        public enum ModuleFailureStage { Trigger, Condition, Result }

        private readonly struct RuleDebugRecord : IEquatable<RuleDebugRecord>
        {
            public RuleDebugRecord(EventRuleDefinition rule, string stage, EventResultStatus status)
            {
                Rule = rule;
                Stage = stage;
                Status = status;
            }

            public EventRuleDefinition Rule { get; }
            public string Stage { get; }
            public EventResultStatus Status { get; }

            public bool Equals(RuleDebugRecord other) =>
                Rule == other.Rule && Stage == other.Stage && Status == other.Status;

            public override bool Equals(object obj) => obj is RuleDebugRecord other && Equals(other);
            public override int GetHashCode() => (Rule, Stage, Status).GetHashCode();
        }

        private sealed class RecordingTrigger : EventTriggerModule
        {
            private List<string> order;
            private bool matches;

            public void Initialize(List<string> order, bool matches)
            {
                this.order = order;
                this.matches = matches;
            }

            public override bool Matches(EventSignal signal)
            {
                order.Add("trigger");
                return matches;
            }
        }

        private sealed class RecordingCondition : EventConditionModule
        {
            private List<string> order;
            private string label;
            private bool passes;

            public void Initialize(List<string> order, string label, bool passes)
            {
                this.order = order;
                this.label = label;
                this.passes = passes;
            }

            public override EventConditionResult Evaluate(EventEvaluationContext context)
            {
                order.Add(label);
                return new EventConditionResult(passes, label);
            }
        }

        private sealed class RecordingResult : EventResultModule
        {
            private List<string> order;
            private string label;
            private EventResultStatus status;

            public void Initialize(List<string> order, string label, EventResultStatus status)
            {
                this.order = order;
                this.label = label;
                this.status = status;
            }

            public override EventResult Execute(EventExecutionContext context)
            {
                order.Add(label);
                return new EventResult(status, label);
            }
        }

        private sealed class ReenteringResult : EventResultModule
        {
            public EventResultStatus NestedStatus { get; private set; }
            private EventRuleRuntime runtime;

            public void Initialize(EventRuleRuntime runtime)
            {
                this.runtime = runtime;
            }

            public override EventResult Execute(EventExecutionContext context)
            {
                NestedStatus = runtime.TryHandle(context.Signal, context.Services).Status;
                return new EventResult(EventResultStatus.Success, "reentered");
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

        private sealed class ThrowingTrigger : EventTriggerModule
        {
            public override bool Matches(EventSignal signal) => throw new InvalidOperationException("trigger");
        }

        private sealed class ThrowingCondition : EventConditionModule
        {
            public override EventConditionResult Evaluate(EventEvaluationContext context) => throw new InvalidOperationException("condition");
        }

        private sealed class ThrowingResult : EventResultModule
        {
            public override EventResult Execute(EventExecutionContext context) => throw new InvalidOperationException("result");
        }

        private sealed class FakeRuleServices : IEventRuleServices
        {
            public List<Exception> Exceptions { get; } = new();
            public List<RuleDebugRecord> DebugRecords { get; } = new();
            public bool ThrowOnRecordRuleDebug { get; set; }
            public DiceEventBudget EventBudget => null;
            public RoundProjectileStatistic RoundProjectileStatistic => null;
            public bool RequestProjectile(ProjectileDefinition definition, Vector3 origin, Vector3 direction, AttackEffectOverride attackEffectOverride, bool isPrimary) => false;
            public bool Schedule(float delaySeconds, Action callback) => false;
            public bool RequestBonusActivation(int face, float maximumSpreadAngle, float minimumSpreadSeparation, EventRuleDefinition sourceRule) => false;
            public bool RequestRefillAndForceNextFace(int face) => false;
            public bool RequestLightningChain(ProjectileHandle origin, IReadOnlyList<ProjectileHandle> targets, LightningChainDefinition definition) => false;
            public bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay) => false;
            public IReadOnlyList<ProjectileHandle> FindOwnedProjectiles(Vector3 origin, float radius, ProjectileTagDefinition requiredTag, Projectile excludedProjectile) => Array.Empty<ProjectileHandle>();
            public void SetDrawPriority(int priority) { }
            public void RejectDrawCandidate(string reason) { }
            public void MultiplyProjectileDamage(float multiplier) { }
            public void RecordRuleDebug(EventRuleDefinition rule, string stage, string description, EventResultStatus status)
            {
                if (ThrowOnRecordRuleDebug)
                {
                    throw new InvalidOperationException("debug");
                }

                DebugRecords.Add(new RuleDebugRecord(rule, stage, status));
            }
            public void ReportException(Exception exception, ScriptableObject module) => Exceptions.Add(exception);
        }
    }
}
