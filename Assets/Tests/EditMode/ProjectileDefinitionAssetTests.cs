using System.Linq;
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
        public void LibraryAndSpawnEffectReferenceTheBasicDefinition()
        {
            ProjectileDefinition definition =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(DefinitionPath);
            ProjectileDefinitionLibrary library =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinitionLibrary>(LibraryPath);
            ProjectileSpawnEffect spawnEffect =
                AssetDatabase.LoadAssetAtPath<ProjectileSpawnEffect>(SpawnEffectPath);

            Assert.That(library, Is.Not.Null);
            Assert.That(library.Definitions, Has.Count.EqualTo(1));
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
        public void ExistingConstructionEntriesKeepTheirIndependentEventStages()
        {
            DiceFaceEntry doubleTap = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(
                "Assets/Resources/DiceFacePrototype/DiceFaces/DoubleTap.asset");
            DiceFaceEntry blastRound = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(
                "Assets/Resources/DiceFacePrototype/DiceFaces/BlastRound.asset");
            DiceFaceEntry loadedFour = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(
                "Assets/Resources/DiceFacePrototype/DiceFaces/LoadedFour.asset");

            Assert.That(doubleTap.OnFireEffects.OfType<ExtraShotOnFireEffect>().Count(), Is.EqualTo(1));
            Assert.That(doubleTap.OnHitEffects, Is.Empty);
            Assert.That(doubleTap.OnFireEndEffects, Is.Empty);

            Assert.That(blastRound.OnFireEffects, Is.Empty);
            Assert.That(blastRound.OnHitEffects.OfType<ExplosionOnHitEffect>().Count(), Is.EqualTo(1));
            Assert.That(blastRound.OnFireEndEffects, Is.Empty);

            Assert.That(loadedFour.OnFireEffects, Is.Empty);
            Assert.That(loadedFour.OnHitEffects, Is.Empty);
            Assert.That(loadedFour.OnFireEndEffects.OfType<ForceFaceFourOnFireEndEffect>().Count(), Is.EqualTo(1));
        }
    }
}
