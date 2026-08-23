using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class EnemyStatusModuleTests
    {
        [Test]
        public void ApplyStatusResultAddsStatusToHitTarget()
        {
            GameObject target = new GameObject("Target");
            target.AddComponent<EnemyHealth>();
            EnemyStatusHost host = target.AddComponent<EnemyStatusHost>();
            BoxCollider collider = target.AddComponent<BoxCollider>();
            EnemyStatusDefinition ignite = CreateIgnite();
            ApplyEnemyStatusResultModule apply = ScriptableObject.CreateInstance<ApplyEnemyStatusResultModule>();
            SetField(apply, "statusDefinition", ignite);
            EventSignal signal = new EventSignal(
                EventSignalType.ProjectileHit,
                1,
                1,
                DiceFaceSlotType.OnHit,
                null,
                null,
                default,
                collider,
                Vector3.zero,
                System.Array.Empty<int>(),
                0,
                default,
                null,
                false,
                default);
            try
            {
                EventResult result = apply.Execute(new EventExecutionContext(signal, null, null));
                Assert.That(result.Status, Is.EqualTo(EventResultStatus.Success));
                Assert.That(host.HasStatus("ignite"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(collider);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(ignite);
                Object.DestroyImmediate(apply);
            }
        }

        [Test]
        public void ApplyStatusResultSkipsTargetsWithoutStatusHost()
        {
            GameObject target = new GameObject("NoHost");
            target.AddComponent<EnemyHealth>();
            BoxCollider collider = target.AddComponent<BoxCollider>();
            EnemyStatusDefinition ignite = CreateIgnite();
            ApplyEnemyStatusResultModule apply = ScriptableObject.CreateInstance<ApplyEnemyStatusResultModule>();
            SetField(apply, "statusDefinition", ignite);
            EventSignal signal = new EventSignal(
                EventSignalType.ProjectileHit,
                1,
                1,
                DiceFaceSlotType.OnHit,
                null,
                null,
                default,
                collider,
                Vector3.zero,
                System.Array.Empty<int>(),
                0,
                default,
                null,
                false,
                default);
            try
            {
                EventResult result = apply.Execute(new EventExecutionContext(signal, null, null));
                Assert.That(result.Status, Is.EqualTo(EventResultStatus.Skipped));
            }
            finally
            {
                Object.DestroyImmediate(collider);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(ignite);
                Object.DestroyImmediate(apply);
            }
        }

        [Test]
        public void HasStatusConditionPassesOnlyWhenTargetIsAfflicted()
        {
            GameObject target = new GameObject("Target");
            target.AddComponent<EnemyHealth>();
            EnemyStatusHost host = target.AddComponent<EnemyStatusHost>();
            BoxCollider collider = target.AddComponent<BoxCollider>();
            EnemyStatusDefinition ignite = CreateIgnite();
            HasEnemyStatusConditionModule condition =
                ScriptableObject.CreateInstance<HasEnemyStatusConditionModule>();
            SetField(condition, "statusDefinition", ignite);
            EventSignal signal = new EventSignal(
                EventSignalType.EnemyStatusApplied,
                3,
                1,
                DiceFaceSlotType.Passive,
                null,
                null,
                default,
                collider,
                Vector3.zero,
                System.Array.Empty<int>(),
                0,
                default,
                null,
                false,
                default,
                null,
                host);
            try
            {
                Assert.That(condition.Evaluate(new EventEvaluationContext(signal, null, null)).Passed, Is.False);
                host.ApplyStatus(ignite);
                Assert.That(condition.Evaluate(new EventEvaluationContext(signal, null, null)).Passed, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(collider);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(ignite);
                Object.DestroyImmediate(condition);
            }
        }

        private static EnemyStatusDefinition CreateIgnite()
        {
            EnemyStatusDefinition definition = ScriptableObject.CreateInstance<EnemyStatusDefinition>();
            SetField(definition, "statusId", "ignite");
            SetField(definition, "displayName", "点燃");
            return definition;
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
