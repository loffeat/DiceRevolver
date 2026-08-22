using System;
using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class DiceFacePassiveSlotTests
    {
        [Test]
        public void PassiveBaseEntryMarksFaceAsPassiveAndEquipsIntoBaseSlot()
        {
            DiceFaceConfiguration configuration = new DiceFaceConfiguration();
            DiceFaceEntry baseEntry = CreateActiveEntry(DiceFaceSlotType.Base);
            DiceFaceEntry passiveBaseEntry = CreatePassiveBaseEntry();

            try
            {
                Assert.That(configuration.Equip(baseEntry), Is.True);
                DiceFaceConfigurationSnapshot normal = configuration.CreateSnapshot();
                Assert.That(normal.IsPassiveFace, Is.False);

                Assert.That(configuration.Equip(passiveBaseEntry), Is.True);
                DiceFaceConfigurationSnapshot passive = configuration.CreateSnapshot();
                Assert.That(passive.IsPassiveFace, Is.True);
                Assert.That(passive.GetEntry(DiceFaceSlotType.Base), Is.SameAs(passiveBaseEntry));
            }
            finally
            {
                DestroyEntry(passiveBaseEntry);
                DestroyEntry(baseEntry);
            }
        }

        [Test]
        public void PassiveBaseSnapshotRemainsStableAfterLoadoutReplacement()
        {
            GameObject owner = new GameObject("Loadout");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            DiceFaceEntry original = CreatePassiveBaseEntry();
            DiceFaceEntry replacement = CreatePassiveBaseEntry();

            try
            {
                loadout.Equip(2, original);
                DiceFaceConfigurationSnapshot snapshot = loadout.GetSnapshot(2);
                loadout.Equip(2, replacement);

                Assert.That(snapshot.IsPassiveFace, Is.True);
                Assert.That(loadout.GetEntry(2, DiceFaceSlotType.Base), Is.SameAs(replacement));
            }
            finally
            {
                DestroyEntry(replacement);
                DestroyEntry(original);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void SnapshotIsPassiveFaceFollowsBaseEntryFlag()
        {
            DiceFaceEntry passive = ScriptableObject.CreateInstance<DiceFaceEntry>();
            SetField(passive, "isPassiveBase", true);
            DiceFaceEntry normal = ScriptableObject.CreateInstance<DiceFaceEntry>();

            try
            {
                DiceFaceConfigurationSnapshot passiveSnapshot =
                    new DiceFaceConfigurationSnapshot(passive, null, null, null);
                DiceFaceConfigurationSnapshot normalSnapshot =
                    new DiceFaceConfigurationSnapshot(normal, null, null, null);
                Assert.That(passiveSnapshot.IsPassiveFace, Is.True);
                Assert.That(normalSnapshot.IsPassiveFace, Is.False);
            }
            finally
            {
                DestroyEntry(passive);
                DestroyEntry(normal);
            }
        }

        [Test]
        public void PassiveBaseEntryWithoutRuleEquipsWithoutValidation()
        {
            DiceFaceConfiguration configuration = new DiceFaceConfiguration();
            DiceFaceEntry passiveBaseEntry = CreatePassiveBaseEntry();

            try
            {
                Assert.That(configuration.Equip(passiveBaseEntry), Is.True);
            }
            finally
            {
                DestroyEntry(passiveBaseEntry);
            }
        }

        [Test]
        public void PassiveBaseEntryIsRejectedOutsideTheBaseSlot()
        {
            DiceFaceConfiguration configuration = new DiceFaceConfiguration();
            DiceFaceEntry passiveBaseEntry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            SetField(passiveBaseEntry, "slotType", DiceFaceSlotType.OnFire);
            SetField(passiveBaseEntry, "isPassiveBase", true);

            try
            {
                Assert.That(configuration.Equip(passiveBaseEntry), Is.False);
                Assert.That(configuration.GetEntry(DiceFaceSlotType.OnFire), Is.Null);
            }
            finally
            {
                DestroyEntry(passiveBaseEntry);
            }
        }

        [Test]
        public void LoadoutCollectsPassiveFacesFromBaseEntryFlags()
        {
            GameObject owner = new GameObject("Loadout");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            DiceFaceEntry passiveFace1 = CreatePassiveBaseEntry();
            DiceFaceEntry passiveFace3 = CreatePassiveBaseEntry();

            try
            {
                loadout.Equip(1, passiveFace1);
                loadout.Equip(3, passiveFace3);

                Assert.That(loadout.GetPassiveFaceSet(), Is.EqualTo(new[] { 1, 3 }));
            }
            finally
            {
                DestroyEntry(passiveFace3);
                DestroyEntry(passiveFace1);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void InvalidRuleEntriesLeaveThePreviouslyEquippedSlotUnchanged()
        {
            DiceFaceConfiguration configuration = new DiceFaceConfiguration();
            DiceFaceEntry previous = CreateActiveEntry(DiceFaceSlotType.OnFire);
            DiceFaceEntry missingTrigger = CreateRuleEntry(
                DiceFaceSlotType.OnFire,
                null,
                CreateResultList());
            TestTrigger trigger = ScriptableObject.CreateInstance<TestTrigger>();
            DiceFaceEntry missingResults = CreateRuleEntry(
                DiceFaceSlotType.OnFire,
                trigger,
                new List<EventResultEntry>());
            TestResult result = ScriptableObject.CreateInstance<TestResult>();
            DiceFaceEntry slotConflict = CreateRuleEntry(
                DiceFaceSlotType.OnFire,
                trigger,
                new List<EventResultEntry>
                {
                    new EventResultEntry(Array.Empty<EventConditionModule>(), result)
                },
                DiceFaceSlotMask.OnHit);

            try
            {
                Assert.That(configuration.Equip(previous), Is.True);
                Assert.That(configuration.Equip(missingTrigger), Is.False);
                Assert.That(configuration.GetEntry(DiceFaceSlotType.OnFire), Is.SameAs(previous));
                Assert.That(configuration.Equip(missingResults), Is.False);
                Assert.That(configuration.GetEntry(DiceFaceSlotType.OnFire), Is.SameAs(previous));
                Assert.That(configuration.Equip(slotConflict), Is.False);
                Assert.That(configuration.GetEntry(DiceFaceSlotType.OnFire), Is.SameAs(previous));
            }
            finally
            {
                DestroyRuleEntry(slotConflict);
                DestroyRuleEntry(missingResults);
                DestroyRuleEntry(missingTrigger);
                UnityEngine.Object.DestroyImmediate(trigger);
                DestroyEntry(previous);
            }
        }

        private static DiceFaceEntry CreateActiveEntry(DiceFaceSlotType slotType)
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            EmptyBulletEffect effect = ScriptableObject.CreateInstance<EmptyBulletEffect>();
            SetField(entry, "slotType", slotType);
            SetField(entry, "effect", effect);
            return entry;
        }

        private static DiceFaceEntry CreatePassiveBaseEntry()
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            SetField(entry, "slotType", DiceFaceSlotType.Base);
            SetField(entry, "isPassiveBase", true);
            return entry;
        }

        private static DiceFaceEntry CreateRuleEntry(
            DiceFaceSlotType slotType,
            EventTriggerModule trigger,
            List<EventResultEntry> results,
            DiceFaceSlotMask allowedSlots = DiceFaceSlotMask.OnFire)
        {
            EventRuleDefinition rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
            SetField(rule, "allowedSlots", allowedSlots);
            SetField(rule, "trigger", trigger);
            SetField(rule, "results", results);
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            SetField(entry, "slotType", slotType);
            SetField(entry, "rule", rule);
            return entry;
        }

        private static List<EventResultEntry> CreateResultList()
        {
            TestResult result = ScriptableObject.CreateInstance<TestResult>();
            return new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), result)
            };
        }

        private static void DestroyRuleEntry(DiceFaceEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            EventRuleDefinition rule = entry.Rule;
            if (rule != null)
            {
                IReadOnlyList<EventResultEntry> results = rule.Results;
                if (results != null)
                {
                    for (int index = 0; index < results.Count; index++)
                    {
                        if (results[index]?.Result != null)
                        {
                            UnityEngine.Object.DestroyImmediate(results[index].Result);
                        }
                    }
                }

                UnityEngine.Object.DestroyImmediate(rule);
            }

            UnityEngine.Object.DestroyImmediate(entry);
        }

        private static void DestroyEntry(DiceFaceEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.Effect != null)
            {
                UnityEngine.Object.DestroyImmediate(entry.Effect);
            }

            if (entry.PassiveEffect != null)
            {
                UnityEngine.Object.DestroyImmediate(entry.PassiveEffect);
            }

            UnityEngine.Object.DestroyImmediate(entry);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{name}");
            field.SetValue(target, value);
        }

        private sealed class EmptyBulletEffect : BulletEventEffect
        {
            public override void Trigger(BulletEventContext context)
            {
            }
        }

        private sealed class TestTrigger : EventTriggerModule
        {
            public override bool Matches(EventSignal signal) => true;
        }

        private sealed class TestResult : EventResultModule
        {
            public override EventResult Execute(EventExecutionContext context) =>
                new EventResult(EventResultStatus.Success, "test");
        }
    }
}
