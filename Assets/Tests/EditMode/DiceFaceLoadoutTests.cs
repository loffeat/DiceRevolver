using DiceRevolver.Prototype;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class DiceFaceLoadoutTests
    {
        [Test]
        public void FourSlotEntriesCoexistOnOneFace()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            List<Object> created = new List<Object>();
            DiceFaceEntry baseEntry = CreateEntry(DiceFaceSlotType.Base, created);
            DiceFaceEntry onFireEntry = CreateEntry(DiceFaceSlotType.OnFire, created);
            DiceFaceEntry onHitEntry = CreateEntry(DiceFaceSlotType.OnHit, created);
            DiceFaceEntry onFireEndEntry = CreateEntry(DiceFaceSlotType.OnFireEnd, created);

            loadout.Equip(3, baseEntry);
            loadout.Equip(3, onFireEntry);
            loadout.Equip(3, onHitEntry);
            loadout.Equip(3, onFireEndEntry);

            Assert.That(loadout.GetEntry(3, DiceFaceSlotType.Base), Is.SameAs(baseEntry));
            Assert.That(loadout.GetEntry(3, DiceFaceSlotType.OnFire), Is.SameAs(onFireEntry));
            Assert.That(loadout.GetEntry(3, DiceFaceSlotType.OnHit), Is.SameAs(onHitEntry));
            Assert.That(loadout.GetEntry(3, DiceFaceSlotType.OnFireEnd), Is.SameAs(onFireEndEntry));

            Object.DestroyImmediate(owner);
            DestroyAll(created);
        }

        [Test]
        public void ReplacingOneSlotPreservesTheOtherThree()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            List<Object> created = new List<Object>();
            DiceFaceEntry baseEntry = CreateEntry(DiceFaceSlotType.Base, created);
            DiceFaceEntry firstOnFire = CreateEntry(DiceFaceSlotType.OnFire, created);
            DiceFaceEntry secondOnFire = CreateEntry(DiceFaceSlotType.OnFire, created);
            DiceFaceEntry onHitEntry = CreateEntry(DiceFaceSlotType.OnHit, created);
            DiceFaceEntry onFireEndEntry = CreateEntry(DiceFaceSlotType.OnFireEnd, created);

            loadout.Equip(2, baseEntry);
            loadout.Equip(2, firstOnFire);
            loadout.Equip(2, onHitEntry);
            loadout.Equip(2, onFireEndEntry);
            loadout.Equip(2, secondOnFire);

            Assert.That(loadout.GetEntry(2, DiceFaceSlotType.Base), Is.SameAs(baseEntry));
            Assert.That(loadout.GetEntry(2, DiceFaceSlotType.OnFire), Is.SameAs(secondOnFire));
            Assert.That(loadout.GetEntry(2, DiceFaceSlotType.OnHit), Is.SameAs(onHitEntry));
            Assert.That(loadout.GetEntry(2, DiceFaceSlotType.OnFireEnd), Is.SameAs(onFireEndEntry));

            Object.DestroyImmediate(owner);
            DestroyAll(created);
        }

        [Test]
        public void EquipIgnoresFacesOutsideOneToSix()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();

            loadout.Equip(0, entry);
            loadout.Equip(7, entry);

            Assert.That(loadout.GetEntry(0, DiceFaceSlotType.OnFire), Is.Null);
            Assert.That(loadout.GetEntry(7, DiceFaceSlotType.OnFire), Is.Null);

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(entry);
        }

        [Test]
        public void EquipRaisesEntryChangedForValidFace()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            List<Object> created = new List<Object>();
            DiceFaceEntry entry = CreateEntry(DiceFaceSlotType.OnHit, created);
            int changedFace = 0;
            DiceFaceSlotType changedSlot = DiceFaceSlotType.Base;
            DiceFaceEntry changedEntry = null;
            loadout.SlotChanged += (face, slot, changed) =>
            {
                changedFace = face;
                changedSlot = slot;
                changedEntry = changed;
            };

            loadout.Equip(3, entry);

            Assert.That(changedFace, Is.EqualTo(3));
            Assert.That(changedSlot, Is.EqualTo(DiceFaceSlotType.OnHit));
            Assert.That(changedEntry, Is.SameAs(entry));

            Object.DestroyImmediate(owner);
            DestroyAll(created);
        }

        [Test]
        public void EmptyDataAssetsExposeEmptyCollections()
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            DiceFaceLibrary faceLibrary = ScriptableObject.CreateInstance<DiceFaceLibrary>();
            BulletEventLibrary eventLibrary = ScriptableObject.CreateInstance<BulletEventLibrary>();

            Assert.That(entry.Effect, Is.Null);
            Assert.That(faceLibrary.Entries, Is.Not.Null);
            Assert.That(eventLibrary.Effects, Is.Not.Null);
            Assert.That(faceLibrary.Entries.Count, Is.EqualTo(0));
            Assert.That(eventLibrary.Effects.Count, Is.EqualTo(0));

            Object.DestroyImmediate(entry);
            Object.DestroyImmediate(faceLibrary);
            Object.DestroyImmediate(eventLibrary);
        }

        [Test]
        public void LoadoutRepairsMalformedSerializedSlotArray()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            List<Object> created = new List<Object>();
            DiceFaceEntry entry = CreateEntry(DiceFaceSlotType.OnFire, created);
            FieldInfo entriesField = typeof(DiceFaceLoadout).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
            entriesField.SetValue(loadout, new DiceFaceEntry[1]);

            loadout.Equip(6, entry);

            Assert.That(loadout.GetEntry(6, DiceFaceSlotType.OnFire), Is.SameAs(entry));

            Object.DestroyImmediate(owner);
            DestroyAll(created);
        }

        [Test]
        public void BaseEffectSlotsAreIndependentForEveryFace()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            TestBulletEventEffect faceTwoEffect = ScriptableObject.CreateInstance<TestBulletEventEffect>();
            TestBulletEventEffect faceSixEffect = ScriptableObject.CreateInstance<TestBulletEventEffect>();

            loadout.SetBaseEffect(2, faceTwoEffect);
            loadout.SetBaseEffect(6, faceSixEffect);

            Assert.That(loadout.GetBaseEffect(1), Is.Null);
            Assert.That(loadout.GetBaseEffect(2), Is.SameAs(faceTwoEffect));
            Assert.That(loadout.GetBaseEffect(6), Is.SameAs(faceSixEffect));

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(faceTwoEffect);
            Object.DestroyImmediate(faceSixEffect);
        }

        [Test]
        public void BaseEffectSlotsRepairMalformedSerializedArray()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            TestBulletEventEffect effect = ScriptableObject.CreateInstance<TestBulletEventEffect>();
            FieldInfo field = typeof(DiceFaceLoadout).GetField(
                "baseEffects",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(loadout, new BulletEventEffect[1]);

            loadout.SetBaseEffect(6, effect);

            Assert.That(loadout.GetBaseEffect(6), Is.SameAs(effect));

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void SnapshotRemainsStableAfterAWorkingLoadoutSlotChanges()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            List<Object> created = new List<Object>();
            DiceFaceEntry original = CreateEntry(DiceFaceSlotType.OnHit, created);
            DiceFaceEntry replacement = CreateEntry(DiceFaceSlotType.OnHit, created);

            loadout.Equip(5, original);
            DiceFaceConfigurationSnapshot snapshot = loadout.GetSnapshot(5);
            loadout.Equip(5, replacement);

            Assert.That(snapshot.GetEntry(DiceFaceSlotType.OnHit), Is.SameAs(original));
            Assert.That(loadout.GetEntry(5, DiceFaceSlotType.OnHit), Is.SameAs(replacement));

            Object.DestroyImmediate(owner);
            DestroyAll(created);
        }

        [Test]
        public void LegacyBaseEffectIsAvailableThroughSnapshot()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            TestBulletEventEffect effect = ScriptableObject.CreateInstance<TestBulletEventEffect>();

            loadout.SetBaseEffect(4, effect);

            DiceFaceConfigurationSnapshot snapshot = loadout.GetSnapshot(4);

            Assert.That(snapshot.GetEffect(DiceFaceSlotType.Base), Is.SameAs(effect));

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(effect);
        }

        private static DiceFaceEntry CreateEntry(DiceFaceSlotType slotType, ICollection<Object> created)
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            TestBulletEventEffect effect = ScriptableObject.CreateInstance<TestBulletEventEffect>();
            typeof(DiceFaceEntry).GetField("slotType", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(entry, slotType);
            typeof(DiceFaceEntry).GetField("effect", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(entry, effect);
            created.Add(entry);
            created.Add(effect);
            return entry;
        }

        private static void DestroyAll(IEnumerable<Object> objects)
        {
            foreach (Object target in objects)
            {
                Object.DestroyImmediate(target);
            }
        }

        private sealed class TestBulletEventEffect : BulletEventEffect
        {
            public override void Trigger(BulletEventContext context)
            {
            }
        }
    }
}
