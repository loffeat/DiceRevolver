using System;
using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class DiceShotPipelineTests
    {
        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
        }

        [Test]
        public void ExecuteShotKeepsApprovedStageOrder()
        {
            List<string> order = new List<string>();
            DiceShotPipeline pipeline = CreatePipeline();
            DiceFaceConfigurationSnapshot snapshot = CreateSnapshot(
                Effect(_ => order.Add("base")),
                Effect(_ => order.Add("on-fire")),
                null,
                Effect(_ => order.Add("on-fire-end")));

            pipeline.ExecuteShot(
                2,
                snapshot,
                Vector3.zero,
                Vector3.forward,
                32,
                _ => order.Add("fire-started"),
                _ => order.Add("fire-ended"));

            Assert.That(order, Is.EqualTo(new[]
            {
                "fire-started", "base", "on-fire", "fire-ended", "on-fire-end"
            }));
        }

        [Test]
        public void HandleHitNotifiesObserverBeforeQualifiedOnHit()
        {
            List<string> order = new List<string>();
            DiceShotPipeline pipeline = CreatePipeline();
            DiceFaceActivation activation = pipeline.ExecuteShot(
                3,
                CreateSnapshot(null, null, Effect(_ => order.Add("on-hit")), null),
                Vector3.zero,
                Vector3.forward,
                4,
                null,
                null);
            DiceRevolverShotContext shot = CreateShot(activation, true);

            pipeline.HandleHit(
                shot,
                null,
                Vector3.one,
                _ => order.Add("projectile-hit"));

            Assert.That(order, Is.EqualTo(new[] { "projectile-hit", "on-hit" }));
        }

        [Test]
        public void ExecuteShotRetainsFourSlotSnapshotAfterEquipmentChanges()
        {
            List<string> triggered = new List<string>();
            DiceFaceConfiguration configuration = new DiceFaceConfiguration();
            DiceFaceEntry oldBase = Entry(DiceFaceSlotType.Base, Effect(_ => triggered.Add("old-base")));
            DiceFaceEntry oldOnFire = Entry(DiceFaceSlotType.OnFire, Effect(_ => triggered.Add("old-on-fire")));
            DiceFaceEntry oldOnHit = Entry(DiceFaceSlotType.OnHit, Effect(_ => triggered.Add("old-on-hit")));
            DiceFaceEntry oldOnFireEnd = Entry(
                DiceFaceSlotType.OnFireEnd,
                Effect(_ => triggered.Add("old-on-fire-end")));
            configuration.Equip(oldBase);
            configuration.Equip(oldOnFire);
            configuration.Equip(oldOnHit);
            configuration.Equip(oldOnFireEnd);
            DiceFaceConfigurationSnapshot snapshot = configuration.CreateSnapshot();

            configuration.Equip(Entry(DiceFaceSlotType.Base, Effect(_ => triggered.Add("new-base"))));
            configuration.Equip(Entry(DiceFaceSlotType.OnFire, Effect(_ => triggered.Add("new-on-fire"))));
            configuration.Equip(Entry(DiceFaceSlotType.OnHit, Effect(_ => triggered.Add("new-on-hit"))));
            configuration.Equip(Entry(
                DiceFaceSlotType.OnFireEnd,
                Effect(_ => triggered.Add("new-on-fire-end"))));

            DiceShotPipeline pipeline = CreatePipeline();
            DiceFaceActivation activation = pipeline.ExecuteShot(
                4,
                snapshot,
                Vector3.zero,
                Vector3.forward,
                8,
                null,
                null);
            pipeline.HandleHit(CreateShot(activation, true), null, Vector3.zero, null);

            Assert.That(triggered, Is.EqualTo(new[]
            {
                "old-base", "old-on-fire", "old-on-fire-end", "old-on-hit"
            }));
        }

        [Test]
        public void HandleHitSkipsOnHitWhenShotIsNotQualified()
        {
            int triggerCount = 0;
            DiceShotPipeline pipeline = CreatePipeline();
            DiceFaceActivation activation = pipeline.ExecuteShot(
                1,
                CreateSnapshot(null, null, Effect(_ => triggerCount++), null),
                Vector3.zero,
                Vector3.forward,
                2,
                null,
                null);

            pipeline.HandleHit(CreateShot(activation, false), null, Vector3.zero, null);

            Assert.That(triggerCount, Is.Zero);
            Assert.That(activation.RemainingEventBudget, Is.EqualTo(2));
        }

        [Test]
        public void DelayedCallbackRetainsTheActivationThatScheduledIt()
        {
            float now = 5f;
            DiceFaceActivation observedActivation = null;
            DiceShotPipeline pipeline = CreatePipeline(() => now);
            DiceFaceActivation firstActivation = pipeline.ExecuteShot(
                2,
                CreateSnapshot(
                    null,
                    Effect(context => context.Schedule(
                        1f,
                        delayed => observedActivation = delayed.Activation)),
                    null,
                    null),
                Vector3.zero,
                Vector3.forward,
                4,
                null,
                null);

            now = 5.5f;
            DiceFaceActivation secondActivation = pipeline.ExecuteShot(
                6,
                default,
                Vector3.one,
                Vector3.left,
                4,
                null,
                null);
            pipeline.Tick(6f);

            Assert.That(secondActivation, Is.Not.SameAs(firstActivation));
            Assert.That(observedActivation, Is.SameAs(firstActivation));
        }

        [Test]
        public void AllEffectStagesConsumeOneSharedBudget()
        {
            List<string> stages = new List<string>();
            List<string> warnings = new List<string>();
            DiceShotPipeline pipeline = CreatePipeline(logWarning: warnings.Add);
            DiceFaceActivation activation = pipeline.ExecuteShot(
                5,
                CreateSnapshot(
                    Effect(_ => stages.Add("base")),
                    Effect(_ => stages.Add("on-fire")),
                    Effect(_ => stages.Add("on-hit")),
                    Effect(_ => stages.Add("on-fire-end"))),
                Vector3.zero,
                Vector3.forward,
                3,
                null,
                null);

            pipeline.HandleHit(CreateShot(activation, true), null, Vector3.zero, null);

            Assert.That(stages, Is.EqualTo(new[] { "base", "on-fire", "on-fire-end" }));
            Assert.That(activation.RemainingEventBudget, Is.Zero);
            Assert.That(warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void ExhaustedActivationWarnsOnlyOnceAcrossRepeatedStages()
        {
            List<string> warnings = new List<string>();
            DiceShotPipeline pipeline = CreatePipeline(logWarning: warnings.Add);
            DiceFaceActivation activation = pipeline.ExecuteShot(
                2,
                CreateSnapshot(
                    Effect(_ => { }),
                    Effect(_ => { }),
                    Effect(_ => { }),
                    Effect(_ => { })),
                Vector3.zero,
                Vector3.forward,
                1,
                null,
                null);
            DiceRevolverShotContext shot = CreateShot(activation, true);

            pipeline.HandleHit(shot, null, Vector3.zero, null);
            pipeline.HandleHit(shot, null, Vector3.zero, null);

            Assert.That(warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void NewShotUsesNewBudgetWhileInFlightActivationRetainsCapturedBudget()
        {
            List<string> hits = new List<string>();
            DiceShotPipeline pipeline = CreatePipeline();
            DiceFaceConfigurationSnapshot snapshot = CreateSnapshot(
                Effect(_ => { }),
                null,
                Effect(context => hits.Add($"face-{context.Activation.Face}")),
                null);

            DiceFaceActivation oldActivation = pipeline.ExecuteShot(
                1,
                snapshot,
                Vector3.zero,
                Vector3.forward,
                1,
                null,
                null);
            DiceFaceActivation newActivation = pipeline.ExecuteShot(
                4,
                snapshot,
                Vector3.zero,
                Vector3.forward,
                3,
                null,
                null);

            pipeline.HandleHit(CreateShot(oldActivation, true), null, Vector3.zero, null);
            pipeline.HandleHit(CreateShot(newActivation, true), null, Vector3.zero, null);

            Assert.That(hits, Is.EqualTo(new[] { "face-4" }));
            Assert.That(oldActivation.RemainingEventBudget, Is.Zero);
            Assert.That(newActivation.RemainingEventBudget, Is.EqualTo(1));
        }

        [Test]
        public void SpawnCallbackReceivesTheActivationThatRequestedTheProjectile()
        {
            DiceFaceActivation spawnedFor = null;
            ProjectileSpawnRequest spawnedRequest = default;
            ProjectileDefinition definition = Own(ScriptableObject.CreateInstance<ProjectileDefinition>());
            DiceShotPipeline pipeline = CreatePipeline(
                spawnProjectile: (activation, request) =>
                {
                    spawnedFor = activation;
                    spawnedRequest = request;
                });
            DiceFaceActivation activation = pipeline.ExecuteShot(
                3,
                CreateSnapshot(
                    Effect(context => context.RequestProjectile(
                        definition,
                        AttackEffectOverride.ForceDisabled,
                        true)),
                    null,
                    null,
                    null),
                new Vector3(2f, 0f, 4f),
                Vector3.right,
                2,
                null,
                null);

            Assert.That(spawnedFor, Is.SameAs(activation));
            Assert.That(spawnedRequest.Definition, Is.SameAs(definition));
            Assert.That(spawnedRequest.Origin, Is.EqualTo(new Vector3(2f, 0f, 4f)));
            Assert.That(spawnedRequest.Direction, Is.EqualTo(Vector3.right));
            Assert.That(spawnedRequest.IsPrimary, Is.True);
        }

        [Test]
        public void EffectExceptionIsLoggedAndDoesNotBlockLaterEffectsOrActivations()
        {
            List<string> triggered = new List<string>();
            List<(Exception Exception, UnityEngine.Object Context)> exceptions =
                new List<(Exception, UnityEngine.Object)>();
            RecordingEffect throwingEffect = Effect(_ => throw new InvalidOperationException("expected"));
            DiceShotPipeline pipeline = CreatePipeline(
                logException: (exception, context) => exceptions.Add((exception, context)));

            Assert.DoesNotThrow(() => pipeline.ExecuteShot(
                1,
                CreateSnapshot(
                    throwingEffect,
                    Effect(_ => triggered.Add("same-activation")),
                    null,
                    null),
                Vector3.zero,
                Vector3.forward,
                4,
                null,
                null));
            pipeline.ExecuteShot(
                2,
                CreateSnapshot(Effect(_ => triggered.Add("next-activation")), null, null, null),
                Vector3.zero,
                Vector3.forward,
                2,
                null,
                null);

            Assert.That(triggered, Is.EqualTo(new[] { "same-activation", "next-activation" }));
            Assert.That(exceptions, Has.Count.EqualTo(1));
            Assert.That(exceptions[0].Exception, Is.TypeOf<InvalidOperationException>());
            Assert.That(exceptions[0].Context, Is.SameAs(throwingEffect));
        }

        [Test]
        public void TickIsolatesDelayedCallbackExceptionAndRunsLaterCallbacks()
        {
            List<string> callbacks = new List<string>();
            List<(Exception Exception, UnityEngine.Object Context)> exceptions =
                new List<(Exception, UnityEngine.Object)>();
            DiceShotPipeline pipeline = CreatePipeline(
                () => 10f,
                logException: (exception, context) => exceptions.Add((exception, context)));
            pipeline.ExecuteShot(
                1,
                CreateSnapshot(
                    Effect(context =>
                    {
                        context.Schedule(0.5f, _ => throw new InvalidOperationException("delayed"));
                        context.Schedule(0.5f, _ => callbacks.Add("later"));
                    }),
                    null,
                    null,
                    null),
                Vector3.zero,
                Vector3.forward,
                4,
                null,
                null);

            pipeline.Tick(10.5f);

            Assert.That(callbacks, Is.EqualTo(new[] { "later" }));
            Assert.That(exceptions, Has.Count.EqualTo(1));
            Assert.That(exceptions[0].Exception, Is.TypeOf<InvalidOperationException>());
            Assert.That(exceptions[0].Context, Is.Null);
        }

        [Test]
        public void ClearCancelsPendingCallbacks()
        {
            int callbackCount = 0;
            DiceShotPipeline pipeline = CreatePipeline(() => 2f);
            pipeline.ExecuteShot(
                1,
                CreateSnapshot(
                    Effect(context => context.Schedule(1f, _ => callbackCount++)),
                    null,
                    null,
                    null),
                Vector3.zero,
                Vector3.forward,
                2,
                null,
                null);

            pipeline.Clear();
            pipeline.Tick(3f);

            Assert.That(callbackCount, Is.Zero);
        }

        private DiceShotPipeline CreatePipeline(
            Func<float> currentTime = null,
            Action<DiceFaceActivation, ProjectileSpawnRequest> spawnProjectile = null,
            Func<int, bool> refillAndForceNextFace = null,
            Action<string> logWarning = null,
            Action<Exception, UnityEngine.Object> logException = null)
        {
            return new DiceShotPipeline(
                currentTime ?? (() => 0f),
                spawnProjectile,
                refillAndForceNextFace,
                logWarning,
                logException);
        }

        private DiceFaceConfigurationSnapshot CreateSnapshot(
            BulletEventEffect baseEffect,
            BulletEventEffect onFireEffect,
            BulletEventEffect onHitEffect,
            BulletEventEffect onFireEndEffect)
        {
            return new DiceFaceConfigurationSnapshot(
                EntryOrNull(DiceFaceSlotType.Base, baseEffect),
                EntryOrNull(DiceFaceSlotType.OnFire, onFireEffect),
                EntryOrNull(DiceFaceSlotType.OnHit, onHitEffect),
                EntryOrNull(DiceFaceSlotType.OnFireEnd, onFireEndEffect));
        }

        private DiceFaceEntry EntryOrNull(DiceFaceSlotType slotType, BulletEventEffect effect)
        {
            return effect == null ? null : Entry(slotType, effect);
        }

        private DiceFaceEntry Entry(DiceFaceSlotType slotType, BulletEventEffect effect)
        {
            DiceFaceEntry entry = Own(ScriptableObject.CreateInstance<DiceFaceEntry>());
            SetPrivate(entry, "slotType", slotType);
            SetPrivate(entry, "effect", effect);
            return entry;
        }

        private RecordingEffect Effect(Action<BulletEventContext> action)
        {
            RecordingEffect effect = Own(ScriptableObject.CreateInstance<RecordingEffect>());
            effect.Action = action;
            return effect;
        }

        private static DiceRevolverShotContext CreateShot(
            DiceFaceActivation activation,
            bool canTriggerHitEffects)
        {
            return new DiceRevolverShotContext(
                activation.Face,
                activation.Origin,
                activation.Direction,
                null,
                activation.Configuration,
                default,
                null,
                null,
                activation,
                canTriggerHitEffects);
        }

        private T Own<T>(T ownedObject) where T : UnityEngine.Object
        {
            ownedObjects.Add(ownedObject);
            return ownedObject;
        }

        private static void SetPrivate<TValue>(UnityEngine.Object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class RecordingEffect : BulletEventEffect
        {
            public Action<BulletEventContext> Action { get; set; }

            public override void Trigger(BulletEventContext context)
            {
                Action?.Invoke(context);
            }
        }
    }
}
