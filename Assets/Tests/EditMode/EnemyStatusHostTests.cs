using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class EnemyStatusHostTests
    {
        private static EnemyStatusDefinition CreateIgnite(float duration = 2f, float dps = 5f, int maxStacks = 1)
        {
            EnemyStatusDefinition definition = ScriptableObject.CreateInstance<EnemyStatusDefinition>();
            SetField(definition, "statusId", "ignite");
            SetField(definition, "displayName", "点燃");
            SetField(definition, "durationSeconds", duration);
            SetField(definition, "damagePerSecond", dps);
            SetField(definition, "maxStacks", maxStacks);
            return definition;
        }

        [Test]
        public void StatusTicksDamagePerSecondAndExpires()
        {
            GameObject go = new GameObject("Host");
            EnemyHealth health = go.AddComponent<EnemyHealth>();
            health.MaxHealth = 100;
            InvokePrivate(health, "Awake");
            EnemyStatusHost host = go.AddComponent<EnemyStatusHost>();
            EnemyStatusDefinition ignite = CreateIgnite();
            try
            {
                host.ApplyStatus(ignite);
                Assert.That(host.HasStatus("ignite"), Is.True);

                host.Tick(1f);
                Assert.That(health.CurrentHealth, Is.EqualTo(95));

                host.Tick(1f);
                Assert.That(host.HasStatus("ignite"), Is.False);
                Assert.That(health.CurrentHealth, Is.EqualTo(90));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(ignite);
            }
        }

        [Test]
        public void ReapplyingRefreshesDurationAndStacksUpToMax()
        {
            GameObject go = new GameObject("Host");
            EnemyHealth health = go.AddComponent<EnemyHealth>();
            health.MaxHealth = 100;
            InvokePrivate(health, "Awake");
            EnemyStatusHost host = go.AddComponent<EnemyStatusHost>();
            EnemyStatusDefinition ignite = CreateIgnite(duration: 2f, dps: 5f, maxStacks: 3);
            try
            {
                host.ApplyStatus(ignite);
                host.ApplyStatus(ignite);
                host.ApplyStatus(ignite);
                host.ApplyStatus(ignite); // 超过上限
                Assert.That(host.GetStacks("ignite"), Is.EqualTo(3));

                host.Tick(1.5f); // 刷新后剩余 0.5 秒，仍在
                Assert.That(host.HasStatus("ignite"), Is.True);

                host.Tick(0.5f); // 到期
                Assert.That(host.HasStatus("ignite"), Is.False);
                // 逐跳 CeilToInt：22.5→23，7.5→8
                Assert.That(health.CurrentHealth, Is.EqualTo(100 - 23 - 8));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(ignite);
            }
        }

        [Test]
        public void StatusAppliedEventFiresOnApply()
        {
            GameObject go = new GameObject("Host");
            go.AddComponent<EnemyHealth>();
            EnemyStatusHost host = go.AddComponent<EnemyStatusHost>();
            EnemyStatusDefinition ignite = CreateIgnite();
            int applications = 0;
            host.StatusApplied += (_, _) => applications++;
            try
            {
                host.ApplyStatus(ignite);
                host.ApplyStatus(ignite);
                Assert.That(applications, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(ignite);
            }
        }

        [Test]
        public void TickWithoutEnemyHealthIsSafeNoOp()
        {
            GameObject go = new GameObject("NoHealth");
            EnemyStatusHost host = go.AddComponent<EnemyStatusHost>();
            EnemyStatusDefinition ignite = CreateIgnite();
            try
            {
                host.ApplyStatus(ignite);
                Assert.DoesNotThrow(() => host.Tick(1f));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(ignite);
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

        private static void SetField(object target, string name, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{name}");
            field.SetValue(target, value);
        }
    }
}
