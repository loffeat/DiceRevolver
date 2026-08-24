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

        private readonly Dictionary<int, Image> faceImages = new();
        private readonly Dictionary<int, Text> faceLabels = new();

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

            revolver.ChamberChanged += HandleChamberChanged;
            HandleChamberChanged(revolver.RemainingFaces);
        }

        private void OnDisable()
        {
            if (revolver == null)
            {
                return;
            }

            revolver.ChamberChanged -= HandleChamberChanged;
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

        private void HandleChamberChanged(IReadOnlyList<int> remainingFaces)
        {
            HashSet<int> loadedFaces = remainingFaces != null
                ? new HashSet<int>(remainingFaces)
                : new HashSet<int>();
            foreach (KeyValuePair<int, Image> pair in faceImages)
            {
                bool isLoaded = loadedFaces.Contains(pair.Key);
                pair.Value.color = isLoaded ? loadedColor : spentColor;
                if (faceLabels.TryGetValue(pair.Key, out Text label))
                {
                    label.color = isLoaded ? loadedTextColor : spentTextColor;
                }
            }
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
