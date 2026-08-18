using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DiceRevolver.Tests
{
    public sealed class TargetDummyTests
    {
        [Test]
        public void ReceiveDamageBroadcastsEveryHitWithoutDestroyingTarget()
        {
            GameObject owner = new GameObject("TargetDummy");
            TargetDummy target = owner.AddComponent<TargetDummy>();
            GameObject source = new GameObject("DamageSource");
            List<DamageInfo> received = new List<DamageInfo>();
            target.DamageReceived += received.Add;

            try
            {
                target.ReceiveDamage(new DamageInfo(4f, new Vector3(1f, 2f, 3f), source));
                target.ReceiveDamage(new DamageInfo(7.5f, new Vector3(4f, 5f, 6f), source));

                Assert.That(target, Is.Not.Null);
                Assert.That(target.HitCount, Is.EqualTo(2));
                Assert.That(received, Has.Count.EqualTo(2));
                Assert.That(received[0].Amount, Is.EqualTo(4f));
                Assert.That(received[0].HitPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
                Assert.That(received[0].Source, Is.SameAs(source));
                Assert.That(target.LastDamage.Amount, Is.EqualTo(7.5f));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ProjectileDeliversConfiguredDamageToReceiverOnColliderParent()
        {
            GameObject projectileOwner = new GameObject("Projectile");
            Projectile projectile = projectileOwner.AddComponent<Projectile>();
            projectile.Configure(new ProjectileRuntimeStats("Default", "PlayerBullet", 6.25f, 10f, 20f, 0));
            projectileOwner.transform.position = new Vector3(2f, 0.5f, -3f);

            GameObject targetOwner = new GameObject("TargetDummy");
            TargetDummy target = targetOwner.AddComponent<TargetDummy>();
            GameObject hitboxOwner = new GameObject("Hitbox");
            hitboxOwner.transform.SetParent(targetOwner.transform, false);
            BoxCollider hitbox = hitboxOwner.AddComponent<BoxCollider>();

            try
            {
                MethodInfo trigger = typeof(Projectile).GetMethod(
                    "OnTriggerEnter",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(trigger, Is.Not.Null);
                LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
                trigger.Invoke(projectile, new object[] { hitbox });

                Assert.That(target.HitCount, Is.EqualTo(1));
                Assert.That(target.LastDamage.Amount, Is.EqualTo(6.25f));
                Assert.That(target.LastDamage.HitPosition, Is.EqualTo(projectileOwner.transform.position));
                Assert.That(target.LastDamage.Source, Is.SameAs(projectileOwner));
            }
            finally
            {
                Object.DestroyImmediate(projectileOwner);
                Object.DestroyImmediate(targetOwner);
            }
        }

        [Test]
        public void DamageNumberFormatsIntegersAndSingleDecimalValues()
        {
            GameObject owner = new GameObject(
                "DamageNumber",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(CanvasGroup),
                typeof(WorldDamageNumber));
            WorldDamageNumber view = owner.GetComponent<WorldDamageNumber>();
            view.Configure(owner.GetComponent<Text>(), owner.GetComponent<CanvasGroup>());

            try
            {
                view.SetDamage(8f);
                Assert.That(view.DisplayText, Is.EqualTo("8"));

                view.SetDamage(6.25f);
                Assert.That(view.DisplayText, Is.EqualTo("6.3"));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void EveryHitSpawnsAnIndependentDamageNumber()
        {
            GameObject targetOwner = new GameObject("TargetDummy");
            TargetDummy target = targetOwner.AddComponent<TargetDummy>();
            WorldDamageNumberSpawner spawner = targetOwner.AddComponent<WorldDamageNumberSpawner>();

            GameObject templateOwner = new GameObject(
                "DamageNumberTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(CanvasGroup),
                typeof(WorldDamageNumber));
            templateOwner.transform.SetParent(targetOwner.transform, false);
            WorldDamageNumber template = templateOwner.GetComponent<WorldDamageNumber>();
            template.Configure(templateOwner.GetComponent<Text>(), templateOwner.GetComponent<CanvasGroup>());
            templateOwner.SetActive(false);
            spawner.Configure(target, template, targetOwner.transform);

            try
            {
                target.ReceiveDamage(new DamageInfo(3f, Vector3.zero, null));
                WorldDamageNumber first = spawner.LastSpawned;
                target.ReceiveDamage(new DamageInfo(7.5f, Vector3.one, null));
                WorldDamageNumber second = spawner.LastSpawned;

                Assert.That(spawner.SpawnedCount, Is.EqualTo(2));
                Assert.That(first, Is.Not.Null);
                Assert.That(second, Is.Not.Null);
                Assert.That(first, Is.Not.SameAs(second));
                Assert.That(first.DisplayText, Is.EqualTo("3"));
                Assert.That(second.DisplayText, Is.EqualTo("7.5"));
                Assert.That(first.gameObject.activeSelf, Is.True);
                Assert.That(second.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(targetOwner);
            }
        }
    }
}
