using System.Linq;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiceRevolver.Tests
{
    public sealed class TestRobotAssetTests
    {
        private const string PrefabPath = "Assets/Prefab/TestRobot.prefab";
        private const string ScenePath = "Assets/Scenes/TopDownShooterPrototype.unity";
        private const string BasicDefinitionPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/BasicRevolverBullet.asset";
        private const string RobotDefinitionPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/TestRobotRevolverBullet.asset";
        private const string RobotEffectPath =
            "Assets/Resources/DiceFacePrototype/BulletEvents/FireTestRobotRevolverProjectile.asset";

        [Test]
        public void PrefabPackagesInfiniteTargetSharedControllerAimAnimationAndGun()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<TargetDummy>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<WorldDamageNumberSpawner>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CharacterController>(), Is.Not.Null);

            TestRobotController robot = prefab.GetComponent<TestRobotController>();
            DiceFaceLoadout loadout = prefab.GetComponent<DiceFaceLoadout>();
            DiceRevolverGun gun = prefab.GetComponent<DiceRevolverGun>();
            TopDownAimHandRig aimRig = prefab.GetComponentInChildren<TopDownAimHandRig>(true);
            PlayerMovementAnimatorBridge animator =
                prefab.GetComponentInChildren<PlayerMovementAnimatorBridge>(true);

            Assert.That(robot, Is.Not.Null);
            Assert.That(loadout, Is.Not.Null);
            Assert.That(gun, Is.Not.Null);
            Assert.That(aimRig, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(ReadReference(aimRig, "player"), Is.SameAs(robot));
            Assert.That(ReadReference(animator, "player"), Is.SameAs(robot));
            Assert.That(ReadReference(gun, "player"), Is.SameAs(robot));
            Assert.That(ReadReference(gun, "loadout"), Is.SameAs(loadout));
            Assert.That(ReadReference(gun, "muzzle"), Is.Not.Null);
        }

        [Test]
        public void RobotProjectileIsZeroDamageAndReusesBasicRuntimePrefab()
        {
            ProjectileDefinition basic =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(BasicDefinitionPath);
            ProjectileDefinition robot =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(RobotDefinitionPath);
            ProjectileSpawnEffect effect =
                AssetDatabase.LoadAssetAtPath<ProjectileSpawnEffect>(RobotEffectPath);

            Assert.That(robot, Is.Not.Null);
            Assert.That(robot.ProjectilePrefab, Is.SameAs(basic.ProjectilePrefab));
            Assert.That(robot.BuildRuntimeStats().Damage, Is.Zero);
            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.ProjectileDefinition, Is.SameAs(robot));
            Assert.That(effect.DelaySeconds, Is.Zero);
            Assert.That(effect.PrimaryProjectile, Is.True);
        }

        [Test]
        public void RobotLoadoutUsesZeroDamageEffectOnAllSixFacesAndLibrariesContainAssets()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            ProjectileDefinition definition =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(RobotDefinitionPath);
            ProjectileSpawnEffect effect =
                AssetDatabase.LoadAssetAtPath<ProjectileSpawnEffect>(RobotEffectPath);
            ProjectileDefinitionLibrary definitions = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionLibrary>(
                "Assets/Resources/DiceFacePrototype/Projectiles/ProjectileDefinitionLibrary.asset");
            BulletEventLibrary effects = AssetDatabase.LoadAssetAtPath<BulletEventLibrary>(
                "Assets/Resources/DiceFacePrototype/BulletEventLibrary.asset");

            Assert.That(definitions.Definitions, Does.Contain(definition));
            Assert.That(effects.Effects, Does.Contain(effect));

            DiceFaceLoadout loadout = prefab.GetComponent<DiceFaceLoadout>();
            for (int face = 1; face <= 6; face++)
            {
                Assert.That(loadout.GetBaseEffect(face), Is.SameAs(effect), $"Face {face}");
            }
        }

        [Test]
        public void PrototypeSceneContainsExactlyOneRobotPrefabInstance()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject[] robots = scene.GetRootGameObjects()
                    .Where(root => root.GetComponent<TestRobotController>() != null)
                    .ToArray();

                Assert.That(robots, Has.Length.EqualTo(1));
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(robots[0]), Is.Not.Null);
                Assert.That(robots[0].transform.position.y, Is.Zero.Within(0.0001f));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Object ReadReference(Object owner, string propertyName)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null);
            return property.objectReferenceValue;
        }
    }
}
