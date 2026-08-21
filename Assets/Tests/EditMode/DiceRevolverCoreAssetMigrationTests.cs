using System;
using System.Linq;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class DiceRevolverCoreAssetMigrationTests
    {
        private const string PlayerPrefabPath = "Assets/Prefab/Player.prefab";
        private const string RobotPrefabPath = "Assets/Prefab/TestRobot.prefab";
        private const string BasicProjectilePrefabPath =
            "Assets/Prefab/Projectiles/BasicRevolverBullet.prefab";
        private const string PrototypeProjectilePrefabPath = "Assets/PrototypeProjectile.prefab";

        [TestCase(PlayerPrefabPath)]
        [TestCase(RobotPrefabPath)]
        public void ProtectedGunPrefabsKeepLegacyProjectileSpawnBaseEffects(string prefabPath)
        {
            GameObject prefab = LoadPrefab(prefabPath);
            DiceFaceLoadout loadout = prefab.GetComponentInChildren<DiceFaceLoadout>(true);
            SerializedProperty baseEffects = RequiredProperty(new SerializedObject(loadout), "baseEffects");

            Assert.That(baseEffects.arraySize, Is.GreaterThan(0));
            for (int index = 0; index < baseEffects.arraySize; index++)
            {
                Assert.That(
                    baseEffects.GetArrayElementAtIndex(index).objectReferenceValue,
                    Is.TypeOf<ProjectileSpawnEffect>());
            }
        }

        [Test]
        public void PlayerGunPrefabKeepsItsApprovedSettingsReferencesAndPose()
        {
            GameObject prefab = LoadPrefab(PlayerPrefabPath);
            DiceRevolverGun gun = prefab.GetComponentInChildren<DiceRevolverGun>(true);
            SerializedObject serializedGun = new SerializedObject(gun);

            AssertGunSettings(serializedGun, expectedDriveWeaponPose: false);
            Assert.That(ReadReference(serializedGun, "player"),
                Is.SameAs(prefab.GetComponent<TopDownPlayerController>()));

            Transform visualRoot = prefab.transform.Find(
                "VisualRoot/HandRig/AimRoot/ArmVisual");
            Transform muzzle = prefab.transform.Find("VisualRoot/HandRig/AimRoot/Muzzle");
            Assert.That(ReadReference(serializedGun, "visualRoot"), Is.SameAs(visualRoot));
            Assert.That(ReadReference(serializedGun, "muzzle"), Is.SameAs(muzzle));
            Assert.That(ReadReference(serializedGun, "ownerCollider"),
                Is.SameAs(prefab.GetComponent<CharacterController>()));
            Assert.That(ReadReference(serializedGun, "loadout"), Is.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(ReadReference(serializedGun, "projectilePrefab")),
                Is.EqualTo(PrototypeProjectilePrefabPath));

            AssertTransform(gun.transform, Vector3.zero, Quaternion.identity, Vector3.one);
            AssertTransform(
                visualRoot,
                Vector3.zero,
                new Quaternion(0.4055798f, -0.579228f, 0.579228f, 0.4055798f),
                Vector3.one);
            AssertTransform(muzzle, new Vector3(-0.43f, 0.02f, 1.36f), Quaternion.identity, Vector3.one);
            AssertSorting(visualRoot.GetComponent<Renderer>(), "Gun", 12);
        }

        [Test]
        public void TestRobotGunPrefabKeepsItsApprovedSettingsReferencesAndPose()
        {
            GameObject prefab = LoadPrefab(RobotPrefabPath);
            DiceRevolverGun gun = prefab.GetComponent<DiceRevolverGun>();
            SerializedObject serializedGun = new SerializedObject(gun);

            AssertGunSettings(serializedGun, expectedDriveWeaponPose: false);
            Assert.That(ReadReference(serializedGun, "player"),
                Is.SameAs(prefab.GetComponent<TestRobotController>()));

            Transform visualRoot = prefab.transform.Find(
                "VisualRoot/HandRig/AimRoot/GunBody");
            Transform muzzle = prefab.transform.Find("VisualRoot/HandRig/AimRoot/Muzzle");
            Assert.That(ReadReference(serializedGun, "visualRoot"), Is.SameAs(visualRoot));
            Assert.That(ReadReference(serializedGun, "muzzle"), Is.SameAs(muzzle));
            Assert.That(ReadReference(serializedGun, "ownerCollider"),
                Is.SameAs(prefab.GetComponent<CapsuleCollider>()));
            Assert.That(ReadReference(serializedGun, "loadout"),
                Is.SameAs(prefab.GetComponent<DiceFaceLoadout>()));
            Assert.That(
                AssetDatabase.GetAssetPath(ReadReference(serializedGun, "projectilePrefab")),
                Is.EqualTo(BasicProjectilePrefabPath));

            AssertTransform(gun.transform, Vector3.zero, Quaternion.identity, Vector3.one);
            AssertTransform(
                visualRoot,
                new Vector3(-0.36f, 0.02f, 0.97f),
                Quaternion.identity,
                new Vector3(0.12f, 0.08f, 0.48f));
            AssertTransform(muzzle, new Vector3(-0.43f, 0.02f, 1.36f), Quaternion.identity, Vector3.one);
            AssertSorting(visualRoot.GetComponent<Renderer>(), "Default", 0);
        }

        [Test]
        public void PlayerFallbackProjectilePrefabHasNoMissingComponentAndKeepsItsConfiguration()
        {
            GameObject playerPrefab = LoadPrefab(PlayerPrefabPath);
            DiceRevolverGun gun = playerPrefab.GetComponentInChildren<DiceRevolverGun>(true);
            SerializedObject serializedGun = new SerializedObject(gun);
            Projectile projectile = ReadReference(serializedGun, "projectilePrefab") as Projectile;

            Assert.That(projectile, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(projectile),
                Is.EqualTo(PrototypeProjectilePrefabPath));

            GameObject prefab = projectile.gameObject;
            Type[] componentTypes = prefab.GetComponents<Component>()
                .Select(component => component?.GetType())
                .ToArray();
            Assert.That(
                componentTypes,
                Is.EqualTo(new[]
                {
                    typeof(Transform),
                    typeof(MeshFilter),
                    typeof(SphereCollider),
                    typeof(MeshRenderer),
                    typeof(Rigidbody),
                    typeof(Projectile)
                }));

            AssertTransform(
                prefab.transform,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one * 0.24f);
            Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);

            MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();
            Assert.That(
                AssetDatabase.GetAssetPath(renderer.sharedMaterial),
                Is.EqualTo("Assets/Prototype_Bullet.mat"));
            AssertSorting(renderer, "Default", 0);

            SphereCollider collider = prefab.GetComponent<SphereCollider>();
            Assert.That(collider.isTrigger, Is.True);
            Assert.That(collider.radius, Is.EqualTo(0.5f));
            Assert.That(collider.center, Is.EqualTo(Vector3.zero));

            Rigidbody body = prefab.GetComponent<Rigidbody>();
            Assert.That(body.useGravity, Is.False);
            Assert.That(body.isKinematic, Is.True);
            Assert.That(body.mass, Is.EqualTo(1f));
            Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.Discrete));

            SerializedObject serializedProjectile = new SerializedObject(projectile);
            Assert.That(RequiredProperty(serializedProjectile, "speed").floatValue, Is.EqualTo(18f));
            Assert.That(RequiredProperty(serializedProjectile, "lifetime").floatValue, Is.EqualTo(1.6f));
            Assert.That(typeof(Projectile).GetEvent(nameof(Projectile.Hit)), Is.Not.Null);
        }

        [Test]
        public void BasicProjectilePrefabUsesProjectileAsItsOnlyHitOwner()
        {
            GameObject prefab = LoadPrefab(BasicProjectilePrefabPath);
            Type[] componentTypes = prefab.GetComponents<Component>()
                .Select(component => component?.GetType())
                .ToArray();

            Assert.That(
                componentTypes,
                Is.EqualTo(new[]
                {
                    typeof(Transform),
                    typeof(SphereCollider),
                    typeof(Rigidbody),
                    typeof(Projectile),
                    typeof(ProjectileVisualWrapper)
                }));
            Assert.That(typeof(Projectile).GetEvent(nameof(Projectile.Hit)), Is.Not.Null);

            SphereCollider collider = prefab.GetComponent<SphereCollider>();
            Assert.That(collider.isTrigger, Is.True);
            Assert.That(collider.radius, Is.EqualTo(0.18f));

            Rigidbody body = prefab.GetComponent<Rigidbody>();
            Assert.That(body.useGravity, Is.False);
            Assert.That(body.isKinematic, Is.True);
            Assert.That(
                body.collisionDetectionMode,
                Is.EqualTo(CollisionDetectionMode.ContinuousSpeculative));

            ProjectileVisualWrapper wrapper = prefab.GetComponent<ProjectileVisualWrapper>();
            Assert.That(
                AssetDatabase.GetAssetPath(wrapper.VisualPrefab),
                Is.EqualTo("Assets/Art/Effect/perfab/fire_1.prefab"));
            SerializedObject serializedWrapper = new SerializedObject(wrapper);
            Assert.That(
                RequiredProperty(serializedWrapper, "localEulerAngles").vector3Value,
                Is.EqualTo(new Vector3(0f, 90f, 0f)));
            Assert.That(
                RequiredProperty(serializedWrapper, "visualScale").floatValue,
                Is.EqualTo(0.4f));
        }

        private static void AssertGunSettings(
            SerializedObject serializedGun,
            bool expectedDriveWeaponPose)
        {
            Assert.That(serializedGun.FindProperty("faceCount"), Is.Null);
            Assert.That(serializedGun.FindProperty("reloadDropDistance"), Is.Null);
            Assert.That(
                RequiredProperty(serializedGun, "eventBudgetPerActivation").intValue,
                Is.EqualTo(DiceFaceActivation.DefaultEventBudget));
            Assert.That(DiceFaceActivation.DefaultEventBudget, Is.EqualTo(32));
            Assert.That(RequiredProperty(serializedGun, "holdDistance").floatValue, Is.EqualTo(0.85f));
            Assert.That(RequiredProperty(serializedGun, "holdHeight").floatValue, Is.EqualTo(0.72f));
            Assert.That(
                RequiredProperty(serializedGun, "driveWeaponPose").boolValue,
                Is.EqualTo(expectedDriveWeaponPose));
            Assert.That(RequiredProperty(serializedGun, "shotsPerSecond").floatValue, Is.EqualTo(2f));
            Assert.That(RequiredProperty(serializedGun, "reloadDuration").floatValue, Is.EqualTo(2f));
            Assert.That(RequiredProperty(serializedGun, "automaticReloadWhenEmpty").boolValue, Is.True);
            Assert.That(RequiredProperty(serializedGun, "allowManualReload").boolValue, Is.True);
            Assert.That(RequiredProperty(serializedGun, "reloadBlinkSpeed").floatValue, Is.EqualTo(8f));
            Assert.That(
                RequiredProperty(serializedGun, "reloadDimColor").colorValue,
                Is.EqualTo(new Color(0.35f, 0.35f, 0.35f, 1f)));
        }

        private static GameObject LoadPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }

        private static SerializedProperty RequiredProperty(
            SerializedObject serializedObject,
            string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, propertyName);
            return property;
        }

        private static UnityEngine.Object ReadReference(
            SerializedObject serializedObject,
            string propertyName)
        {
            return RequiredProperty(serializedObject, propertyName).objectReferenceValue;
        }

        private static void AssertTransform(
            Transform transform,
            Vector3 expectedPosition,
            Quaternion expectedRotation,
            Vector3 expectedScale)
        {
            Assert.That(transform, Is.Not.Null);
            Assert.That(transform.localPosition, Is.EqualTo(expectedPosition));
            Assert.That(Quaternion.Angle(transform.localRotation, expectedRotation), Is.LessThan(0.001f));
            Assert.That(transform.localScale, Is.EqualTo(expectedScale));
        }

        private static void AssertSorting(Renderer renderer, string expectedLayer, int expectedOrder)
        {
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sortingLayerName, Is.EqualTo(expectedLayer));
            Assert.That(renderer.sortingOrder, Is.EqualTo(expectedOrder));
        }
    }
}
