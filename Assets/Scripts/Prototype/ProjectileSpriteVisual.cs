using UnityEngine;

namespace DiceRevolver.Prototype
{
    /// <summary>运行时为弹丸挂上专属 2D 立绘（SpriteRenderer）。
    /// 贴图来源：优先 ProjectileDefinition.ProjectileSprite（正式美术挂载点），
    /// 否则按定义名称生成程序化形状（ProjectileSpriteFactory）。</summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileSpriteVisual : MonoBehaviour
    {
        [SerializeField, InspectorName("覆盖粒子视觉")] private bool hideParticleVisual = true;
        [SerializeField, InspectorName("立绘缩放")] private float spriteScale = 0.6f;
        [SerializeField, InspectorName("立绘朝向（俯视）")] private Vector3 spriteLocalEulerAngles = new Vector3(90f, 0f, 0f);

        private ProjectileDefinition definition;

        public void SetDefinition(ProjectileDefinition projectileDefinition)
        {
            definition = projectileDefinition;
        }

        private void Start()
        {
            Sprite sprite = definition != null && definition.ProjectileSprite != null
                ? definition.ProjectileSprite
                : ProjectileSpriteFactory.GetShape(definition != null ? definition.name : null);
            if (sprite == null)
            {
                return;
            }

            GameObject child = new GameObject("ProjectileSprite");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.Euler(spriteLocalEulerAngles);
            child.transform.localScale = Vector3.one * Mathf.Max(0.0001f, spriteScale);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "projectile";

            if (hideParticleVisual)
            {
                ProjectileVisualWrapper wrapper = GetComponent<ProjectileVisualWrapper>();
                if (wrapper != null && wrapper.VisualInstance != null)
                {
                    wrapper.VisualInstance.SetActive(false);
                }
            }
        }
    }
}
