using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRevolver.Tests
{
    public sealed class CombatDebugOverlayTests
    {
        private GameObject owner;

        [TearDown]
        public void TearDown()
        {
            if (owner != null)
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void OverlayShowsNewestLinesAndExpiresOldRecords()
        {
            owner = new GameObject(
                "CombatDebug",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(CombatDebugOverlay));
            Text label = owner.GetComponent<Text>();
            CombatDebugOverlay overlay = owner.GetComponent<CombatDebugOverlay>();
            CombatDebugTrace trace = new CombatDebugTrace(16);
            overlay.Configure(label, trace, 2, 2f, 18);
            CombatDebugScope scope = trace.BeginActivation(2, false, default, 0f);

            trace.Record(scope, CombatDebugEventType.ShotStarted, "射击", "第一条", null, 0, 0f);
            trace.Record(scope, CombatDebugEventType.EffectTriggered, "基础", "第二条", null, 1, 1f);
            trace.Record(scope, CombatDebugEventType.ShotEnded, "射击", "第三条", null, 0, 2f);

            Assert.That(label.text, Does.Not.Contain("第一条"));
            Assert.That(label.text, Does.Contain("第二条"));
            Assert.That(label.text, Does.Contain("第三条"));
            Assert.That(label.fontSize, Is.EqualTo(18));

            overlay.Refresh(3.1f);

            Assert.That(label.text, Does.Not.Contain("第二条"));
            Assert.That(label.text, Does.Contain("第三条"));
        }

        [Test]
        public void RuntimeViewAnchorsDebugPanelToTopLeft()
        {
            owner = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = owner.GetComponent<Canvas>();
            GameObject gunOwner = new GameObject("Gun", typeof(DiceRevolverGun));
            gunOwner.transform.SetParent(owner.transform, false);

            CombatDebugOverlay overlay = CombatDebugRuntimeView.EnsureCreated(
                canvas,
                gunOwner.GetComponent<DiceRevolverGun>(),
                null);

            RectTransform rect = overlay.GetComponent<RectTransform>();
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rect.pivot, Is.EqualTo(new Vector2(0f, 1f)));
            Object.DestroyImmediate(gunOwner);
        }

        [Test]
        public void DefaultSettingsAssetIsAvailableForPersistentInspectorTuning()
        {
            CombatDebugSettings settings = AssetDatabase.LoadAssetAtPath<CombatDebugSettings>(
                "Assets/Resources/DiceFacePrototype/CombatDebugSettings.asset");

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.DebugEnabled, Is.True);
            Assert.That(settings.MaximumLines, Is.EqualTo(14));
            Assert.That(settings.LineLifetime, Is.EqualTo(10f));
            Assert.That(settings.FontSize, Is.EqualTo(16));
        }
    }
}
