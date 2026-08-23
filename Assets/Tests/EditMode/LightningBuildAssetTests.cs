using System.Linq;
using DiceRevolver.Editor;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class LightningBuildAssetTests
    {
        private const string Root = "Assets/Resources/DiceFacePrototype";

        [Test]
        public void LightningOrbAssetUsesApprovedTypeTagsAndRuntimeDefaults()
        {
            ProjectileDefinition definition = AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(
                Root + "/Projectiles/LightningOrb.asset");
            ProjectileTypeDefinition type = AssetDatabase.LoadAssetAtPath<ProjectileTypeDefinition>(
                Root + "/ProjectileTypes/LightningOrb.asset");
            ProjectileTagDefinition lightning = AssetDatabase.LoadAssetAtPath<ProjectileTagDefinition>(
                Root + "/ProjectileTags/Lightning.asset");
            ProjectileTagDefinition elemental = AssetDatabase.LoadAssetAtPath<ProjectileTagDefinition>(
                Root + "/ProjectileTags/Elemental.asset");

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.ProjectilePrefab, Is.Not.Null);
            Assert.That(definition.ProjectileTypeDefinition, Is.SameAs(type));
            Assert.That(definition.ProjectileTags, Does.Contain(lightning));
            Assert.That(definition.ProjectileTags, Does.Contain(elemental));
            Assert.That(definition.DefaultAttackEffect, Is.False);
            ProjectileRuntimeStats stats = definition.BuildRuntimeStats();
            Assert.That(stats.Damage, Is.EqualTo(1f));
            Assert.That(stats.FlightSpeed, Is.EqualTo(5f));
            Assert.That(stats.FlightDistance, Is.EqualTo(15f));
            Assert.That(stats.EnemyPierceCount, Is.EqualTo(4));
            Assert.That(
                definition.ProjectilePrefab.GetComponent<SphereCollider>().radius,
                Is.EqualTo(0.35f));
            Assert.That(
                definition.ProjectilePrefab.GetComponent<ProjectileVisualWrapper>().VisualPrefab,
                Is.Not.Null);
        }

        [Test]
        public void LightningLibrariesContainEveryNewResource()
        {
            ProjectileTypeLibrary typeLibrary =
                AssetDatabase.LoadAssetAtPath<ProjectileTypeLibrary>(
                    Root + "/ProjectileTypes/ProjectileTypeLibrary.asset");
            ProjectileTagLibrary tagLibrary =
                AssetDatabase.LoadAssetAtPath<ProjectileTagLibrary>(
                    Root + "/ProjectileTags/ProjectileTagLibrary.asset");
            ProjectileDefinitionLibrary projectileLibrary =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinitionLibrary>(
                    Root + "/Projectiles/ProjectileDefinitionLibrary.asset");
            DiceFaceLibrary faceLibrary = AssetDatabase.LoadAssetAtPath<DiceFaceLibrary>(
                Root + "/DiceFaceLibrary.asset");
            BulletEventLibrary eventLibrary = AssetDatabase.LoadAssetAtPath<BulletEventLibrary>(
                Root + "/BulletEventLibrary.asset");

            Assert.That(typeLibrary, Is.Not.Null);
            Assert.That(typeLibrary.Types.Select(item => item.DisplayName),
                Is.EquivalentTo(new[] { "LightningOrb", "LightningChain" }));
            Assert.That(tagLibrary, Is.Not.Null);
            Assert.That(tagLibrary.Tags.Select(item => item.DisplayName),
                Is.EquivalentTo(new[] { "Lightning", "Elemental" }));
            Assert.That(projectileLibrary.Definitions.Any(item => item.name == "LightningOrb"), Is.True);
            string[] newEntries =
            {
                "LightningOrb",
                "Finisher",
                "ElectromagneticResonance",
                "Tesla",
                "EchoSynergy",
                "ChainReaction"
            };
            Assert.That(faceLibrary.Entries.Select(item => item.name), Does.Contain(newEntries[0]));
            foreach (string entryName in newEntries)
            {
                Assert.That(faceLibrary.Entries.Any(item => item.name == entryName), Is.True);
            }

            Assert.That(eventLibrary.Effects.Any(item => item is ProjectileSpawnEffect &&
                ((ProjectileSpawnEffect)item).ProjectileDefinition?.name == "LightningOrb"), Is.True);
        }

        [Test]
        public void LightningEntriesUseApprovedIndependentSlots()
        {
            EventRuleMigrationUtility.MigratePassiveBaseEvents();
            AssertEntry("LightningOrb", DiceFaceSlotType.Base);
            AssertEntry("Finisher", DiceFaceSlotType.Base, false);
            AssertEntry("ElectromagneticResonance", DiceFaceSlotType.OnFire);
            AssertEntry("Tesla", DiceFaceSlotType.OnFire, false);
            AssertEntry("EchoSynergy", DiceFaceSlotType.Base, true);
            AssertEntry("ChainReaction", DiceFaceSlotType.OnFireEnd);
        }

        [Test]
        public void LightningChainDefinitionUsesApprovedDefaultsAndExecutorPrefab()
        {
            LightningChainDefinition definition =
                AssetDatabase.LoadAssetAtPath<LightningChainDefinition>(
                    Root + "/Lightning/LightningChainDefinition.asset");

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.ExecutorPrefab, Is.Not.Null);
            Assert.That(
                definition.ExecutorPrefab.GetComponent<LineRenderer>().sharedMaterial,
                Is.Not.Null);
            Assert.That(definition.Damage, Is.EqualTo(1f));
            Assert.That(definition.ChainWidth, Is.EqualTo(0.25f));
            Assert.That(definition.VisualDuration, Is.EqualTo(0.2f));
        }

        [Test]
        public void NewEntriesAreNotBoundToPlayerOrTestRobotPrefabs()
        {
            string[] protectedPrefabs =
            {
                "Assets/Prefab/Player.prefab",
                "Assets/Prefab/TestRobot.prefab"
            };

            foreach (string prefab in protectedPrefabs)
            {
                string[] dependencies = AssetDatabase.GetDependencies(prefab, true);
                Assert.That(dependencies.Any(path =>
                    path.Contains("/DiceFaces/LightningOrb.asset") ||
                    path.Contains("/DiceFaces/Finisher.asset") ||
                    path.Contains("/DiceFaces/ElectromagneticResonance.asset") ||
                    path.Contains("/DiceFaces/Tesla.asset") ||
                    path.Contains("/DiceFaces/EchoSynergy.asset") ||
                    path.Contains("/DiceFaces/ChainReaction.asset")),
                    Is.False,
                    prefab);
            }
        }

        private static void AssertEntry(
            string name,
            DiceFaceSlotType expectedSlot,
            bool passiveBase = false)
        {
            DiceFaceEntry entry = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(
                $"{Root}/DiceFaces/{name}.asset");
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.SlotType, Is.EqualTo(expectedSlot));
            Assert.That(entry.IsPassiveBase, Is.EqualTo(passiveBase));
            Assert.That(entry.Rule, Is.Not.Null);
            Assert.That(entry.Rule.AllowsSlot(expectedSlot), Is.True);
            Assert.That(entry.Effect, Is.Null);
            Assert.That(entry.PassiveEffect, Is.Null);
        }
    }
}
