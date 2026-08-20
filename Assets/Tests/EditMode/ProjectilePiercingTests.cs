using System.Reflection;
using System.Text.RegularExpressions;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiceRevolver.Tests
{
    public sealed class ProjectilePiercingTests
    {
        [Test]
        public void PierceTwoDamagesThreeDistinctReceiversBeforeDestroying()
        {
            Projectile projectile = CreateProjectile(enemyPierceCount: 2, out GameObject projectileOwner);
            ReceiverFixture first = CreateReceiver("First");
            ReceiverFixture second = CreateReceiver("Second");
            ReceiverFixture third = CreateReceiver("Third");

            try
            {
                InvokeTrigger(projectile, first.RootCollider);
                InvokeTrigger(projectile, second.RootCollider);
                ExpectEditModeDestroy();
                InvokeTrigger(projectile, third.RootCollider);

                Assert.That(first.Receiver.HitCount, Is.EqualTo(1));
                Assert.That(second.Receiver.HitCount, Is.EqualTo(1));
                Assert.That(third.Receiver.HitCount, Is.EqualTo(1));
            }
            finally
            {
                first.Dispose();
                second.Dispose();
                third.Dispose();
                Object.DestroyImmediate(projectileOwner);
            }
        }

        [Test]
        public void MultipleCollidersOnSameReceiverDoNotConsumePierceOrHitTwice()
        {
            Projectile projectile = CreateProjectile(enemyPierceCount: 1, out GameObject projectileOwner);
            ReceiverFixture first = CreateReceiver("First", includeChildCollider: true);
            ReceiverFixture second = CreateReceiver("Second");

            try
            {
                int hitBroadcastCount = 0;
                projectile.Hit += (_, _) => hitBroadcastCount++;

                InvokeTrigger(projectile, first.RootCollider);
                InvokeTrigger(projectile, first.ChildCollider);
                ExpectEditModeDestroy();
                InvokeTrigger(projectile, second.RootCollider);

                Assert.That(first.Receiver.HitCount, Is.EqualTo(1));
                Assert.That(second.Receiver.HitCount, Is.EqualTo(1));
                Assert.That(hitBroadcastCount, Is.EqualTo(2));
            }
            finally
            {
                first.Dispose();
                second.Dispose();
                Object.DestroyImmediate(projectileOwner);
            }
        }

        [Test]
        public void ColliderWithoutDamageReceiverDestroysProjectileImmediately()
        {
            Projectile projectile = CreateProjectile(enemyPierceCount: 4, out GameObject projectileOwner);
            GameObject obstacle = new GameObject("Obstacle");

            try
            {
                BoxCollider obstacleCollider = obstacle.AddComponent<BoxCollider>();
                ExpectEditModeDestroy();

                InvokeTrigger(projectile, obstacleCollider);
            }
            finally
            {
                Object.DestroyImmediate(obstacle);
                Object.DestroyImmediate(projectileOwner);
            }
        }

        private static Projectile CreateProjectile(int enemyPierceCount, out GameObject owner)
        {
            owner = new GameObject("Projectile");
            Projectile projectile = owner.AddComponent<Projectile>();
            projectile.Configure(new ProjectileRuntimeStats(
                "Piercing",
                "PlayerBullet",
                3f,
                12f,
                24f,
                enemyPierceCount));
            return projectile;
        }

        private static ReceiverFixture CreateReceiver(string name, bool includeChildCollider = false)
        {
            GameObject root = new GameObject(name);
            CountingDamageReceiver receiver = root.AddComponent<CountingDamageReceiver>();
            BoxCollider rootCollider = root.AddComponent<BoxCollider>();
            BoxCollider childCollider = null;

            if (includeChildCollider)
            {
                GameObject child = new GameObject("Child Hitbox");
                child.transform.SetParent(root.transform);
                childCollider = child.AddComponent<BoxCollider>();
            }

            return new ReceiverFixture(root, receiver, rootCollider, childCollider);
        }

        private static void InvokeTrigger(Projectile projectile, Collider collider)
        {
            MethodInfo onTriggerEnter = typeof(Projectile).GetMethod(
                "OnTriggerEnter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            onTriggerEnter.Invoke(projectile, new object[] { collider });
        }

        private static void ExpectEditModeDestroy()
        {
            LogAssert.Expect(
                LogType.Error,
                new Regex("Destroy may not be called from edit mode!.*", RegexOptions.Singleline));
        }

        private sealed class ReceiverFixture
        {
            public ReceiverFixture(
                GameObject root,
                CountingDamageReceiver receiver,
                BoxCollider rootCollider,
                BoxCollider childCollider)
            {
                Root = root;
                Receiver = receiver;
                RootCollider = rootCollider;
                ChildCollider = childCollider;
            }

            public GameObject Root { get; }
            public CountingDamageReceiver Receiver { get; }
            public BoxCollider RootCollider { get; }
            public BoxCollider ChildCollider { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
            }
        }

        private sealed class CountingDamageReceiver : MonoBehaviour, IDamageReceiver
        {
            public int HitCount { get; private set; }

            public void ReceiveDamage(DamageInfo damageInfo)
            {
                HitCount++;
            }
        }
    }
}
