using DiceRevolver.Prototype;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class DiceFaceLoadoutTests
    {
        [Test]
        public void EquipStoresEntryForFace()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();

            loadout.Equip(3, entry);

            Assert.That(loadout.GetEntry(3), Is.SameAs(entry));

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(entry);
        }

        [Test]
        public void EquipIgnoresFacesOutsideOneToSix()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();

            loadout.Equip(0, entry);
            loadout.Equip(7, entry);

            Assert.That(loadout.GetEntry(0), Is.Null);
            Assert.That(loadout.GetEntry(7), Is.Null);

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(entry);
        }

        [Test]
        public void EquipRaisesEntryChangedForValidFace()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            int changedFace = 0;
            DiceFaceEntry changedEntry = null;
            loadout.EntryChanged += (face, changed) =>
            {
                changedFace = face;
                changedEntry = changed;
            };

            loadout.Equip(3, entry);

            Assert.That(changedFace, Is.EqualTo(3));
            Assert.That(changedEntry, Is.SameAs(entry));

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(entry);
        }

        [Test]
        public void EmptyDataAssetsExposeEmptyCollections()
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            DiceFaceLibrary faceLibrary = ScriptableObject.CreateInstance<DiceFaceLibrary>();
            BulletEventLibrary eventLibrary = ScriptableObject.CreateInstance<BulletEventLibrary>();

            Assert.That(entry.OnFireEffects, Is.Not.Null);
            Assert.That(entry.OnHitEffects, Is.Not.Null);
            Assert.That(entry.OnFireEndEffects, Is.Not.Null);
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
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            FieldInfo entriesField = typeof(DiceFaceLoadout).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
            entriesField.SetValue(loadout, new DiceFaceEntry[1]);

            loadout.Equip(6, entry);

            Assert.That(loadout.GetEntry(6), Is.SameAs(entry));

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(entry);
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

        private sealed class TestBulletEventEffect : BulletEventEffect
        {
            public override void Trigger(BulletEventContext context)
            {
            }
        }
    }
}
