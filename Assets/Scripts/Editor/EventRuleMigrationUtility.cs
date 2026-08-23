using System;
using System.Linq;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Editor
{
    public static class EventRuleMigrationUtility
    {
        private const string Root = "Assets/Resources/DiceFacePrototype";
        private const string CoreRuleFolder = Root + "/EventRules/Core";

        [MenuItem("Dice Revolver/Migrate Core Event Rules")]
        public static void MigrateCoreRules()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "DiceFacePrototype");
            EnsureFolder(Root, "EventRules");
            EnsureFolder(Root + "/EventRules", "Core");

            ProjectileSpawnEffect basic = LoadRequired<ProjectileSpawnEffect>(
                Root + "/BulletEvents/FireBasicRevolverProjectile.asset");
            ProjectileDefinition blast = LoadRequired<ProjectileDefinition>(
                Root + "/Projectiles/BlastExplosion.asset");

            MigrateBasicShot(basic);
            MigrateDoubleTap();
            MigrateBlastRound(blast);
            MigrateLoadedFour();
        }

        [MenuItem("Dice Revolver/Migrate Passive Base Events")]
        public static void MigratePassiveBaseEvents()
        {
            MigratePassiveBaseEntries();
            MigratePassiveRuleSlots();
            AssetDatabase.SaveAssets();
        }

        public static void MigratePassiveBaseEntries()
        {
            // 终态：EchoSynergy 为被动基础（基础槽 + 被动标志）；Tesla 为开火时普通词条；Finisher 为普通基础事件（最后抽到 + 穿甲弹，不占被动面）。
            SetEntryState("Tesla", DiceFaceSlotType.OnFire, false);
            SetEntryState("EchoSynergy", DiceFaceSlotType.Base, true);
            SetEntryState("Finisher", DiceFaceSlotType.Base, false);
            AssetDatabase.SaveAssets();
        }

        private static void SetEntryState(string name, DiceFaceSlotType slotType, bool passiveBase)
        {
            string path = Root + "/DiceFaces/" + name + ".asset";
            DiceFaceEntry entry = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(path);
            if (entry == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(entry);
            SerializedProperty slotTypeProperty = serialized.FindProperty("slotType");
            SerializedProperty isPassiveBase = serialized.FindProperty("isPassiveBase");
            bool changed = false;
            if (slotTypeProperty != null && slotTypeProperty.intValue != (int)slotType)
            {
                slotTypeProperty.intValue = (int)slotType;
                changed = true;
            }

            if (isPassiveBase != null && isPassiveBase.boolValue != passiveBase)
            {
                isPassiveBase.boolValue = passiveBase;
                changed = true;
            }

            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(entry);
            }
        }

        public static void MigratePassiveRuleSlots()
        {
            SetRuleSlot("TeslaRule", DiceFaceSlotMask.OnFire);
            SetRuleSlot("EchoSynergyRule", DiceFaceSlotMask.Base);
            SetRuleSlot("FinisherRule", DiceFaceSlotMask.Base);
            AssetDatabase.SaveAssets();
        }

        private static void SetRuleSlot(string ruleName, DiceFaceSlotMask expectedMask)
        {
            string[] paths = AssetDatabase.FindAssets("t:EventRuleDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(
                    System.IO.Path.GetFileName(path),
                    ruleName,
                    StringComparison.Ordinal))
                .ToArray();
            foreach (string path in paths)
            {
                EventRuleDefinition rule = AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(path);
                if (rule == null)
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(rule);
                SerializedProperty allowedSlots = serialized.FindProperty("allowedSlots");
                if (allowedSlots != null && allowedSlots.intValue != (int)expectedMask)
                {
                    allowedSlots.intValue = (int)expectedMask;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(rule);
                }
            }
        }

        public static EventRuleDefinition MigrateRule(
            string entryPath,
            string rulePath,
            DiceFaceSlotType slot,
            EventSignalMask signal,
            UnityEngine.Object expectedLegacyEffect,
            Action<EventRuleDefinition> ensureModules,
            Func<EventRuleDefinition, bool> hasParity)
        {
            DiceFaceEntry entry = LoadRequired<DiceFaceEntry>(entryPath);
            EventRuleDefinition rule = AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(rulePath);
            if (rule == null)
            {
                rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
                rule.name = System.IO.Path.GetFileNameWithoutExtension(rulePath);
                AssetDatabase.CreateAsset(rule, rulePath);
                SerializedObject serialized = new SerializedObject(rule);
                serialized.FindProperty("displayName").stringValue = entry.DisplayName;
                serialized.FindProperty("description").stringValue = entry.Description;
                serialized.FindProperty("displayColor").colorValue = entry.DisplayColor;
                serialized.FindProperty("allowedSlots").intValue = (int)ToMask(slot);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(rule);
            }

            EnsureTrigger(rule, signal);
            ensureModules?.Invoke(rule);
            SaveRuleObjects(rule);
            AssetDatabase.ImportAsset(rulePath, ImportAssetOptions.ForceUpdate);
            rule = LoadRequired<EventRuleDefinition>(rulePath);

            bool equivalent = HasExpectedTriggerAndSlot(rule, slot, signal) &&
                (hasParity?.Invoke(rule) ?? false);
            if (equivalent)
            {
                LinkEntryAfterParity(entryPath, rule, slot, expectedLegacyEffect);
            }

            return rule;
        }

        private static void MigrateBasicShot(ProjectileSpawnEffect legacy)
        {
            MigrateRule(
                Root + "/DiceFaces/BasicShot.asset",
                CoreRuleFolder + "/BasicShotRule.asset",
                DiceFaceSlotType.Base,
                EventSignalMask.Base,
                legacy,
                rule => EnsureResult<SpawnProjectileResultModule>(rule, module =>
                {
                    SerializedObject serialized = new SerializedObject(module);
                    serialized.FindProperty("projectileDefinition").objectReferenceValue =
                        legacy.ProjectileDefinition;
                    serialized.FindProperty("delaySeconds").floatValue = legacy.DelaySeconds;
                    serialized.FindProperty("attackEffectOverride").enumValueIndex =
                        (int)legacy.AttackEffectOverride;
                    serialized.FindProperty("primaryProjectile").boolValue =
                        legacy.PrimaryProjectile;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }),
                rule => HasSingleSpawnParity(
                    rule,
                    legacy.ProjectileDefinition,
                    false,
                    false,
                    legacy.DelaySeconds,
                    legacy.AttackEffectOverride,
                    legacy.PrimaryProjectile,
                    false));
        }

        private static void MigrateDoubleTap()
        {
            MigrateRule(
                Root + "/DiceFaces/DoubleTap.asset",
                CoreRuleFolder + "/DoubleTapRule.asset",
                DiceFaceSlotType.OnFire,
                EventSignalMask.OnFire,
                null,
                rule => EnsureResult<SpawnProjectileResultModule>(rule, module =>
                {
                    SerializedObject serialized = new SerializedObject(module);
                    serialized.FindProperty("useCurrentPrimaryDefinition").boolValue = true;
                    serialized.FindProperty("delaySeconds").floatValue = 0.25f;
                    serialized.FindProperty("attackEffectOverride").enumValueIndex =
                        (int)AttackEffectOverride.ForceDisabled;
                    serialized.FindProperty("primaryProjectile").boolValue = false;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }),
                rule => HasSingleSpawnParity(
                    rule,
                    null,
                    true,
                    false,
                    0.25f,
                    AttackEffectOverride.ForceDisabled,
                    false,
                    false));
        }

        private static void MigrateBlastRound(ProjectileDefinition explosionDefinition)
        {
            MigrateRule(
                Root + "/DiceFaces/BlastRound.asset",
                CoreRuleFolder + "/BlastRoundRule.asset",
                DiceFaceSlotType.OnHit,
                EventSignalMask.OnHit,
                null,
                rule =>
                {
                    EnsureRuleCondition<AttackEffectConditionModule>(rule, module =>
                    {
                        SerializedObject serialized = new SerializedObject(module);
                        serialized.FindProperty("expectedCanTriggerHitEffects").boolValue = true;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    });
                    EnsureResult<SpawnProjectileResultModule>(rule, module =>
                    {
                        SerializedObject serialized = new SerializedObject(module);
                        serialized.FindProperty("projectileDefinition").objectReferenceValue =
                            explosionDefinition;
                        serialized.FindProperty("useHitOrigin").boolValue = true;
                        serialized.FindProperty("attackEffectOverride").enumValueIndex =
                            (int)AttackEffectOverride.UseProjectileDefault;
                        serialized.FindProperty("primaryProjectile").boolValue = false;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    });
                },
                rule => HasSingleAttackEffectCondition(rule, true) &&
                    HasSingleSpawnParity(
                        rule,
                        explosionDefinition,
                        false,
                        true,
                        0f,
                        AttackEffectOverride.UseProjectileDefault,
                        false,
                        true));
        }

        private static void MigrateLoadedFour()
        {
            MigrateRule(
                Root + "/DiceFaces/LoadedFour.asset",
                CoreRuleFolder + "/LoadedFourRule.asset",
                DiceFaceSlotType.OnFireEnd,
                EventSignalMask.OnFireEnd,
                null,
                rule => EnsureResult<ForceFaceResultModule>(rule, module =>
                {
                    SerializedObject serialized = new SerializedObject(module);
                    serialized.FindProperty("face").intValue = 4;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }),
                rule =>
                {
                    if (!HasOneResultWithoutLocalConditions(rule, out EventResultModule result) ||
                        result is not ForceFaceResultModule force || rule.Conditions.Count != 0)
                    {
                        return false;
                    }

                    return new SerializedObject(force).FindProperty("face").intValue == 4;
                });
        }

        private static void EnsureTrigger(EventRuleDefinition rule, EventSignalMask signal)
        {
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty trigger = serializedRule.FindProperty("trigger");
            if (trigger.objectReferenceValue != null)
            {
                return;
            }

            SignalTypeTriggerModule module = CreateModule<SignalTypeTriggerModule>(rule);
            SerializedObject serializedModule = new SerializedObject(module);
            serializedModule.FindProperty("signals").intValue = (int)signal;
            serializedModule.ApplyModifiedPropertiesWithoutUndo();
            trigger.objectReferenceValue = module;
            serializedRule.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(module);
            AssetDatabase.SaveAssetIfDirty(rule);
        }

        private static void EnsureRuleCondition<T>(EventRuleDefinition rule, Action<T> configure)
            where T : EventConditionModule
        {
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty conditions = serializedRule.FindProperty("conditions");
            if (conditions.arraySize != 0)
            {
                return;
            }

            T module = CreateModule<T>(rule);
            configure(module);
            conditions.arraySize = 1;
            conditions.GetArrayElementAtIndex(0).objectReferenceValue = module;
            serializedRule.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(module);
            AssetDatabase.SaveAssetIfDirty(rule);
        }

        private static void EnsureResult<T>(EventRuleDefinition rule, Action<T> configure)
            where T : EventResultModule
        {
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty results = serializedRule.FindProperty("results");
            if (results.arraySize != 0)
            {
                return;
            }

            T module = CreateModule<T>(rule);
            configure(module);
            results.arraySize = 1;
            SerializedProperty entry = results.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("conditions").arraySize = 0;
            entry.FindPropertyRelative("result").objectReferenceValue = module;
            serializedRule.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(module);
            AssetDatabase.SaveAssetIfDirty(rule);
        }

        private static T CreateModule<T>(EventRuleDefinition rule) where T : ScriptableObject
        {
            T module = ScriptableObject.CreateInstance<T>();
            module.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(module, rule);
            return module;
        }

        private static void LinkEntryAfterParity(
            string entryPath,
            EventRuleDefinition rule,
            DiceFaceSlotType slot,
            UnityEngine.Object expectedLegacyEffect)
        {
            DiceFaceEntry entry = LoadRequired<DiceFaceEntry>(entryPath);
            SerializedObject serializedEntry = new SerializedObject(entry);
            if (!HasOnlyExpectedLegacyReferences(serializedEntry, slot, expectedLegacyEffect))
            {
                return;
            }

            SerializedProperty ruleReference = serializedEntry.FindProperty("rule");
            if (ruleReference.objectReferenceValue == null)
            {
                ruleReference.objectReferenceValue = rule;
            }

            if (ruleReference.objectReferenceValue != rule)
            {
                return;
            }

            SerializedProperty legacyProperty = serializedEntry.FindProperty("effect");
            legacyProperty.objectReferenceValue = null;
            ClearArray(serializedEntry.FindProperty("onFireEffects"));
            ClearArray(serializedEntry.FindProperty("onHitEffects"));
            ClearArray(serializedEntry.FindProperty("onFireEndEffects"));

            serializedEntry.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(entry);
            AssetDatabase.ImportAsset(entryPath, ImportAssetOptions.ForceUpdate);
        }

        private static bool HasOnlyExpectedLegacyReferences(
            SerializedObject serializedEntry,
            DiceFaceSlotType slot,
            UnityEngine.Object expectedLegacyEffect)
        {
            if (expectedLegacyEffect == null)
            {
                return serializedEntry.FindProperty("effect").objectReferenceValue == null &&
                    serializedEntry.FindProperty("passiveEffect").objectReferenceValue == null &&
                    ArrayContainsOnly(serializedEntry.FindProperty("onFireEffects"), null) &&
                    ArrayContainsOnly(serializedEntry.FindProperty("onHitEffects"), null) &&
                    ArrayContainsOnly(serializedEntry.FindProperty("onFireEndEffects"), null);
            }

            if (expectedLegacyEffect is not BulletEventEffect)
            {
                return false;
            }

            UnityEngine.Object direct = serializedEntry.FindProperty("effect")
                .objectReferenceValue;
            if (direct != null && direct != expectedLegacyEffect)
            {
                return false;
            }

            return ArrayContainsOnly(serializedEntry.FindProperty("onFireEffects"), expectedLegacyEffect) &&
                ArrayContainsOnly(serializedEntry.FindProperty("onHitEffects"), expectedLegacyEffect) &&
                ArrayContainsOnly(serializedEntry.FindProperty("onFireEndEffects"), expectedLegacyEffect);
        }

        private static bool ArrayContainsOnly(SerializedProperty array, UnityEngine.Object expected)
        {
            if (array == null || !array.isArray)
            {
                return false;
            }

            for (int index = 0; index < array.arraySize; index++)
            {
                UnityEngine.Object value = array.GetArrayElementAtIndex(index).objectReferenceValue;
                if (value != null && value != expected)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasExpectedTriggerAndSlot(
            EventRuleDefinition rule,
            DiceFaceSlotType slot,
            EventSignalMask signal)
        {
            return rule != null && rule.AllowedSlots == ToMask(slot) &&
                rule.Trigger is SignalTypeTriggerModule trigger && trigger.Signals == signal;
        }

        private static bool HasSingleSpawnParity(
            EventRuleDefinition rule,
            ProjectileDefinition definition,
            bool useCurrentPrimary,
            bool useHitOrigin,
            float delay,
            AttackEffectOverride attackEffectOverride,
            bool primary,
            bool allowRuleConditions)
        {
            if ((!allowRuleConditions && rule.Conditions.Count != 0) ||
                !HasOneResultWithoutLocalConditions(rule, out EventResultModule result) ||
                result is not SpawnProjectileResultModule spawn)
            {
                return false;
            }

            SerializedObject serialized = new SerializedObject(spawn);
            return serialized.FindProperty("projectileDefinition").objectReferenceValue == definition &&
                serialized.FindProperty("useCurrentPrimaryDefinition").boolValue == useCurrentPrimary &&
                serialized.FindProperty("useHitOrigin").boolValue == useHitOrigin &&
                Mathf.Approximately(serialized.FindProperty("delaySeconds").floatValue, delay) &&
                serialized.FindProperty("attackEffectOverride").enumValueIndex ==
                    (int)attackEffectOverride &&
                serialized.FindProperty("primaryProjectile").boolValue == primary;
        }

        private static bool HasSingleAttackEffectCondition(EventRuleDefinition rule, bool expected)
        {
            if (rule.Conditions.Count != 1 ||
                rule.Conditions[0] is not AttackEffectConditionModule condition)
            {
                return false;
            }

            return new SerializedObject(condition)
                .FindProperty("expectedCanTriggerHitEffects").boolValue == expected;
        }

        private static bool HasOneResultWithoutLocalConditions(
            EventRuleDefinition rule,
            out EventResultModule result)
        {
            result = null;
            if (rule.Results == null || rule.Results.Count != 1 ||
                rule.Results[0] == null || rule.Results[0].Result == null ||
                rule.Results[0].Conditions.Count != 0)
            {
                return false;
            }

            result = rule.Results[0].Result;
            return true;
        }

        private static void SaveRuleObjects(EventRuleDefinition rule)
        {
            string path = AssetDatabase.GetAssetPath(rule);
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        private static void ClearArray(SerializedProperty property)
        {
            if (property != null && property.isArray)
            {
                property.arraySize = 0;
            }
        }

        private static DiceFaceSlotMask ToMask(DiceFaceSlotType slot)
        {
            return slot switch
            {
                DiceFaceSlotType.Base => DiceFaceSlotMask.Base,
                DiceFaceSlotType.OnFire => DiceFaceSlotMask.OnFire,
                DiceFaceSlotType.OnHit => DiceFaceSlotMask.OnHit,
                DiceFaceSlotType.OnFireEnd => DiceFaceSlotMask.OnFireEnd,
                _ => DiceFaceSlotMask.None
            };
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Required asset is missing: {path}");
            }

            return asset;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
