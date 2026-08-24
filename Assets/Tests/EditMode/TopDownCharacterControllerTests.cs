using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class TopDownCharacterControllerTests
    {
        [Test]
        public void PlayerPrefabConsumersReferencePlayerThroughSharedController()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
            GameObject instance = Object.Instantiate(prefab);

            try
            {
                TopDownPlayerController player = instance.GetComponent<TopDownPlayerController>();
                TopDownCharacterController shared = instance.GetComponent<TopDownCharacterController>();
                TopDownAimHandRig aimRig = instance.GetComponentInChildren<TopDownAimHandRig>();
                PlayerMovementAnimatorBridge animator =
                    instance.GetComponentInChildren<PlayerMovementAnimatorBridge>();
                DiceRevolverGun gun = instance.GetComponentInChildren<DiceRevolverGun>();

                Assert.That(shared, Is.SameAs(player));
                Assert.That(GetControllerReference(aimRig), Is.SameAs(shared));
                Assert.That(GetControllerReference(animator), Is.SameAs(shared));
                Assert.That(GetControllerReference(gun), Is.SameAs(shared));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void RobotRefreshFeedsCombatDecisionIntoSharedControllerState()
        {
            GameObject targetOwner = new GameObject("Target");
            targetOwner.AddComponent<CharacterController>();
            TopDownPlayerController target = targetOwner.AddComponent<TopDownPlayerController>();
            targetOwner.transform.position = new Vector3(0f, 0f, 10f);

            GameObject robotOwner = new GameObject("Robot");
            robotOwner.AddComponent<CharacterController>();
            TestRobotController robot = robotOwner.AddComponent<TestRobotController>();
            robot.Target = target;
            SerializedObject serialized = new SerializedObject(robot);
            serialized.FindProperty("enable").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                robot.RefreshControlIntent(0f);

                TopDownCharacterController shared = robot;
                Assert.That(shared.MoveInput, Is.EqualTo(Vector2.up));
                Assert.That(shared.AimWorldPoint, Is.EqualTo(targetOwner.transform.position));
                Assert.That(shared.AimDirection, Is.EqualTo(Vector3.forward));
                Assert.That(shared.FireHeld, Is.True);
                Assert.That(shared.ReloadPressedThisFrame, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(robotOwner);
                Object.DestroyImmediate(targetOwner);
            }
        }

        [Test]
        public void RobotRefreshIgnoresCombatDecisionWhenDisabled()
        {
            GameObject targetOwner = new GameObject("Target");
            targetOwner.AddComponent<CharacterController>();
            TopDownPlayerController target = targetOwner.AddComponent<TopDownPlayerController>();
            targetOwner.transform.position = new Vector3(0f, 0f, 10f);

            GameObject robotOwner = new GameObject("Robot");
            robotOwner.AddComponent<CharacterController>();
            TestRobotController robot = robotOwner.AddComponent<TestRobotController>();
            robot.Target = target;

            try
            {
                robot.RefreshControlIntent(0f);

                TopDownCharacterController shared = robot;
                Assert.That(shared.MoveInput, Is.EqualTo(Vector2.zero));
                Assert.That(shared.AimWorldPoint, Is.EqualTo(Vector3.zero));
                Assert.That(shared.AimDirection, Is.EqualTo(Vector3.forward));
                Assert.That(shared.FireHeld, Is.False);
                Assert.That(shared.ReloadPressedThisFrame, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(robotOwner);
                Object.DestroyImmediate(targetOwner);
            }
        }

        private static TopDownCharacterController GetControllerReference(object consumer)
        {
            FieldInfo field = consumer.GetType().GetField(
                "player",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(TopDownCharacterController)));
            return (TopDownCharacterController)field.GetValue(consumer);
        }
    }
}
