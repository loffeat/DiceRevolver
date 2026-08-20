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
        public void PassiveEntryCoexistsWithEveryActiveSlot()
        {
            DiceFaceConfiguration configuration = new DiceFaceConfiguration();
            DiceFaceEntry baseEntry = CreateActiveEntry(DiceFaceSlotType.Base);
            DiceFaceEntry onFireEntry = CreateActiveEntry(DiceFaceSlotType.OnFire);
            DiceFaceEntry onHitEntry = CreateActiveEntry(DiceFaceSlotType.OnHit);
            DiceFaceEntry onFireEndEntry = CreateActiveEntry(DiceFaceSlotType.OnFireEnd);
            DiceFaceEntry passiveEntry = CreatePassiveEntry();

            try
            {
                configuration.Equip(baseEntry);
                configuration.Equip(onFireEntry);
                configuration.Equip(onHitEntry);
                configuration.Equip(onFireEndEntry);
                configuration.Equip(passiveEntry);

                DiceFaceConfigurationSnapshot snapshot = configuration.CreateSnapshot();
                Assert.That(snapshot.GetEntry(DiceFaceSlotType.Base), Is.SameAs(baseEntry));
                Assert.That(snapshot.GetEntry(DiceFaceSlotType.OnFire), Is.SameAs(onFireEntry));
                Assert.That(snapshot.GetEntry(DiceFaceSlotType.OnHit), Is.SameAs(onHitEntry));
                Assert.That(snapshot.GetEntry(DiceFaceSlotType.OnFireEnd), Is.SameAs(onFireEndEntry));
                Assert.That(snapshot.GetEntry(DiceFaceSlotType.Passive), Is.SameAs(passiveEntry));
                Assert.That(snapshot.GetPassiveEffect(), Is.SameAs(passiveEntry.PassiveEffect));
            }
            finally
            {
                DestroyEntry(passiveEntry);
                DestroyEntry(onFireEndEntry);
                DestroyEntry(onHitEntry);
                DestroyEntry(onFireEntry);
                DestroyEntry(baseEntry);
            }
        }

        [Test]
        public void PassiveSnapshotRemainsStableAfterLoadoutReplacement()
        {
            GameObject owner = new GameObject("Loadout");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            DiceFaceEntry original = CreatePassiveEntry();
            DiceFaceEntry replacement = CreatePassiveEntry();

            try
            {
                loadout.Equip(2, original);
                DiceFaceConfigurationSnapshot snapshot = loadout.GetSnapshot(2);
                loadout.Equip(2, replacement);

                Assert.That(snapshot.GetEntry(DiceFaceSlotType.Passive), Is.SameAs(original));
                Assert.That(loadout.GetEntry(2, DiceFaceSlotType.Passive), Is.SameAs(replacement));
            }
            finally
            {
                DestroyEntry(replacement);
                DestroyEntry(original);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void PassiveSlotUsesChineseLabel()
        {
            Assert.That(DiceFaceSlotType.Passive.ToChineseLabel(), Is.EqualTo("被动"));
        }

        private static DiceFaceEntry CreateActiveEntry(DiceFaceSlotType slotType)
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            EmptyBulletEffect effect = ScriptableObject.CreateInstance<EmptyBulletEffect>();
            SetField(entry, "slotType", slotType);
            SetField(entry, "effect", effect);
            return entry;
        }

        private static DiceFaceEntry CreatePassiveEntry()
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            EmptyPassiveEffect effect = ScriptableObject.CreateInstance<EmptyPassiveEffect>();
            SetField(entry, "slotType", DiceFaceSlotType.Passive);
            SetField(entry, "passiveEffect", effect);
            return entry;
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

        private sealed class EmptyPassiveEffect : PassiveEventEffect
        {
            public override IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context)
            {
                return new EmptyPassiveRuntime();
            }
        }

        private sealed class EmptyPassiveRuntime : IDicePassiveEffectRuntime
        {
            public bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces)
            {
                return true;
            }

            public void OnReloadStarted()
            {
            }

            public void OnReloadCompleted()
            {
            }

            public void OnFaceConsumed(int face)
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
