using DiceRevolver.Prototype;
using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class TopDownAimSolverTests
    {
        [TestCase(8f, 3f, -0.43f)]
        [TestCase(-8f, 3f, 0.43f)]
        public void ResolveRotationAlignsMuzzleRayWithFarTarget(float targetX, float targetZ, float muzzleX)
        {
            Vector3 pivot = Vector3.zero;
            Vector3 target = new Vector3(targetX, 0f, targetZ);
            Vector3 localMuzzlePosition = new Vector3(muzzleX, 0f, 1.36f);

            Quaternion rootRotation = TopDownAimSolver.ResolveRotation(
                pivot,
                target,
                localMuzzlePosition,
                Quaternion.identity,
                target.normalized);

            Vector3 muzzlePosition = pivot + rootRotation * localMuzzlePosition;
            Vector3 muzzleForward = rootRotation * Vector3.forward;
            Vector3 muzzleToTarget = (target - muzzlePosition).normalized;

            Assert.That(Vector3.Angle(muzzleForward, muzzleToTarget), Is.LessThan(0.05f));
        }

        [Test]
        public void ResolveRotationRemainsContinuousInsideMuzzleOrbit()
        {
            Vector3 localMuzzlePosition = new Vector3(-0.43f, 0f, 1.36f);
            Vector3 direction = new Vector3(0.7f, 0f, 0.3f).normalized;
            float muzzleOrbitRadius = localMuzzlePosition.magnitude;

            Quaternion first = TopDownAimSolver.ResolveRotation(
                Vector3.zero,
                direction * (muzzleOrbitRadius - 0.01f),
                localMuzzlePosition,
                Quaternion.identity,
                direction);
            Quaternion second = TopDownAimSolver.ResolveRotation(
                Vector3.zero,
                direction * (muzzleOrbitRadius + 0.01f),
                localMuzzlePosition,
                Quaternion.identity,
                direction);

            Assert.That(IsFinite(first), Is.True);
            Assert.That(IsFinite(second), Is.True);
            Assert.That(Quaternion.Angle(first, second), Is.LessThan(1f));
        }

        [TestCase(9f, 3f)]
        [TestCase(-9f, 3f)]
        public void HandRigKeepsMuzzleLocalPoseAndAimsVirtualShotAtTarget(float targetX, float targetZ)
        {
            GameObject playerOwner = new GameObject("Player");
            playerOwner.AddComponent<CharacterController>();
            TopDownPlayerController player = playerOwner.AddComponent<TopDownPlayerController>();
            GameObject handOwner = new GameObject("HandRig");
            handOwner.transform.SetParent(playerOwner.transform);
            handOwner.SetActive(false);
            GameObject aimOwner = new GameObject("AimRoot");
            aimOwner.transform.SetParent(handOwner.transform);
            GameObject armOwner = new GameObject("ArmVisual");
            armOwner.transform.SetParent(aimOwner.transform);
            armOwner.transform.localRotation = new Quaternion(0.4055798f, -0.579228f, 0.579228f, 0.4055798f);
            GameObject muzzleOwner = new GameObject("Muzzle");
            muzzleOwner.transform.SetParent(aimOwner.transform);
            muzzleOwner.transform.localPosition = new Vector3(-0.43f, 0.02f, 1.36f);
            muzzleOwner.transform.localRotation = Quaternion.identity;

            TopDownAimHandRig rig = handOwner.AddComponent<TopDownAimHandRig>();
            SetPrivate(rig, "player", player);
            SetPrivate(rig, "aimRoot", aimOwner.transform);
            SetPrivate(rig, "armVisual", armOwner.transform);
            SetPrivate(rig, "muzzle", muzzleOwner.transform);
            Vector3 defaultMuzzlePosition = muzzleOwner.transform.localPosition;
            Quaternion defaultMuzzleRotation = muzzleOwner.transform.localRotation;
            handOwner.SetActive(true);

            Vector3 target = new Vector3(targetX, 0f, targetZ);
            SetAutoProperty(player, "AimWorldPoint", target);
            SetAutoProperty(player, "AimDirection", target.normalized);
            rig.RefreshAimPose();

            Vector3 shotTarget = target;
            shotTarget.y = rig.ShotOrigin.y;
            Assert.That(Vector3.Angle(rig.ShotDirection, shotTarget - rig.ShotOrigin), Is.LessThan(0.05f));
            Assert.That(muzzleOwner.transform.localPosition, Is.EqualTo(defaultMuzzlePosition));
            Assert.That(Quaternion.Angle(muzzleOwner.transform.localRotation, defaultMuzzleRotation), Is.LessThan(0.001f));

            Object.DestroyImmediate(playerOwner);
        }

        [Test]
        public void PlayerPrefabAimRefreshKeepsArmAboveGameplayPlane()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
            GameObject playerInstance = Object.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            TopDownPlayerController player = playerInstance.GetComponent<TopDownPlayerController>();
            TopDownAimHandRig rig = playerInstance.GetComponentInChildren<TopDownAimHandRig>();
            SpriteRenderer armRenderer = playerInstance.transform
                .Find("VisualRoot/HandRig/AimRoot/ArmVisual")
                .GetComponent<SpriteRenderer>();

            try
            {
                InvokePrivate(rig, "Awake");
                SetAutoProperty(player, "AimWorldPoint", new Vector3(8f, 0f, 3f));
                SetAutoProperty(player, "AimDirection", new Vector3(8f, 0f, 3f).normalized);

                rig.RefreshAimPose();

                Assert.That(armRenderer.enabled, Is.True);
                Assert.That(armRenderer.sprite, Is.Not.Null);
                Assert.That(armRenderer.color.a, Is.GreaterThan(0f));
                Assert.That(armRenderer.bounds.center.y, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(playerInstance);
            }
        }

        private static bool IsFinite(Quaternion rotation)
        {
            return float.IsFinite(rotation.x)
                && float.IsFinite(rotation.y)
                && float.IsFinite(rotation.z)
                && float.IsFinite(rotation.w);
        }

        private static void SetPrivate<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void SetAutoProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
        {
            FieldInfo field = typeof(TTarget).GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object owner, string methodName)
        {
            MethodInfo method = owner.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(owner, null);
        }

    }
}
