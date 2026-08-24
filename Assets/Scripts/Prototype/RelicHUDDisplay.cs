using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRevolver.Prototype
{
    public sealed class RelicHUDDisplay : MonoBehaviour
    {
        [SerializeField, InspectorName("左轮")] private DiceRevolverGun revolver;
        [SerializeField, InspectorName("遗物立绘")] private Image relicImage;
        [SerializeField, InspectorName("空状态透明")] private float emptyAlpha = 0f;

        private void Reset()
        {
            if (revolver == null)
            {
                revolver = Object.FindFirstObjectByType<DiceRevolverGun>(FindObjectsInactive.Include);
            }

            if (relicImage == null)
            {
                relicImage = GetComponentInChildren<Image>(true);
            }
        }

        private void OnEnable()
        {
            if (revolver == null)
            {
                return;
            }

            revolver.RelicsChanged += HandleRelicsChanged;
            SetFromRelics(revolver.Relics);
        }

        private void OnDisable()
        {
            if (revolver == null)
            {
                return;
            }

            revolver.RelicsChanged -= HandleRelicsChanged;
        }

        private void HandleRelicsChanged(IReadOnlyList<RelicDefinition> relics)
        {
            SetFromRelics(relics);
        }

        private void SetFromRelics(IReadOnlyList<RelicDefinition> relics)
        {
            if (relicImage == null)
            {
                return;
            }

            RelicDefinition relic = relics != null && relics.Count > 0
                ? relics[relics.Count - 1]
                : null;
            Sprite icon = relic != null ? relic.Icon : null;

            relicImage.sprite = icon;
            Color current = relicImage.color;
            current.a = icon == null ? emptyAlpha : 1f;
            relicImage.color = current;

            if (icon != null)
            {
                relicImage.SetNativeSize();
            }
        }
    }
}
