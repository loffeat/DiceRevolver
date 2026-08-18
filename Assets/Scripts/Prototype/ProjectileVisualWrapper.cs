using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class ProjectileVisualWrapper : MonoBehaviour
    {
        public const string CompatibleParticleShaderName = "DiceRevolver/Projectile Particle Unlit";

        private const string MissingShaderName = "Hidden/InternalErrorShader";
        private static readonly Dictionary<Material, Material> CompatibleMaterials =
            new Dictionary<Material, Material>();

        [SerializeField, InspectorName("视觉 Prefab")] private GameObject visualPrefab;
        [SerializeField, InspectorName("视觉局部旋转")] private Vector3 localEulerAngles = new Vector3(0f, 90f, 0f);
        [SerializeField, Min(0.0001f), InspectorName("视觉缩放")] private float visualScale = 0.2f;

        [Header("渲染层级")]
        [SerializeField, InspectorName("弹幕 Sorting Layer")] private string sortingLayerName = "projectile";
        [SerializeField, InspectorName("弹幕 Sorting Order")] private int sortingOrder;

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
            ReplaceMissingParticleShaders(instance);
            ApplySorting(instance);
        }

        private void ApplySorting(GameObject visualInstance)
        {
            ParticleSystemRenderer[] renderers =
                visualInstance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                renderers[rendererIndex].sortingLayerName = sortingLayerName;
                renderers[rendererIndex].sortingOrder = sortingOrder;
            }
        }

        private static void ReplaceMissingParticleShaders(GameObject visualInstance)
        {
            Shader compatibleShader = Shader.Find(CompatibleParticleShaderName);
            if (compatibleShader == null)
            {
                Debug.LogError($"Projectile visual shader '{CompatibleParticleShaderName}' was not found.");
                return;
            }

            ParticleSystemRenderer[] renderers =
                visualInstance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] materials = renderers[rendererIndex].sharedMaterials;
                bool changed = false;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material source = materials[materialIndex];
                    if (source != null && source.shader != null && source.shader.name != MissingShaderName)
                    {
                        continue;
                    }

                    materials[materialIndex] = GetCompatibleMaterial(source, compatibleShader);
                    changed = true;
                }

                if (changed)
                {
                    renderers[rendererIndex].sharedMaterials = materials;
                }
            }
        }

        private static Material GetCompatibleMaterial(Material source, Shader compatibleShader)
        {
            if (source != null && CompatibleMaterials.TryGetValue(source, out Material cached))
            {
                return cached;
            }

            Material compatible = new Material(compatibleShader)
            {
                name = source != null ? $"{source.name} (Projectile Compatible)" : "Projectile Compatible",
                hideFlags = HideFlags.HideAndDontSave,
            };

            if (source != null)
            {
                if (source.HasProperty("_MainTex"))
                {
                    compatible.SetTexture("_MainTex", source.GetTexture("_MainTex"));
                    compatible.SetTextureScale("_MainTex", source.GetTextureScale("_MainTex"));
                    compatible.SetTextureOffset("_MainTex", source.GetTextureOffset("_MainTex"));
                }

                if (source.HasProperty("_MainColor"))
                {
                    compatible.SetColor("_Color", source.GetColor("_MainColor"));
                }
                else if (source.HasProperty("_Color"))
                {
                    compatible.SetColor("_Color", source.GetColor("_Color"));
                }

                CompatibleMaterials[source] = compatible;
            }

            return compatible;
        }
    }
}
