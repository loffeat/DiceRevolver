using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiceRevolver.Tests
{
    public sealed class ProjectileCollisionTests
    {
        [Test]
        public void CollisionPolicyIgnoresOtherProjectiles()
        {
            GameObject other = new GameObject("Other Projectile");
            try
            {
                other.AddComponent<SphereCollider>();
                other.AddComponent<Projectile>();

                Assert.That(
                    Projectile.ShouldIgnoreCollision(other.GetComponent<Collider>()),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void CollisionPolicyIgnoresPlayerObjects()
        {
            GameObject player = new GameObject("Player") { tag = "Player" };
            try
            {
                Collider collider = player.AddComponent<BoxCollider>();

                Assert.That(Projectile.ShouldIgnoreCollision(collider), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void CollisionPolicyKeepsWorldTargets()
        {
            GameObject target = new GameObject("Target");
            try
            {
                Collider collider = target.AddComponent<BoxCollider>();

                Assert.That(Projectile.ShouldIgnoreCollision(collider), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void HitBroadcastOccursBeforeDirectDamage()
        {
            List<string> order = new List<string>();
            GameObject projectileOwner = new GameObject("Projectile");
            GameObject target = new GameObject("Target");
            try
            {
                Projectile projectile = projectileOwner.AddComponent<Projectile>();
                BoxCollider collider = target.AddComponent<BoxCollider>();
                RecordingDamageReceiver receiver = target.AddComponent<RecordingDamageReceiver>();
                receiver.Order = order;
                projectile.Hit += (_, _) => order.Add("hit");

                ExpectEditModeDestroy();
                InvokeTrigger(projectile, collider);

                Assert.That(order, Is.EqualTo(new[] { "hit", "damage" }));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(projectileOwner);
            }
        }

        [Test]
        public void IgnoredProjectileCollisionDoesNotBroadcastOrDamage()
        {
            List<string> order = new List<string>();
            GameObject projectileOwner = new GameObject("Projectile");
            GameObject otherProjectileOwner = new GameObject("Other Projectile");
            try
            {
                Projectile projectile = projectileOwner.AddComponent<Projectile>();
                SphereCollider collider = otherProjectileOwner.AddComponent<SphereCollider>();
                otherProjectileOwner.AddComponent<Projectile>();
                RecordingDamageReceiver receiver = otherProjectileOwner.AddComponent<RecordingDamageReceiver>();
                receiver.Order = order;
                projectile.Hit += (_, _) => order.Add("hit");

                InvokeTrigger(projectile, collider);

                Assert.That(order, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(otherProjectileOwner);
                Object.DestroyImmediate(projectileOwner);
            }
        }

        [Test]
        public void IgnoredPlayerCollisionDoesNotBroadcastOrDamage()
        {
            List<string> order = new List<string>();
            GameObject projectileOwner = new GameObject("Projectile");
            GameObject player = new GameObject("Player") { tag = "Player" };
            try
            {
                Projectile projectile = projectileOwner.AddComponent<Projectile>();
                BoxCollider collider = player.AddComponent<BoxCollider>();
                RecordingDamageReceiver receiver = player.AddComponent<RecordingDamageReceiver>();
                receiver.Order = order;
                projectile.Hit += (_, _) => order.Add("hit");

                InvokeTrigger(projectile, collider);

                Assert.That(order, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(projectileOwner);
            }
        }

        [Test]
        public void HitUsesProjectilePositionAtCollision()
        {
            GameObject projectileOwner = new GameObject("Projectile");
            GameObject target = new GameObject("Target");
            try
            {
                Projectile projectile = projectileOwner.AddComponent<Projectile>();
                projectile.transform.position = new Vector3(3f, 4f, 5f);
                BoxCollider collider = target.AddComponent<BoxCollider>();
                target.AddComponent<RecordingDamageReceiver>();
                Vector3 hitPosition = Vector3.zero;
                projectile.Hit += (_, position) => hitPosition = position;

                ExpectEditModeDestroy();
                InvokeTrigger(projectile, collider);

                Assert.That(hitPosition, Is.EqualTo(new Vector3(3f, 4f, 5f)));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(projectileOwner);
            }
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

        public sealed class RecordingDamageReceiver : MonoBehaviour, IDamageReceiver
        {
            public List<string> Order { get; set; }

            public void ReceiveDamage(DamageInfo damageInfo)
            {
                Order?.Add("damage");
            }
        }
    }
}
