using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            string entryPath = $"{Root}/DiceFaces/{assetName}.asset";
            string rulePath = $"{Root}/EventRules/Core/{assetName}Rule.asset";
            DiceFaceEntry entry = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(entryPath);
            EventRuleDefinition rule = AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(rulePath);

            Assert.That(entry, Is.Not.Null, entryPath);
            Assert.That(rule, Is.Not.Null, rulePath);
            Assert.That(entry.Rule, Is.SameAs(rule));
            Assert.That(entry.Effect, Is.Null);
            SerializedObject serializedEntry = new SerializedObject(entry);
            Assert.That(serializedEntry.FindProperty("effect").objectReferenceValue, Is.Null);
            Assert.That(serializedEntry.FindProperty("onFireEffects").arraySize, Is.Zero);
            Assert.That(serializedEntry.FindProperty("onHitEffects").arraySize, Is.Zero);
            Assert.That(serializedEntry.FindProperty("onFireEndEffects").arraySize, Is.Zero);
            Assert.That(entry.SlotType, Is.EqualTo(expectedSlot));
            Assert.That(rule.DisplayName, Is.EqualTo(entry.DisplayName));
            Assert.That(rule.Description, Is.EqualTo(entry.Description));
            Assert.That(rule.DisplayColor, Is.EqualTo(entry.DisplayColor));
            Assert.That(rule.AllowsSlot(expectedSlot), Is.True);
            Assert.That(rule.Trigger, Is.TypeOf<SignalTypeTriggerModule>());
            Assert.That(((SignalTypeTriggerModule)rule.Trigger).Matches(
                Signal(expectedSignal, expectedSlot)), Is.True);
            Assert.That(rule.CanEquip(expectedSlot), Is.True);

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(rulePath);
            ScriptableObject[] modules = assets.OfType<ScriptableObject>()
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
        public void BasicShotRuleMatchesLegacyPrimaryProjectileRequest()
        {
            ProjectileSpawnEffect legacy = Load<ProjectileSpawnEffect>(
                $"{Root}/BulletEvents/FireBasicRevolverProjectile.asset");
            LegacyCapture legacyCapture = RunLegacy(legacy, EventSignalType.Base, true);
            RuleCapture migrated = RunRule("BasicShot", EventSignalType.Base, DiceFaceSlotType.Base, true);

            Assert.That(legacyCapture.Requests, Has.Count.EqualTo(1));
            Assert.That(migrated.Services.Requests, Has.Count.EqualTo(1));
            Assert.That(migrated.Services.Requests[0].Definition,
                Is.SameAs(legacyCapture.Requests[0].Definition));
            Assert.That(migrated.Services.Requests[0].IsPrimary, Is.True);
            Assert.That(legacyCapture.Requests[0].IsPrimary, Is.True);
        }

        [Test]
        public void DoubleTapRuleMatchesLegacyDelayedCurrentPrimaryRequest()
        {
            ExtraShotOnFireEffect legacy = Load<ExtraShotOnFireEffect>(
                $"{Root}/BulletEvents/ExtraShotOnFireEffect.asset");
            LegacyCapture legacyCapture = RunLegacy(legacy, EventSignalType.OnFire, true);
            RuleCapture migrated = RunRule(
                "DoubleTap", EventSignalType.OnFire, DiceFaceSlotType.OnFire, true);

            Assert.That(legacyCapture.Delay, Is.EqualTo(0.25f));
            Assert.That(migrated.Services.Delay, Is.EqualTo(legacyCapture.Delay));
            Assert.That(legacyCapture.Requests, Is.Empty);
            Assert.That(migrated.Services.Requests, Is.Empty);

            legacyCapture.Callback.Invoke();
            migrated.Services.Callback.Invoke();

            Assert.That(legacyCapture.Requests, Has.Count.EqualTo(1));
            Assert.That(migrated.Services.Requests, Has.Count.EqualTo(1));
            Assert.That(migrated.Services.Requests[0].Definition,
                Is.SameAs(legacyCapture.Requests[0].Definition));
            Assert.That(migrated.Services.Requests[0].AttackEffectOverride,
                Is.EqualTo(AttackEffectOverride.ForceDisabled));
            Assert.That(migrated.Services.Requests[0].IsPrimary, Is.False);
            Assert.That(legacyCapture.Requests[0].CanTriggerHitEffects, Is.False);
        }

        [Test]
        public void BlastRoundRuleMatchesLegacyHitSpawnAndRejectsNonAttackHits()
        {
            ExplosionOnHitEffect legacy = Load<ExplosionOnHitEffect>(
                $"{Root}/BulletEvents/ExplosionOnHitEffect.asset");
            LegacyCapture legacyCapture = RunLegacy(legacy, EventSignalType.OnHit, true);
            RuleCapture migrated = RunRule(
                "BlastRound", EventSignalType.OnHit, DiceFaceSlotType.OnHit, true);

            Assert.That(legacyCapture.Requests, Has.Count.EqualTo(1));
            Assert.That(migrated.Services.Requests, Has.Count.EqualTo(1));
            Assert.That(migrated.Services.Requests[0].Definition,
                Is.SameAs(legacyCapture.Requests[0].Definition));
            Assert.That(migrated.Services.Requests[0].Origin, Is.EqualTo(legacyCapture.HitPosition));

            RuleCapture rejected = RunRule(
                "BlastRound", EventSignalType.OnHit, DiceFaceSlotType.OnHit, false);
            Assert.That(rejected.Services.Requests, Is.Empty);
        }

        [Test]
        public void LoadedFourRuleMatchesLegacyRefillAndForceRequest()
        {
            ForceFaceFourOnFireEndEffect legacy = Load<ForceFaceFourOnFireEndEffect>(
                $"{Root}/BulletEvents/ForceFaceFourOnFireEndEffect.asset");
            LegacyCapture legacyCapture = RunLegacy(legacy, EventSignalType.OnFireEnd, true);
            RuleCapture migrated = RunRule(
                "LoadedFour", EventSignalType.OnFireEnd, DiceFaceSlotType.OnFireEnd, true);

            Assert.That(legacyCapture.ForcedFaces, Is.EqualTo(new[] { 4 }));
            Assert.That(migrated.Services.ForcedFaces, Is.EqualTo(legacyCapture.ForcedFaces));
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

        private static LegacyCapture RunLegacy(
            BulletEventEffect effect,
            EventSignalType signalType,
            bool canTriggerHitEffects)
        {
            LegacyCapture capture = new LegacyCapture();
            DiceFaceActivation activation = new DiceFaceActivation(
                2,
                default,
                Vector3.zero,
                Vector3.forward,
                (delay, callback) =>
                {
                    capture.Delay = delay;
                    capture.Callback = callback;
                },
                request => capture.Requests.Add(request),
                face =>
                {
                    capture.ForcedFaces.Add(face);
                    return true;
                },
                null);
            ProjectileDefinition primary = Load<ProjectileDefinition>(
                $"{Root}/Projectiles/BasicRevolverBullet.asset");
            activation.RequestProjectile(
                primary,
                AttackEffectOverride.UseProjectileDefault,
                true,
                Vector3.zero,
                Vector3.forward);
            capture.Requests.Clear();
            capture.HitPosition = new Vector3(4f, 0f, -2f);
            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                2,
                Vector3.zero,
                Vector3.forward,
                null,
                default,
                default,
                null,
                primary,
                activation,
                canTriggerHitEffects);

            if (signalType != EventSignalType.OnHit || canTriggerHitEffects)
            {
                effect.Trigger(new BulletEventContext(
                    activation,
                    shot,
                    null,
                    capture.HitPosition));
            }

            if (effect is ProjectileSpawnEffect)
            {
                capture.Callback.Invoke();
            }

            return capture;
        }

        private static RuleCapture RunRule(
            string assetName,
            EventSignalType signalType,
            DiceFaceSlotType slot,
            bool canTriggerHitEffects)
        {
            EventRuleDefinition rule = Load<EventRuleDefinition>(
                $"{Root}/EventRules/Core/{assetName}Rule.asset");
            CapturingServices services = new CapturingServices();
            DiceFaceActivation activation = new DiceFaceActivation(
                2,
                default,
                Vector3.zero,
                Vector3.forward,
                null,
                _ => { },
                null,
                null);
            ProjectileDefinition primary = Load<ProjectileDefinition>(
                $"{Root}/Projectiles/BasicRevolverBullet.asset");
            activation.RequestProjectile(
                primary,
                AttackEffectOverride.UseProjectileDefault,
                true,
                Vector3.zero,
                Vector3.forward);
            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                2,
                Vector3.zero,
                Vector3.forward,
                null,
                default,
                default,
                null,
                primary,
                activation,
                canTriggerHitEffects);
            EventSignal signal = Signal(
                signalType,
                slot,
                activation,
                shot,
                new Vector3(4f, 0f, -2f),
                services.EventBudget);

            EventRuleInvocationResult result =
                new EventRuleRuntime(rule, 2, slot).TryHandle(signal, services);
            return new RuleCapture(result, services);
        }

        private static EventSignal Signal(
            EventSignalType signalType,
            DiceFaceSlotType slot,
            DiceFaceActivation activation = null,
            DiceRevolverShotContext shot = null,
            Vector3 hitPosition = default,
            DiceEventBudget budget = null)
        {
            return new EventSignal(
                signalType,
                2,
                2,
                slot,
                activation,
                shot,
                default,
                null,
                hitPosition,
                Array.Empty<int>(),
                0,
                default,
                budget ?? new DiceEventBudget(32),
                false,
                default);
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

        private sealed class LegacyCapture
        {
            public readonly List<ProjectileSpawnRequest> Requests = new List<ProjectileSpawnRequest>();
            public readonly List<int> ForcedFaces = new List<int>();
            public float Delay = -1f;
            public Action Callback;
            public Vector3 HitPosition;
        }

        private readonly struct RuleCapture
        {
            public RuleCapture(EventRuleInvocationResult result, CapturingServices services)
            {
                Result = result;
                Services = services;
            }

            public EventRuleInvocationResult Result { get; }
            public CapturingServices Services { get; }
        }

        private readonly struct ProjectileRequest
        {
            public ProjectileRequest(
                ProjectileDefinition definition,
                Vector3 origin,
                Vector3 direction,
                AttackEffectOverride attackEffectOverride,
                bool isPrimary)
            {
                Definition = definition;
                Origin = origin;
                Direction = direction;
                AttackEffectOverride = attackEffectOverride;
                IsPrimary = isPrimary;
            }

            public ProjectileDefinition Definition { get; }
            public Vector3 Origin { get; }
            public Vector3 Direction { get; }
            public AttackEffectOverride AttackEffectOverride { get; }
            public bool IsPrimary { get; }
        }

        private sealed class CapturingServices : IEventRuleServices
        {
            public readonly List<ProjectileRequest> Requests = new List<ProjectileRequest>();
            public readonly List<int> ForcedFaces = new List<int>();
            public DiceEventBudget EventBudget { get; } = new DiceEventBudget(32);
            public float Delay { get; private set; } = -1f;
            public Action Callback { get; private set; }

            public bool RequestProjectile(
                ProjectileDefinition definition,
                Vector3 origin,
                Vector3 direction,
                AttackEffectOverride attackEffectOverride,
                bool isPrimary)
            {
                Requests.Add(new ProjectileRequest(
                    definition, origin, direction, attackEffectOverride, isPrimary));
                return true;
            }

            public bool Schedule(float delaySeconds, Action callback)
            {
                Delay = delaySeconds;
                Callback = callback;
                return true;
            }

            public bool RequestRefillAndForceNextFace(int face)
            {
                ForcedFaces.Add(face);
                return true;
            }

            public bool RequestBonusActivation(int face, float maximumSpreadAngle,
                float minimumSpreadSeparation, EventRuleDefinition sourceRule) => false;
            public bool RequestLightningChain(ProjectileHandle origin,
                IReadOnlyList<ProjectileHandle> targets, LightningChainDefinition definition) => false;
            public bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay) => false;
            public IReadOnlyList<ProjectileHandle> FindOwnedProjectiles(Vector3 origin, float radius,
                ProjectileTagDefinition requiredTag, Projectile excludedProjectile) =>
                Array.Empty<ProjectileHandle>();
            public void SetDrawPriority(int priority) { }
            public void RejectDrawCandidate(string reason) { }
            public void MultiplyProjectileDamage(float multiplier) { }
            public void RecordRuleDebug(EventRuleDefinition rule, string stage,
                string description, EventResultStatus status) { }
            public void ReportException(Exception exception, ScriptableObject module) { }
        }
    }
}
