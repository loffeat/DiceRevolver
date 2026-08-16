using System;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRevolver.Prototype
{
    public sealed class DiceBuildFaceSlotUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text faceLabel;
        [SerializeField] private Text entryLabel;

        private int face;
        private Action<int> clicked;
        private bool isWired;

        public void Configure(Button configuredButton, Text configuredFaceLabel, Text configuredEntryLabel)
        {
            button = configuredButton;
            faceLabel = configuredFaceLabel;
            entryLabel = configuredEntryLabel;
            isWired = false;
            EnsureButtonWired();
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            EnsureButtonWired();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }

            isWired = false;
        }

        public void Bind(int face, DiceFaceEntry entry, Action<int> clicked)
        {
            this.face = face;
            this.clicked = clicked;
            EnsureButtonWired();

            if (faceLabel != null)
            {
                faceLabel.text = face.ToString();
            }

            SetEntry(entry);
        }

        public void SetEntry(DiceFaceEntry entry)
        {
            if (entryLabel != null)
            {
                entryLabel.text = entry != null ? entry.DisplayName : "Empty";
            }
        }

        private void HandleClicked()
        {
            clicked?.Invoke(face);
        }

        private void EnsureButtonWired()
        {
            if (isWired)
            {
                return;
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClicked);
            button.onClick.AddListener(HandleClicked);
            isWired = true;
        }
    }
}
