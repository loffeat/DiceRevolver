using DiceRevolver.Prototype;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class BulletEventEffectTests
    {
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
            System.Func<int, bool> refillAndForceNextFaceAction = null,
            System.Action<float, System.Action> schedule = null,
            System.Action<ProjectileSpawnRequest> spawn = null)
        {
            return new DiceFaceActivation(
                2,
                default,
                Vector3.zero,
                Vector3.forward,
                schedule,
                spawn ?? (_ => { }),
                refillAndForceNextFaceAction,
                null);
        }
    }
}
