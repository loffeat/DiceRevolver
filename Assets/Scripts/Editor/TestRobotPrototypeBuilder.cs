using System;
using System.Linq;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiceRevolver.Editor
{
    public static class TestRobotPrototypeBuilder
    {
        private const string TargetPrefabPath = "Assets/Prefab/TargetDummy.prefab";
        private const string RobotPrefabPath = "Assets/Prefab/TestRobot.prefab";
        private const string ScenePath = "Assets/Scenes/TopDownShooterPrototype.unity";
        private const string BasicDefinitionPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/BasicRevolverBullet.asset";
        private const string RobotDefinitionPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/TestRobotRevolverBullet.asset";
        private const string DefinitionLibraryPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/ProjectileDefinitionLibrary.asset";
        private const string RobotEffectPath =
            "Assets/Resources/DiceFacePrototype/BulletEvents/FireTestRobotRevolverProjectile.asset";
        private const string EffectLibraryPath =
            "Assets/Resources/DiceFacePrototype/BulletEventLibrary.asset";

        [MenuItem("Dice Revolver/Create Test Robot")]
        public static void Build()
        {
            try
            {
                BuildAssets();
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                throw;
            }
        }

        private static void BuildAssets()
        {
            GameObject targetPrefab = RequireAsset<GameObject>(TargetPrefabPath);
            ProjectileDefinition basicDefinition = RequireAsset<ProjectileDefinition>(BasicDefinitionPath);
            ProjectileDefinition robotDefinition = LoadOrCreateRobotDefinition(basicDefinition);
            ProjectileSpawnEffect robotEffect = LoadOrCreateRobotEffect(robotDefinition);

            AppendToDefinitionLibrary(robotDefinition);
            AppendToEffectLibrary(robotEffect);
            GameObject robotPrefab = LoadOrCreateRobotPrefab(targetPrefab, basicDefinition, robotEffect);
            AddRobotToPrototypeScene(robotPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Test robot prototype assets are ready.");
        }

        private static ProjectileDefinition LoadOrCreateRobotDefinition(ProjectileDefinition basic)
        {
            ProjectileDefinition definition =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(RobotDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
                AssetDatabase.CreateAsset(definition, RobotDefinitionPath);
            }

            ProjectileRuntimeStats basicStats = basic.BuildRuntimeStats();
            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("displayName").stringValue = "测试机器人左轮子弹";
            serialized.FindProperty("projectilePrefab").objectReferenceValue = basic.ProjectilePrefab;
            serialized.FindProperty("projectileType").stringValue = basicStats.ProjectileType;
            serialized.FindProperty("projectileTag").stringValue = basicStats.ProjectileTag;
            serialized.FindProperty("damage").floatValue = 0f;
            serialized.FindProperty("flightDistance").floatValue = basicStats.FlightDistance;
            serialized.FindProperty("flightSpeed").floatValue = basicStats.FlightSpeed;
            serialized.FindProperty("enemyPierceCount").intValue = basicStats.EnemyPierceCount;
            serialized.FindProperty("defaultAttackEffect").boolValue = basic.DefaultAttackEffect;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static ProjectileSpawnEffect LoadOrCreateRobotEffect(ProjectileDefinition definition)
        {
            ProjectileSpawnEffect effect =
                AssetDatabase.LoadAssetAtPath<ProjectileSpawnEffect>(RobotEffectPath);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<ProjectileSpawnEffect>();
                AssetDatabase.CreateAsset(effect, RobotEffectPath);
            }

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

        private static GameObject LoadOrCreateRobotPrefab(
            GameObject targetPrefab,
            ProjectileDefinition basicDefinition,
            ProjectileSpawnEffect robotEffect)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RobotPrefabPath);
            if (existing != null)
            {
                GameObject existingRoot = PrefabUtility.LoadPrefabContents(RobotPrefabPath);
                try
                {
                    ConfigureRobotPresentationAndRhythm(
                        existingRoot.GetComponent<TestRobotController>());
                    PrefabUtility.SaveAsPrefabAsset(existingRoot, RobotPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(existingRoot);
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(RobotPrefabPath);
            }

            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
            try
            {
                PrefabUtility.UnpackPrefabInstance(
                    root,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                root.name = "TestRobot";

                Rigidbody body = root.GetComponent<Rigidbody>();
                if (body != null)
                {
                    UnityEngine.Object.DestroyImmediate(body);
                }

                CharacterController characterController = root.AddComponent<CharacterController>();
                characterController.height = 1f;
                characterController.radius = 0.42f;
                characterController.center = Vector3.zero;

                TestRobotController robot = root.AddComponent<TestRobotController>();
                ConfigureRobotPresentationAndRhythm(robot);
                DiceFaceLoadout loadout = root.AddComponent<DiceFaceLoadout>();
                for (int face = 1; face <= 6; face++)
                {
                    loadout.SetBaseEffect(face, robotEffect);
                }

                TopDownAimHandRig aimRig = root.GetComponentInChildren<TopDownAimHandRig>(true);
                PlayerMovementAnimatorBridge animator =
                    root.GetComponentInChildren<PlayerMovementAnimatorBridge>(true);
                Transform aimRoot = root.transform.Find("VisualRoot/HandRig/AimRoot");
                Transform muzzle = root.transform.Find("VisualRoot/HandRig/AimRoot/Muzzle");
                Transform gunBody = root.transform.Find("VisualRoot/HandRig/AimRoot/GunBody");
                if (aimRig == null || animator == null || aimRoot == null || muzzle == null || gunBody == null)
                {
                    throw new InvalidOperationException(
                        "TargetDummy visual hierarchy is missing the aim, animation, muzzle, or gun ports required by TestRobot.");
                }

                gunBody.gameObject.SetActive(true);
                SetReference(aimRig, "player", robot);
                SetReference(animator, "player", robot);

                DiceRevolverGun gun = root.AddComponent<DiceRevolverGun>();
                SetReference(gun, "player", robot);
                SetReference(gun, "visualRoot", gunBody);
                SetReference(gun, "muzzle", muzzle);
                SetReference(gun, "projectilePrefab", basicDefinition.ProjectilePrefab);
                SetReference(gun, "ownerCollider", root.GetComponent<CapsuleCollider>());
                SetReference(gun, "loadout", loadout);
                SetBoolean(gun, "driveWeaponPose", false);
                SetFloat(gun, "shotsPerSecond", 2f);
                SetFloat(gun, "reloadDuration", 2f);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, RobotPrefabPath);
                return saved;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureRobotPresentationAndRhythm(TestRobotController robot)
        {
            if (robot == null)
            {
                throw new InvalidOperationException("TestRobot prefab is missing TestRobotController.");
            }

            SerializedObject serialized = new SerializedObject(robot);
            serialized.FindProperty("rotateBodyTowardAim").boolValue = false;
            serialized.FindProperty("movementDuration").floatValue = 0.7f;
            serialized.FindProperty("holdingDuration").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AppendToDefinitionLibrary(ProjectileDefinition definition)
        {
            ProjectileDefinitionLibrary library = RequireAsset<ProjectileDefinitionLibrary>(DefinitionLibraryPath);
            AppendReference(library, "definitions", definition);
        }

        private static void AppendToEffectLibrary(ProjectileSpawnEffect effect)
        {
            BulletEventLibrary library = RequireAsset<BulletEventLibrary>(EffectLibraryPath);
            AppendReference(library, "effects", effect);
        }

        private static void AddRobotToPrototypeScene(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (scene.GetRootGameObjects().Any(root => root.GetComponent<TestRobotController>() != null))
            {
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "TestRobot";
            instance.transform.position = new Vector3(-4f, 0f, 4f);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Required asset is missing: {path}");
            }

            return asset;
        }

        private static void AppendReference(
            UnityEngine.Object owner,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty array = serialized.FindProperty(propertyName);
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                {
                    return;
                }
            }

            int index = array.arraySize;
            array.arraySize++;
            array.GetArrayElementAtIndex(index).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
        }

        private static void SetReference(
            UnityEngine.Object owner,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolean(UnityEngine.Object owner, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object owner, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
