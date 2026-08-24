using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRevolver.Tests
{
    public sealed class DiceRevolverAmmoUITests
    {
        private static readonly Color LoadedColor = new Color(0.2f, 0.8f, 0.3f, 1f);
        private static readonly Color SpentColor = new Color(0.25f, 0.25f, 0.25f, 1f);

        [Test]
        public void LateEnabledAmmoUiReflectsAlreadyConsumedFace()
        {
            GameObject gunOwner = CreateGun(out DiceRevolverGun gun, out DiceRevolverRuntime runtime);
            GameObject uiOwner = null;
            DiceRevolverAmmoUI ui = null;
            try
            {
                DiceRevolverDrawResult draw = runtime.TryBeginShot(0f);
                uiOwner = CreateAmmoUi(gun, out ui, out Dictionary<int, Image> images);

                Assert.That(draw.Status, Is.EqualTo(DiceRevolverDrawStatus.Fired));
                Assert.That(images[draw.Face].color, Is.EqualTo(SpentColor));
            }
            finally
            {
                DisableAndDestroy(uiOwner, ui);
                Object.DestroyImmediate(gunOwner);
            }
        }

        [Test]
        public void AmmoUiUpdatesWhenRuntimeConsumesFace()
        {
            GameObject gunOwner = CreateGun(out DiceRevolverGun gun, out DiceRevolverRuntime runtime);
            GameObject uiOwner = CreateAmmoUi(
                gun,
                out DiceRevolverAmmoUI ui,
                out Dictionary<int, Image> images);
            try
            {
                DiceRevolverDrawResult draw = runtime.TryBeginShot(0f);

                Assert.That(draw.Status, Is.EqualTo(DiceRevolverDrawStatus.Fired));
                Assert.That(images[draw.Face].color, Is.EqualTo(SpentColor));
            }
            finally
            {
                DisableAndDestroy(uiOwner, ui);
                Object.DestroyImmediate(gunOwner);
            }
        }

        [Test]
        public void AmmoUiRestoresFacesWhenManualReloadCompletes()
        {
            GameObject gunOwner = CreateGun(out DiceRevolverGun gun, out DiceRevolverRuntime runtime);
            GameObject uiOwner = CreateAmmoUi(
                gun,
                out DiceRevolverAmmoUI ui,
                out Dictionary<int, Image> images);
            try
            {
                runtime.TryBeginShot(0f);
                foreach (Image image in images.Values)
                {
                    image.color = SpentColor;
                }

                Assert.That(runtime.Tick(0f, true).ReloadStarted, Is.True);
                Assert.That(runtime.Tick(2f, false).ReloadCompleted, Is.True);
                Assert.That(images.Values, Has.All.Matches<Image>(image => image.color == LoadedColor));
            }
            finally
            {
                DisableAndDestroy(uiOwner, ui);
                Object.DestroyImmediate(gunOwner);
            }
        }

        private static GameObject CreateGun(
            out DiceRevolverGun gun,
            out DiceRevolverRuntime runtime)
        {
            GameObject owner = new GameObject("Gun");
            gun = owner.AddComponent<DiceRevolverGun>();
            InvokePrivate(gun, "Awake");
            runtime = GetPrivateField<DiceRevolverRuntime>(gun, "runtime");
            return owner;
        }

        private static GameObject CreateAmmoUi(
            DiceRevolverGun gun,
            out DiceRevolverAmmoUI ui,
            out Dictionary<int, Image> images)
        {
            GameObject owner = new GameObject("AmmoUI");
            ui = owner.AddComponent<DiceRevolverAmmoUI>();
            SetPrivateField(ui, "revolver", gun);
            SetPrivateField(ui, "loadedColor", LoadedColor);
            SetPrivateField(ui, "spentColor", SpentColor);
            images = new Dictionary<int, Image>();

            for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
            {
                GameObject faceOwner = new GameObject($"Face{face}");
                faceOwner.transform.SetParent(owner.transform);
                DiceRevolverAmmoFace ammoFace = faceOwner.AddComponent<DiceRevolverAmmoFace>();
                ammoFace.FaceValue = face;
                images[face] = faceOwner.AddComponent<Image>();

                GameObject labelOwner = new GameObject("Label");
                labelOwner.transform.SetParent(faceOwner.transform);
                labelOwner.AddComponent<Text>();
            }

            InvokePrivate(ui, "Awake");
            InvokePrivate(ui, "OnEnable");
            return owner;
        }

        private static void DisableAndDestroy(GameObject owner, DiceRevolverAmmoUI ui)
        {
            if (ui != null)
            {
                InvokePrivate(ui, "OnDisable");
            }

            if (owner != null)
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
