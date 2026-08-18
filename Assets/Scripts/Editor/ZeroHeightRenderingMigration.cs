using DiceRevolver.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ZeroHeightRenderingMigration
{
    private const string ScenePath = "Assets/Scenes/TopDownShooterPrototype.unity";
    private const string GroundSpriteSheetPath = "Assets/Art/Map/map1.png";
    private const string GroundSpriteName = "map1_58";
    private const string GroundSpriteAssetPath = "Assets/Art/Map/GroundBackgroundTile.asset";

    [MenuItem("Dice Revolver/Migrate Zero Height Rendering")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject ground = FindRoot(scene, "Ground");
        if (ground == null)
        {
            throw new MissingReferenceException($"Ground was not found in {ScenePath}.");
        }

        ConfigureGround(ground);
        SnapRootToGameplayPlane(FindRootComponent<TopDownPlayerController>(scene));
        SnapRootToGameplayPlane(FindRootComponent<TargetDummy>(scene));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureGround(GameObject ground)
    {
        DestroyIfPresent<MeshCollider>(ground);
        DestroyIfPresent<MeshRenderer>(ground);
        DestroyIfPresent<MeshFilter>(ground);

        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = ground.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = LoadGroundSprite();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = new Vector2(40f, 40f);
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = 0;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ground.transform.position = Vector3.zero;
        ground.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ground.transform.localScale = Vector3.one;
    }

    private static Sprite LoadGroundSprite()
    {
        Sprite generatedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundSpriteAssetPath);
        if (generatedSprite != null)
        {
            return generatedSprite;
        }

        Object[] spriteSheetAssets = AssetDatabase.LoadAllAssetsAtPath(GroundSpriteSheetPath);
        for (int i = 0; i < spriteSheetAssets.Length; i++)
        {
            if (spriteSheetAssets[i] is Sprite source && source.name == GroundSpriteName)
            {
                Vector2 normalizedPivot = new Vector2(
                    source.pivot.x / source.rect.width,
                    source.pivot.y / source.rect.height);
                Sprite sprite = Sprite.Create(
                    source.texture,
                    source.rect,
                    normalizedPivot,
                    source.pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect,
                    source.border,
                    false);
                sprite.name = "GroundBackgroundTile";
                AssetDatabase.CreateAsset(sprite, GroundSpriteAssetPath);
                return sprite;
            }
        }

        throw new MissingReferenceException(
            $"Ground sprite '{GroundSpriteName}' was not found in {GroundSpriteSheetPath}.");
    }

    private static void SnapRootToGameplayPlane(Component component)
    {
        if (component == null)
        {
            return;
        }

        Transform root = component.transform;
        Vector3 position = root.position;
        position.y = 0f;
        root.position = position;
    }

    private static void DestroyIfPresent<T>(GameObject owner) where T : Component
    {
        T component = owner.GetComponent<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component);
        }
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
            {
                return roots[i];
            }
        }

        return null;
    }

    private static T FindRootComponent<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponent<T>();
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }
}
