using System;
using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class ChainReactionTests
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = owned.Count - 1; index >= 0; index--)
            {
                if (owned[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(owned[index]);
                }
            }

            owned.Clear();
        }

        [Test]
        public void ChainReactionOverlaysOnlyNonEmptyActiveSlotsAndPreservesTargetPassive()
        {
            DiceFaceEntry sourceBase = Entry(DiceFaceSlotType.Base);
            DiceFaceEntry sourceOnFire = Entry(DiceFaceSlotType.OnFire);
            ChainReactionOnFireEndEffect chain = Own(
                ScriptableObject.CreateInstance<ChainReactionOnFireEndEffect>());
            DiceFaceEntry sourceChain = Entry(DiceFaceSlotType.OnFireEnd, chain);
            DiceFaceConfigurationSnapshot source = new DiceFaceConfigurationSnapshot(
                sourceBase,
                sourceOnFire,
                null,
                sourceChain,
                null);
            DiceFaceEntry targetBase = Entry(DiceFaceSlotType.Base);
            DiceFaceEntry targetOnHit = Entry(DiceFaceSlotType.OnHit);
            DiceFaceEntry targetOnFireEnd = Entry(DiceFaceSlotType.OnFireEnd);
            DiceFaceEntry targetPassive = PassiveEntry();
            DiceFaceConfigurationSnapshot target = new DiceFaceConfigurationSnapshot(
                targetBase,
                null,
                targetOnHit,
                targetOnFireEnd,
                targetPassive);
            DiceShotPipeline pipeline = CreatePipeline();

            pipeline.ExecuteShot(1, source, Vector3.zero, Vector3.forward, 8, null, null);
            DiceFaceActivation activation = pipeline.ExecuteShot(
                5,
                target,
                Vector3.zero,
                Vector3.forward,
                8,
                null,
                null);

            Assert.That(activation.Configuration.GetEntry(DiceFaceSlotType.Base), Is.SameAs(sourceBase));
            Assert.That(activation.Configuration.GetEntry(DiceFaceSlotType.OnFire), Is.SameAs(sourceOnFire));
            Assert.That(activation.Configuration.GetEntry(DiceFaceSlotType.OnHit), Is.SameAs(targetOnHit));
            Assert.That(activation.Configuration.GetEntry(DiceFaceSlotType.OnFireEnd), Is.SameAs(targetOnFireEnd));
            Assert.That(activation.Configuration.GetEntry(DiceFaceSlotType.Passive), Is.SameAs(targetPassive));
            Assert.That(target.GetEntry(DiceFaceSlotType.Base), Is.SameAs(targetBase));
        }

        [Test]
        public void LaterNonEmptyOverlayWinsAndEmptySlotsNeverErase()
        {
            DiceFaceEntry firstBase = Entry(DiceFaceSlotType.Base);
            DiceFaceEntry firstOnHit = Entry(DiceFaceSlotType.OnHit);
            DiceFaceEntry laterBase = Entry(DiceFaceSlotType.Base);
            DiceFaceEntry targetOnFire = Entry(DiceFaceSlotType.OnFire);
            DiceShotPipeline pipeline = CreatePipeline();
            pipeline.QueueNextShotOverlay(new DiceFaceActiveOverlay(
                firstBase,
                null,
                firstOnHit,
                null));
            pipeline.QueueNextShotOverlay(new DiceFaceActiveOverlay(
                laterBase,
                null,
                null,
                null));

            DiceFaceActivation activation = pipeline.ExecuteShot(
                2,
                new DiceFaceConfigurationSnapshot(null, targetOnFire, null, null),
                Vector3.zero,
                Vector3.forward,
                8,
                null,
                null);

            Assert.That(activation.Configuration.GetEntry(DiceFaceSlotType.Base), Is.SameAs(laterBase));
            Assert.That(activation.Configuration.GetEntry(DiceFaceSlotType.OnFire), Is.SameAs(targetOnFire));
            Assert.That(activation.Configuration.GetEntry(DiceFaceSlotType.OnHit), Is.SameAs(firstOnHit));
        }

        [Test]
        public void BonusActivationDoesNotConsumePendingNormalShotOverlay()
        {
            DiceFaceEntry overlayBase = Entry(DiceFaceSlotType.Base);
            DiceFaceEntry bonusBase = Entry(DiceFaceSlotType.Base);
            DiceFaceEntry normalBase = Entry(DiceFaceSlotType.Base);
            DiceShotPipeline pipeline = CreatePipeline();
            pipeline.QueueNextShotOverlay(new DiceFaceActiveOverlay(
                overlayBase,
                null,
                null,
                null));

            DiceFaceActivation bonus = pipeline.ExecuteBonusShot(
                3,
                new DiceFaceConfigurationSnapshot(bonusBase, null, null, null),
                Vector3.zero,
                Vector3.forward,
                new DiceEventBudget(8),
                1,
                null,
                null);
            DiceFaceActivation normal = pipeline.ExecuteShot(
                4,
                new DiceFaceConfigurationSnapshot(normalBase, null, null, null),
                Vector3.zero,
                Vector3.forward,
                8,
                null,
                null);

            Assert.That(bonus.Configuration.GetEntry(DiceFaceSlotType.Base), Is.SameAs(bonusBase));
            Assert.That(normal.Configuration.GetEntry(DiceFaceSlotType.Base), Is.SameAs(overlayBase));
        }

        [Test]
        public void OverlayIsOneShotAndReloadClearsUnusedOverlay()
        {
            DiceFaceEntry overlayBase = Entry(DiceFaceSlotType.Base);
            DiceFaceEntry targetBase = Entry(DiceFaceSlotType.Base);
            DiceShotPipeline pipeline = CreatePipeline();
            pipeline.QueueNextShotOverlay(new DiceFaceActiveOverlay(
                overlayBase,
                null,
                null,
                null));
            pipeline.ClearForReload();

            DiceFaceActivation afterReload = pipeline.ExecuteShot(
                1,
                new DiceFaceConfigurationSnapshot(targetBase, null, null, null),
                Vector3.zero,
                Vector3.forward,
                8,
                null,
                null);
            pipeline.QueueNextShotOverlay(new DiceFaceActiveOverlay(
                overlayBase,
                null,
                null,
                null));
            pipeline.ExecuteShot(
                2,
                new DiceFaceConfigurationSnapshot(targetBase, null, null, null),
                Vector3.zero,
                Vector3.forward,
                8,
                null,
                null);
            DiceFaceActivation following = pipeline.ExecuteShot(
                3,
                new DiceFaceConfigurationSnapshot(targetBase, null, null, null),
                Vector3.zero,
                Vector3.forward,
                8,
                null,
                null);

            Assert.That(afterReload.Configuration.GetEntry(DiceFaceSlotType.Base), Is.SameAs(targetBase));
            Assert.That(following.Configuration.GetEntry(DiceFaceSlotType.Base), Is.SameAs(targetBase));
        }

        private DiceShotPipeline CreatePipeline()
        {
            return new DiceShotPipeline(
                () => 0f,
                (Action<DiceFaceActivation, ProjectileSpawnRequest>)null,
                null,
                null,
                null);
        }

        private DiceFaceEntry Entry(
            DiceFaceSlotType slotType,
            BulletEventEffect effect = null)
        {
            DiceFaceEntry entry = Own(ScriptableObject.CreateInstance<DiceFaceEntry>());
            SetField(entry, "slotType", slotType);
            SetField(entry, "effect", effect ?? Own(
                ScriptableObject.CreateInstance<EmptyEffect>()));
            return entry;
        }

        private DiceFaceEntry PassiveEntry()
        {
            DiceFaceEntry entry = Own(ScriptableObject.CreateInstance<DiceFaceEntry>());
            SetField(entry, "slotType", DiceFaceSlotType.Passive);
            return entry;
        }

        private T Own<T>(T target) where T : UnityEngine.Object
        {
            owned.Add(target);
            return target;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private sealed class EmptyEffect : BulletEventEffect
        {
            public override void Trigger(BulletEventContext context)
            {
            }
        }
    }
}
