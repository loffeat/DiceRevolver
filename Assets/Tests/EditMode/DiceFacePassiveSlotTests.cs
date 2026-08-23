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
        public void PassiveListenerRuleEquipsAsPassiveBaseWithoutPrimaryProjectile()
        {
            SignalTypeTriggerModule trigger = ScriptableObject.CreateInstance<SignalTypeTriggerModule>();
            SetField(trigger, "signals", EventSignalMask.EnemyStatusApplied);
            TestResult result = ScriptableObject.CreateInstance<TestResult>();
            EventRuleDefinition rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
            SetField(rule, "allowedSlots", DiceFaceSlotMask.Base);
            SetField(rule, "trigger", trigger);
            SetField(rule, "results", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), result)
            });
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            SetField(entry, "slotType", DiceFaceSlotType.Base);
            SetField(entry, "isPassiveBase", true);
            SetField(entry, "rule", rule);
            DiceFaceConfiguration configuration = new DiceFaceConfiguration();
            try
            {
                Assert.That(rule.CanEquip(DiceFaceSlotType.Base), Is.True,
                    "被动监听规则（触发器不含基础信号）不应要求主弹丸定义");
                Assert.That(configuration.Equip(entry), Is.True);
                Assert.That(configuration.CreateSnapshot().IsPassiveFace, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(result);
                UnityEngine.Object.DestroyImmediate(trigger);
                UnityEngine.Object.DestroyImmediate(rule);
                UnityEngine.Object.DestroyImmediate(entry);
            }
        }

        [Test]
        public void BaseFiringRuleStillRequiresPrimaryProjectile()
        {
            SignalTypeTriggerModule trigger = ScriptableObject.CreateInstance<SignalTypeTriggerModule>();
            SetField(trigger, "signals", EventSignalMask.Base);
            TestResult result = ScriptableObject.CreateInstance<TestResult>();
            EventRuleDefinition rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
            SetField(rule, "allowedSlots", DiceFaceSlotMask.Base);
            SetField(rule, "trigger", trigger);
            SetField(rule, "results", new List<EventResultEntry>
            {
                new EventResultEntry(Array.Empty<EventConditionModule>(), result)
            });
            try
            {
                Assert.That(rule.CanEquip(DiceFaceSlotType.Base), Is.False,
                    "触发器含基础信号的规则仍必须提供主弹丸定义");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(result);
                UnityEngine.Object.DestroyImmediate(trigger);
                UnityEngine.Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void EntryDisplayMetadataFollowsTheBoundRule()
        {
            EventRuleDefinition rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
            SetField(rule, "displayName", "规则名");
            SetField(rule, "description", "规则描述");
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            SetField(entry, "displayName", "词条名");
            SetField(entry, "description", "词条描述");
            SetField(entry, "rule", rule);
            try
            {
                Assert.That(entry.DisplayName, Is.EqualTo("规则名"));
                Assert.That(entry.Description, Is.EqualTo("规则描述"));

                SetField(entry, "rule", null);
                Assert.That(entry.DisplayName, Is.EqualTo("词条名"));
                Assert.That(entry.Description, Is.EqualTo("词条描述"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rule);
                UnityEngine.Object.DestroyImmediate(entry);
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

        [Test]
        public void BurningBulletEntryEquipsIntoOnHitSlot()
        {
            DiceFaceEntry entry = Resources.Load<DiceFaceEntry>("DiceFacePrototype/DiceFaces/BurningBullet");
            Assert.That(entry, Is.Not.Null, "燃烧子弹词条应存在于 Resources");
            Assert.That(entry.Rule, Is.Not.Null, "燃烧子弹词条应绑定规则");

            List<EventRuleValidationIssue> issues = entry.Rule.CollectValidationIssues(DiceFaceSlotType.OnHit);
            string messages = string.Empty;
            if (issues != null && issues.Count > 0)
            {
                for (int index = 0; index < issues.Count; index++)
                {
                    messages += $"[{issues[index].Code}] {issues[index].Message} ";
                }
            }

            Assert.That(issues, Is.Empty, "燃烧子弹规则在命中时槽位不应有校验错误：" + messages);
            Assert.That(entry.Rule.CanEquip(DiceFaceSlotType.OnHit), Is.True);

            DiceFaceConfiguration configuration = new DiceFaceConfiguration();
            Assert.That(configuration.Equip(entry), Is.True, "燃烧子弹应可装备到命中时槽位");
            Assert.That(configuration.GetEntry(DiceFaceSlotType.OnHit), Is.SameAs(entry));
        }

        [Test]
        public void NewLightningRulesPassTheirSlotValidation()
        {
            AssertRuleValid("BurningBullet", DiceFaceSlotType.OnHit);
            AssertRuleValid("Tesla", DiceFaceSlotType.OnFire);
            AssertRuleValid("Finisher", DiceFaceSlotType.Base);
            AssertRuleValid("EchoSynergy", DiceFaceSlotType.Base);
        }

        private static void AssertRuleValid(string entryName, DiceFaceSlotType slot)
        {
            DiceFaceEntry entry = Resources.Load<DiceFaceEntry>($"DiceFacePrototype/DiceFaces/{entryName}");
            Assert.That(entry, Is.Not.Null, $"{entryName} 词条应存在");
            Assert.That(entry.Rule, Is.Not.Null, $"{entryName} 应绑定规则");
            Assert.That(entry.SlotType, Is.EqualTo(slot), $"{entryName} 词条槽位类型应为 {slot}");
            List<EventRuleValidationIssue> issues = entry.Rule.CollectValidationIssues(slot);
            Assert.That(issues, Is.Empty, $"{entryName} 在 {slot} 槽校验失败");
            Assert.That(entry.Rule.CanEquip(slot), Is.True, $"{entryName} 应可装备到 {slot}");
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
