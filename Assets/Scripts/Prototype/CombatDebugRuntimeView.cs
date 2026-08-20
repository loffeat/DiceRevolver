using UnityEngine;
using UnityEngine.UI;

namespace DiceRevolver.Prototype
{
    public static class CombatDebugRuntimeView
    {
        private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.62f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapCurrentScene()
        {
            TopDownPlayerController player = Object.FindFirstObjectByType<TopDownPlayerController>();
            DiceRevolverGun gun = player != null ? player.GetComponentInChildren<DiceRevolverGun>(true) : null;
            Canvas canvas = FindHudCanvas();
            CombatDebugSettings settings = Resources.Load<CombatDebugSettings>("DiceFacePrototype/CombatDebugSettings");
            if (gun != null && canvas != null && (settings == null || settings.DebugEnabled))
            {
                EnsureCreated(canvas, gun, settings);
            }
        }

        public static CombatDebugOverlay EnsureCreated(Canvas canvas, DiceRevolverGun gun, CombatDebugSettings settings)
        {
            if (canvas == null || gun == null)
            {
                return null;
            }

            CombatDebugOverlay existing = canvas.GetComponentInChildren<CombatDebugOverlay>(true);
            if (existing != null)
            {
                return existing;
            }

            GameObject panel = new GameObject("CombatEventDebug", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CombatDebugOverlay));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(16f, -16f);
            panelRect.sizeDelta = new Vector2(settings != null ? settings.PanelWidth : 620f, settings != null ? settings.PanelHeight : 420f);
            Image background = panel.GetComponent<Image>();
            background.color = BackgroundColor;
            background.raycastTarget = false;

            GameObject labelOwner = new GameObject("EventLog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelOwner.transform.SetParent(panel.transform, false);
            RectTransform labelRect = labelOwner.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 10f);
            labelRect.offsetMax = new Vector2(-12f, -10f);
            Text label = labelOwner.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;

            CombatDebugOverlay overlay = panel.GetComponent<CombatDebugOverlay>();
            overlay.Configure(
                label,
                gun.DebugTrace,
                settings != null ? settings.MaximumLines : 14,
                settings != null ? settings.LineLifetime : 10f,
                settings != null ? settings.FontSize : 16);
            return overlay;
        }

        private static Canvas FindHudCanvas()
        {
            GameObject hud = GameObject.Find("DiceRevolverHUD");
            if (hud != null && hud.TryGetComponent(out Canvas hudCanvas))
            {
                return hudCanvas;
            }

            return Object.FindFirstObjectByType<Canvas>();
        }
    }
}
