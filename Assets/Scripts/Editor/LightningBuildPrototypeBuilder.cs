using System;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Editor
{
    public static class LightningBuildPrototypeBuilder
    {
        private const string Root = "Assets/Resources/DiceFacePrototype";
        private const string FireVisualPath = "Assets/Art/Effect/perfab/fire_1.prefab";
        private const string OrbPrefabPath = "Assets/Prefab/Projectiles/LightningOrb.prefab";
        private const string ChainPrefabPath = "Assets/Prefab/Effects/LightningChain.prefab";
        private const string ChainMaterialPath =
            "Assets/Prefab/Effects/LightningChainMaterial.mat";

        [MenuItem("Dice Revolver/Build Lightning Prototype Content")]
        public static void Build()
        {
            GameObject fireVisual = AssetDatabase.LoadAssetAtPath<GameObject>(FireVisualPath);
            if (fireVisual == null || fireVisual.GetComponentInChildren<ParticleSystem>(true) == null)
            {
                throw new InvalidOperationException(
                    $"{FireVisualPath} is missing or is not a usable particle visual.");
            }

            EnsureFolders();

            ProjectileTypeDefinition orbType = CreateNamed<ProjectileTypeDefinition>(
                Root + "/ProjectileTypes/LightningOrb.asset",
                "displayName",
                LightningProjectileDefaults.ProjectileTypeName);
            ProjectileTypeDefinition chainType = CreateNamed<ProjectileTypeDefinition>(
                Root + "/ProjectileTypes/LightningChain.asset",
                "displayName",
                "LightningChain");
            ProjectileTagDefinition lightningTag = CreateNamed<ProjectileTagDefinition>(
                Root + "/ProjectileTags/Lightning.asset",
                "displayName",
                LightningProjectileDefaults.LightningTagName);
            ProjectileTagDefinition elementalTag = CreateNamed<ProjectileTagDefinition>(
                Root + "/ProjectileTags/Elemental.asset",
                "displayName",
                LightningProjectileDefaults.ElementalTagName);

            Projectile orbPrefab = LoadOrCreateOrbPrefab(fireVisual);
            Material chainMaterial = LoadOrCreateChainMaterial();
            LightningChainExecutor chainPrefab = LoadOrCreateChainPrefab(chainMaterial);
            LightningChainDefinition chainDefinition = LoadOrCreateChainDefinition(chainPrefab);
            ProjectileDefinition orbDefinition = LoadOrCreateOrbDefinition(
                orbPrefab,
                orbType,
                lightningTag,
                elementalTag);

            ProjectileSpawnEffect spawnOrb = LoadOrCreateSpawnEffect(orbDefinition);
            FinisherPassiveEffect finisher = LoadOrCreate<FinisherPassiveEffect>(
                Root + "/BulletEvents/FinisherPassiveEffect.asset",
                out _);
            ElectromagneticResonanceEffect resonance = LoadOrCreateResonance(
                lightningTag,
                chainDefinition);
            TeslaPassiveEffect tesla = LoadOrCreateTesla(lightningTag);
            EchoSynergyPassiveEffect echo = LoadOrCreate<EchoSynergyPassiveEffect>(
                Root + "/BulletEvents/EchoSynergyPassiveEffect.asset",
                out _);
            ChainReactionOnFireEndEffect chainReaction =
                LoadOrCreate<ChainReactionOnFireEndEffect>(
                    Root + "/BulletEvents/ChainReactionOnFireEndEffect.asset",
                    out _);

            DiceFaceEntry orbEntry = LoadOrCreateEntry(
                "LightningOrb",
                "雷电球",
                "发射一颗缓慢飞行、可穿透敌人的雷电球。",
                new Color(0.28f, 0.78f, 1f, 1f),
                DiceFaceSlotType.Base,
                spawnOrb,
                null);
            DiceFaceEntry finisherEntry = LoadOrCreateEntry(
                "Finisher",
                "收尾者",
                "该骰面会等待普通骰面消耗后再进入抽取池。",
                new Color(0.94f, 0.32f, 0.28f, 1f),
                DiceFaceSlotType.Passive,
                null,
                finisher);
            DiceFaceEntry resonanceEntry = LoadOrCreateEntry(
                "ElectromagneticResonance",
                "电磁共鸣",
                "开火时让主雷电球与附近同枪雷电球形成闪电链。",
                new Color(0.22f, 0.88f, 0.75f, 1f),
                DiceFaceSlotType.OnFire,
                resonance,
                null);
            DiceFaceEntry teslaEntry = LoadOrCreateEntry(
                "Tesla",
                "特斯拉",
                "本轮每生成一颗雷电弹幕，所属骰面的基础伤害提高 5%。",
                new Color(1f, 0.83f, 0.22f, 1f),
                DiceFaceSlotType.Passive,
                null,
                tesla);
            DiceFaceEntry echoEntry = LoadOrCreateEntry(
                "EchoSynergy",
                "呼应协同",
                "同类型弹幕命中时立即额外激活该骰面，每轮最多四次。",
                new Color(0.48f, 0.64f, 1f, 1f),
                DiceFaceSlotType.Passive,
                null,
                echo);
            DiceFaceEntry chainReactionEntry = LoadOrCreateEntry(
                "ChainReaction",
                "链式反应",
                "开火后把本骰面的非空主动槽覆盖到下一次正常射击。",
                new Color(0.94f, 0.52f, 0.84f, 1f),
                DiceFaceSlotType.OnFireEnd,
                chainReaction,
                null);

            ProjectileTypeLibrary typeLibrary = LoadOrCreate<ProjectileTypeLibrary>(
                Root + "/ProjectileTypes/ProjectileTypeLibrary.asset",
                out _);
            AppendMissing(typeLibrary, "types", orbType, chainType);
            ProjectileTagLibrary tagLibrary = LoadOrCreate<ProjectileTagLibrary>(
                Root + "/ProjectileTags/ProjectileTagLibrary.asset",
                out _);
            AppendMissing(tagLibrary, "tags", lightningTag, elementalTag);

            ProjectileDefinitionLibrary projectileLibrary =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinitionLibrary>(
                    Root + "/Projectiles/ProjectileDefinitionLibrary.asset");
            AppendMissing(projectileLibrary, "definitions", orbDefinition);
            DiceFaceLibrary faceLibrary = AssetDatabase.LoadAssetAtPath<DiceFaceLibrary>(
                Root + "/DiceFaceLibrary.asset");
            AppendMissing(
                faceLibrary,
                "entries",
                orbEntry,
                finisherEntry,
                resonanceEntry,
                teslaEntry,
                echoEntry,
                chainReactionEntry);
            BulletEventLibrary eventLibrary = AssetDatabase.LoadAssetAtPath<BulletEventLibrary>(
                Root + "/BulletEventLibrary.asset");
            AppendMissing(eventLibrary, "effects", spawnOrb, resonance, chainReaction);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Lightning build prototype content is ready.");
        }

        private static Projectile LoadOrCreateOrbPrefab(GameObject fireVisual)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(OrbPrefabPath);
            if (existing != null)
            {
                Projectile projectile = existing.GetComponent<Projectile>();
                if (projectile == null)
                {
                    throw new InvalidOperationException(
                        $"{OrbPrefabPath} exists without a Projectile component.");
                }

                return projectile;
            }

            GameObject root = new GameObject("LightningOrb");
            try
            {
                SphereCollider collider = root.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = LightningProjectileDefaults.ColliderRadius;
                Rigidbody body = root.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.isKinematic = true;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                root.AddComponent<Projectile>();
                ProjectileVisualWrapper wrapper = root.AddComponent<ProjectileVisualWrapper>();
                SerializedObject wrapperData = new SerializedObject(wrapper);
                wrapperData.FindProperty("visualPrefab").objectReferenceValue = fireVisual;
                wrapperData.FindProperty("localEulerAngles").vector3Value = new Vector3(0f, 90f, 0f);
                wrapperData.FindProperty("visualScale").floatValue = 0.8f;
                wrapperData.ApplyModifiedPropertiesWithoutUndo();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, OrbPrefabPath);
                return saved.GetComponent<Projectile>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Material LoadOrCreateChainMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ChainMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "No compatible unlit shader was found for the lightning chain material.");
            }

            material = new Material(shader)
            {
                name = "LightningChainMaterial"
            };
            AssetDatabase.CreateAsset(material, ChainMaterialPath);
            return material;
        }

        private static LightningChainExecutor LoadOrCreateChainPrefab(Material material)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ChainPrefabPath);
            if (existing != null)
            {
                LightningChainExecutor executor = existing.GetComponent<LightningChainExecutor>();
                if (executor == null)
                {
                    throw new InvalidOperationException(
                        $"{ChainPrefabPath} exists without a LightningChainExecutor component.");
                }

                LineRenderer existingLine = existing.GetComponent<LineRenderer>();
                if (existingLine != null && existingLine.sharedMaterial == null && material != null)
                {
                    GameObject contents = PrefabUtility.LoadPrefabContents(ChainPrefabPath);
                    try
                    {
                        LineRenderer line = contents.GetComponent<LineRenderer>();
                        line.sharedMaterial = material;
                        PrefabUtility.SaveAsPrefabAsset(contents, ChainPrefabPath);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }

                    existing = AssetDatabase.LoadAssetAtPath<GameObject>(ChainPrefabPath);
                    executor = existing.GetComponent<LightningChainExecutor>();
                }

                return executor;
            }

            GameObject root = new GameObject("LightningChain");
            try
            {
                LineRenderer line = root.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.widthMultiplier = 0.25f;
                line.sharedMaterial = material;
                LightningChainExecutor executor = root.AddComponent<LightningChainExecutor>();
                SerializedObject data = new SerializedObject(executor);
                data.FindProperty("lineRenderer").objectReferenceValue = line;
                data.ApplyModifiedPropertiesWithoutUndo();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ChainPrefabPath);
                return saved.GetComponent<LightningChainExecutor>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static LightningChainDefinition LoadOrCreateChainDefinition(
            LightningChainExecutor executorPrefab)
        {
            LightningChainDefinition definition = LoadOrCreate<LightningChainDefinition>(
                Root + "/Lightning/LightningChainDefinition.asset",
                out bool created);
            if (!created)
            {
                SetReferenceIfNull(definition, "executorPrefab", executorPrefab);
                return definition;
            }

            SerializedObject data = new SerializedObject(definition);
            data.FindProperty("executorPrefab").objectReferenceValue = executorPrefab;
            data.FindProperty("damage").floatValue = 1f;
            data.FindProperty("chainWidth").floatValue = 0.25f;
            data.FindProperty("visualDuration").floatValue = 0.2f;
            data.FindProperty("targetLayers").intValue = ~0;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static ProjectileDefinition LoadOrCreateOrbDefinition(
            Projectile prefab,
            ProjectileTypeDefinition type,
            ProjectileTagDefinition lightning,
            ProjectileTagDefinition elemental)
        {
            ProjectileDefinition definition = LoadOrCreate<ProjectileDefinition>(
                Root + "/Projectiles/LightningOrb.asset",
                out bool created);
            if (!created)
            {
                SetReferenceIfNull(definition, "projectilePrefab", prefab);
                SetReferenceIfNull(definition, "projectileTypeDefinition", type);
                return definition;
            }

            SerializedObject data = new SerializedObject(definition);
            data.FindProperty("displayName").stringValue = "雷电球";
            data.FindProperty("projectilePrefab").objectReferenceValue = prefab;
            data.FindProperty("projectileType").stringValue =
                LightningProjectileDefaults.ProjectileTypeName;
            data.FindProperty("projectileTag").stringValue =
                LightningProjectileDefaults.LightningTagName;
            data.FindProperty("projectileTypeDefinition").objectReferenceValue = type;
            SetObjectArray(data.FindProperty("projectileTags"), lightning, elemental);
            data.FindProperty("damage").floatValue = LightningProjectileDefaults.Damage;
            data.FindProperty("flightDistance").floatValue = LightningProjectileDefaults.FlightDistance;
            data.FindProperty("flightSpeed").floatValue = LightningProjectileDefaults.FlightSpeed;
            data.FindProperty("enemyPierceCount").intValue =
                LightningProjectileDefaults.EnemyPierceCount;
            data.FindProperty("defaultAttackEffect").boolValue =
                LightningProjectileDefaults.DefaultAttackEffect;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static ProjectileSpawnEffect LoadOrCreateSpawnEffect(
            ProjectileDefinition definition)
        {
            ProjectileSpawnEffect effect = LoadOrCreate<ProjectileSpawnEffect>(
                Root + "/BulletEvents/FireLightningOrbProjectile.asset",
                out bool created);
            if (!created)
            {
                SetReferenceIfNull(effect, "projectileDefinition", definition);
                return effect;
            }

            SerializedObject data = new SerializedObject(effect);
            data.FindProperty("projectileDefinition").objectReferenceValue = definition;
            data.FindProperty("delaySeconds").floatValue = 0f;
            data.FindProperty("attackEffectOverride").enumValueIndex =
                (int)AttackEffectOverride.UseProjectileDefault;
            data.FindProperty("primaryProjectile").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static ElectromagneticResonanceEffect LoadOrCreateResonance(
            ProjectileTagDefinition lightning,
            LightningChainDefinition chain)
        {
            ElectromagneticResonanceEffect effect =
                LoadOrCreate<ElectromagneticResonanceEffect>(
                    Root + "/BulletEvents/ElectromagneticResonanceEffect.asset",
                    out bool created);
            if (!created)
            {
                SetReferenceIfNull(effect, "lightningTag", lightning);
                SetReferenceIfNull(effect, "chainDefinition", chain);
                return effect;
            }

            SerializedObject data = new SerializedObject(effect);
            data.FindProperty("lightningTag").objectReferenceValue = lightning;
            data.FindProperty("chainDefinition").objectReferenceValue = chain;
            data.FindProperty("searchRadius").floatValue = 6f;
            data.FindProperty("maximumConnections").intValue = 3;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static TeslaPassiveEffect LoadOrCreateTesla(ProjectileTagDefinition lightning)
        {
            TeslaPassiveEffect effect = LoadOrCreate<TeslaPassiveEffect>(
                Root + "/BulletEvents/TeslaPassiveEffect.asset",
                out bool created);
            if (!created)
            {
                SetReferenceIfNull(effect, "lightningTag", lightning);
                return effect;
            }

            SerializedObject data = new SerializedObject(effect);
            data.FindProperty("lightningTag").objectReferenceValue = lightning;
            data.FindProperty("damagePerStack").floatValue = 0.05f;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static DiceFaceEntry LoadOrCreateEntry(
            string assetName,
            string displayName,
            string description,
            Color color,
            DiceFaceSlotType slot,
            BulletEventEffect activeEffect,
            PassiveEventEffect passiveEffect)
        {
            DiceFaceEntry entry = LoadOrCreate<DiceFaceEntry>(
                Root + "/DiceFaces/" + assetName + ".asset",
                out bool created);
            if (!created)
            {
                return entry;
            }

            SerializedObject data = new SerializedObject(entry);
            data.FindProperty("displayName").stringValue = displayName;
            data.FindProperty("description").stringValue = description;
            data.FindProperty("displayColor").colorValue = color;
            data.FindProperty("slotType").enumValueIndex = (int)slot;
            data.FindProperty("effect").objectReferenceValue = activeEffect;
            data.FindProperty("passiveEffect").objectReferenceValue = passiveEffect;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
            return entry;
        }

        private static T CreateNamed<T>(string path, string property, string value)
            where T : ScriptableObject
        {
            T asset = LoadOrCreate<T>(path, out bool created);
            if (created)
            {
                SerializedObject data = new SerializedObject(asset);
                data.FindProperty(property).stringValue = value;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            return asset;
        }

        private static T LoadOrCreate<T>(string path, out bool created)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                created = false;
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created = true;
            return asset;
        }

        private static void AppendMissing(
            UnityEngine.Object target,
            string propertyName,
            params UnityEngine.Object[] values)
        {
            if (target == null)
            {
                throw new InvalidOperationException(
                    $"Required library for property '{propertyName}' is missing.");
            }

            SerializedObject data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(propertyName);
            for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
            {
                UnityEngine.Object value = values[valueIndex];
                if (value == null || Contains(property, value))
                {
                    continue;
                }

                int index = property.arraySize;
                property.arraySize++;
                property.GetArrayElementAtIndex(index).objectReferenceValue = value;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static bool Contains(SerializedProperty property, UnityEngine.Object value)
        {
            for (int index = 0; index < property.arraySize; index++)
            {
                if (property.GetArrayElementAtIndex(index).objectReferenceValue == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetReferenceIfNull(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(propertyName);
            if (property.objectReferenceValue != null || value == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArray(
            SerializedProperty property,
            params UnityEngine.Object[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefab", "Projectiles");
            EnsureFolder("Assets/Prefab", "Effects");
            EnsureFolder(Root, "ProjectileTypes");
            EnsureFolder(Root, "ProjectileTags");
            EnsureFolder(Root, "Lightning");
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
