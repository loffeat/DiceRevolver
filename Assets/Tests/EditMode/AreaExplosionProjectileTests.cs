using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class AreaExplosionProjectileTests
    {
        [Test]
        public void DetonateDamagesEachReceiverOnceIncludingCenterTarget()
        {
            GameObject explosionOwner = new GameObject("Explosion");
            Projectile projectile = explosionOwner.AddComponent<Projectile>();
            AreaExplosionProjectile explosion = explosionOwner.AddComponent<AreaExplosionProjectile>();
            GameObject centerTarget = CreateTarget("CenterTarget", Vector3.zero, true);
            GameObject edgeTarget = CreateTarget("EdgeTarget", new Vector3(2f, 0f, 0f), false);
            GameObject outsideTarget = CreateTarget("OutsideTarget", new Vector3(4f, 0f, 0f), false);

            try
            {
                SetPrivate(explosion, "radius", 2.5f);
                projectile.Configure(new ProjectileRuntimeStats(
                    "Explosion",
                    "PlayerExplosion",
                    3f,
                    1f,
                    1f,
                    0));
                Physics.SyncTransforms();

                explosion.Detonate();

                TargetDummy center = centerTarget.GetComponent<TargetDummy>();
                TargetDummy edge = edgeTarget.GetComponent<TargetDummy>();
                TargetDummy outside = outsideTarget.GetComponent<TargetDummy>();
                Assert.That(center.HitCount, Is.EqualTo(1), "Multiple colliders must not duplicate damage.");
                Assert.That(center.LastDamage.Amount, Is.EqualTo(3f));
                Assert.That(center.LastDamage.HitPosition, Is.EqualTo(Vector3.zero));
                Assert.That(edge.HitCount, Is.EqualTo(1));
                Assert.That(outside.HitCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(outsideTarget);
                Object.DestroyImmediate(edgeTarget);
                Object.DestroyImmediate(centerTarget);
                Object.DestroyImmediate(explosionOwner);
            }
        }

        [Test]
        public void DetonateDoesNotDamageProjectileOwner()
        {
            GameObject owner = CreateTarget("Owner", Vector3.zero, false);
            Collider ownerCollider = owner.GetComponent<Collider>();
            GameObject enemy = CreateTarget("Enemy", new Vector3(1f, 0f, 0f), false);
            GameObject explosionOwner = new GameObject("Explosion");
            Projectile projectile = explosionOwner.AddComponent<Projectile>();
            AreaExplosionProjectile explosion = explosionOwner.AddComponent<AreaExplosionProjectile>();

            try
            {
                SetPrivate(explosion, "radius", 2.5f);
                projectile.Configure(new ProjectileRuntimeStats(
                    "Explosion",
                    "PlayerExplosion",
                    3f,
                    1f,
                    1f,
                    0));
                projectile.Launch(Vector3.forward, ownerCollider);
                Physics.SyncTransforms();

                explosion.Detonate();

                Assert.That(owner.GetComponent<TargetDummy>().HitCount, Is.Zero);
                Assert.That(enemy.GetComponent<TargetDummy>().HitCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(explosionOwner);
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void OwnerImmunityDoesNotIncludeSiblingUnderSharedSceneParent()
        {
            GameObject sharedParent = new GameObject("Characters");
            GameObject owner = CreateTarget("Owner", Vector3.zero, false);
            owner.transform.SetParent(sharedParent.transform);
            Collider ownerCollider = owner.GetComponent<Collider>();
            GameObject enemy = CreateTarget("Enemy", new Vector3(1f, 0f, 0f), false);
            enemy.transform.SetParent(sharedParent.transform);
            GameObject explosionOwner = new GameObject("Explosion");
            Projectile projectile = explosionOwner.AddComponent<Projectile>();
            AreaExplosionProjectile explosion = explosionOwner.AddComponent<AreaExplosionProjectile>();

            try
            {
                SetPrivate(explosion, "radius", 2.5f);
                projectile.Configure(new ProjectileRuntimeStats(
                    "Explosion",
                    "PlayerExplosion",
                    3f,
                    1f,
                    1f,
                    0));
                projectile.Launch(Vector3.forward, ownerCollider);
                Physics.SyncTransforms();

                explosion.Detonate();

                Assert.That(owner.GetComponent<TargetDummy>().HitCount, Is.Zero);
                Assert.That(enemy.GetComponent<TargetDummy>().HitCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(explosionOwner);
                Object.DestroyImmediate(sharedParent);
            }
        }

        [Test]
        public void DetonateOnlyAppliesDamageOnce()
        {
            GameObject explosionOwner = new GameObject("Explosion");
            Projectile projectile = explosionOwner.AddComponent<Projectile>();
            AreaExplosionProjectile explosion = explosionOwner.AddComponent<AreaExplosionProjectile>();
            GameObject target = CreateTarget("Target", Vector3.zero, false);

            try
            {
                projectile.Configure(new ProjectileRuntimeStats(
                    "Explosion",
                    "PlayerExplosion",
                    3f,
                    1f,
                    1f,
                    0));
                Physics.SyncTransforms();

                explosion.Detonate();
                explosion.Detonate();

                Assert.That(target.GetComponent<TargetDummy>().HitCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(explosionOwner);
            }
        }

        private static GameObject CreateTarget(string name, Vector3 position, bool addChildCollider)
        {
            GameObject target = new GameObject(name);
            target.transform.position = position;
            target.AddComponent<TargetDummy>();
            target.AddComponent<SphereCollider>().radius = 0.4f;
            if (addChildCollider)
            {
                GameObject child = new GameObject("ExtraCollider");
                child.transform.SetParent(target.transform, false);
                child.AddComponent<SphereCollider>().radius = 0.3f;
            }

            return target;
        }

        private static void SetPrivate<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            FieldInfo field = typeof(TTarget).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
