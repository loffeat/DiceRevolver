using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    public sealed class EchoSynergyPassiveTests
    {
        [Test]
        public void MatchingProjectileTypeRequestsImmediateBonusWithSharedBudget()
        {
            ProjectileTypeDefinition type = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            EchoSynergyPassiveEffect effect = ScriptableObject.CreateInstance<EchoSynergyPassiveEffect>();
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            List<BonusDiceActivationRequest> requests = new List<BonusDiceActivationRequest>();
            passives.ConfigureBonusActivation(request =>
            {
                requests.Add(request);
                return true;
            });
            passives.RebuildFace(3, effect, type);
            DiceFaceActivation activation = CreateActivation(8);
            DiceRevolverShotContext shot = CreateShot(activation, type);

            passives.NotifyProjectileHit(shot, null, Vector3.one);

            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.That(requests[0].Face, Is.EqualTo(3));
            Assert.That(requests[0].EventBudget, Is.SameAs(activation.EventBudget));
            Assert.That(requests[0].SuppressedPassiveInstanceId, Is.Not.Zero);

            Destroy(type, effect);
        }

        [Test]
        public void TagMatchWithoutTypeIdentityDoesNotTrigger()
        {
            ProjectileTypeDefinition ownerType = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            ProjectileTypeDefinition otherType = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            EchoSynergyPassiveEffect effect = ScriptableObject.CreateInstance<EchoSynergyPassiveEffect>();
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            int requests = 0;
            passives.ConfigureBonusActivation(_ =>
            {
                requests++;
                return true;
            });
            passives.RebuildFace(2, effect, ownerType);

            passives.NotifyProjectileHit(CreateShot(CreateActivation(8), otherType), null, Vector3.zero);

            Assert.That(requests, Is.Zero);
            Destroy(ownerType, otherType, effect);
        }

        [Test]
        public void EchoStopsAfterFourTriggersOrNormalOwnerFaceConsumption()
        {
            ProjectileTypeDefinition type = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            EchoSynergyPassiveEffect effect = ScriptableObject.CreateInstance<EchoSynergyPassiveEffect>();
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            int requests = 0;
            passives.ConfigureBonusActivation(_ =>
            {
                requests++;
                return true;
            });
            passives.RebuildFace(4, effect, type);
            DiceRevolverShotContext shot = CreateShot(CreateActivation(32), type);

            for (int index = 0; index < 6; index++)
            {
                passives.NotifyProjectileHit(shot, null, Vector3.zero);
            }

            Assert.That(requests, Is.EqualTo(4));
            passives.NotifyReloadCompleted();
            passives.NotifyFaceConsumed(4);
            passives.NotifyProjectileHit(shot, null, Vector3.zero);
            Assert.That(requests, Is.EqualTo(4));

            Destroy(type, effect);
        }

        [Test]
        public void SuppressionSkipsOnlyOriginatingEchoInstance()
        {
            ProjectileTypeDefinition type = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            EchoSynergyPassiveEffect effect = ScriptableObject.CreateInstance<EchoSynergyPassiveEffect>();
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            List<BonusDiceActivationRequest> requests = new List<BonusDiceActivationRequest>();
            passives.ConfigureBonusActivation(request =>
            {
                requests.Add(request);
                return true;
            });
            passives.RebuildFace(1, effect, type);
            passives.RebuildFace(6, effect, type);
            DiceRevolverShotContext shot = CreateShot(CreateActivation(32), type);

            passives.NotifyProjectileHit(shot, null, Vector3.zero);
            long suppressed = requests[0].SuppressedPassiveInstanceId;
            int beforeSuppressedHit = requests.Count;
            passives.NotifyProjectileHit(shot, null, Vector3.zero, suppressed);

            Assert.That(beforeSuppressedHit, Is.EqualTo(2));
            Assert.That(requests, Has.Count.EqualTo(3));
            Assert.That(requests[2].SuppressedPassiveInstanceId, Is.Not.EqualTo(suppressed));

            Destroy(type, effect);
        }

        [Test]
        public void ReloadRestoresTriggerCount()
        {
            ProjectileTypeDefinition type = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            EchoSynergyPassiveEffect effect = ScriptableObject.CreateInstance<EchoSynergyPassiveEffect>();
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            int requests = 0;
            passives.ConfigureBonusActivation(_ =>
            {
                requests++;
                return true;
            });
            passives.RebuildFace(5, effect, type);
            DiceRevolverShotContext shot = CreateShot(CreateActivation(32), type);
            for (int index = 0; index < 4; index++)
            {
                passives.NotifyProjectileHit(shot, null, Vector3.zero);
            }

            passives.NotifyReloadStarted();
            passives.NotifyReloadCompleted();
            passives.NotifyProjectileHit(shot, null, Vector3.zero);

            Assert.That(requests, Is.EqualTo(5));
            Destroy(type, effect);
        }

        private static DiceFaceActivation CreateActivation(int budget)
        {
            return new DiceFaceActivation(
                2,
                default,
                Vector3.zero,
                Vector3.forward,
                null,
                (System.Action<ProjectileSpawnRequest>)null,
                null,
                null,
                budget);
        }

        private static DiceRevolverShotContext CreateShot(
            DiceFaceActivation activation,
            ProjectileTypeDefinition type)
        {
            ProjectileRuntimeStats stats = new ProjectileRuntimeStats(
                "Shared",
                "Default",
                type,
                System.Array.Empty<ProjectileTagDefinition>(),
                1f,
                10f,
                10f,
                0);
            return new DiceRevolverShotContext(
                activation.Face,
                Vector3.zero,
                Vector3.forward,
                null,
                default,
                stats,
                null,
                null,
                activation,
                true);
        }

        private static void Destroy(params Object[] objects)
        {
            foreach (Object target in objects)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
