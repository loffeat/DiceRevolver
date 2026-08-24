using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    /// <summary>为弹丸生成程序化形状贴图（占位立绘）。后续替换为正式美术时，
    /// 直接在 ProjectileDefinition.ProjectileSprite 指定 Sprite 即可覆盖。</summary>
    public static class ProjectileSpriteFactory
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>按弹丸定义名称解析默认形状：基础=灰色长方形、雷电球=蓝色圆形、收尾者=黑色细长方形。</summary>
        public static Sprite GetShape(string definitionName)
        {
            string key = string.IsNullOrEmpty(definitionName) ? "Default" : definitionName;
            if (Cache.TryGetValue(key, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = CreateShapeSprite(key);
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite CreateShapeSprite(string key)
        {
            if (key.IndexOf("LightningOrb", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CreateProceduralSprite(32, 32, true, new Color(0.35f, 0.65f, 1f));
            }

            if (key.IndexOf("ArmorPiercing", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Finisher", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("收尾者", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CreateProceduralSprite(72, 10, false, new Color(0.08f, 0.08f, 0.08f));
            }

            // 默认：基础左轮子弹 = 灰色长方形
            return CreateProceduralSprite(48, 16, false, new Color(0.55f, 0.55f, 0.55f));
        }

        private static Sprite CreateProceduralSprite(int width, int height, bool circle, Color color)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (circle)
                    {
                        float dx = x - (width - 1) * 0.5f;
                        float dy = y - (height - 1) * 0.5f;
                        float radius = Mathf.Min(width, height) * 0.5f;
                        texture.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? color : Color.clear);
                    }
                    else
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
