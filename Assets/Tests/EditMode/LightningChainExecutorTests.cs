using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    public sealed class LightningChainExecutorTests
    {
        private readonly List<Object> owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = owned.Count - 1; index >= 0; index--)
            {
                if (owned[index] != null)
                {
                    Object.DestroyImmediate(owned[index]);
                }
            }

            owned.Clear();
        }

        [Test]
        public void WholeChainDamagesEachReceiverOnlyOnce()
        {
            LightningChainExecutor executor = CreateExecutor();
            LightningChainDefinition definition = CreateDefinition();
            CountingReceiver receiver = CreateReceiver("Joint Target", new Vector3(4f, 0f, 0f));
            receiver.gameObject.AddComponent<SphereCollider>().radius = 0.4f;
            GameObject child = Own(new GameObject("Second Hitbox"));
            child.transform.SetParent(receiver.transform);
            child.AddComponent<SphereCollider>().radius = 0.4f;
            Physics.SyncTransforms();

            int damaged = executor.Execute(
                new[] { Vector3.zero, Vector3.right * 4f, Vector3.right * 8f },
                null,
                definition);

            Assert.That(damaged, Is.EqualTo(1));
            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(receiver.LastDamage.Amount, Is.EqualTo(1f));
        }

        [Test]
        public void ChainDamagesDistinctReceiversAlongDifferentSegments()
        {
            LightningChainExecutor executor = CreateExecutor();
            LightningChainDefinition definition = CreateDefinition();
            CountingReceiver first = CreateReceiver("First", new Vector3(2f, 0f, 0f));
            CountingReceiver second = CreateReceiver("Second", new Vector3(6f, 0f, 0f));
            Physics.SyncTransforms();

            int damaged = executor.Execute(
                new[] { Vector3.zero, Vector3.right * 4f, Vector3.right * 8f },
                null,
                definition);

            Assert.That(damaged, Is.EqualTo(2));
            Assert.That(first.HitCount, Is.EqualTo(1));
            Assert.That(second.HitCount, Is.EqualTo(1));
        }

        [Test]
        public void ChainDoesNotDamageOwnerHierarchy()
        {
            LightningChainExecutor executor = CreateExecutor();
            LightningChainDefinition definition = CreateDefinition();
            GameObject owner = Own(new GameObject("Owner"));
            CountingReceiver receiver = owner.AddComponent<CountingReceiver>();
            owner.transform.position = new Vector3(2f, 0f, 0f);
            owner.AddComponent<SphereCollider>();
            Physics.SyncTransforms();

            executor.Execute(
                new[] { Vector3.zero, Vector3.right * 4f },
                owner.transform,
                definition);

            Assert.That(receiver.HitCount, Is.Zero);
        }

        [Test]
        public void DirectChainDamageDoesNotPublishProjectileHitEffects()
        {
            int onHitCount = 0;
            LightningChainExecutor executor = CreateExecutor();
            LightningChainDefinition definition = CreateDefinition();
            CreateReceiver("Target", new Vector3(2f, 0f, 0f));
            Physics.SyncTransforms();
            DiceShotPipeline pipeline = new DiceShotPipeline(
                () => 0f,
                (System.Action<DiceFaceActivation, ProjectileSpawnRequest>)null,
                null,
                null,
                null);

            executor.Execute(
                new[] { Vector3.zero, Vector3.right * 4f },
                null,
                definition);

            Assert.That(onHitCount, Is.Zero);
            Assert.That(pipeline, Is.Not.Null);
        }

        private LightningChainExecutor CreateExecutor()
        {
            return Own(new GameObject("Chain Executor")).AddComponent<LightningChainExecutor>();
        }

        private LightningChainDefinition CreateDefinition()
        {
            LightningChainDefinition definition = Own(
                ScriptableObject.CreateInstance<LightningChainDefinition>());
            SetField(definition, "damage", 1f);
            SetField(definition, "chainWidth", 0.25f);
            SetField(definition, "visualDuration", 0.2f);
            SetField(definition, "targetLayers", (LayerMask)~0);
            return definition;
        }

        private CountingReceiver CreateReceiver(string name, Vector3 position)
        {
            GameObject owner = Own(new GameObject(name));
            owner.transform.position = position;
            owner.AddComponent<SphereCollider>().radius = 0.4f;
            return owner.AddComponent<CountingReceiver>();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private T Own<T>(T target) where T : Object
        {
            owned.Add(target);
            return target;
        }

        private sealed class CountingReceiver : MonoBehaviour, IDamageReceiver
        {
            public int HitCount { get; private set; }
            public DamageInfo LastDamage { get; private set; }

            public void ReceiveDamage(DamageInfo damage)
            {
                HitCount++;
                LastDamage = damage;
            }
        }
    }
}
