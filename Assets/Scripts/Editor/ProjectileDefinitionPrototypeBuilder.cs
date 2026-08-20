using System;
using System.Linq;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Editor
{
    public static class ProjectileDefinitionPrototypeBuilder
    {
        private const string FireVisualPath = "Assets/Art/Effect/perfab/fire_1.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefab/Projectiles/BasicRevolverBullet.prefab";
        private const string ProjectileRoot = "Assets/Resources/DiceFacePrototype/Projectiles";
        private const string BulletEventRoot = "Assets/Resources/DiceFacePrototype/BulletEvents";
        private const string DefinitionPath = ProjectileRoot + "/BasicRevolverBullet.asset";
        private const string LibraryPath = ProjectileRoot + "/ProjectileDefinitionLibrary.asset";
        private const string SpawnEffectPath = BulletEventRoot + "/FireBasicRevolverProjectile.asset";
        private const string PlayerPrefabPath = "Assets/Prefab/Player.prefab";
        private const string BulletEventLibraryPath = "Assets/Resources/DiceFacePrototype/BulletEventLibrary.asset";

        [MenuItem("Dice Revolver/Setup Projectile Definition Prototype")]
        public static void Build()
        {
            GameObject fireVisual = AssetDatabase.LoadAssetAtPath<GameObject>(FireVisualPath);
            if (fireVisual == null || fireVisual.GetComponentInChildren<ParticleSystem>(true) == null)
            {
                throw new InvalidOperationException(
                    $"{FireVisualPath} is missing or does not contain a ParticleSystem. Projectile assets were not generated.");
            }

            EnsureFolder("Assets/Prefab", "Projectiles");
            EnsureFolder("Assets/Resources/DiceFacePrototype", "Projectiles");
            EnsureFolder("Assets/Resources/DiceFacePrototype", "BulletEvents");

            Projectile runtimePrefab = LoadOrCreateRuntimePrefab(fireVisual);
            ProjectileDefinition definition = LoadOrCreateDefinition(runtimePrefab);
            ProjectileSpawnEffect spawnEffect = LoadOrCreateSpawnEffect(definition);
            LoadOrCreateDefinitionLibrary(definition);
            AppendToBulletEventLibrary(spawnEffect);
            BindAllPlayerFaces(spawnEffect);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Projectile definition prototype assets are ready.");
        }

        private static Projectile LoadOrCreateRuntimePrefab(GameObject fireVisual)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (existingPrefab != null)
            {
                Projectile existing = existingPrefab.GetComponent<Projectile>();
                if (existing == null)
                {
                    throw new InvalidOperationException(
                        $"{ProjectilePrefabPath} exists but does not contain a Projectile component.");
                }

                return existing;
            }

            GameObject root = new GameObject("BasicRevolverBullet");
            try
            {
                SphereCollider collider = root.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.18f;

                Rigidbody body = root.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.isKinematic = true;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                root.AddComponent<Projectile>();
                ProjectileVisualWrapper wrapper = root.AddComponent<ProjectileVisualWrapper>();
                SerializedObject wrapperData = new SerializedObject(wrapper);
                wrapperData.FindProperty("visualPrefab").objectReferenceValue = fireVisual;
                wrapperData.FindProperty("localEulerAngles").vector3Value = new Vector3(0f, 90f, 0f);
                wrapperData.FindProperty("visualScale").floatValue = 0.2f;
                wrapperData.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
                return saved.GetComponent<Projectile>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static ProjectileDefinition LoadOrCreateDefinition(Projectile runtimePrefab)
        {
            ProjectileDefinition definition = AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(DefinitionPath);
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);

            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("displayName").stringValue = "基础左轮子弹";
            serialized.FindProperty("projectilePrefab").objectReferenceValue = runtimePrefab;
            serialized.FindProperty("projectileType").stringValue = "Revolver";
            serialized.FindProperty("projectileTag").stringValue = "PlayerBullet";
            serialized.FindProperty("damage").floatValue = 1f;
            serialized.FindProperty("flightDistance").floatValue = 18f;
            serialized.FindProperty("flightSpeed").floatValue = 18f;
            serialized.FindProperty("enemyPierceCount").intValue = 0;
            serialized.FindProperty("defaultAttackEffect").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static ProjectileSpawnEffect LoadOrCreateSpawnEffect(ProjectileDefinition definition)
        {
            ProjectileSpawnEffect effect = AssetDatabase.LoadAssetAtPath<ProjectileSpawnEffect>(SpawnEffectPath);
            if (effect != null)
            {
                return effect;
            }

            effect = ScriptableObject.CreateInstance<ProjectileSpawnEffect>();
            AssetDatabase.CreateAsset(effect, SpawnEffectPath);

            SerializedObject serialized = new SerializedObject(effect);
            serialized.FindProperty("projectileDefinition").objectReferenceValue = definition;
            serialized.FindProperty("delaySeconds").floatValue = 0f;
            serialized.FindProperty("attackEffectOverride").enumValueIndex =
                (int)AttackEffectOverride.UseProjectileDefault;
            serialized.FindProperty("primaryProjectile").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static void LoadOrCreateDefinitionLibrary(ProjectileDefinition definition)
        {
            ProjectileDefinitionLibrary library =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinitionLibrary>(LibraryPath);
            if (library != null)
            {
                return;
            }

            library = ScriptableObject.CreateInstance<ProjectileDefinitionLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
            SetObjectArray(library, "definitions", definition);
        }

        private static void AppendToBulletEventLibrary(ProjectileSpawnEffect spawnEffect)
        {
            BulletEventLibrary library = AssetDatabase.LoadAssetAtPath<BulletEventLibrary>(BulletEventLibraryPath);
            if (library == null || library.Effects.Contains(spawnEffect))
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(library);
            SerializedProperty effects = serialized.FindProperty("effects");
            int index = effects.arraySize;
            effects.arraySize++;
            effects.GetArrayElementAtIndex(index).objectReferenceValue = spawnEffect;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
        }

        private static void BindAllPlayerFaces(ProjectileSpawnEffect spawnEffect)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                DiceFaceLoadout loadout = root.GetComponent<DiceFaceLoadout>();
                if (loadout == null)
                {
                    loadout = root.AddComponent<DiceFaceLoadout>();
                }

                SerializedObject serialized = new SerializedObject(loadout);
                SerializedProperty baseEffects = serialized.FindProperty("baseEffects");
                baseEffects.arraySize = DiceRevolverRules.FaceCount;
                for (int i = 0; i < baseEffects.arraySize; i++)
                {
                    SerializedProperty slot = baseEffects.GetArrayElementAtIndex(i);
                    if (slot.objectReferenceValue == null)
                    {
                        slot.objectReferenceValue = spawnEffect;
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetObjectArray(UnityEngine.Object target, string propertyName, params UnityEngine.Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
