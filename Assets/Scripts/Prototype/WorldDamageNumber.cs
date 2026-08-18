using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class WorldDamageNumber : MonoBehaviour
    {
        [SerializeField, InspectorName("伤害文字")] private Text label;
        [SerializeField, InspectorName("透明度控制")] private CanvasGroup canvasGroup;
        [SerializeField, Min(0.01f), InspectorName("持续时间（秒）")] private float duration = 0.8f;
        [SerializeField, Min(0f), InspectorName("上浮距离")] private float riseDistance = 0.65f;

        private Camera targetCamera;
        private Vector3 startPosition;
        private Vector3 riseDirection = Vector3.forward;
        private float elapsed;

        public string DisplayText => label != null ? label.text : string.Empty;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            elapsed = 0f;
            startPosition = transform.position;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            UpdateCameraFrame();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            transform.position = startPosition + riseDirection * (riseDistance * normalizedTime);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - normalizedTime;
            }

            FaceCamera();
            if (normalizedTime >= 1f)
            {
                Destroy(gameObject);
            }
        }

        public void Configure(Text damageLabel, CanvasGroup alphaGroup)
        {
            label = damageLabel;
            canvasGroup = alphaGroup;
        }

        public void SetDamage(float amount)
        {
            ResolveReferences();
            if (label != null)
            {
                label.text = amount.ToString("0.#", CultureInfo.InvariantCulture);
            }

            elapsed = 0f;
            startPosition = transform.position;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            UpdateCameraFrame();
        }

        private void ResolveReferences()
        {
            if (label == null)
            {
                label = GetComponentInChildren<Text>(true);
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void UpdateCameraFrame()
        {
            targetCamera = Camera.main;
            riseDirection = targetCamera != null ? targetCamera.transform.up : Vector3.forward;
            FaceCamera();
        }

        private void FaceCamera()
        {
            if (targetCamera != null)
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }
    }
}
