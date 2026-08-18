using System.Linq;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TargetDummyPrototypeBuilder
{
    private const string ScenePath = "Assets/Scenes/TopDownShooterPrototype.unity";
    private const string PrefabPath = "Assets/Prefab/TargetDummy.prefab";
    private const string MaterialsFolder = "Assets/Materials";

    [MenuItem("Dice Revolver/Create Target Dummy")]
    public static void BuildTargetDummy()
    {
        EnsureFolder(MaterialsFolder);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            prefab = CreateTargetDummyPrefab();
        }

        AddTargetToPrototypeScene(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject CreateTargetDummyPrefab()
    {
        Material straw = GetOrCreateMaterial(
            $"{MaterialsFolder}/TargetDummyStraw.mat",
            new Color(0.84f, 0.66f, 0.24f));
        Material wood = GetOrCreateMaterial(
            $"{MaterialsFolder}/TargetDummyWood.mat",
            new Color(0.24f, 0.15f, 0.10f));
        Material accent = GetOrCreateMaterial(
            $"{MaterialsFolder}/TargetDummyAccent.mat",
            new Color(0.78f, 0.16f, 0.13f));
        Material center = GetOrCreateMaterial(
            $"{MaterialsFolder}/TargetDummyCenter.mat",
            new Color(0.94f, 0.86f, 0.62f));

        GameObject root = new GameObject("TargetDummy");
        TargetDummy target = root.AddComponent<TargetDummy>();
        WorldDamageNumberSpawner spawner = root.AddComponent<WorldDamageNumberSpawner>();

        BoxCollider hitbox = root.AddComponent<BoxCollider>();
        hitbox.isTrigger = true;
        hitbox.center = new Vector3(0f, 0.8f, 0f);
        hitbox.size = new Vector3(1.8f, 1.8f, 1.9f);

        Rigidbody rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        GameObject visualRoot = CreateChild(root.transform, "VisualRoot");
        CreatePrimitive(visualRoot.transform, "Crossbar", PrimitiveType.Cube,
            new Vector3(0f, 0.12f, 0.05f), new Vector3(1.85f, 0.16f, 0.22f), wood);
        CreatePrimitive(visualRoot.transform, "Body", PrimitiveType.Cube,
            new Vector3(0f, 0.18f, -0.48f), new Vector3(0.78f, 0.22f, 1.08f), straw);
        CreatePrimitive(visualRoot.transform, "LeftStraw", PrimitiveType.Cube,
            new Vector3(-0.82f, 0.22f, 0.05f), new Vector3(0.32f, 0.12f, 0.42f), straw,
            Quaternion.Euler(0f, -18f, 0f));
        CreatePrimitive(visualRoot.transform, "RightStraw", PrimitiveType.Cube,
            new Vector3(0.82f, 0.22f, 0.05f), new Vector3(0.32f, 0.12f, 0.42f), straw,
            Quaternion.Euler(0f, 18f, 0f));
        CreatePrimitive(visualRoot.transform, "Head", PrimitiveType.Cylinder,
            new Vector3(0f, 0.22f, 0.54f), new Vector3(0.58f, 0.10f, 0.58f), straw);
        CreatePrimitive(visualRoot.transform, "OuterTarget", PrimitiveType.Cylinder,
            new Vector3(0f, 0.34f, 0.54f), new Vector3(0.42f, 0.025f, 0.42f), accent);
        CreatePrimitive(visualRoot.transform, "InnerTarget", PrimitiveType.Cylinder,
            new Vector3(0f, 0.38f, 0.54f), new Vector3(0.25f, 0.025f, 0.25f), center);
        CreatePrimitive(visualRoot.transform, "Bullseye", PrimitiveType.Cylinder,
            new Vector3(0f, 0.42f, 0.54f), new Vector3(0.10f, 0.025f, 0.10f), accent);
        CreatePrimitive(visualRoot.transform, "Base", PrimitiveType.Cylinder,
            new Vector3(0f, 0.08f, -1.02f), new Vector3(0.68f, 0.08f, 0.42f), wood);

        GameObject numberContainer = CreateChild(root.transform, "DamageNumbers");
        WorldDamageNumber numberTemplate = CreateDamageNumberTemplate(numberContainer.transform);
        spawner.Configure(target, numberTemplate, numberContainer.transform);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static WorldDamageNumber CreateDamageNumberTemplate(Transform parent)
    {
        GameObject owner = new GameObject(
            "DamageNumberTemplate",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(CanvasGroup),
            typeof(WorldDamageNumber));
        owner.transform.SetParent(parent, false);

        RectTransform rect = owner.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(140f, 64f);
        rect.localScale = Vector3.one * 0.01f;

        Canvas canvas = owner.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = owner.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        GameObject labelOwner = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        labelOwner.transform.SetParent(owner.transform, false);
        RectTransform labelRect = labelOwner.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelOwner.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 48;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.84f, 0.22f);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = labelOwner.GetComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.07f, 0.04f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        WorldDamageNumber view = owner.GetComponent<WorldDamageNumber>();
        view.Configure(label, owner.GetComponent<CanvasGroup>());
        owner.SetActive(false);
        return view;
    }

    private static void AddTargetToPrototypeScene(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        bool alreadyPresent = scene.GetRootGameObjects()
            .Any(root => root.GetComponent<TargetDummy>() != null);
        if (alreadyPresent)
        {
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "TargetDummy";
        instance.transform.position = new Vector3(3f, 0f, 0f);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static GameObject CreatePrimitive(
        Transform parent,
        string name,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        Quaternion? localRotation = null)
    {
        GameObject visual = GameObject.CreatePrimitive(primitiveType);
        visual.name = name;
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = localRotation ?? Quaternion.identity;
        visual.transform.localScale = localScale;
        visual.GetComponent<Renderer>().sharedMaterial = material;

        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return visual;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else
        {
            material.color = color;
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string child = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
