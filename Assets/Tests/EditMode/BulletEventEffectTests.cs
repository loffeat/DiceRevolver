using DiceRevolver.Prototype;
using NUnit.Framework;
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
            effect.Trigger(new BulletEventContext(null, chamber, null, null, Vector3.zero, false));

            chamber.TryDrawFace(out int face);

            Assert.That(face, Is.EqualTo(4));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void ExtraShotRequestsOneAdditionalShotWhenAllowed()
        {
            ExtraShotOnFireEffect effect = ScriptableObject.CreateInstance<ExtraShotOnFireEffect>();
            DiceRevolverShotContext shot = new DiceRevolverShotContext(2, Vector3.zero, Vector3.forward, null);
            int requestCount = 0;
            DiceRevolverShotContext requestedShot = null;
            BulletEventContext context = new BulletEventContext(
                null,
                null,
                shot,
                null,
                Vector3.zero,
                true,
                requested =>
                {
                    requestCount++;
                    requestedShot = requested;
                });

            effect.Trigger(context);

            Assert.That(requestCount, Is.EqualTo(1));
            Assert.That(requestedShot, Is.SameAs(shot));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void ExtraShotDoesNotRequestShotWhenRecursionIsBlocked()
        {
            ExtraShotOnFireEffect effect = ScriptableObject.CreateInstance<ExtraShotOnFireEffect>();
            DiceRevolverShotContext shot = new DiceRevolverShotContext(2, Vector3.zero, Vector3.forward, null);
            int requestCount = 0;
            BulletEventContext context = new BulletEventContext(
                null,
                null,
                shot,
                null,
                Vector3.zero,
                false,
                _ => requestCount++);

            effect.Trigger(context);

            Assert.That(requestCount, Is.EqualTo(0));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void ExplosionSkipsMissingPrefab()
        {
            ExplosionOnHitEffect effect = ScriptableObject.CreateInstance<ExplosionOnHitEffect>();

            LogAssert.Expect(LogType.Warning, "ExplosionOnHitEffect skipped because no explosion projectile prefab is assigned.");
            Assert.DoesNotThrow(() => effect.Trigger(new BulletEventContext(null, null, null, null, Vector3.zero, false)));

            Object.DestroyImmediate(effect);
        }
    }
}
