using System;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRevolver.Prototype
{
    public sealed class DiceBuildFaceSlotUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text faceLabel;
        [SerializeField] private Text baseLabel;
        [SerializeField] private Text onFireLabel;
        [SerializeField] private Text onHitLabel;
        [SerializeField] private Text onFireEndLabel;
        [SerializeField] private Text passiveLabel;

        private int face;
        private Action<int> clicked;
        private bool isWired;

        public void Configure(
            Button configuredButton,
            Text configuredFaceLabel,
            Text configuredBaseLabel,
            Text configuredOnFireLabel,
            Text configuredOnHitLabel,
            Text configuredOnFireEndLabel,
            Text configuredPassiveLabel = null)
        {
            button = configuredButton;
            faceLabel = configuredFaceLabel;
            baseLabel = configuredBaseLabel;
            onFireLabel = configuredOnFireLabel;
            onHitLabel = configuredOnHitLabel;
            onFireEndLabel = configuredOnFireEndLabel;
            passiveLabel = configuredPassiveLabel;
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

        public void Bind(int face, DiceFaceConfigurationSnapshot configuration, Action<int> clicked)
        {
            this.face = face;
            this.clicked = clicked;
            EnsureButtonWired();

            if (faceLabel != null)
            {
                faceLabel.text = configuration.IsPassiveFace
                    ? $"{face}（被动）"
                    : face.ToString();
            }

            SetConfiguration(configuration);
        }

        public void SetConfiguration(DiceFaceConfigurationSnapshot configuration)
        {
            SetSlotLabel(baseLabel, DiceFaceSlotType.Base, configuration);
            SetSlotLabel(onFireLabel, DiceFaceSlotType.OnFire, configuration);
            SetSlotLabel(onHitLabel, DiceFaceSlotType.OnHit, configuration);
            SetSlotLabel(onFireEndLabel, DiceFaceSlotType.OnFireEnd, configuration);
        }

        private static void SetSlotLabel(
            Text label,
            DiceFaceSlotType slotType,
            DiceFaceConfigurationSnapshot configuration)
        {
            if (label == null)
            {
                return;
            }

            DiceFaceEntry entry = configuration.GetEntry(slotType);
            BulletEventEffect effect = configuration.GetEffect(slotType);
            string value = entry != null
                ? entry.DisplayName
                : effect != null ? effect.name : "空";
            label.text = $"{slotType.ToChineseLabel()}: {value}";
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
