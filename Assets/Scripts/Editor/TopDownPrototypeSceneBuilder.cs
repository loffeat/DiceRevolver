using DiceRevolver.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TopDownPrototypeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/TopDownShooterPrototype.unity";
    private const string PlayerPrefabPath = "Assets/Prefab/Player.prefab";
    private const string ProjectilePrefabPath = "Assets/PrototypeProjectile.prefab";
    private const string PlayerLeftControllerPath = "Assets/Art/PlayerAnimation/Player_Left.controller";
    private const string PlayerLeftIdleFolder = "Assets/Art/Player/Player_left/Idle";
    private const string PlayerLeftRootFolder = "Assets/Art/Player/Player_left";

    [MenuItem("Dice Revolver/Build Top-Down Shooter Prototype Scene")]
    public static void BuildPrototypeScene()
    {
        EnsureFolders();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "TopDownShooterPrototype";

        Material groundMaterial = CreateMaterial("Prototype_Ground", new Color(0.18f, 0.2f, 0.18f));
        Material gunMaterial = CreateMaterial("Prototype_Gun", new Color(0.08f, 0.08f, 0.09f));
        Material bulletMaterial = CreateMaterial("Prototype_Bullet", new Color(1f, 0.82f, 0.22f));
        Material obstacleMaterial = CreateMaterial("Prototype_Obstacle", new Color(0.45f, 0.4f, 0.34f));

        GameObject projectilePrefab = CreateProjectilePrefab(bulletMaterial);
        GameObject playerPrefab = CreatePlayerPrefab(projectilePrefab.GetComponent<Projectile>(), gunMaterial);
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(0f, 1f, 0f);

        TopDownPlayerController playerController = player.GetComponent<TopDownPlayerController>();
        DiceRevolverGun revolver = player.GetComponentInChildren<DiceRevolverGun>();

        CreateGround(groundMaterial);
        CreateObstacles(obstacleMaterial);
        CreateCamera(playerController);
        CreateLight();
        CreateAmmoCanvas(revolver);
        CreateReadmeLabel();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Scripts");
        EnsureFolder("Assets/Scripts", "Prototype");
        EnsureFolder("Assets/Scripts", "Editor");
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Prefab");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = $"Assets/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateProjectilePrefab(Material material)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        if (prefab != null)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(ProjectilePrefabPath);
            NormalizeProjectileObject(contents, material);
            PrefabUtility.SaveAsPrefabAsset(contents, ProjectilePrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        }

        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "PrototypeProjectile";
        bullet.transform.localScale = Vector3.one * 0.24f;
        bullet.GetComponent<Renderer>().sharedMaterial = material;

        SphereCollider collider = bullet.GetComponent<SphereCollider>();
        collider.isTrigger = true;

        Rigidbody body = bullet.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;

        bullet.AddComponent<Projectile>();

        prefab = PrefabUtility.SaveAsPrefabAsset(bullet, ProjectilePrefabPath);
        Object.DestroyImmediate(bullet);
        return prefab;
    }

    private static void NormalizeProjectileObject(GameObject prefab, Material material)
    {
        Renderer renderer = prefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        SphereCollider collider = prefab.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = prefab.AddComponent<SphereCollider>();
        }

        collider.isTrigger = true;

        Rigidbody body = prefab.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = prefab.AddComponent<Rigidbody>();
        }

        body.useGravity = false;
        body.isKinematic = true;

        if (prefab.GetComponent<Projectile>() == null)
        {
            prefab.AddComponent<Projectile>();
        }

        EditorUtility.SetDirty(prefab);
    }

    private static GameObject CreatePlayerPrefab(Projectile projectilePrefab, Material gunMaterial)
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = Vector3.zero;

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.center = Vector3.zero;
        controller.height = 1.8f;
        controller.radius = 0.42f;

        TopDownPlayerController playerController = player.AddComponent<TopDownPlayerController>();
        SerializedObject serializedPlayer = new SerializedObject(playerController);
        serializedPlayer.FindProperty("rotateBodyTowardAim").boolValue = false;
        serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

        GameObject visualRoot = CreateChild(player.transform, "VisualRoot", Vector3.zero, Quaternion.identity);

        GameObject body = CreateChild(visualRoot.transform, "Body", new Vector3(0f, 0f, 0f), Quaternion.Euler(90f, 0f, 0f));
        body.transform.localScale = Vector3.one;
        SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = LoadFirstSprite(PlayerLeftIdleFolder);

        Animator animator = body.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerLeftControllerPath);

        PlayerMovementAnimatorBridge animationBridge = body.AddComponent<PlayerMovementAnimatorBridge>();
        SerializedObject serializedBridge = new SerializedObject(animationBridge);
        serializedBridge.FindProperty("player").objectReferenceValue = playerController;
        serializedBridge.FindProperty("animator").objectReferenceValue = animator;
        serializedBridge.ApplyModifiedPropertiesWithoutUndo();

        GameObject handRig = CreateChild(visualRoot.transform, "HandRig", Vector3.zero, Quaternion.identity);
        GameObject aimRoot = CreateChild(handRig.transform, "AimRoot", Vector3.zero, Quaternion.identity);

        GameObject armVisual = CreateChild(
            aimRoot.transform,
            "ArmVisual",
            Vector3.zero,
            new Quaternion(0.4055798f, -0.579228f, 0.579228f, 0.4055798f));
        SpriteRenderer armRenderer = armVisual.AddComponent<SpriteRenderer>();
        armRenderer.sprite = LoadArmSprite();

        GameObject gunBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gunBody.name = "GunBody";
        gunBody.transform.SetParent(aimRoot.transform, false);
        gunBody.transform.localPosition = new Vector3(-0.36f, 0.02f, 0.97f);
        gunBody.transform.localRotation = Quaternion.identity;
        gunBody.transform.localScale = new Vector3(0.12f, 0.08f, 0.48f);
        gunBody.GetComponent<Renderer>().sharedMaterial = gunMaterial;
        Object.DestroyImmediate(gunBody.GetComponent<BoxCollider>());

        GameObject muzzle = CreateChild(aimRoot.transform, "Muzzle", new Vector3(-0.43f, 0.02f, 1.36f), Quaternion.identity);
        CreateChild(player.transform, "CameraTarget", new Vector3(0f, 0f, 0f), Quaternion.identity);

        TopDownAimHandRig aimRig = handRig.AddComponent<TopDownAimHandRig>();
        SerializedObject serializedAimRig = new SerializedObject(aimRig);
        serializedAimRig.FindProperty("player").objectReferenceValue = playerController;
        serializedAimRig.FindProperty("aimRoot").objectReferenceValue = aimRoot.transform;
        serializedAimRig.FindProperty("armVisual").objectReferenceValue = armVisual.transform;
        serializedAimRig.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
        serializedAimRig.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
        serializedAimRig.FindProperty("armRenderer").objectReferenceValue = armRenderer;
        serializedAimRig.FindProperty("orbitRadius").floatValue = 0f;
        serializedAimRig.FindProperty("visualHeight").floatValue = -0.58f;
        serializedAimRig.FindProperty("armScaleMultiplier").floatValue = 1f;
        serializedAimRig.FindProperty("bodyFacesRightByDefault").boolValue = false;
        serializedAimRig.FindProperty("facingDeadZone").floatValue = 0.03f;
        serializedAimRig.ApplyModifiedPropertiesWithoutUndo();

        GameObject weaponRoot = CreateChild(player.transform, "WeaponRoot", Vector3.zero, Quaternion.identity);
        DiceRevolverGun gun = weaponRoot.AddComponent<DiceRevolverGun>();
        SerializedObject serializedGun = new SerializedObject(gun);
        serializedGun.FindProperty("player").objectReferenceValue = playerController;
        serializedGun.FindProperty("visualRoot").objectReferenceValue = armVisual.transform;
        serializedGun.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
        serializedGun.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        serializedGun.FindProperty("ownerCollider").objectReferenceValue = controller;
        serializedGun.FindProperty("driveWeaponPose").boolValue = false;
        serializedGun.FindProperty("shotsPerSecond").floatValue = 5f;
        serializedGun.FindProperty("reloadDuration").floatValue = 1.8f;
        serializedGun.FindProperty("eventBudgetPerActivation").intValue =
            DiceFaceActivation.DefaultEventBudget;
        serializedGun.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        Object.DestroyImmediate(player);
        return prefab;
    }

    private static GameObject CreateChild(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = localRotation;
        return child;
    }

    private static Sprite LoadFirstSprite(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private static Sprite LoadArmSprite()
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { PlayerLeftRootFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string normalizedPath = path.Replace("\\", "/");
            if (normalizedPath.Contains("/Idle/") || normalizedPath.Contains("/Walk/"))
            {
                continue;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private static void CreateAmmoCanvas(DiceRevolverGun revolver)
    {
        GameObject canvasObject = new GameObject("DiceRevolverHUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("AmmoDiceNet");
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        const float cellSize = 44f;
        const float gap = 6f;
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-28f, -28f);
        panelRect.sizeDelta = new Vector2(cellSize * 4f + gap * 3f, cellSize * 3f + gap * 2f);

        DiceRevolverAmmoUI ammoUi = panel.AddComponent<DiceRevolverAmmoUI>();
        SerializedObject serializedUi = new SerializedObject(ammoUi);
        serializedUi.FindProperty("revolver").objectReferenceValue = revolver;
        serializedUi.ApplyModifiedPropertiesWithoutUndo();

        Font font = GetBuiltinFont();
        CreateAmmoFace(panelRect, 1, 1, 0, cellSize, gap, font);
        CreateAmmoFace(panelRect, 2, 0, 1, cellSize, gap, font);
        CreateAmmoFace(panelRect, 3, 1, 1, cellSize, gap, font);
        CreateAmmoFace(panelRect, 4, 2, 1, cellSize, gap, font);
        CreateAmmoFace(panelRect, 5, 3, 1, cellSize, gap, font);
        CreateAmmoFace(panelRect, DiceRevolverRules.FaceCount, 1, 2, cellSize, gap, font);
    }

    private static void CreateAmmoFace(RectTransform parent, int face, int column, int row, float cellSize, float gap, Font font)
    {
        GameObject faceObject = new GameObject($"AmmoFace_{face}");
        faceObject.transform.SetParent(parent, false);

        RectTransform faceRect = faceObject.AddComponent<RectTransform>();
        faceRect.anchorMin = new Vector2(0f, 1f);
        faceRect.anchorMax = new Vector2(0f, 1f);
        faceRect.pivot = new Vector2(0.5f, 0.5f);
        faceRect.sizeDelta = new Vector2(cellSize, cellSize);
        faceRect.anchoredPosition = new Vector2(
            column * (cellSize + gap) + cellSize * 0.5f,
            -(row * (cellSize + gap) + cellSize * 0.5f));

        Image image = faceObject.AddComponent<Image>();
        image.color = new Color(0.95f, 0.86f, 0.3f, 1f);

        DiceRevolverAmmoFace ammoFace = faceObject.AddComponent<DiceRevolverAmmoFace>();
        SerializedObject serializedFace = new SerializedObject(ammoFace);
        serializedFace.FindProperty("faceValue").intValue = face;
        serializedFace.ApplyModifiedPropertiesWithoutUndo();

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(faceObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.AddComponent<Text>();
        label.text = face.ToString();
        label.font = font;
        label.fontSize = 24;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
    }

    private static Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private static void CreateGround(Material material)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        ground.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateObstacles(Material material)
    {
        Vector3[] positions =
        {
            new Vector3(-4f, 0.5f, 3f),
            new Vector3(4f, 0.5f, 2f),
            new Vector3(1.5f, 0.5f, -4f),
            new Vector3(-3f, 0.5f, -3f)
        };

        Vector3[] scales =
        {
            new Vector3(1.4f, 1f, 2.2f),
            new Vector3(2.4f, 1f, 1.2f),
            new Vector3(1.2f, 1f, 2.6f),
            new Vector3(2.2f, 1f, 1.1f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"PrototypeObstacle_{i + 1}";
            obstacle.transform.position = positions[i];
            obstacle.transform.localScale = scales[i];
            obstacle.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    private static void CreateCamera(TopDownPlayerController target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 8f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 80f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;

        PrototypeCameraFollow follow = cameraObject.AddComponent<PrototypeCameraFollow>();
        SerializedObject serializedFollow = new SerializedObject(follow);
        serializedFollow.FindProperty("target").objectReferenceValue = target;
        serializedFollow.ApplyModifiedPropertiesWithoutUndo();

        cameraObject.transform.position = new Vector3(0f, 14f, -1.5f);
        cameraObject.transform.rotation = Quaternion.Euler(85f, 0f, 0f);
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject volumeObject = new GameObject("Global Volume");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
    }

    private static void CreateReadmeLabel()
    {
        GameObject label = new GameObject("PrototypeControlsLabel");
        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = "WASD move   Mouse aim   Left click shoot";
        textMesh.fontSize = 32;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
        label.transform.position = new Vector3(0f, 0.05f, 6.25f);
        label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        label.transform.localScale = Vector3.one * 0.12f;
    }
}
