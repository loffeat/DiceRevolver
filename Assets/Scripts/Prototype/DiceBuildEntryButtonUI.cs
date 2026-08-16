using System;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRevolver.Prototype
{
    public sealed class DiceBuildEntryButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color selectedColor = new Color(0.95f, 0.86f, 0.3f, 1f);
        [SerializeField] private Color unselectedColor = new Color(0.16f, 0.16f, 0.18f, 0.92f);

        private DiceFaceEntry entry;
        private Action<DiceFaceEntry> clicked;
        private bool isWired;

        public void Configure(
            Button configuredButton,
            Text configuredNameLabel,
            Text configuredDescriptionLabel,
            Image configuredBackgroundImage)
        {
            button = configuredButton;
            nameLabel = configuredNameLabel;
            descriptionLabel = configuredDescriptionLabel;
            backgroundImage = configuredBackgroundImage;
            isWired = false;
            EnsureButtonWired();
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
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

        public void Bind(DiceFaceEntry entry, Action<DiceFaceEntry> clicked)
        {
            this.entry = entry;
            this.clicked = clicked;
            EnsureButtonWired();

            if (nameLabel != null)
            {
                nameLabel.text = entry != null ? entry.DisplayName : string.Empty;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = entry != null ? entry.Description : string.Empty;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedColor : unselectedColor;
            }
        }

        private void HandleClicked()
        {
            if (entry != null)
            {
                clicked?.Invoke(entry);
            }
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
