using System;
using System.Collections.Generic;
using System.Linq;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Editor
{
    public static class LightningBuildPrototypeBuilder
    {
        private const string Root = "Assets/Resources/DiceFacePrototype";
        private const string RuleFolder = Root + "/EventRules/Lightning";
        private const string StacksKey = "stacks";
        private const string EchoCounterKey = "bonusActivationTriggers";
        private const string EchoConsumedKey = "consumed";

        [MenuItem("Dice Revolver/Build Lightning Prototype Content")]
        public static void Build()
        {
            EnsureFolder(Root, "EventRules");
            EnsureFolder(Root + "/EventRules", "Lightning");

            ProjectileSpawnEffect legacyOrb = LoadRequired<ProjectileSpawnEffect>(
                Root + "/BulletEvents/FireLightningOrbProjectile.asset");
            ElectromagneticResonanceEffect legacyResonance =
                LoadRequired<ElectromagneticResonanceEffect>(
                    Root + "/BulletEvents/ElectromagneticResonanceEffect.asset");
            TeslaPassiveEffect legacyTesla = LoadRequired<TeslaPassiveEffect>(
                Root + "/BulletEvents/TeslaPassiveEffect.asset");
            EchoSynergyPassiveEffect legacyEcho = LoadRequired<EchoSynergyPassiveEffect>(
                Root + "/BulletEvents/EchoSynergyPassiveEffect.asset");
            ChainReactionOnFireEndEffect legacyReaction =
                LoadRequired<ChainReactionOnFireEndEffect>(
                    Root + "/BulletEvents/ChainReactionOnFireEndEffect.asset");
            FinisherPassiveEffect legacyFinisher = LoadRequired<FinisherPassiveEffect>(
                Root + "/BulletEvents/FinisherPassiveEffect.asset");

            MigrateLightningOrb(legacyOrb);
            MigrateResonance(legacyResonance);
            MigrateTesla(legacyTesla);
            MigrateEcho(legacyEcho);
            MigrateChainReaction(legacyReaction);
            MigrateFinisher(legacyFinisher);
            Debug.Log("Lightning Event Rules are ready.");
        }

        private static void MigrateLightningOrb(ProjectileSpawnEffect legacy)
        {
            EventRuleMigrationUtility.MigrateRule(
                EntryPath("LightningOrb"), RulePath("LightningOrb"),
                DiceFaceSlotType.Base, EventSignalMask.Base, legacy,
                rule => EnsureSingleResult<SpawnProjectileResultModule>(rule, result =>
                {
                    Set(result, "projectileDefinition", legacy.ProjectileDefinition);
                    Set(result, "useCurrentPrimaryDefinition", false);
                    Set(result, "useHitOrigin", false);
                    Set(result, "delaySeconds", legacy.DelaySeconds);
                    Set(result, "attackEffectOverride", legacy.AttackEffectOverride);
                    Set(result, "primaryProjectile", legacy.PrimaryProjectile);
                }),
                rule => rule.Conditions.Count == 0 &&
                    TrySingleResult(rule, out SpawnProjectileResultModule spawn) &&
                    Read<UnityEngine.Object>(spawn, "projectileDefinition") ==
                        legacy.ProjectileDefinition &&
                    !Read<bool>(spawn, "useCurrentPrimaryDefinition") &&
                    !Read<bool>(spawn, "useHitOrigin") &&
                    Mathf.Approximately(Read<float>(spawn, "delaySeconds"),
                        legacy.DelaySeconds) &&
                    Read<AttackEffectOverride>(spawn, "attackEffectOverride") ==
                        legacy.AttackEffectOverride &&
                    Read<bool>(spawn, "primaryProjectile") == legacy.PrimaryProjectile);
        }

        private static void MigrateResonance(ElectromagneticResonanceEffect legacy)
        {
            EventRuleMigrationUtility.MigrateRule(
                EntryPath("ElectromagneticResonance"), RulePath("ElectromagneticResonance"),
                DiceFaceSlotType.OnFire, EventSignalMask.OnFire, legacy,
                rule =>
                {
                    EnsureSingleRuleCondition<ProjectileTagConditionModule>(
                        rule, condition => Set(
                            condition, "projectileTag", legacy.LightningTag));
                    EnsureSingleResult<CreateLightningChainResultModule>(rule, result =>
                    {
                        Set(result, "lightningTag", legacy.LightningTag);
                        Set(result, "chainDefinition", legacy.ChainDefinition);
                        Set(result, "searchRadius", legacy.SearchRadius);
                        Set(result, "maximumConnections", legacy.MaximumConnections);
                    });
                },
                rule => rule.Conditions.Count == 1 &&
                    rule.Conditions[0] is ProjectileTagConditionModule tag &&
                    Read<UnityEngine.Object>(tag, "projectileTag") == legacy.LightningTag &&
                    TrySingleResult(rule, out CreateLightningChainResultModule result) &&
                    Read<UnityEngine.Object>(result, "lightningTag") == legacy.LightningTag &&
                    Read<UnityEngine.Object>(result, "chainDefinition") ==
                        legacy.ChainDefinition &&
                    Mathf.Approximately(Read<float>(result, "searchRadius"),
                        legacy.SearchRadius) &&
                    Read<int>(result, "maximumConnections") == legacy.MaximumConnections);
        }

        private static void MigrateTesla(TeslaPassiveEffect legacy)
        {
            EventRuleMigrationUtility.MigrateRule(
                EntryPath("Tesla"), RulePath("Tesla"),
                DiceFaceSlotType.Passive,
                EventSignalMask.ProjectileSpawned |
                EventSignalMask.BeforeProjectileStats |
                EventSignalMask.ReloadStarted,
                legacy,
                rule => EnsureTeslaResults(rule, legacy),
                rule => HasTeslaParity(rule, legacy));
        }

        private static void MigrateEcho(EchoSynergyPassiveEffect legacy)
        {
            EventRuleMigrationUtility.MigrateRule(
                EntryPath("EchoSynergy"), RulePath("EchoSynergy"),
                DiceFaceSlotType.Passive,
                EventSignalMask.ProjectileHit |
                EventSignalMask.FaceConsumed |
                EventSignalMask.ReloadStarted,
                legacy,
                rule => EnsureEchoResults(rule, legacy),
                rule => HasEchoParity(rule, legacy));
        }

        private static void MigrateChainReaction(ChainReactionOnFireEndEffect legacy)
        {
            EventRuleMigrationUtility.MigrateRule(
                EntryPath("ChainReaction"), RulePath("ChainReaction"),
                DiceFaceSlotType.OnFireEnd, EventSignalMask.OnFireEnd, legacy,
                rule => EnsureSingleResult<QueueActiveOverlayResultModule>(rule, _ => { }),
                rule => rule.Conditions.Count == 0 &&
                    TrySingleResult(rule, out QueueActiveOverlayResultModule _));
        }

        private static void MigrateFinisher(FinisherPassiveEffect legacy)
        {
            EventRuleMigrationUtility.MigrateRule(
                EntryPath("Finisher"), RulePath("Finisher"),
                DiceFaceSlotType.Passive, EventSignalMask.DrawCandidate, legacy,
                rule =>
                {
                    if (rule.Results.Count == 0)
                    {
                        EnsureSingleResult(
                            rule,
                            CreateCondition<SourceFaceConditionModule>(rule, _ => { }),
                            CreateResult<SetDrawPriorityResultModule>(rule,
                                result => Set(result, "priority", 1)));
                    }
                },
                rule => rule.Conditions.Count == 0 && rule.Results.Count == 1 &&
                    HasConditions<SourceFaceConditionModule>(rule.Results[0]) &&
                    rule.Results[0].Result is SetDrawPriorityResultModule priority &&
                    Read<int>(priority, "priority") == 1);
        }

        private static void EnsureTeslaResults(
            EventRuleDefinition rule,
            TeslaPassiveEffect legacy)
        {
            if (rule.Results.Count != 0)
            {
                return;
            }

            SerializedObject serialized = new(rule);
            SerializedProperty results = serialized.FindProperty("results");
            AppendResult(results,
                CreateResult<IncrementCounterResultModule>(rule, result =>
                {
                    Set(result, "counterKey", StacksKey);
                    Set(result, "amount", 1);
                }),
                CreateSignalCondition(rule, EventSignalMask.ProjectileSpawned),
                CreateCondition<ProjectileTagConditionModule>(rule,
                    condition => Set(condition, "projectileTag", legacy.LightningTag)));
            AppendResult(results,
                CreateResult<MultiplyProjectileDamageFromCounterResultModule>(rule, result =>
                {
                    Set(result, "counterKey", StacksKey);
                    Set(result, "damagePerStack", legacy.DamagePerStack);
                }),
                CreateSignalCondition(rule, EventSignalMask.BeforeProjectileStats),
                CreateCondition<SourceFaceConditionModule>(rule, _ => { }),
                CreateCondition<SameProjectileTypeConditionModule>(rule, _ => { }));
            AppendResult(results,
                CreateResult<ResetCounterResultModule>(rule,
                    result => Set(result, "counterKey", StacksKey)),
                CreateSignalCondition(rule, EventSignalMask.ReloadStarted));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool HasTeslaParity(
            EventRuleDefinition rule,
            TeslaPassiveEffect legacy)
        {
            if (rule.Conditions.Count != 0 || rule.Results.Count != 3)
            {
                return false;
            }

            EventResultEntry incrementEntry = rule.Results[0];
            EventResultEntry damageEntry = rule.Results[1];
            EventResultEntry resetEntry = rule.Results[2];
            return HasConditions<SignalTypeConditionModule, ProjectileTagConditionModule>(
                       incrementEntry) &&
                   Read<EventSignalMask>(incrementEntry.Conditions[0], "signals") ==
                       EventSignalMask.ProjectileSpawned &&
                   Read<UnityEngine.Object>(incrementEntry.Conditions[1], "projectileTag") ==
                       legacy.LightningTag &&
                   incrementEntry.Result is IncrementCounterResultModule increment &&
                   Read<string>(increment, "counterKey") == StacksKey &&
                   Read<int>(increment, "amount") == 1 &&
                   HasConditions<SignalTypeConditionModule, SourceFaceConditionModule,
                       SameProjectileTypeConditionModule>(damageEntry) &&
                   Read<EventSignalMask>(damageEntry.Conditions[0], "signals") ==
                       EventSignalMask.BeforeProjectileStats &&
                   damageEntry.Result is MultiplyProjectileDamageFromCounterResultModule damage &&
                   Read<string>(damage, "counterKey") == StacksKey &&
                   Mathf.Approximately(Read<float>(damage, "damagePerStack"),
                       legacy.DamagePerStack) &&
                   HasConditions<SignalTypeConditionModule>(resetEntry) &&
                   Read<EventSignalMask>(resetEntry.Conditions[0], "signals") ==
                       EventSignalMask.ReloadStarted &&
                   resetEntry.Result is ResetCounterResultModule reset &&
                   Read<string>(reset, "counterKey") == StacksKey;
        }

        private static void EnsureEchoResults(
            EventRuleDefinition rule,
            EchoSynergyPassiveEffect legacy)
        {
            if (rule.Results.Count != 0)
            {
                return;
            }

            SerializedObject serialized = new(rule);
            SerializedProperty results = serialized.FindProperty("results");
            AppendResult(results,
                CreateResult<RequestBonusActivationResultModule>(rule, result =>
                {
                    Set(result, "maximumTriggers", legacy.MaximumTriggersPerChamber);
                    Set(result, "maximumSpreadAngle", legacy.MaximumSpreadAngle);
                    Set(result, "minimumSpreadSeparation", legacy.MinimumSpreadSeparation);
                    Set(result, "counterKey", EchoCounterKey);
                }),
                CreateSignalCondition(rule, EventSignalMask.ProjectileHit),
                CreateCondition<SameProjectileTypeConditionModule>(rule, _ => { }),
                CreateCondition<BooleanStateConditionModule>(rule, condition =>
                {
                    Set(condition, "stateKey", EchoConsumedKey);
                    Set(condition, "expectedValue", false);
                }));
            AppendResult(results,
                CreateResult<SetBooleanStateResultModule>(rule, result =>
                {
                    Set(result, "stateKey", EchoConsumedKey);
                    Set(result, "value", true);
                }),
                CreateSignalCondition(rule, EventSignalMask.FaceConsumed),
                CreateCondition<SourceFaceConditionModule>(rule, _ => { }));
            AppendResult(results,
                CreateResult<ResetCounterResultModule>(rule,
                    result => Set(result, "counterKey", EchoCounterKey)),
                CreateSignalCondition(rule, EventSignalMask.ReloadStarted));
            AppendResult(results,
                CreateResult<SetBooleanStateResultModule>(rule, result =>
                {
                    Set(result, "stateKey", EchoConsumedKey);
                    Set(result, "value", false);
                }),
                CreateSignalCondition(rule, EventSignalMask.ReloadStarted));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool HasEchoParity(
            EventRuleDefinition rule,
            EchoSynergyPassiveEffect legacy)
        {
            if (rule.Conditions.Count != 0 || rule.Results.Count != 4)
            {
                return false;
            }

            EventResultEntry bonusEntry = rule.Results[0];
            EventResultEntry consumedEntry = rule.Results[1];
            EventResultEntry resetEntry = rule.Results[2];
            EventResultEntry reactivateEntry = rule.Results[3];
            return HasConditions<SignalTypeConditionModule, SameProjectileTypeConditionModule,
                       BooleanStateConditionModule>(bonusEntry) &&
                   Read<EventSignalMask>(bonusEntry.Conditions[0], "signals") ==
                       EventSignalMask.ProjectileHit &&
                   Read<string>(bonusEntry.Conditions[2], "stateKey") == EchoConsumedKey &&
                   !Read<bool>(bonusEntry.Conditions[2], "expectedValue") &&
                   bonusEntry.Result is RequestBonusActivationResultModule bonus &&
                   Read<int>(bonus, "maximumTriggers") ==
                       legacy.MaximumTriggersPerChamber &&
                   Mathf.Approximately(Read<float>(bonus, "maximumSpreadAngle"),
                       legacy.MaximumSpreadAngle) &&
                   Mathf.Approximately(Read<float>(bonus, "minimumSpreadSeparation"),
                       legacy.MinimumSpreadSeparation) &&
                   Read<string>(bonus, "counterKey") == EchoCounterKey &&
                   HasConditions<SignalTypeConditionModule, SourceFaceConditionModule>(
                       consumedEntry) &&
                   Read<EventSignalMask>(consumedEntry.Conditions[0], "signals") ==
                       EventSignalMask.FaceConsumed &&
                   IsBooleanResult(consumedEntry, EchoConsumedKey, true) &&
                   HasSignalReset(resetEntry, EchoCounterKey) &&
                   HasConditions<SignalTypeConditionModule>(reactivateEntry) &&
                   Read<EventSignalMask>(reactivateEntry.Conditions[0], "signals") ==
                       EventSignalMask.ReloadStarted &&
                   IsBooleanResult(reactivateEntry, EchoConsumedKey, false);
        }

        private static bool HasSignalReset(EventResultEntry entry, string key)
        {
            return HasConditions<SignalTypeConditionModule>(entry) &&
                   Read<EventSignalMask>(entry.Conditions[0], "signals") ==
                       EventSignalMask.ReloadStarted &&
                   entry.Result is ResetCounterResultModule reset &&
                   Read<string>(reset, "counterKey") == key;
        }

        private static bool IsBooleanResult(EventResultEntry entry, string key, bool value)
        {
            return entry.Result is SetBooleanStateResultModule set &&
                   Read<string>(set, "stateKey") == key &&
                   Read<bool>(set, "value") == value;
        }

        private static void EnsureSingleRuleCondition<T>(
            EventRuleDefinition rule,
            Action<T> configure)
            where T : EventConditionModule
        {
            if (rule.Conditions.Count != 0)
            {
                return;
            }

            T condition = CreateCondition(rule, configure);
            SerializedObject serialized = new(rule);
            SerializedProperty conditions = serialized.FindProperty("conditions");
            conditions.arraySize = 1;
            conditions.GetArrayElementAtIndex(0).objectReferenceValue = condition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureSingleResult<T>(
            EventRuleDefinition rule,
            Action<T> configure)
            where T : EventResultModule
        {
            if (rule.Results.Count != 0)
            {
                return;
            }

            EnsureSingleResult(rule, Array.Empty<EventConditionModule>(),
                CreateResult(rule, configure));
        }

        private static void EnsureSingleResult(
            EventRuleDefinition rule,
            EventConditionModule condition,
            EventResultModule result)
        {
            EnsureSingleResult(rule, new[] { condition }, result);
        }

        private static void EnsureSingleResult(
            EventRuleDefinition rule,
            IReadOnlyList<EventConditionModule> conditions,
            EventResultModule result)
        {
            if (rule.Results.Count != 0)
            {
                return;
            }

            SerializedObject serialized = new(rule);
            AppendResult(serialized.FindProperty("results"), result, conditions.ToArray());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AppendResult(
            SerializedProperty results,
            EventResultModule result,
            params EventConditionModule[] conditions)
        {
            int index = results.arraySize;
            results.arraySize++;
            SerializedProperty entry = results.GetArrayElementAtIndex(index);
            SerializedProperty serializedConditions = entry.FindPropertyRelative("conditions");
            serializedConditions.arraySize = conditions.Length;
            for (int conditionIndex = 0; conditionIndex < conditions.Length; conditionIndex++)
            {
                serializedConditions.GetArrayElementAtIndex(conditionIndex).objectReferenceValue =
                    conditions[conditionIndex];
            }

            entry.FindPropertyRelative("result").objectReferenceValue = result;
        }

        private static SignalTypeConditionModule CreateSignalCondition(
            EventRuleDefinition rule,
            EventSignalMask signals)
        {
            return CreateCondition<SignalTypeConditionModule>(
                rule, condition => Set(condition, "signals", signals));
        }

        private static T CreateCondition<T>(EventRuleDefinition rule, Action<T> configure)
            where T : EventConditionModule => CreateModule(rule, configure);

        private static T CreateResult<T>(EventRuleDefinition rule, Action<T> configure)
            where T : EventResultModule => CreateModule(rule, configure);

        private static T CreateModule<T>(EventRuleDefinition rule, Action<T> configure)
            where T : ScriptableObject
        {
            T module = ScriptableObject.CreateInstance<T>();
            module.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(module, rule);
            configure?.Invoke(module);
            EditorUtility.SetDirty(module);
            return module;
        }

        private static bool TrySingleResult<T>(EventRuleDefinition rule, out T result)
            where T : EventResultModule
        {
            result = null;
            if (rule.Results.Count != 1 || rule.Results[0].Conditions.Count != 0)
            {
                return false;
            }

            result = rule.Results[0].Result as T;
            return result != null;
        }

        private static bool HasConditions<T1>(EventResultEntry entry) =>
            HasConditionTypes(entry, typeof(T1));

        private static bool HasConditions<T1, T2>(EventResultEntry entry) =>
            HasConditionTypes(entry, typeof(T1), typeof(T2));

        private static bool HasConditions<T1, T2, T3>(EventResultEntry entry) =>
            HasConditionTypes(entry, typeof(T1), typeof(T2), typeof(T3));

        private static bool HasConditionTypes(EventResultEntry entry, params Type[] types)
        {
            return entry != null && entry.Conditions.Count == types.Length &&
                entry.Conditions.Select(condition => condition?.GetType()).SequenceEqual(types);
        }

        private static void Set(UnityEngine.Object target, string propertyName, object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            switch (value)
            {
                case UnityEngine.Object reference:
                    property.objectReferenceValue = reference;
                    break;
                case string text:
                    property.stringValue = text;
                    break;
                case int number:
                    property.intValue = number;
                    break;
                case float number:
                    property.floatValue = number;
                    break;
                case bool flag:
                    property.boolValue = flag;
                    break;
                case Enum enumValue:
                    property.intValue = Convert.ToInt32(enumValue);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported serialized value for {propertyName}.");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T Read<T>(UnityEngine.Object target, string propertyName)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            object value = typeof(T) == typeof(string) ? property.stringValue :
                typeof(T) == typeof(float) ? property.floatValue :
                typeof(T) == typeof(bool) ? property.boolValue :
                typeof(T) == typeof(int) ? property.intValue :
                typeof(T).IsEnum ? Enum.ToObject(typeof(T), property.intValue) :
                typeof(UnityEngine.Object).IsAssignableFrom(typeof(T))
                    ? property.objectReferenceValue
                    : throw new InvalidOperationException(
                        $"Unsupported serialized read for {propertyName}.");
            return (T)value;
        }

        private static string EntryPath(string name) => Root + "/DiceFaces/" + name + ".asset";
        private static string RulePath(string name) => RuleFolder + "/" + name + "Rule.asset";

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Required lightning asset is missing: {path}");
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
