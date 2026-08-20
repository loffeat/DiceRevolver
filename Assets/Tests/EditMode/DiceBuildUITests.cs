using DiceRevolver.Prototype;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace DiceRevolver.Tests
{
    public sealed class DiceBuildUITests
    {
        [Test]
        public void FaceSlotBindShowsFourIndependentSlotLabelsAndInvokesClick()
        {
            GameObject slotOwner = new GameObject("FaceSlot");
            slotOwner.SetActive(false);
            Button button = slotOwner.AddComponent<Button>();
            Text faceLabel = CreateText("FaceLabel", slotOwner.transform);
            Text baseLabel = CreateText("Base", slotOwner.transform);
            Text onFireLabel = CreateText("OnFire", slotOwner.transform);
            Text onHitLabel = CreateText("OnHit", slotOwner.transform);
            Text onFireEndLabel = CreateText("OnFireEnd", slotOwner.transform);
            DiceBuildFaceSlotUI slot = slotOwner.AddComponent<DiceBuildFaceSlotUI>();
            slot.Configure(
                button,
                faceLabel,
                baseLabel,
                onFireLabel,
                onHitLabel,
                onFireEndLabel);
            slotOwner.SetActive(true);
            int clickedFace = 0;
            DiceFaceEntry baseEntry = CreateEntry("基础射击", DiceFaceSlotType.Base);
            DiceFaceEntry onFireEntry = CreateEntry("双重射击", DiceFaceSlotType.OnFire);
            DiceFaceEntry onHitEntry = CreateEntry("爆炸弹", DiceFaceSlotType.OnHit);
            DiceFaceEntry onFireEndEntry = CreateEntry("强制四点", DiceFaceSlotType.OnFireEnd);
            DiceFaceConfigurationSnapshot snapshot = new DiceFaceConfigurationSnapshot(
                baseEntry,
                onFireEntry,
                onHitEntry,
                onFireEndEntry);

            slot.Bind(4, snapshot, face => clickedFace = face);
            button.onClick.Invoke();

            Assert.That(faceLabel.text, Is.EqualTo("4"));
            Assert.That(baseLabel.text, Does.Contain("基础射击"));
            Assert.That(onFireLabel.text, Does.Contain("双重射击"));
            Assert.That(onHitLabel.text, Does.Contain("爆炸弹"));
            Assert.That(onFireEndLabel.text, Does.Contain("强制四点"));
            Assert.That(clickedFace, Is.EqualTo(4));

            Object.DestroyImmediate(slotOwner);
            Object.DestroyImmediate(baseEntry);
            Object.DestroyImmediate(onFireEntry);
            Object.DestroyImmediate(onHitEntry);
            Object.DestroyImmediate(onFireEndEntry);
        }

        [Test]
        public void EntryButtonBindInvokesClickAndSetsSelectedColor()
        {
            GameObject buttonOwner = new GameObject("EntryButton");
            buttonOwner.SetActive(false);
            Button button = buttonOwner.AddComponent<Button>();
            Image image = buttonOwner.AddComponent<Image>();
            Text nameLabel = CreateText("NameLabel", buttonOwner.transform);
            Text descriptionLabel = CreateText("DescriptionLabel", buttonOwner.transform);
            DiceBuildEntryButtonUI entryButton = buttonOwner.AddComponent<DiceBuildEntryButtonUI>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            SetPrivate(entryButton, "button", button);
            SetPrivate(entryButton, "backgroundImage", image);
            SetPrivate(entryButton, "nameLabel", nameLabel);
            SetPrivate(entryButton, "descriptionLabel", descriptionLabel);
            buttonOwner.SetActive(true);
            DiceFaceEntry clickedEntry = null;

            entryButton.Bind(entry, clicked => clickedEntry = clicked);
            Color unselectedColor = image.color;
            entryButton.SetSelected(true);
            Color selectedColor = image.color;
            button.onClick.Invoke();

            Assert.That(nameLabel.text, Is.EqualTo(entry.DisplayName));
            Assert.That(clickedEntry, Is.SameAs(entry));
            Assert.That(selectedColor, Is.Not.EqualTo(unselectedColor));

            Object.DestroyImmediate(buttonOwner);
            Object.DestroyImmediate(entry);
        }

        [Test]
        public void PageSetVisibleControlsPageRoot()
        {
            GameObject owner = new GameObject("BuildPageController");
            GameObject pageRoot = new GameObject("PageRoot");
            pageRoot.transform.SetParent(owner.transform);
            DiceBuildPageUI page = owner.AddComponent<DiceBuildPageUI>();
            SetPrivate(page, "pageRoot", pageRoot);

            page.SetVisible(false);
            Assert.That(pageRoot.activeSelf, Is.False);

            page.Toggle();
            Assert.That(pageRoot.activeSelf, Is.True);

            Object.DestroyImmediate(owner);
        }

        [Test]
        public void EnsureRuntimePageCreatesToggleablePageUnderCanvas()
        {
            GameObject canvasOwner = new GameObject("DiceRevolverHUD");
            Canvas canvas = canvasOwner.AddComponent<Canvas>();
            GameObject playerOwner = new GameObject("Player");
            DiceFaceLoadout loadout = playerOwner.AddComponent<DiceFaceLoadout>();

            DiceBuildPageUI page = DiceBuildPageUI.EnsureRuntimePage(canvas, loadout, null);

            Assert.That(page, Is.Not.Null);
            Assert.That(page.IsVisible, Is.False);

            page.Toggle();

            Assert.That(page.IsVisible, Is.True);

            Object.DestroyImmediate(canvasOwner);
            Object.DestroyImmediate(playerOwner);
        }

        [Test]
        public void EKeyPressOpensAndClosesRuntimePage()
        {
            GameObject canvasOwner = new GameObject("DiceRevolverHUD");
            Canvas canvas = canvasOwner.AddComponent<Canvas>();
            GameObject playerOwner = new GameObject("Player");
            DiceFaceLoadout loadout = playerOwner.AddComponent<DiceFaceLoadout>();
            DiceBuildPageUI page = DiceBuildPageUI.EnsureRuntimePage(canvas, loadout, null);
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();

            try
            {
                PressE(keyboard, page);
                Assert.That(page.IsVisible, Is.True);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InvokePageUpdate(page);
                PressE(keyboard, page);
                Assert.That(page.IsVisible, Is.False);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                Object.DestroyImmediate(canvasOwner);
                Object.DestroyImmediate(playerOwner);
            }
        }

        [Test]
        public void RuntimePageEquipsFourIndependentEntriesToOneFace()
        {
            GameObject canvasOwner = new GameObject("DiceRevolverHUD");
            Canvas canvas = canvasOwner.AddComponent<Canvas>();
            GameObject playerOwner = new GameObject("Player");
            DiceFaceLoadout loadout = playerOwner.AddComponent<DiceFaceLoadout>();
            DiceFaceEntry baseEntry = CreateEntry("基础射击", DiceFaceSlotType.Base);
            DiceFaceEntry onFireEntry = CreateEntry("双重射击", DiceFaceSlotType.OnFire);
            DiceFaceEntry onHitEntry = CreateEntry("爆炸弹", DiceFaceSlotType.OnHit);
            DiceFaceEntry onFireEndEntry = CreateEntry("强制四点", DiceFaceSlotType.OnFireEnd);
            DiceFaceLibrary library = ScriptableObject.CreateInstance<DiceFaceLibrary>();
            SetPrivate(library, "entries", new[] { baseEntry, onFireEntry, onHitEntry, onFireEndEntry });

            DiceBuildPageUI page = DiceBuildPageUI.EnsureRuntimePage(canvas, loadout, library);
            DiceBuildFaceSlotUI[] slots = page.GetComponentsInChildren<DiceBuildFaceSlotUI>(true);
            DiceBuildEntryButtonUI[] entries = page.GetComponentsInChildren<DiceBuildEntryButtonUI>(true);

            Assert.That(slots, Has.Length.EqualTo(DiceRevolverRules.FaceCount));
            Assert.That(entries, Has.Length.EqualTo(4));

            for (int i = 0; i < entries.Length; i++)
            {
                entries[i].GetComponent<Button>().onClick.Invoke();
                slots[0].GetComponent<Button>().onClick.Invoke();
            }

            Assert.That(loadout.GetEntry(1, DiceFaceSlotType.Base), Is.SameAs(baseEntry));
            Assert.That(loadout.GetEntry(1, DiceFaceSlotType.OnFire), Is.SameAs(onFireEntry));
            Assert.That(loadout.GetEntry(1, DiceFaceSlotType.OnHit), Is.SameAs(onHitEntry));
            Assert.That(loadout.GetEntry(1, DiceFaceSlotType.OnFireEnd), Is.SameAs(onFireEndEntry));
            Assert.That(slots[0].transform.Find("Base").GetComponent<Text>().text, Does.Contain("基础射击"));
            Assert.That(slots[0].transform.Find("OnFire").GetComponent<Text>().text, Does.Contain("双重射击"));
            Assert.That(slots[0].transform.Find("OnHit").GetComponent<Text>().text, Does.Contain("爆炸弹"));
            Assert.That(slots[0].transform.Find("OnFireEnd").GetComponent<Text>().text, Does.Contain("强制四点"));

            Object.DestroyImmediate(canvasOwner);
            Object.DestroyImmediate(playerOwner);
            Object.DestroyImmediate(baseEntry);
            Object.DestroyImmediate(onFireEntry);
            Object.DestroyImmediate(onHitEntry);
            Object.DestroyImmediate(onFireEndEntry);
            Object.DestroyImmediate(library);
        }

        [Test]
        public void PrototypeResourceLibraryContainsFourEntries()
        {
            DiceFaceLibrary library = Resources.Load<DiceFaceLibrary>("DiceFacePrototype/DiceFaceLibrary");

            Assert.That(library, Is.Not.Null);
            Assert.That(library.Entries.Count, Is.EqualTo(4));
        }

        private static Text CreateText(string name, Transform parent)
        {
            GameObject owner = new GameObject(name);
            owner.transform.SetParent(parent);
            return owner.AddComponent<Text>();
        }

        private static DiceFaceEntry CreateEntry(string displayName, DiceFaceSlotType slotType)
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            SetPrivate(entry, "displayName", displayName);
            SetPrivate(entry, "slotType", slotType);
            return entry;
        }

        private static void SetPrivate<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void PressE(Keyboard keyboard, DiceBuildPageUI page)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            InputSystem.Update();
            Assert.That(Keyboard.current, Is.SameAs(keyboard));
            Assert.That(keyboard.eKey.isPressed, Is.True);
            InvokePageUpdate(page);
        }

        private static void InvokePageUpdate(DiceBuildPageUI page)
        {
            MethodInfo update = typeof(DiceBuildPageUI).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            update.Invoke(page, null);
        }
    }
}
