using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiceRevolver.Prototype
{
    public sealed class DiceBuildPageUI : MonoBehaviour
    {
        [SerializeField] private GameObject pageRoot;
        [SerializeField] private DiceFaceLoadout loadout;
        [SerializeField] private DiceFaceLibrary library;
        [SerializeField] private Transform faceSlotParent;
        [SerializeField] private Transform entryListParent;
        [SerializeField] private DiceBuildFaceSlotUI faceSlotTemplate;
        [SerializeField] private DiceBuildEntryButtonUI entryButtonTemplate;

        private readonly Dictionary<int, DiceBuildFaceSlotUI> faceSlots = new();
        private readonly List<DiceBuildEntryButtonUI> entryButtons = new();
        private DiceFaceEntry selectedEntry;
        private bool toggleKeyWasPressed;

        public bool IsVisible => pageRoot != null && pageRoot.activeSelf;

        private static readonly Vector2[] FacePositions =
        {
            new(1f, 0f),
            new(0f, -1f),
            new(1f, -1f),
            new(2f, -1f),
            new(3f, -1f),
            new(1f, -2f),
        };

        private void Awake()
        {
            if (loadout == null)
            {
                loadout = GetComponentInParent<DiceFaceLoadout>();
            }

            Build();
            if (pageRoot != null)
            {
                pageRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            SubscribeToLoadout();
        }

        private void OnDisable()
        {
            UnsubscribeFromLoadout();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            bool toggleKeyIsPressed = keyboard != null && keyboard.eKey.isPressed;
            if (toggleKeyIsPressed && !toggleKeyWasPressed)
            {
                Toggle();
            }

            toggleKeyWasPressed = toggleKeyIsPressed;
        }

        public void Toggle()
        {
            if (pageRoot != null)
            {
                SetVisible(!pageRoot.activeSelf);
            }
        }

        public void SetVisible(bool visible)
        {
            if (pageRoot != null)
            {
                pageRoot.SetActive(visible);
                if (visible)
                {
                    // 每次打开时重建词条列表与骰面展示，反映词条库/规则的最新状态。
                    Build();
                }
            }
        }

        public void Initialize(
            GameObject configuredPageRoot,
            DiceFaceLoadout configuredLoadout,
            DiceFaceLibrary configuredLibrary,
            Transform configuredFaceSlotParent,
            Transform configuredEntryListParent)
        {
            UnsubscribeFromLoadout();
            pageRoot = configuredPageRoot;
            loadout = configuredLoadout;
            library = configuredLibrary;
            faceSlotParent = configuredFaceSlotParent;
            entryListParent = configuredEntryListParent;
            SubscribeToLoadout();
            Build();
            SetVisible(false);
        }

        public static DiceBuildPageUI EnsureRuntimePage(
            Canvas canvas,
            DiceFaceLoadout loadout,
            DiceFaceLibrary library)
        {
            return DiceBuildRuntimeView.EnsureCreated(canvas, loadout, library);
        }

        public void Build()
        {
            BuildFaceSlots();
            BuildEntryButtons();
        }

        private void BuildFaceSlots()
        {
            faceSlots.Clear();
            if (faceSlotParent == null)
            {
                return;
            }

            DiceBuildFaceSlotUI[] existingSlots = faceSlotParent.GetComponentsInChildren<DiceBuildFaceSlotUI>(true);
            for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
            {
                DiceBuildFaceSlotUI slot = face <= existingSlots.Length ? existingSlots[face - 1] : CreateFaceSlot(face);
                if (slot == null)
                {
                    continue;
                }

                faceSlots[face] = slot;
                PositionFaceSlot(face, slot);
                slot.gameObject.SetActive(true);
                DiceFaceConfigurationSnapshot configuration = loadout != null
                    ? loadout.GetSnapshot(face)
                    : default;
                slot.Bind(face, configuration, HandleFaceClicked, HandleClearClicked);
            }
        }

        private DiceBuildFaceSlotUI CreateFaceSlot(int face)
        {
            if (faceSlotTemplate == null)
            {
                return null;
            }

            DiceBuildFaceSlotUI slot = Instantiate(faceSlotTemplate, faceSlotParent);
            slot.name = $"Dice Face {face}";
            return slot;
        }

        private static void PositionFaceSlot(int face, DiceBuildFaceSlotUI slot)
        {
            RectTransform rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            Vector2 cell = FacePositions[face - 1];
            float cellSize = Mathf.Max(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y) + 12f;
            rectTransform.anchoredPosition = new Vector2(cell.x * cellSize, cell.y * cellSize);
        }

        private void BuildEntryButtons()
        {
            entryButtons.Clear();
            if (entryListParent == null)
            {
                return;
            }

            DiceBuildEntryButtonUI[] existingButtons = entryListParent.GetComponentsInChildren<DiceBuildEntryButtonUI>(true);
            int index = 0;
            foreach (DiceFaceEntry entry in library != null ? library.Entries : System.Array.Empty<DiceFaceEntry>())
            {
                DiceBuildEntryButtonUI button = index < existingButtons.Length ? existingButtons[index] : CreateEntryButton(index);
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(true);
                button.Bind(entry, HandleEntryClicked);
                entryButtons.Add(button);
                index++;
            }

            for (int i = index; i < existingButtons.Length; i++)
            {
                existingButtons[i].gameObject.SetActive(false);
            }
        }

        private DiceBuildEntryButtonUI CreateEntryButton(int index)
        {
            if (entryButtonTemplate == null)
            {
                return null;
            }

            DiceBuildEntryButtonUI button = Instantiate(entryButtonTemplate, entryListParent);
            button.name = $"Dice Entry {index + 1}";
            return button;
        }

        private void HandleEntryClicked(DiceFaceEntry entry)
        {
            selectedEntry = entry;
            for (int i = 0; i < entryButtons.Count; i++)
            {
                entryButtons[i].SetSelected(entryButtons[i] != null && entry == GetEntryForButton(i));
            }
        }

        private DiceFaceEntry GetEntryForButton(int index)
        {
            if (library == null || index < 0 || index >= library.Entries.Count)
            {
                return null;
            }

            return library.Entries[index];
        }

        private void HandleFaceClicked(int face)
        {
            if (loadout == null || selectedEntry == null)
            {
                return;
            }

            if (!loadout.Equip(face, selectedEntry))
            {
                string detail = "未知原因";
                if (selectedEntry.Rule != null)
                {
                    IReadOnlyList<EventRuleValidationIssue> issues =
                        selectedEntry.Rule.CollectValidationIssues(selectedEntry.SlotType);
                    if (issues != null && issues.Count > 0)
                    {
                        detail = string.Join("；", issues.Select(issue => $"[{issue.Code}] {issue.Message}"));
                    }
                }

                Debug.LogWarning(
                    $"装备失败：{selectedEntry.DisplayName} 无法装到骰面 {face} 的 {selectedEntry.SlotType.ToChineseLabel()} 槽。{detail}",
                    this);
            }
        }

        private void HandleClearClicked(int face)
        {
            if (loadout == null)
            {
                return;
            }

            loadout.ClearFace(face);
        }

        private void HandleSlotChanged(int face, DiceFaceSlotType slotType, DiceFaceEntry entry)
        {
            if (faceSlots.TryGetValue(face, out DiceBuildFaceSlotUI slot))
            {
                slot.SetConfiguration(loadout.GetSnapshot(face));
            }
        }

        private void SubscribeToLoadout()
        {
            if (isActiveAndEnabled && loadout != null)
            {
                loadout.SlotChanged -= HandleSlotChanged;
                loadout.SlotChanged += HandleSlotChanged;
            }
        }

        private void UnsubscribeFromLoadout()
        {
            if (loadout != null)
            {
                loadout.SlotChanged -= HandleSlotChanged;
            }
        }
    }
}
