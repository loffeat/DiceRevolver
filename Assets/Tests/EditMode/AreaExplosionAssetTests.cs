using System.Linq;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class AreaExplosionAssetTests
    {
        private const string PrefabPath = "Assets/Prefab/Projectiles/BlastExplosion.prefab";
        private const string DefinitionPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/BlastExplosion.asset";
        private const string EffectPath =
            "Assets/Resources/DiceFacePrototype/BulletEvents/ExplosionOnHitEffect.asset";
        private const string LibraryPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/ProjectileDefinitionLibrary.asset";

        [Test]
        public void ExplosionPrefabOwnsAreaDamageAndCircularVisual()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Projectile>(), Is.Not.Null);
            AreaExplosionProjectile explosion = prefab.GetComponent<AreaExplosionProjectile>();
            Assert.That(explosion, Is.Not.Null);
            Assert.That(explosion.Radius, Is.EqualTo(2.5f));
            Assert.That(explosion.VisualDuration, Is.EqualTo(0.35f));
            LineRenderer ring = prefab.GetComponent<LineRenderer>();
            Assert.That(ring, Is.Not.Null);
            Assert.That(ring.loop, Is.True);
            Assert.That(ring.sortingLayerName, Is.EqualTo("projectile"));
            Assert.That(prefab.GetComponent<Collider>(), Is.Null);
        }

        [Test]
        public void ExplosionDefinitionProvidesDamageAndUsesExplosionPrefab()
        {
            ProjectileDefinition definition =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(DefinitionPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(definition.ProjectilePrefab), Is.EqualTo(PrefabPath));
            Assert.That(definition.BuildRuntimeStats().Damage, Is.EqualTo(3f));
            Assert.That(definition.DefaultAttackEffect, Is.False);
        }

        [Test]
        public void HitEffectAndProjectileLibraryReferenceExplosionDefinition()
        {
            ProjectileDefinition definition =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(DefinitionPath);
            ExplosionOnHitEffect effect =
                AssetDatabase.LoadAssetAtPath<ExplosionOnHitEffect>(EffectPath);
            ProjectileDefinitionLibrary library =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinitionLibrary>(LibraryPath);

            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.ExplosionProjectileDefinition, Is.SameAs(definition));
            Assert.That(library, Is.Not.Null);
            Assert.That(library.Definitions.Contains(definition), Is.True);
        }
    }
}
