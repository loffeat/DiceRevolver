using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

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
    }
}
