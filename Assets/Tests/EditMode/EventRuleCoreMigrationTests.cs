using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using DiceRevolver.Editor;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class EventRuleCoreMigrationTests
    {
        private const string Root = "Assets/Resources/DiceFacePrototype";

        [TestCase("BasicShot", DiceFaceSlotType.Base, EventSignalType.Base)]
        [TestCase("DoubleTap", DiceFaceSlotType.OnFire, EventSignalType.OnFire)]
        [TestCase("BlastRound", DiceFaceSlotType.OnHit, EventSignalType.OnHit)]
        [TestCase("LoadedFour", DiceFaceSlotType.OnFireEnd, EventSignalType.OnFireEnd)]
        public void MigratedEntriesUseLoadableRulesWithOwnedPersistentModules(
            string assetName,
            DiceFaceSlotType expectedSlot,
            EventSignalType expectedSignal)
        {
            string rulePath = $"{Root}/EventRules/Core/{assetName}Rule.asset";
            DiceFaceEntry entry = Load<DiceFaceEntry>($"{Root}/DiceFaces/{assetName}.asset");
            EventRuleDefinition rule = Load<EventRuleDefinition>(rulePath);

            Assert.That(entry.Rule, Is.SameAs(rule));
            Assert.That(entry.Effect, Is.Null);
            Assert.That(entry.PassiveEffect, Is.Null);
            Assert.That(entry.SlotType, Is.EqualTo(expectedSlot));
            Assert.That(rule.DisplayName, Is.EqualTo(entry.DisplayName));
            Assert.That(rule.Description, Is.EqualTo(entry.Description));
            Assert.That(rule.DisplayColor, Is.EqualTo(entry.DisplayColor));
            Assert.That(rule.AllowsSlot(expectedSlot), Is.True);
            Assert.That(rule.Trigger, Is.TypeOf<SignalTypeTriggerModule>());
            Assert.That(((SignalTypeTriggerModule)rule.Trigger).Signals,
                Is.EqualTo(ToMask(expectedSignal)));
            Assert.That(rule.CanEquip(expectedSlot), Is.True);

            ScriptableObject[] modules = AssetDatabase.LoadAllAssetsAtPath(rulePath)
                .OfType<ScriptableObject>()
                .Where(asset => asset != rule)
                .ToArray();
            Assert.That(modules, Is.Not.Empty);
            Assert.That(modules, Does.Contain(rule.Trigger));
            Assert.That(modules, Does.Contain(rule.Results.Single().Result));
            foreach (ScriptableObject module in modules)
            {
                Assert.That(AssetDatabase.GetAssetPath(module), Is.EqualTo(rulePath));
                MonoScript script = MonoScript.FromScriptableObject(module);
                Assert.That(script, Is.Not.Null, module.GetType().Name);
                Assert.That(
                    Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(script)),
                    Is.EqualTo(module.GetType().Name));
            }
        }

        [Test]
        public void CoreEntriesRemainMembersOfThePublicLibrary()
        {
            DiceFaceLibrary library = Load<DiceFaceLibrary>($"{Root}/DiceFaceLibrary.asset");
            Assert.That(
                library.Entries.Select(entry => entry.name),
                Is.SupersetOf(new[] { "BasicShot", "DoubleTap", "BlastRound", "LoadedFour" }));
        }

        [Test]
        public void PublicEntriesHaveExactlyOneImplementationAndRulesOwnEveryModule()
        {
            DiceFaceLibrary faceLibrary = Load<DiceFaceLibrary>($"{Root}/DiceFaceLibrary.asset");
            Assert.That(faceLibrary.Entries.Select(entry => entry.name), Is.EquivalentTo(new[]
            {
                "BasicShot", "DoubleTap", "BlastRound", "LoadedFour", "LightningOrb",
                "Finisher", "ElectromagneticResonance", "Tesla", "EchoSynergy", "ChainReaction"
            }));

            foreach (DiceFaceEntry entry in faceLibrary.Entries)
            {
                int implementationCount = entry.Rule != null ? 1 : 0;
                implementationCount += entry.Effect != null ? 1 : 0;
                implementationCount += entry.PassiveEffect != null ? 1 : 0;
                Assert.That(implementationCount, Is.EqualTo(1), entry.name);
                Assert.That(entry.Rule, Is.Not.Null, entry.name);
                string rulePath = AssetDatabase.GetAssetPath(entry.Rule);
                foreach (ScriptableObject asset in AssetDatabase.LoadAllAssetsAtPath(rulePath)
                             .OfType<ScriptableObject>())
                {
                    Assert.That(AssetDatabase.GetAssetPath(asset), Is.EqualTo(rulePath), asset.name);
                    Assert.That(MonoScript.FromScriptableObject(asset), Is.Not.Null, asset.name);
                }
            }

            BulletEventLibrary eventLibrary =
                Load<BulletEventLibrary>($"{Root}/BulletEventLibrary.asset");
            Assert.That(eventLibrary.Effects, Is.All.Not.Null);
        }

        [Test]
        public void CoreMigrationIsIdempotentAndDoesNotTouchProtectedAssets()
        {
            string[] protectedPaths =
            {
                "Assets/Prefab/Player.prefab",
                "Assets/Prefab/TestRobot.prefab",
                "Assets/Prefab/TargetDummy.prefab",
                "Assets/Scenes/TopDownShooterPrototype.unity",
                "Assets/PrototypeProjectile.prefab",
                "Assets/Prefab/Projectiles/BasicRevolverBullet.prefab",
                "Assets/Prefab/Projectiles/BlastExplosion.prefab",
                "Assets/Prefab/Projectiles/LightningOrb.prefab"
            };
            string[] migrationPaths =
            {
                $"{Root}/DiceFaces/BasicShot.asset",
                $"{Root}/DiceFaces/DoubleTap.asset",
                $"{Root}/DiceFaces/BlastRound.asset",
                $"{Root}/DiceFaces/LoadedFour.asset",
                $"{Root}/EventRules/Core/BasicShotRule.asset",
                $"{Root}/EventRules/Core/DoubleTapRule.asset",
                $"{Root}/EventRules/Core/BlastRoundRule.asset",
                $"{Root}/EventRules/Core/LoadedFourRule.asset"
            };
            string[] protectedBefore = protectedPaths.Select(Hash).ToArray();

            EventRuleMigrationUtility.MigrateCoreRules();
            string[] afterFirst = migrationPaths.Select(Hash).ToArray();
            EventRuleMigrationUtility.MigrateCoreRules();

            Assert.That(migrationPaths.Select(Hash), Is.EqualTo(afterFirst));
            Assert.That(protectedPaths.Select(Hash), Is.EqualTo(protectedBefore));
        }

        [Test]
        public void MigrationUtilityAndBuilderKeepTargetedSaveContract()
        {
            string utilitySource = File.ReadAllText(
                "Assets/Scripts/Editor/EventRuleMigrationUtility.cs");
            string builderSource = File.ReadAllText(
                "Assets/Scripts/Editor/DiceFacePrototypeAssetBuilder.cs");

            Assert.That(utilitySource, Does.Not.Contain("AssetDatabase.SaveAssets("));
            Assert.That(builderSource, Does.Not.Contain("AssetDatabase.SaveAssets("));

            MethodInfo method = typeof(EventRuleMigrationUtility).GetMethod(
                "MigrateRule",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(DiceFaceSlotType),
                    typeof(EventSignalMask),
                    typeof(UnityEngine.Object),
                    typeof(Action<EventRuleDefinition>),
                    typeof(Func<EventRuleDefinition, bool>)
                },
                null);
            Assert.That(method, Is.Not.Null);
        }

        private static EventSignalMask ToMask(EventSignalType signal)
        {
            return signal switch
            {
                EventSignalType.Base => EventSignalMask.Base,
                EventSignalType.OnFire => EventSignalMask.OnFire,
                EventSignalType.OnHit => EventSignalMask.OnHit,
                EventSignalType.OnFireEnd => EventSignalMask.OnFireEnd,
                _ => EventSignalMask.None
            };
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private static string Hash(string assetPath)
        {
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(assetPath)))
                .Replace("-", string.Empty);
        }
    }
}
