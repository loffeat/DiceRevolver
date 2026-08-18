using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class ProjectileVisualWrapper : MonoBehaviour
    {
        [SerializeField, InspectorName("视觉 Prefab")] private GameObject visualPrefab;
        [SerializeField, InspectorName("视觉局部旋转")] private Vector3 localEulerAngles = new Vector3(0f, 90f, 0f);
        [SerializeField, Min(0.0001f), InspectorName("视觉缩放")] private float visualScale = 0.2f;

        public GameObject VisualPrefab => visualPrefab;

        private void Awake()
        {
            if (visualPrefab == null)
            {
                return;
            }

            GameObject instance = Instantiate(visualPrefab, transform);
            instance.name = visualPrefab.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(localEulerAngles);
            instance.transform.localScale = Vector3.one * Mathf.Max(0.0001f, visualScale);
        }
    }
}
