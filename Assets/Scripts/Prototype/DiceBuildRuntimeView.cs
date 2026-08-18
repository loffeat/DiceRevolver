using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DiceRevolver.Prototype
{
    internal static class DiceBuildRuntimeView
    {
        private static readonly Color OverlayColor = new(0.035f, 0.04f, 0.05f, 0.96f);
        private static readonly Color SectionColor = new(0.10f, 0.11f, 0.13f, 0.98f);
        private static readonly Color SlotColor = new(0.20f, 0.22f, 0.25f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapCurrentScene()
        {
            if (Object.FindFirstObjectByType<DiceBuildPageUI>() != null)
            {
                return;
            }

            Canvas canvas = FindHudCanvas();
            TopDownPlayerController player = Object.FindFirstObjectByType<TopDownPlayerController>();
            if (canvas == null || player == null)
            {
                return;
            }

            DiceFaceLoadout loadout = player.GetComponent<DiceFaceLoadout>();
            if (loadout == null)
            {
                loadout = player.gameObject.AddComponent<DiceFaceLoadout>();
            }

            DiceFaceLibrary library = Resources.Load<DiceFaceLibrary>("DiceFacePrototype/DiceFaceLibrary");
            EnsureCreated(canvas, loadout, library);
        }

        public static DiceBuildPageUI EnsureCreated(
            Canvas canvas,
            DiceFaceLoadout loadout,
            DiceFaceLibrary library)
        {
            if (canvas == null)
            {
                return null;
            }

            DiceBuildPageUI existing = canvas.GetComponentInChildren<DiceBuildPageUI>(true);
            if (existing != null)
            {
                return existing;
            }

            EnsureEventSystem(canvas.transform);

            GameObject controllerOwner = CreateRectObject("DiceBuildPageController", canvas.transform);
            Stretch(controllerOwner.GetComponent<RectTransform>());

            GameObject pageRoot = CreateImageObject("DiceBuildPage", controllerOwner.transform, OverlayColor);
            Stretch(pageRoot.GetComponent<RectTransform>());

            CreateText("Title", pageRoot.transform, "骰面构筑", 34, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f));

            GameObject leftSection = CreateImageObject("DiceFaces", pageRoot.transform, SectionColor);
            SetAnchors(leftSection.GetComponent<RectTransform>(), new Vector2(0.04f, 0.10f), new Vector2(0.48f, 0.88f));
            CreateText("LeftTitle", leftSection.transform, "左轮骰面", 26, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f));

            GameObject faceSlotParent = CreateRectObject("FaceSlots", leftSection.transform);
            RectTransform faceSlotRect = faceSlotParent.GetComponent<RectTransform>();
            SetAnchors(faceSlotRect, new Vector2(0.16f, 0.18f), new Vector2(0.84f, 0.78f));
            faceSlotRect.pivot = new Vector2(0f, 1f);

            for (int face = 1; face <= 6; face++)
            {
                CreateFaceSlot(face, faceSlotParent.transform);
            }

            GameObject rightSection = CreateImageObject("EntryLibrary", pageRoot.transform, SectionColor);
            SetAnchors(rightSection.GetComponent<RectTransform>(), new Vector2(0.52f, 0.10f), new Vector2(0.96f, 0.88f));
            CreateText("RightTitle", rightSection.transform, "可装备骰面词条", 26, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f));

            Transform entryContent = CreateEntryList(rightSection.transform);
            if (library != null)
            {
                for (int i = 0; i < library.Entries.Count; i++)
                {
                    CreateEntryButton(i + 1, entryContent);
                }
            }

            if (library == null || library.Entries.Count == 0)
            {
                CreateText("EmptyLibrary", rightSection.transform, "词条库为空", 22, TextAnchor.MiddleCenter,
                    new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.65f));
            }

            DiceBuildPageUI page = controllerOwner.AddComponent<DiceBuildPageUI>();
            page.Initialize(pageRoot, loadout, library, faceSlotParent.transform, entryContent);
            return page;
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

        private static void EnsureEventSystem(Transform parent)
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject owner = new("DiceBuildEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            owner.transform.SetParent(parent, false);
        }

        private static void CreateFaceSlot(int face, Transform parent)
        {
            GameObject owner = CreateImageObject($"Dice Face {face}", parent, SlotColor);
            RectTransform rect = owner.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(144f, 144f);

            Button button = owner.AddComponent<Button>();
            button.targetGraphic = owner.GetComponent<Image>();
            Text faceLabel = CreateText("Face", owner.transform, face.ToString(), 28, TextAnchor.UpperCenter,
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.96f));
            Text baseLabel = CreateSlotText("Base", owner.transform, new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.72f));
            Text onFireLabel = CreateSlotText("OnFire", owner.transform, new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.54f));
            Text onHitLabel = CreateSlotText("OnHit", owner.transform, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.36f));
            Text onFireEndLabel = CreateSlotText("OnFireEnd", owner.transform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.18f));
            DiceBuildFaceSlotUI slot = owner.AddComponent<DiceBuildFaceSlotUI>();
            slot.Configure(button, faceLabel, baseLabel, onFireLabel, onHitLabel, onFireEndLabel);
        }

        private static Transform CreateEntryList(Transform parent)
        {
            GameObject viewport = CreateImageObject("Viewport", parent, new Color(0.06f, 0.065f, 0.075f, 1f));
            SetAnchors(viewport.GetComponent<RectTransform>(), new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.86f));
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            GameObject content = CreateRectObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content.transform;
        }

        private static void CreateEntryButton(int index, Transform parent)
        {
            GameObject owner = CreateImageObject($"Dice Entry {index}", parent, SlotColor);
            LayoutElement layout = owner.AddComponent<LayoutElement>();
            layout.preferredHeight = 108f;
            Button button = owner.AddComponent<Button>();
            Image image = owner.GetComponent<Image>();
            button.targetGraphic = image;

            Text nameLabel = CreateText("Name", owner.transform, string.Empty, 21, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.62f), new Vector2(0.72f, 0.94f));
            Text slotLabel = CreateText("Slot", owner.transform, string.Empty, 15, TextAnchor.MiddleRight,
                new Vector2(0.72f, 0.62f), new Vector2(0.96f, 0.94f));
            Text descriptionLabel = CreateText("Description", owner.transform, string.Empty, 15, TextAnchor.UpperLeft,
                new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.60f));
            DiceBuildEntryButtonUI entryButton = owner.AddComponent<DiceBuildEntryButtonUI>();
            entryButton.Configure(button, nameLabel, slotLabel, descriptionLabel, image);
        }

        private static Text CreateSlotText(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Text label = CreateText(name, parent, string.Empty, 13, TextAnchor.MiddleLeft, anchorMin, anchorMax);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 13;
            return label;
        }

        private static GameObject CreateRectObject(string name, Transform parent)
        {
            GameObject owner = new(name, typeof(RectTransform));
            owner.transform.SetParent(parent, false);
            return owner;
        }

        private static GameObject CreateImageObject(string name, Transform parent, Color color)
        {
            GameObject owner = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            owner.transform.SetParent(parent, false);
            owner.GetComponent<Image>().color = color;
            return owner;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject owner = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            owner.transform.SetParent(parent, false);
            RectTransform rect = owner.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax);

            Text text = owner.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            SetAnchors(rect, Vector2.zero, Vector2.one);
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
