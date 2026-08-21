using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class EventRuleModelTests
    {
        [Test]
        public void RuleDefinitionKeepsOrderedResultsAndAllowedSlots()
        {
            EventRuleDefinition rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
            TestTrigger trigger = ScriptableObject.CreateInstance<TestTrigger>();
            TestResult first = ScriptableObject.CreateInstance<TestResult>();
            TestResult second = ScriptableObject.CreateInstance<TestResult>();

            try
            {
                Set(rule, "allowedSlots", DiceFaceSlotMask.OnFire | DiceFaceSlotMask.OnHit);
                Set(rule, "trigger", trigger);
                Set(rule, "results", new List<EventResultEntry>
                {
                    new EventResultEntry(Array.Empty<EventConditionModule>(), first),
                    new EventResultEntry(Array.Empty<EventConditionModule>(), second)
                });

                Assert.That(rule.AllowsSlot(DiceFaceSlotType.OnFire), Is.True);
                Assert.That(rule.AllowsSlot(DiceFaceSlotType.Passive), Is.False);
                Assert.That(rule.Results.Select(entry => entry.Result), Is.EqualTo(new[] { first, second }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(trigger);
                UnityEngine.Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void SignalKeepsConstructorValuesAvailableWithoutSetters()
        {
            List<int> remainingFaces = new List<int> { 1, 3, 5 };
            DiceEventBudget budget = new DiceEventBudget(7);
            CombatDebugScope scope = new CombatDebugScope(11, 12, 13, 2, 4);
            EventSignal signal = new EventSignal(
                EventSignalType.OnHit,
                4,
                2,
                DiceFaceSlotType.OnHit,
                null,
                null,
                default,
                null,
                new Vector3(1f, 2f, 3f),
                remainingFaces,
                6,
                default,
                budget,
                true,
                scope);

            Assert.That(signal.SignalType, Is.EqualTo(EventSignalType.OnHit));
            Assert.That(signal.EquippedFace, Is.EqualTo(4));
            Assert.That(signal.SourceFace, Is.EqualTo(2));
            Assert.That(signal.Slot, Is.EqualTo(DiceFaceSlotType.OnHit));
            Assert.That(signal.HitPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(signal.RemainingFaces, Is.SameAs(remainingFaces));
            Assert.That(signal.DrawCandidate, Is.EqualTo(6));
            Assert.That(signal.EventBudget, Is.SameAs(budget));
            Assert.That(signal.IsBonusActivation, Is.True);
            Assert.That(signal.DebugScope, Is.EqualTo(scope));
            Assert.That(typeof(EventSignal).GetProperties().All(property => !property.CanWrite), Is.True);
        }

        [Test]
        public void StateStoresAreIndependentForTheSameModuleAsset()
        {
            TestResult module = ScriptableObject.CreateInstance<TestResult>();
            EventRuleStateStore first = new EventRuleStateStore();
            EventRuleStateStore second = new EventRuleStateStore();

            try
            {
                first.SetInt(module, "count", 3);

                Assert.That(first.GetInt(module, "count"), Is.EqualTo(3));
                Assert.That(second.GetInt(module, "count"), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(module);
            }
        }

        [Test]
        public void ValidationBlocksInvalidEquipmentWithoutChangingTheRule()
        {
            EventRuleDefinition rule = ScriptableObject.CreateInstance<EventRuleDefinition>();

            try
            {
                Set(rule, "allowedSlots", DiceFaceSlotMask.OnFire);

                List<EventRuleValidationIssue> issues = rule.CollectValidationIssues(DiceFaceSlotType.Passive);

                Assert.That(rule.CanEquip(DiceFaceSlotType.Passive), Is.False);
                Assert.That(issues.Any(issue => issue.Code == "slot-not-allowed"), Is.True);
                Assert.That(issues.Any(issue => issue.Code == "missing-trigger"), Is.True);
                Assert.That(issues.Any(issue => issue.Code == "missing-results"), Is.True);
                Assert.That(rule.AllowedSlots, Is.EqualTo(DiceFaceSlotMask.OnFire));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void StateStoreKeepsValuesSeparatedByTypeAndValidatesKeys()
        {
            TestResult module = ScriptableObject.CreateInstance<TestResult>();
            EventRuleStateStore state = new EventRuleStateStore();

            try
            {
                state.SetFloat(module, "value", 2.5f);
                state.SetBool(module, "enabled", true);

                Assert.That(state.GetFloat(module, "value"), Is.EqualTo(2.5f));
                Assert.That(state.GetBool(module, "enabled"), Is.True);
                Assert.That(state.GetInt(module, "value", 9), Is.EqualTo(9));
                Assert.That(() => state.SetInt(null, "count", 1), Throws.ArgumentException);
                Assert.That(() => state.GetBool(module, " "), Throws.ArgumentException);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(module);
            }
        }

        private static void Set(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private sealed class TestTrigger : EventTriggerModule
        {
            public override bool Matches(EventSignal signal)
            {
                return true;
            }
        }

        private sealed class TestResult : EventResultModule
        {
            public override EventResult Execute(EventExecutionContext context)
            {
                return new EventResult(EventResultStatus.Success, "test");
            }
        }
    }
}
