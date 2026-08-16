using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRevolver.Prototype
{
    public sealed class DiceRevolverAmmoUI : MonoBehaviour
    {
        [SerializeField] private DiceRevolverGun revolver;
        [SerializeField] private Color loadedColor = new Color(0.95f, 0.86f, 0.3f, 1f);
        [SerializeField] private Color spentColor = new Color(0.18f, 0.18f, 0.18f, 0.72f);
        [SerializeField] private Color loadedTextColor = Color.black;
        [SerializeField] private Color spentTextColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.12f;

        private readonly Dictionary<int, Image> faceImages = new();
        private readonly Dictionary<int, Text> faceLabels = new();
        private readonly Dictionary<int, Coroutine> flashCoroutines = new();

        private void Awake()
        {
            CacheFaceViews();
            SetAllLoaded();
        }

        private void OnEnable()
        {
            if (revolver == null)
            {
                return;
            }

            revolver.FireStarted += HandleFireStarted;
            revolver.ReloadCompleted += HandleReloadCompleted;
        }

        private void OnDisable()
        {
            if (revolver == null)
            {
                return;
            }

            revolver.FireStarted -= HandleFireStarted;
            revolver.ReloadCompleted -= HandleReloadCompleted;
        }

        private void CacheFaceViews()
        {
            faceImages.Clear();
            faceLabels.Clear();

            foreach (Transform child in transform)
            {
                DiceRevolverAmmoFace face = child.GetComponent<DiceRevolverAmmoFace>();
                if (face == null)
                {
                    continue;
                }

                Image image = child.GetComponent<Image>();
                Text label = child.GetComponentInChildren<Text>();
                if (image == null || label == null)
                {
                    continue;
                }

                faceImages[face.FaceValue] = image;
                faceLabels[face.FaceValue] = label;
                label.text = face.FaceValue.ToString();
            }
        }

        private void HandleFireStarted(DiceRevolverShotContext shot)
        {
            if (!faceImages.ContainsKey(shot.Face))
            {
                return;
            }

            if (flashCoroutines.TryGetValue(shot.Face, out Coroutine runningFlash))
            {
                StopCoroutine(runningFlash);
            }

            flashCoroutines[shot.Face] = StartCoroutine(FlashThenSpend(shot.Face));
        }

        private IEnumerator FlashThenSpend(int face)
        {
            Image image = faceImages[face];
            image.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            image.color = spentColor;
            if (faceLabels.TryGetValue(face, out Text label))
            {
                label.color = spentTextColor;
            }

            flashCoroutines.Remove(face);
        }

        private void HandleReloadCompleted()
        {
            SetAllLoaded();
        }

        private void SetAllLoaded()
        {
            foreach (Image image in faceImages.Values)
            {
                image.color = loadedColor;
            }

            foreach (Text label in faceLabels.Values)
            {
                label.color = loadedTextColor;
            }
        }
    }
}
