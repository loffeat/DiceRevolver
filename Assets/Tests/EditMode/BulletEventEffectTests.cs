using DiceRevolver.Prototype;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiceRevolver.Tests
{
    public sealed class BulletEventEffectTests
    {
        [Test]
        public void ForceFaceFourRefillsMissingFourAndForcesNextDraw()
        {
            DiceChamber chamber = new DiceChamber(6);
            while (chamber.ContainsFace(4))
            {
                chamber.TryDrawFace(out _);
            }

            ForceFaceFourOnFireEndEffect effect = ScriptableObject.CreateInstance<ForceFaceFourOnFireEndEffect>();
            DiceFaceActivation activation = CreateActivation(chamber);
            effect.Trigger(new BulletEventContext(activation, null, null, Vector3.zero));

            chamber.TryDrawFace(out int face);

            Assert.That(face, Is.EqualTo(4));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void ExtraShotSchedulesOneAdditionalShotAfterDefaultDelay()
        {
            ExtraShotOnFireEffect effect = ScriptableObject.CreateInstance<ExtraShotOnFireEffect>();
            ProjectileDefinition definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            ProjectileSpawnRequest requestedShot = default;
            int requestCount = 0;
            float scheduledDelay = -1f;
            System.Action scheduledCallback = null;
            DiceFaceActivation activation = CreateActivation(
                null,
                (delay, callback) =>
                {
                    scheduledDelay = delay;
                    scheduledCallback = callback;
                },
                requested =>
                {
                    requestCount++;
                    requestedShot = requested;
                });
            activation.RequestProjectile(
                definition,
                AttackEffectOverride.ForceDisabled,
                true,
                Vector3.zero,
                Vector3.forward);
            requestCount = 0;
            BulletEventContext context = new BulletEventContext(activation, null, null, Vector3.zero);

            effect.Trigger(context);

            Assert.That(effect.DelaySeconds, Is.EqualTo(0.25f));
            Assert.That(scheduledDelay, Is.EqualTo(0.25f));
            Assert.That(scheduledCallback, Is.Not.Null);
            Assert.That(requestCount, Is.Zero);

            scheduledCallback.Invoke();

            Assert.That(requestCount, Is.EqualTo(1));
            Assert.That(requestedShot.Definition, Is.SameAs(definition));
            Assert.That(requestedShot.IsPrimary, Is.False);
            Assert.That(requestedShot.CanTriggerHitEffects, Is.False);

            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ExtraShotDoesNotRequestShotWhenRecursionIsBlocked()
        {
            ExtraShotOnFireEffect effect = ScriptableObject.CreateInstance<ExtraShotOnFireEffect>();
            BulletEventContext context = new BulletEventContext(null, null, null, Vector3.zero);

            effect.Trigger(context);

            Assert.That(context.Activation, Is.Null);

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void EventContextSchedulePassesOriginalContextToDelayedCallback()
        {
            System.Action scheduledCallback = null;
            BulletEventContext receivedContext = default;
            DiceFaceActivation activation = CreateActivation(
                null,
                (_, callback) => scheduledCallback = callback);
            BulletEventContext context = new BulletEventContext(activation, null, null, Vector3.zero);

            bool accepted = context.Schedule(0.4f, delayedContext => receivedContext = delayedContext);
            scheduledCallback.Invoke();

            Assert.That(accepted, Is.True);
            Assert.That(receivedContext.Activation, Is.SameAs(activation));
        }

        [Test]
        public void ExtraShotWithoutSchedulerDoesNotFallBackToImmediateFire()
        {
            ExtraShotOnFireEffect effect = ScriptableObject.CreateInstance<ExtraShotOnFireEffect>();
            int requestCount = 0;
            DiceFaceActivation activation = CreateActivation(null, null, _ => requestCount++);
            BulletEventContext context = new BulletEventContext(activation, null, null, Vector3.zero);

            Assert.DoesNotThrow(() => effect.Trigger(context));
            Assert.That(requestCount, Is.Zero);

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void ExplosionSkipsMissingPrefab()
        {
            ExplosionOnHitEffect effect = ScriptableObject.CreateInstance<ExplosionOnHitEffect>();

            LogAssert.Expect(LogType.Warning, "ExplosionOnHitEffect skipped because no explosion projectile definition is assigned.");
            Assert.DoesNotThrow(() => effect.Trigger(new BulletEventContext(null, null, null, Vector3.zero)));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void ProjectileSpawnEffectSchedulesPrimaryProjectileInCurrentFrame()
        {
            ProjectileDefinition definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            ProjectileSpawnEffect effect = ScriptableObject.CreateInstance<ProjectileSpawnEffect>();
            typeof(ProjectileSpawnEffect).GetField(
                "projectileDefinition",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(effect, definition);

            float scheduledDelay = -1f;
            System.Action scheduledCallback = null;
            ProjectileSpawnRequest request = default;
            int requestCount = 0;
            DiceFaceActivation activation = CreateActivation(
                null,
                (delay, callback) =>
                {
                    scheduledDelay = delay;
                    scheduledCallback = callback;
                },
                spawned =>
                {
                    requestCount++;
                    request = spawned;
                });

            effect.Trigger(new BulletEventContext(activation, null, null, Vector3.zero));

            Assert.That(scheduledDelay, Is.Zero);
            Assert.That(requestCount, Is.Zero);
            scheduledCallback.Invoke();
            Assert.That(requestCount, Is.EqualTo(1));
            Assert.That(request.Definition, Is.SameAs(definition));
            Assert.That(request.IsPrimary, Is.True);
            Assert.That(request.CanTriggerHitEffects, Is.True);

            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(definition);
        }

        private static DiceFaceActivation CreateActivation(
            DiceChamber chamber,
            System.Action<float, System.Action> schedule = null,
            System.Action<ProjectileSpawnRequest> spawn = null)
        {
            return new DiceFaceActivation(
                2,
                default,
                Vector3.zero,
                Vector3.forward,
                null,
                chamber,
                schedule,
                spawn ?? (_ => { }));
        }
    }
}
