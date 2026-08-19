using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class ProjectileDefinitionAssetTests
    {
        private const string PrefabPath = "Assets/Prefab/Projectiles/BasicRevolverBullet.prefab";
        private const string DefinitionPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/BasicRevolverBullet.asset";
        private const string LibraryPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/ProjectileDefinitionLibrary.asset";
        private const string SpawnEffectPath =
            "Assets/Resources/DiceFacePrototype/BulletEvents/FireBasicRevolverProjectile.asset";
        private const string FireVisualPath = "Assets/Art/Effect/perfab/fire_1.prefab";
        private const string PlayerPrefabPath = "Assets/Prefab/Player.prefab";

        [Test]
        public void BasicRevolverDefinitionOwnsPrefabStatsAndAttackDefault()
        {
            ProjectileDefinition definition =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(DefinitionPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.ProjectilePrefab, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(definition.ProjectilePrefab), Is.EqualTo(PrefabPath));
            Assert.That(definition.DefaultAttackEffect, Is.False);

            ProjectileRuntimeStats stats = definition.BuildRuntimeStats();
            Assert.That(stats.ProjectileType, Is.EqualTo("Revolver"));
            Assert.That(stats.ProjectileTag, Is.EqualTo("PlayerBullet"));
            Assert.That(stats.Damage, Is.EqualTo(1f));
            Assert.That(stats.FlightDistance, Is.EqualTo(18f));
            Assert.That(stats.FlightSpeed, Is.EqualTo(18f));
            Assert.That(stats.EnemyPierceCount, Is.Zero);
        }

        [Test]
        public void BasicRevolverPrefabWrapsFireOneWithoutModifyingIt()
        {
            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject fireVisual = AssetDatabase.LoadAssetAtPath<GameObject>(FireVisualPath);

            Assert.That(projectilePrefab, Is.Not.Null);
            Assert.That(projectilePrefab.GetComponent<Projectile>(), Is.Not.Null);
            Assert.That(projectilePrefab.GetComponent<ProjectileHitReporter>(), Is.Not.Null);
            Assert.That(projectilePrefab.GetComponent<SphereCollider>()?.isTrigger, Is.True);
            Assert.That(projectilePrefab.GetComponent<Rigidbody>()?.isKinematic, Is.True);

            ProjectileVisualWrapper wrapper = projectilePrefab.GetComponent<ProjectileVisualWrapper>();
            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.VisualPrefab, Is.SameAs(fireVisual));
            Assert.That(fireVisual.GetComponentInChildren<ParticleSystem>(true), Is.Not.Null);
            Assert.That(fireVisual.GetComponent<Projectile>(), Is.Null);
        }

        [Test]
        public void BasicRevolverPrefabUsesDoubleSizedVisualScale()
        {
            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            ProjectileVisualWrapper wrapper = projectilePrefab.GetComponent<ProjectileVisualWrapper>();
            FieldInfo visualScale = typeof(ProjectileVisualWrapper).GetField(
                "visualScale",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(wrapper, Is.Not.Null);
            Assert.That(visualScale, Is.Not.Null);
            Assert.That((float)visualScale.GetValue(wrapper), Is.EqualTo(0.4f));
        }

        [Test]
        public void BasicRevolverRuntimeVisualReplacesMissingSourceShader()
        {
            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = Object.Instantiate(projectilePrefab);

            try
            {
                ProjectileVisualWrapper wrapper = instance.GetComponent<ProjectileVisualWrapper>();
                MethodInfo awake = typeof(ProjectileVisualWrapper).GetMethod(
                    "Awake",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(awake, Is.Not.Null);
                awake.Invoke(wrapper, null);

                ParticleSystemRenderer[] renderers =
                    instance.GetComponentsInChildren<ParticleSystemRenderer>(true);

                Assert.That(renderers, Is.Not.Empty);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Assert.That(renderers[i].sharedMaterial, Is.Not.Null);
                    Assert.That(
                        renderers[i].sharedMaterial.shader.name,
                        Is.EqualTo("DiceRevolver/Projectile Particle Unlit"));
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void BasicRevolverRuntimeVisualUsesProjectileSortingLayer()
        {
            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = Object.Instantiate(projectilePrefab);

            try
            {
                ProjectileVisualWrapper wrapper = instance.GetComponent<ProjectileVisualWrapper>();
                MethodInfo awake = typeof(ProjectileVisualWrapper).GetMethod(
                    "Awake",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(awake, Is.Not.Null);
                awake.Invoke(wrapper, null);

                ParticleSystemRenderer[] renderers =
                    instance.GetComponentsInChildren<ParticleSystemRenderer>(true);

                Assert.That(renderers, Is.Not.Empty);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Assert.That(renderers[i].sortingLayerName, Is.EqualTo("projectile"));
                    Assert.That(renderers[i].sortingOrder, Is.Zero);
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void LibraryAndSpawnEffectReferenceTheBasicDefinition()
        {
            ProjectileDefinition definition =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(DefinitionPath);
            ProjectileDefinitionLibrary library =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinitionLibrary>(LibraryPath);
            ProjectileSpawnEffect spawnEffect =
                AssetDatabase.LoadAssetAtPath<ProjectileSpawnEffect>(SpawnEffectPath);

            Assert.That(library, Is.Not.Null);
            Assert.That(library.Definitions, Does.Contain(definition));
            Assert.That(library.Definitions[0], Is.SameAs(definition));
            Assert.That(spawnEffect, Is.Not.Null);
            Assert.That(spawnEffect.ProjectileDefinition, Is.SameAs(definition));
            Assert.That(spawnEffect.DelaySeconds, Is.Zero);
            Assert.That(spawnEffect.PrimaryProjectile, Is.True);
        }

        [Test]
        public void PlayerPrefabBindsTheBaseSpawnEffectToAllSixFaces()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            ProjectileSpawnEffect spawnEffect =
                AssetDatabase.LoadAssetAtPath<ProjectileSpawnEffect>(SpawnEffectPath);

            DiceFaceLoadout loadout = playerPrefab.GetComponent<DiceFaceLoadout>();
            Assert.That(loadout, Is.Not.Null);
            for (int face = 1; face <= 6; face++)
            {
                Assert.That(loadout.GetBaseEffect(face), Is.SameAs(spawnEffect), $"Face {face}");
            }
        }

        [Test]
        public void PrototypeConstructionEntriesMapToFourIndependentSlots()
        {
            DiceFaceEntry basicShot = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(
                "Assets/Resources/DiceFacePrototype/DiceFaces/BasicShot.asset");
            DiceFaceEntry doubleTap = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(
                "Assets/Resources/DiceFacePrototype/DiceFaces/DoubleTap.asset");
            DiceFaceEntry blastRound = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(
                "Assets/Resources/DiceFacePrototype/DiceFaces/BlastRound.asset");
            DiceFaceEntry loadedFour = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(
                "Assets/Resources/DiceFacePrototype/DiceFaces/LoadedFour.asset");

            Assert.That(basicShot, Is.Not.Null);
            Assert.That(basicShot.SlotType, Is.EqualTo(DiceFaceSlotType.Base));
            Assert.That(basicShot.Effect, Is.TypeOf<ProjectileSpawnEffect>());
            Assert.That(doubleTap.SlotType, Is.EqualTo(DiceFaceSlotType.OnFire));
            Assert.That(doubleTap.Effect, Is.TypeOf<ExtraShotOnFireEffect>());
            Assert.That(blastRound.SlotType, Is.EqualTo(DiceFaceSlotType.OnHit));
            Assert.That(blastRound.Effect, Is.TypeOf<ExplosionOnHitEffect>());
            Assert.That(loadedFour.SlotType, Is.EqualTo(DiceFaceSlotType.OnFireEnd));
            Assert.That(loadedFour.Effect, Is.TypeOf<ForceFaceFourOnFireEndEffect>());
        }
    }
}
