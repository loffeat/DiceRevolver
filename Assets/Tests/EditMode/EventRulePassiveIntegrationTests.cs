using System;
using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    public sealed class EventRulePassiveIntegrationTests
    {
        private readonly List<Object> ownedObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = ownedObjects.Count - 1; index >= 0; index--)
            {
                if (ownedObjects[index] != null)
                {
                    Object.DestroyImmediate(ownedObjects[index]);
                }
            }

            ownedObjects.Clear();
        }

        [Test]
        public void RuleDrawFilteringConsumesLegacyCandidatesThenFallsBackToRealPoolOnce()
        {
            List<string> warnings = new();
            SetDrawPriorityResultModule priority = Own(
                ScriptableObject.CreateInstance<SetDrawPriorityResultModule>());
            Set(priority, "priority", 1);
            SourceFaceConditionModule ownerFace = Own(
                ScriptableObject.CreateInstance<SourceFaceConditionModule>());
            EventRuleDefinition priorityRule = CreateRule(
                priority,
                new EventConditionModule[] { ownerFace });
            RejectEveryCandidateResult reject = Own(
                ScriptableObject.CreateInstance<RejectEveryCandidateResult>());
            EventRuleDefinition rejectRule = CreateRule(reject);
            DiceEventRuleRuntimeSet runtimes = CreateRuntimeSet(warnings: warnings.Add);

            runtimes.RebuildFace(1, PassiveSnapshot(priorityRule));
            DiceDrawConstraintResult filtered = runtimes.FilterDrawCandidates(
                new[] { 1, 3 },
                new[] { 1, 2, 3 },
                2);

            Assert.That(filtered.Candidates, Is.EqualTo(new[] { 3 }),
                "legacy-rejected face 2 must not re-enter and lower-priority face 3 draws first");
            Assert.That(filtered.ForcedFaceEligible, Is.False);

            runtimes.RebuildFace(2, PassiveSnapshot(rejectRule));
            DiceDrawConstraintResult firstFallback = runtimes.FilterDrawCandidates(
                new[] { 1, 3 },
                new[] { 1, 2, 3 },
                2);
            DiceDrawConstraintResult secondFallback = runtimes.FilterDrawCandidates(
                new[] { 1, 3 },
                new[] { 1, 2, 3 },
                2);

            Assert.That(firstFallback.Candidates, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(secondFallback.Candidates, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(firstFallback.ForcedFaceEligible, Is.True);
            Assert.That(warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void RuleDrawSignalsExposeOnlyLegacyAllowedFacesWhileFallbackUsesRealPool()
        {
            CaptureRemainingFacesResult capture = Own(
                ScriptableObject.CreateInstance<CaptureRemainingFacesResult>());
            DiceEventRuleRuntimeSet runtimes = CreateRuntimeSet();
            runtimes.RebuildFace(6, PassiveSnapshot(CreateRule(capture)));

            DiceDrawConstraintResult result = runtimes.FilterDrawCandidates(
                new[] { 1, 3 },
                new[] { 1, 2, 3 },
                null);

            Assert.That(result.Candidates, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(capture.Observed, Has.Count.EqualTo(2));
            Assert.That(capture.Observed[0], Is.EqualTo(new[] { 1, 3 }));
            Assert.That(capture.Observed[1], Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void LowestCandidatePriorityKeepsTiesAndDefersForcedFinisherUntilAlone()
        {
            SetDrawPriorityResultModule firstPriority = Own(
                ScriptableObject.CreateInstance<SetDrawPriorityResultModule>());
            SetDrawPriorityResultModule secondPriority = Own(
                ScriptableObject.CreateInstance<SetDrawPriorityResultModule>());
            SetDrawPriorityResultModule finisherPriority = Own(
                ScriptableObject.CreateInstance<SetDrawPriorityResultModule>());
            Set(firstPriority, "priority", 0);
            Set(secondPriority, "priority", 0);
            Set(finisherPriority, "priority", 1);
            SourceFaceConditionModule firstOwner = Own(
                ScriptableObject.CreateInstance<SourceFaceConditionModule>());
            SourceFaceConditionModule secondOwner = Own(
                ScriptableObject.CreateInstance<SourceFaceConditionModule>());
            SourceFaceConditionModule finisherOwner = Own(
                ScriptableObject.CreateInstance<SourceFaceConditionModule>());
            DiceEventRuleRuntimeSet runtimes = CreateRuntimeSet();
            runtimes.RebuildFace(1, PassiveSnapshot(CreateRule(
                firstPriority,
                new EventConditionModule[] { firstOwner })));
            runtimes.RebuildFace(2, PassiveSnapshot(CreateRule(
                secondPriority,
                new EventConditionModule[] { secondOwner })));
            runtimes.RebuildFace(3, PassiveSnapshot(CreateRule(
                finisherPriority,
                new EventConditionModule[] { finisherOwner })));

            DiceDrawConstraintResult ordinaryForced = runtimes.FilterDrawCandidates(
                new[] { 1, 2, 3 },
                new[] { 1, 2, 3 },
                2);
            DiceDrawConstraintResult finisherForced = runtimes.FilterDrawCandidates(
                new[] { 1, 2, 3 },
                new[] { 1, 2, 3 },
                3);
            DiceDrawConstraintResult finisherAlone = runtimes.FilterDrawCandidates(
                new[] { 3 },
                new[] { 3 },
                3);

            Assert.That(ordinaryForced.Candidates, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(ordinaryForced.ForcedFaceEligible, Is.True);
            Assert.That(finisherForced.Candidates, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(finisherForced.ForcedFaceEligible, Is.False);
            Assert.That(finisherAlone.Candidates, Is.EqualTo(new[] { 3 }));
            Assert.That(finisherAlone.ForcedFaceEligible, Is.True);
        }

        [Test]
        public void SameCandidateStillAccumulatesTheHighestPriorityAcrossRules()
        {
            EventSignal signal = Signal(EventSignalType.DrawCandidate);
            PassiveEventRuleServices services = CreateServices(signal);

            services.SetDrawPriority(2);
            services.SetDrawPriority(1);

            Assert.That(services.HighestDrawPriority, Is.EqualTo(2));
        }

        [Test]
        public void RuleFinisherKeepsBoundFaceUntilItBecomesTheOnlyCandidate()
        {
            SetDrawPriorityResultModule priority = Own(
                ScriptableObject.CreateInstance<SetDrawPriorityResultModule>());
            Set(priority, "priority", 1);
            SourceFaceConditionModule ownerFace = Own(
                ScriptableObject.CreateInstance<SourceFaceConditionModule>());
            DiceEventRuleRuntimeSet rules = CreateRuntimeSet();
            rules.RebuildFace(4, PassiveSnapshot(CreateRule(
                priority,
                new EventConditionModule[] { ownerFace })));
            int[] remaining = { 1, 4, 5 };

            DiceDrawConstraintResult ruleResult = rules.FilterDrawCandidates(
                remaining,
                remaining,
                null);

            Assert.That(ruleResult.Candidates, Is.EqualTo(new[] { 1, 5 }));
            Assert.That(ruleResult.ForcedFaceEligible, Is.False);
        }

        [Test]
        public void GunPassesOnlyLegacyAllowedFacesIntoRuleDrawPriority()
        {
            GameObject owner = Own(new GameObject("Passive Draw Gun"));
            DiceRevolverGun gun = owner.AddComponent<DiceRevolverGun>();
            SelectiveLegacyDrawEffect legacy = Own(
                ScriptableObject.CreateInstance<SelectiveLegacyDrawEffect>());
            legacy.DeniedFace = 2;
            SetDrawPriorityResultModule priority = Own(
                ScriptableObject.CreateInstance<SetDrawPriorityResultModule>());
            Set(priority, "priority", 1);
            SourceFaceConditionModule ownerFace = Own(
                ScriptableObject.CreateInstance<SourceFaceConditionModule>());
            InvokePrivate(gun, "Awake");
            GetPrivate<DicePassiveRuntime>(gun, "passiveRuntime").RebuildFace(4, legacy);
            GetPrivate<DiceEventRuleRuntimeSet>(gun, "eventRuleRuntimes").RebuildFace(
                1,
                PassiveSnapshot(CreateRule(
                    priority,
                    new EventConditionModule[] { ownerFace })));

            DiceDrawConstraintResult result = InvokePrivate<DiceDrawConstraintResult>(
                gun,
                "FilterPassiveDrawCandidates",
                new[] { 1, 2, 3 },
                null);

            Assert.That(result.Candidates, Is.EqualTo(new[] { 3 }),
                "face 2 was rejected by legacy and lower-priority face 3 draws before face 1");
        }

        [Test]
        public void GunAppliesLegacyStatsBeforeRuleMultipliersInStableFaceOrder()
        {
            GameObject owner = Own(new GameObject("Passive Rule Gun"));
            DiceRevolverGun gun = owner.AddComponent<DiceRevolverGun>();
            LegacyAddDamageEffect legacy = Own(
                ScriptableObject.CreateInstance<LegacyAddDamageEffect>());
            OrderedMultiplierResult first = Own(
                ScriptableObject.CreateInstance<OrderedMultiplierResult>());
            OrderedMultiplierResult second = Own(
                ScriptableObject.CreateInstance<OrderedMultiplierResult>());
            List<int> order = new();
            first.Multiplier = 2f;
            first.Order = order;
            second.Multiplier = 3f;
            second.Order = order;
            InvokePrivate(gun, "Awake");
            DicePassiveRuntime legacyRuntime = GetPrivate<DicePassiveRuntime>(gun, "passiveRuntime");
            DiceEventRuleRuntimeSet rules = GetPrivate<DiceEventRuleRuntimeSet>(gun, "eventRuleRuntimes");
            legacyRuntime.RebuildFace(4, legacy);
            rules.RebuildFace(1, PassiveSnapshot(CreateRule(first)));
            rules.RebuildFace(2, PassiveSnapshot(CreateRule(second)));

            ProjectileRuntimeStats result = InvokePrivate<ProjectileRuntimeStats>(
                gun,
                "ModifyPassiveProjectileStats",
                4,
                Stats(1f),
                null);

            Assert.That(result.Damage, Is.EqualTo(12f).Within(0.0001f),
                "(legacy 1 + 1) must be multiplied by face 1 then face 2 rules");
            Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void PassiveSignalsReachOnlyEquippedPassiveRuleRuntimes()
        {
            RecordingSignalResult passive = Own(
                ScriptableObject.CreateInstance<RecordingSignalResult>());
            RecordingSignalResult active = Own(
                ScriptableObject.CreateInstance<RecordingSignalResult>());
            DiceEventRuleRuntimeSet runtimes = CreateRuntimeSet();
            DiceFaceConfigurationSnapshot snapshot = new DiceFaceConfigurationSnapshot(
                null,
                Entry(DiceFaceSlotType.OnFire, CreateRule(active)),
                null,
                null,
                Entry(DiceFaceSlotType.Passive, CreateRule(passive)));
            runtimes.RebuildFace(3, snapshot);

            runtimes.NotifyProjectileSpawned(2, default, null);
            runtimes.NotifyProjectileHit(null, null, Vector3.zero);
            runtimes.NotifyReloadStarted();
            runtimes.NotifyReloadCompleted();
            runtimes.NotifyFaceConsumed(2);

            Assert.That(passive.Signals, Is.EqualTo(new[]
            {
                EventSignalType.ProjectileSpawned,
                EventSignalType.ProjectileHit,
                EventSignalType.ReloadStarted,
                EventSignalType.ReloadCompleted,
                EventSignalType.FaceConsumed
            }));
            Assert.That(active.Signals, Is.Empty);
        }

        [Test]
        public void ProjectileHitSignalCarriesTheExactRegisteredProjectileHandle()
        {
            GameObject projectileObject = Own(new GameObject("Hit Signal Projectile"));
            Projectile projectile = projectileObject.AddComponent<Projectile>();
            ProjectileRuntimeStats stats = Stats(2f);
            OwnedProjectileRegistry registry = new OwnedProjectileRegistry();
            ProjectileHandle handle = registry.Register(projectile, stats);
            DiceEventBudget budget = new DiceEventBudget(8);
            DiceFaceActivation activation = new DiceFaceActivation(
                2,
                default,
                Vector3.zero,
                Vector3.forward,
                null,
                (Func<ProjectileSpawnRequest, ProjectileHandle>)null,
                null,
                null,
                budget,
                false,
                0);
            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                2,
                Vector3.zero,
                Vector3.forward,
                projectile,
                default,
                stats,
                null,
                null,
                activation,
                true);
            GameObject hitObject = Own(new GameObject("Hit Collider"));
            Collider hitCollider = hitObject.AddComponent<BoxCollider>();
            Vector3 hitPosition = new Vector3(2f, 0f, 5f);
            CaptureHitPayloadResult capture = Own(
                ScriptableObject.CreateInstance<CaptureHitPayloadResult>());
            DiceEventRuleRuntimeSet runtimes = CreateRuntimeSet();
            runtimes.RebuildFace(4, PassiveSnapshot(CreateRule(capture)));

            runtimes.NotifyProjectileHit(shot, handle, hitCollider, hitPosition);

            Assert.That(capture.Projectile.Projectile, Is.SameAs(projectile));
            Assert.That(capture.Projectile.Stats.Damage, Is.EqualTo(2f));
            Assert.That(capture.Shot, Is.SameAs(shot));
            Assert.That(capture.Activation, Is.SameAs(activation));
            Assert.That(capture.HitCollider, Is.SameAs(hitCollider));
            Assert.That(capture.HitPosition, Is.EqualTo(hitPosition));
        }

        [Test]
        public void SameRuleAssetHasIndependentStateAcrossGuns()
        {
            IncrementCounterResultModule increment = Own(
                ScriptableObject.CreateInstance<IncrementCounterResultModule>());
            Set(increment, "counterKey", "hits");
            ObserveCounterResult observe = Own(
                ScriptableObject.CreateInstance<ObserveCounterResult>());
            observe.CounterKey = "hits";
            EventRuleDefinition rule = CreateRule(increment, null, observe);
            DiceEventRuleRuntimeSet firstGun = CreateRuntimeSet();
            DiceEventRuleRuntimeSet secondGun = CreateRuntimeSet();
            firstGun.RebuildFace(2, PassiveSnapshot(rule));
            secondGun.RebuildFace(2, PassiveSnapshot(rule));

            firstGun.NotifyFaceConsumed(6);
            firstGun.NotifyFaceConsumed(6);
            secondGun.NotifyFaceConsumed(6);

            Assert.That(observe.CountsByRuntime, Has.Count.EqualTo(2));
            Assert.That(observe.CountsByRuntime, Has.Some.EqualTo(new[] { 1, 2 }));
            Assert.That(observe.CountsByRuntime, Has.Some.EqualTo(new[] { 1 }));
        }

        [Test]
        public void RebuildingOneFaceClearsOnlyThatFacesPassiveRuleState()
        {
            ObserveEquippedFaceCounterResult oldObserver = Own(
                ScriptableObject.CreateInstance<ObserveEquippedFaceCounterResult>());
            ObserveEquippedFaceCounterResult replacementObserver = Own(
                ScriptableObject.CreateInstance<ObserveEquippedFaceCounterResult>());
            EventRuleDefinition original = CounterRule(oldObserver);
            EventRuleDefinition replacement = CounterRule(replacementObserver);
            DiceEventRuleRuntimeSet runtimes = CreateRuntimeSet();
            runtimes.RebuildFace(1, PassiveSnapshot(original));
            runtimes.RebuildFace(2, PassiveSnapshot(original));

            runtimes.NotifyReloadCompleted();
            runtimes.RebuildFace(1, PassiveSnapshot(replacement));
            runtimes.NotifyReloadCompleted();

            Assert.That(oldObserver.Counts[1], Is.EqualTo(new[] { 1 }));
            Assert.That(oldObserver.Counts[2], Is.EqualTo(new[] { 1, 2 }));
            Assert.That(replacementObserver.Counts[1], Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void RuleFailureCannotStopLegacyOrAnotherFaceRuleNotification()
        {
            GameObject owner = Own(new GameObject("Passive Exception Gun"));
            DiceRevolverGun gun = owner.AddComponent<DiceRevolverGun>();
            RecordingLegacyEffect legacy = Own(
                ScriptableObject.CreateInstance<RecordingLegacyEffect>());
            ThrowingResult throwing = Own(ScriptableObject.CreateInstance<ThrowingResult>());
            RecordingSignalResult healthy = Own(
                ScriptableObject.CreateInstance<RecordingSignalResult>());
            List<Exception> exceptions = new();
            InvokePrivate(gun, "Awake");
            DicePassiveRuntime legacyRuntime = GetPrivate<DicePassiveRuntime>(gun, "passiveRuntime");
            DiceEventRuleRuntimeSet rules = GetPrivate<DiceEventRuleRuntimeSet>(gun, "eventRuleRuntimes");
            rules.ConfigurePassiveServices(
                null,
                null,
                null,
                null,
                null,
                (exception, _) => exceptions.Add(exception));
            legacyRuntime.RebuildFace(4, legacy);
            rules.RebuildFace(1, PassiveSnapshot(CreateRule(throwing)));
            rules.RebuildFace(2, PassiveSnapshot(CreateRule(healthy)));

            InvokePrivate(gun, "NotifyReloadStarted");

            Assert.That(legacy.Created.ReloadStartedCount, Is.EqualTo(1));
            Assert.That(healthy.Signals, Is.EqualTo(new[] { EventSignalType.ReloadStarted }));
            Assert.That(exceptions, Has.Count.EqualTo(1));
        }

        [Test]
        public void PassiveStateModulesShareKeysAndDamageUsesNonNegativeStacks()
        {
            EventRuleStateStore state = new EventRuleStateStore();
            IncrementCounterResultModule increment = Own(
                ScriptableObject.CreateInstance<IncrementCounterResultModule>());
            MultiplyProjectileDamageFromCounterResultModule multiply = Own(
                ScriptableObject.CreateInstance<MultiplyProjectileDamageFromCounterResultModule>());
            CounterComparisonConditionModule comparison = Own(
                ScriptableObject.CreateInstance<CounterComparisonConditionModule>());
            SetBooleanStateResultModule setBoolean = Own(
                ScriptableObject.CreateInstance<SetBooleanStateResultModule>());
            BooleanStateConditionModule booleanCondition = Own(
                ScriptableObject.CreateInstance<BooleanStateConditionModule>());
            ResetCounterResultModule reset = Own(
                ScriptableObject.CreateInstance<ResetCounterResultModule>());
            Set(increment, "counterKey", "stacks");
            Set(multiply, "counterKey", "stacks");
            Set(multiply, "damagePerStack", 0.25f);
            Set(comparison, "counterKey", "stacks");
            Set(comparison, "comparison", CounterComparisonOperator.GreaterThanOrEqual);
            Set(comparison, "value", 1);
            Set(setBoolean, "stateKey", "active");
            Set(setBoolean, "value", true);
            Set(booleanCondition, "stateKey", "active");
            Set(booleanCondition, "expectedValue", true);
            Set(reset, "counterKey", "stacks");
            PassiveEventRuleServices services = CreateServices(Signal(EventSignalType.BeforeProjectileStats));
            EventExecutionContext execution = new EventExecutionContext(
                Signal(EventSignalType.BeforeProjectileStats), state, services);

            Assert.That(increment.Execute(execution).Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(comparison.Evaluate(new EventEvaluationContext(
                execution.Signal, state, services)).Passed, Is.True);
            Assert.That(setBoolean.Execute(execution).Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(booleanCondition.Evaluate(new EventEvaluationContext(
                execution.Signal, state, services)).Passed, Is.True);
            Assert.That(multiply.Execute(execution).Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(services.ProjectileDamageMultiplier, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(reset.Execute(execution).Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(state.GetInt("stacks"), Is.Zero);
        }

        [Test]
        public void BonusActivationReusesSourceBudgetAndActivationAndHonorsMaximum()
        {
            DiceEventBudget budget = new DiceEventBudget(8);
            DiceFaceActivation activation = new DiceFaceActivation(
                5,
                default,
                Vector3.zero,
                Vector3.forward,
                null,
                (Func<ProjectileSpawnRequest, ProjectileHandle>)null,
                null,
                null,
                budget,
                false,
                0);
            List<BonusDiceActivationRequest> requests = new();
            EventSignal signal = Signal(
                EventSignalType.ProjectileHit,
                equippedFace: 3,
                sourceFace: 5,
                activation: activation,
                budget: budget);
            PassiveEventRuleServices services = CreateServices(
                signal,
                request =>
                {
                    requests.Add(request);
                    return true;
                });
            RequestBonusActivationResultModule bonus = Own(
                ScriptableObject.CreateInstance<RequestBonusActivationResultModule>());
            Set(bonus, "maximumTriggers", 2);
            Set(bonus, "maximumSpreadAngle", 8f);
            Set(bonus, "minimumSpreadSeparation", 2f);
            EventExecutionContext context = new EventExecutionContext(
                signal,
                new EventRuleStateStore(),
                services);

            Assert.That(bonus.Execute(context).Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(bonus.Execute(context).Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(bonus.Execute(context).Status, Is.EqualTo(EventResultStatus.Skipped));

            Assert.That(requests, Has.Count.EqualTo(2));
            Assert.That(requests[0].Face, Is.EqualTo(3));
            Assert.That(requests[0].EventBudget, Is.SameAs(budget));
            Assert.That(requests[0].SourceActivation, Is.SameAs(activation));
            Assert.That(requests[0].MaximumSpreadAngle, Is.EqualTo(8f));
            Assert.That(requests[0].MinimumSpreadSeparation, Is.EqualTo(2f));
        }

        [Test]
        public void BonusActivationReservesCountBeforeSynchronousReentry()
        {
            DiceEventBudget budget = new DiceEventBudget(12);
            DiceFaceActivation activation = new DiceFaceActivation(
                3,
                default,
                Vector3.zero,
                Vector3.forward,
                null,
                (Func<ProjectileSpawnRequest, ProjectileHandle>)null,
                null,
                null,
                budget,
                false,
                0);
            EventSignal signal = Signal(
                EventSignalType.ProjectileHit,
                equippedFace: 3,
                sourceFace: 3,
                activation: activation,
                budget: budget);
            RequestBonusActivationResultModule bonus = Own(
                ScriptableObject.CreateInstance<RequestBonusActivationResultModule>());
            Set(bonus, "maximumTriggers", 2);
            Set(bonus, "maximumSpreadAngle", 8f);
            Set(bonus, "minimumSpreadSeparation", 2f);
            EventRuleStateStore state = new EventRuleStateStore();
            EventExecutionContext context = default;
            List<BonusDiceActivationRequest> requests = new();
            int reentryDepth = 0;
            PassiveEventRuleServices services = CreateServices(signal, request =>
            {
                requests.Add(request);
                if (reentryDepth < 5)
                {
                    reentryDepth++;
                    bonus.Execute(context);
                    reentryDepth--;
                }

                return true;
            });
            context = new EventExecutionContext(signal, state, services);

            EventResult result = bonus.Execute(context);

            Assert.That(result.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(requests, Has.Count.EqualTo(2));
            Assert.That(state.GetInt("bonusActivationTriggers"), Is.EqualTo(2));
            Assert.That(requests.TrueForAll(request =>
                request.EventBudget == budget && request.SourceActivation == activation), Is.True);
        }

        [Test]
        public void RejectedOrThrownBonusReservationRollsBackWithoutNestedProgress()
        {
            DiceEventBudget budget = new DiceEventBudget(8);
            DiceFaceActivation activation = CreateActivation(3, budget);
            EventSignal signal = Signal(
                EventSignalType.ProjectileHit,
                equippedFace: 3,
                sourceFace: 3,
                activation: activation,
                budget: budget);
            RequestBonusActivationResultModule rejectedBonus = CreateBonusModule(2);
            EventRuleStateStore rejectedState = new EventRuleStateStore();
            EventExecutionContext rejectedContext = new EventExecutionContext(
                signal,
                rejectedState,
                CreateServices(signal, _ => false));

            Assert.That(rejectedBonus.Execute(rejectedContext).Status,
                Is.EqualTo(EventResultStatus.Skipped));
            Assert.That(rejectedState.GetInt("bonusActivationTriggers"), Is.Zero);

            RequestBonusActivationResultModule throwingBonus = CreateBonusModule(2);
            EventRuleStateStore throwingState = new EventRuleStateStore();
            EventExecutionContext throwingContext = new EventExecutionContext(
                signal,
                throwingState,
                CreateServices(signal, _ => throw new InvalidOperationException("expected")));

            Assert.That(() => throwingBonus.Execute(throwingContext),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(throwingState.GetInt("bonusActivationTriggers"), Is.Zero);
        }

        [Test]
        public void RejectedOuterBonusDoesNotEraseSynchronousNestedAcceptedCount()
        {
            DiceEventBudget budget = new DiceEventBudget(8);
            DiceFaceActivation activation = CreateActivation(3, budget);
            EventSignal signal = Signal(
                EventSignalType.ProjectileHit,
                equippedFace: 3,
                sourceFace: 3,
                activation: activation,
                budget: budget);
            RequestBonusActivationResultModule bonus = CreateBonusModule(2);
            EventRuleStateStore state = new EventRuleStateStore();
            EventExecutionContext context = default;
            int requests = 0;
            EventResult nestedResult = default;
            PassiveEventRuleServices services = CreateServices(signal, _ =>
            {
                requests++;
                if (requests == 1)
                {
                    nestedResult = bonus.Execute(context);
                    return false;
                }

                return true;
            });
            context = new EventExecutionContext(signal, state, services);

            EventResult outerResult = bonus.Execute(context);

            Assert.That(outerResult.Status, Is.EqualTo(EventResultStatus.Skipped));
            Assert.That(nestedResult.Status, Is.EqualTo(EventResultStatus.Success));
            Assert.That(requests, Is.EqualTo(2));
            Assert.That(state.GetInt("bonusActivationTriggers"), Is.EqualTo(2),
                "outer rollback must not overwrite synchronous nested progress");
        }

        [Test]
        public void PassiveConditionsUseEquippedFaceBaseTypeAndSignalType()
        {
            ProjectileTypeDefinition type = Own(
                ScriptableObject.CreateInstance<ProjectileTypeDefinition>());
            ProjectileRuntimeStats stats = new ProjectileRuntimeStats(
                "Type", "Tag", type, Array.Empty<ProjectileTagDefinition>(), 1f, 2f, 3f, 0);
            EventSignal signal = Signal(
                EventSignalType.ProjectileHit,
                equippedFace: 2,
                sourceFace: 2,
                stats: stats,
                equippedBaseType: type);
            SourceFaceConditionModule source = Own(
                ScriptableObject.CreateInstance<SourceFaceConditionModule>());
            SameProjectileTypeConditionModule sameType = Own(
                ScriptableObject.CreateInstance<SameProjectileTypeConditionModule>());
            SignalTypeConditionModule signalType = Own(
                ScriptableObject.CreateInstance<SignalTypeConditionModule>());
            Set(signalType, "signals", EventSignalMask.ProjectileHit | EventSignalMask.ReloadStarted);
            EventEvaluationContext context = new EventEvaluationContext(
                signal,
                new EventRuleStateStore(),
                CreateServices(signal));

            Assert.That(source.Evaluate(context).Passed, Is.True);
            Assert.That(sameType.Evaluate(context).Passed, Is.True);
            Assert.That(signalType.Evaluate(context).Passed, Is.True);

            EventSignal mismatch = Signal(
                EventSignalType.ReloadCompleted,
                equippedFace: 2,
                sourceFace: 4,
                stats: stats,
                equippedBaseType: null);
            EventEvaluationContext mismatchContext = new EventEvaluationContext(
                mismatch,
                new EventRuleStateStore(),
                CreateServices(mismatch));
            Assert.That(source.Evaluate(mismatchContext).Passed, Is.False);
            Assert.That(sameType.Evaluate(mismatchContext).Passed, Is.False);
            Assert.That(signalType.Evaluate(mismatchContext).Passed, Is.False);
        }

        [Test]
        public void RuleBackedBaseProjectileTypeFeedsPassiveSameTypeConditions()
        {
            ProjectileTypeDefinition type = Own(
                ScriptableObject.CreateInstance<ProjectileTypeDefinition>());
            ProjectileDefinition definition = Own(
                ScriptableObject.CreateInstance<ProjectileDefinition>());
            Set(definition, "projectileTypeDefinition", type);
            SpawnProjectileResultModule spawn = Own(
                ScriptableObject.CreateInstance<SpawnProjectileResultModule>());
            Set(spawn, "projectileDefinition", definition);
            EventRuleDefinition baseRule = CreateRule(spawn);
            Set(baseRule, "allowedSlots", DiceFaceSlotMask.Base);
            SameProjectileTypeConditionModule sameType = Own(
                ScriptableObject.CreateInstance<SameProjectileTypeConditionModule>());
            OrderedMultiplierResult multiplier = Own(
                ScriptableObject.CreateInstance<OrderedMultiplierResult>());
            multiplier.Multiplier = 2f;
            multiplier.Order = new List<int>();
            EventRuleDefinition passiveRule = CreateRule(
                multiplier,
                new EventConditionModule[] { sameType });
            DiceEventRuleRuntimeSet runtimes = CreateRuntimeSet();
            DiceFaceConfigurationSnapshot snapshot = new DiceFaceConfigurationSnapshot(
                Entry(DiceFaceSlotType.Base, baseRule),
                null,
                null,
                null,
                Entry(DiceFaceSlotType.Passive, passiveRule));
            runtimes.RebuildFace(4, snapshot);
            ProjectileRuntimeStats stats = new ProjectileRuntimeStats(
                "Type", "Tag", type, Array.Empty<ProjectileTagDefinition>(), 3f, 4f, 5f, 0);

            ProjectileRuntimeStats modified = runtimes.ModifyProjectileStats(4, stats);

            Assert.That(modified.Damage, Is.EqualTo(6f).Within(0.0001f));
        }

        [Test]
        public void PassiveModulesHaveChineseMenusInspectorLabelsAndSafeValidation()
        {
            Type[] moduleTypes =
            {
                typeof(SetDrawPriorityResultModule),
                typeof(MultiplyProjectileDamageFromCounterResultModule),
                typeof(IncrementCounterResultModule),
                typeof(ResetCounterResultModule),
                typeof(SetBooleanStateResultModule),
                typeof(RequestBonusActivationResultModule),
                typeof(CounterComparisonConditionModule),
                typeof(BooleanStateConditionModule),
                typeof(SourceFaceConditionModule),
                typeof(SameProjectileTypeConditionModule),
                typeof(SignalTypeConditionModule)
            };

            foreach (Type moduleType in moduleTypes)
            {
                EventRuleModuleMenuAttribute menu = moduleType.GetCustomAttribute<EventRuleModuleMenuAttribute>();
                Assert.That(menu, Is.Not.Null, moduleType.Name);
                Assert.That(menu.Path, Does.Match("[\\u4e00-\\u9fff]"), moduleType.Name);
                foreach (FieldInfo field in moduleType.GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.GetCustomAttribute<SerializeField>() != null)
                    {
                        Assert.That(field.GetCustomAttribute<InspectorNameAttribute>(), Is.Not.Null,
                            $"{moduleType.Name}.{field.Name}");
                    }
                }
            }

            IncrementCounterResultModule invalidKey = Own(
                ScriptableObject.CreateInstance<IncrementCounterResultModule>());
            Set(invalidKey, "counterKey", " ");
            MultiplyProjectileDamageFromCounterResultModule invalidDamage = Own(
                ScriptableObject.CreateInstance<MultiplyProjectileDamageFromCounterResultModule>());
            Set(invalidDamage, "counterKey", "stacks");
            Set(invalidDamage, "damagePerStack", -0.1f);
            List<EventRuleValidationIssue> issues = new();
            invalidKey.CollectValidationIssues(issues);
            invalidDamage.CollectValidationIssues(issues);

            Assert.That(invalidKey.Execute(new EventExecutionContext(
                Signal(EventSignalType.FaceConsumed),
                new EventRuleStateStore(),
                CreateServices(Signal(EventSignalType.FaceConsumed)))).Status,
                Is.EqualTo(EventResultStatus.Skipped));
            Assert.That(invalidDamage.Execute(new EventExecutionContext(
                Signal(EventSignalType.BeforeProjectileStats),
                new EventRuleStateStore(),
                CreateServices(Signal(EventSignalType.BeforeProjectileStats)))).Status,
                Is.EqualTo(EventResultStatus.Skipped));
            Assert.That(issues, Has.Count.EqualTo(2));
            Assert.That(issues.TrueForAll(issue => issue.Severity == EventRuleValidationSeverity.Error),
                Is.True);
        }

        private DiceEventRuleRuntimeSet CreateRuntimeSet(
            Action<string> warnings = null,
            Action<Exception, Object> exceptions = null)
        {
            DiceEventRuleRuntimeSet runtimes = new DiceEventRuleRuntimeSet();
            runtimes.ConfigurePassiveServices(
                null,
                null,
                null,
                () => 0f,
                warnings,
                exceptions);
            return runtimes;
        }

        private PassiveEventRuleServices CreateServices(
            EventSignal signal,
            Func<BonusDiceActivationRequest, bool> bonus = null)
        {
            return new PassiveEventRuleServices(
                signal,
                null,
                bonus,
                null,
                () => 0f,
                null);
        }

        private RequestBonusActivationResultModule CreateBonusModule(int maximumTriggers)
        {
            RequestBonusActivationResultModule bonus = Own(
                ScriptableObject.CreateInstance<RequestBonusActivationResultModule>());
            Set(bonus, "maximumTriggers", maximumTriggers);
            Set(bonus, "maximumSpreadAngle", 8f);
            Set(bonus, "minimumSpreadSeparation", 2f);
            return bonus;
        }

        private static DiceFaceActivation CreateActivation(int face, DiceEventBudget budget)
        {
            return new DiceFaceActivation(
                face,
                default,
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

        private EventRuleDefinition CounterRule(ObserveEquippedFaceCounterResult observer)
        {
            IncrementCounterResultModule increment = Own(
                ScriptableObject.CreateInstance<IncrementCounterResultModule>());
            Set(increment, "counterKey", "count");
            observer.CounterKey = "count";
            return CreateRule(increment, null, observer);
        }

        private EventRuleDefinition CreateRule(
            EventResultModule result,
            IReadOnlyList<EventConditionModule> conditions = null,
            EventResultModule secondResult = null)
        {
            SignalTypeTriggerModule trigger = Own(
                ScriptableObject.CreateInstance<SignalTypeTriggerModule>());
            Set(trigger, "signals", (EventSignalMask)(-1));
            EventRuleDefinition rule = Own(ScriptableObject.CreateInstance<EventRuleDefinition>());
            Set(rule, "trigger", trigger);
            Set(rule, "allowedSlots", DiceFaceSlotMask.Passive);
            Set(rule, "conditions", conditions == null
                ? new List<EventConditionModule>()
                : new List<EventConditionModule>(conditions));
            List<EventResultEntry> results = new()
            {
                new EventResultEntry(null, result)
            };
            if (secondResult != null)
            {
                results.Add(new EventResultEntry(null, secondResult));
            }

            Set(rule, "results", results);
            return rule;
        }

        private DiceFaceConfigurationSnapshot PassiveSnapshot(EventRuleDefinition rule)
        {
            return new DiceFaceConfigurationSnapshot(
                null,
                null,
                null,
                null,
                Entry(DiceFaceSlotType.Passive, rule));
        }

        private DiceFaceEntry Entry(DiceFaceSlotType slot, EventRuleDefinition rule)
        {
            DiceFaceEntry entry = Own(ScriptableObject.CreateInstance<DiceFaceEntry>());
            Set(entry, "slotType", slot);
            Set(entry, "rule", rule);
            return entry;
        }

        private static EventSignal Signal(
            EventSignalType type,
            int equippedFace = 1,
            int sourceFace = 1,
            ProjectileRuntimeStats stats = default,
            ProjectileTypeDefinition equippedBaseType = null,
            DiceFaceActivation activation = null,
            DiceEventBudget budget = null)
        {
            return new EventSignal(
                type,
                equippedFace,
                sourceFace,
                DiceFaceSlotType.Passive,
                activation,
                null,
                default,
                null,
                Vector3.zero,
                Array.Empty<int>(),
                0,
                stats,
                budget,
                false,
                default,
                equippedBaseType);
        }

        private static ProjectileRuntimeStats Stats(float damage)
        {
            return new ProjectileRuntimeStats("Type", "Tag", damage, 10f, 20f, 0);
        }

        private T Own<T>(T value) where T : Object
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

        private static T GetPrivate<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            InvokePrivate<object>(target, methodName, args);
        }

        private static T InvokePrivate<T>(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {target.GetType().Name}.{methodName}");
            object result = method.Invoke(target, args);
            return result == null ? default : (T)result;
        }

        private sealed class RejectEveryCandidateResult : EventResultModule
        {
            public override EventResult Execute(EventExecutionContext context)
            {
                context.Services.RejectDrawCandidate("test rejection");
                return new EventResult(EventResultStatus.Success, "rejected");
            }
        }

        private sealed class CaptureRemainingFacesResult : EventResultModule
        {
            public List<int[]> Observed { get; } = new();

            public override EventResult Execute(EventExecutionContext context)
            {
                int[] snapshot = new int[context.Signal.RemainingFaces.Count];
                for (int index = 0; index < snapshot.Length; index++)
                {
                    snapshot[index] = context.Signal.RemainingFaces[index];
                }

                Observed.Add(snapshot);
                return new EventResult(EventResultStatus.Success, "captured");
            }
        }

        private sealed class CaptureHitPayloadResult : EventResultModule
        {
            public ProjectileHandle Projectile { get; private set; }
            public DiceRevolverShotContext Shot { get; private set; }
            public DiceFaceActivation Activation { get; private set; }
            public Collider HitCollider { get; private set; }
            public Vector3 HitPosition { get; private set; }

            public override EventResult Execute(EventExecutionContext context)
            {
                Projectile = context.Signal.Projectile;
                Shot = context.Signal.Shot;
                Activation = context.Signal.Activation;
                HitCollider = context.Signal.HitCollider;
                HitPosition = context.Signal.HitPosition;
                return new EventResult(EventResultStatus.Success, "captured hit payload");
            }
        }

        private sealed class OrderedMultiplierResult : EventResultModule
        {
            public float Multiplier { get; set; }
            public List<int> Order { get; set; }

            public override EventResult Execute(EventExecutionContext context)
            {
                Order.Add(context.Signal.EquippedFace);
                context.Services.MultiplyProjectileDamage(Multiplier);
                return new EventResult(EventResultStatus.Success, "multiplied");
            }
        }

        private sealed class RecordingSignalResult : EventResultModule
        {
            public List<EventSignalType> Signals { get; } = new();

            public override EventResult Execute(EventExecutionContext context)
            {
                Signals.Add(context.Signal.SignalType);
                return new EventResult(EventResultStatus.Success, "recorded");
            }
        }

        private sealed class ObserveCounterResult : EventResultModule
        {
            private readonly Dictionary<EventRuleStateStore, List<int>> counts = new();
            public string CounterKey { get; set; }
            public ICollection<List<int>> CountsByRuntime => counts.Values;

            public override EventResult Execute(EventExecutionContext context)
            {
                if (!counts.TryGetValue(context.State, out List<int> values))
                {
                    values = new List<int>();
                    counts.Add(context.State, values);
                }

                values.Add(context.State.GetInt(CounterKey));
                return new EventResult(EventResultStatus.Success, "observed");
            }
        }

        private sealed class ObserveEquippedFaceCounterResult : EventResultModule
        {
            public string CounterKey { get; set; }
            public Dictionary<int, List<int>> Counts { get; } = new();

            public override EventResult Execute(EventExecutionContext context)
            {
                int face = context.Signal.EquippedFace;
                if (!Counts.TryGetValue(face, out List<int> values))
                {
                    values = new List<int>();
                    Counts.Add(face, values);
                }

                values.Add(context.State.GetInt(CounterKey));
                return new EventResult(EventResultStatus.Success, "observed");
            }
        }

        private sealed class ThrowingResult : EventResultModule
        {
            public override EventResult Execute(EventExecutionContext context)
            {
                throw new InvalidOperationException("expected passive failure");
            }
        }

        private sealed class LegacyAddDamageEffect : PassiveEventEffect
        {
            public override IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context)
            {
                return new LegacyAddDamageRuntime();
            }
        }

        private sealed class LegacyAddDamageRuntime : IDicePassiveEffectRuntime,
            IDiceProjectileStatsModifier
        {
            public ProjectileRuntimeStats ModifyProjectileStats(
                int sourceFace,
                ProjectileRuntimeStats stats) => stats.WithDamage(stats.Damage + 1f);
            public bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces) => true;
            public void OnReloadStarted() { }
            public void OnReloadCompleted() { }
            public void OnFaceConsumed(int face) { }
            public void Dispose() { }
        }

        private sealed class RecordingLegacyEffect : PassiveEventEffect
        {
            public RecordingLegacyRuntime Created { get; private set; }

            public override IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context)
            {
                Created = new RecordingLegacyRuntime();
                return Created;
            }
        }

        private sealed class RecordingLegacyRuntime : IDicePassiveEffectRuntime
        {
            public int ReloadStartedCount { get; private set; }
            public bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces) => true;
            public void OnReloadStarted() => ReloadStartedCount++;
            public void OnReloadCompleted() { }
            public void OnFaceConsumed(int face) { }
            public void Dispose() { }
        }

        private sealed class SelectiveLegacyDrawEffect : PassiveEventEffect
        {
            public int DeniedFace { get; set; }

            public override IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context)
            {
                return new SelectiveLegacyDrawRuntime(DeniedFace);
            }
        }

        private sealed class SelectiveLegacyDrawRuntime : IDicePassiveEffectRuntime
        {
            private readonly int deniedFace;

            public SelectiveLegacyDrawRuntime(int deniedFace)
            {
                this.deniedFace = deniedFace;
            }

            public bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces) =>
                face != deniedFace;
            public void OnReloadStarted() { }
            public void OnReloadCompleted() { }
            public void OnFaceConsumed(int face) { }
            public void Dispose() { }
        }
    }
}
