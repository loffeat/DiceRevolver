using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class EnemyHealthTests
    {
        [Test]
        public void DamageReducesHealthAndDeathFiresOnce()
        {
            GameObject go = new GameObject("Enemy");
            EnemyHealth health = go.AddComponent<EnemyHealth>();
            InvokePrivate(health, "Awake");
            health.MaxHealth = 10;
            int deaths = 0;
            health.Died += _ => deaths++;
            try
            {
                health.ReceiveDamage(new DamageInfo(6f, Vector3.zero, null));
                Assert.That(health.CurrentHealth, Is.EqualTo(4));
                Assert.That(health.IsDead, Is.False);

                health.ReceiveDamage(new DamageInfo(5f, Vector3.zero, null));
                Assert.That(health.IsDead, Is.True);

                health.ReceiveDamage(new DamageInfo(1f, Vector3.zero, null));
                Assert.That(deaths, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ResetHealthRevivesAfterDeath()
        {
            GameObject go = new GameObject("Enemy");
            EnemyHealth health = go.AddComponent<EnemyHealth>();
            health.MaxHealth = 5;
            try
            {
                health.ReceiveDamage(new DamageInfo(5f, Vector3.zero, null));
                Assert.That(health.IsDead, Is.True);

                health.ResetHealth();
                Assert.That(health.IsDead, Is.False);
                Assert.That(health.CurrentHealth, Is.EqualTo(5));

                health.ReceiveDamage(new DamageInfo(2f, Vector3.zero, null));
                Assert.That(health.CurrentHealth, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TargetDummyDelegatesDamageToFiniteHealthAndKeepsMinimumHealth()
        {
            GameObject go = new GameObject("Dummy");
            TargetDummy dummy = go.AddComponent<TargetDummy>();
            EnemyHealth health = go.AddComponent<EnemyHealth>();
            InvokePrivate(dummy, "Awake");
            InvokePrivate(health, "Awake");
            Assert.That(go.GetComponent<EnemyHealth>(), Is.SameAs(health));
            try
            {
                dummy.ReceiveDamage(new DamageInfo(3f, Vector3.zero, null));
                Assert.That(dummy.HitCount, Is.EqualTo(1));
                Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth - 3));

                dummy.ReceiveDamage(new DamageInfo(999f, Vector3.zero, null));
                Assert.That(health.IsDead, Is.False);
                Assert.That(health.CurrentHealth, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
        private static void InvokePrivate(object owner, string methodName)
        {
            System.Reflection.MethodInfo method = owner.GetType().GetMethod(
                methodName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {owner.GetType().Name}.{methodName}");
            method.Invoke(owner, null);
        }
    }
}
