using System.Linq;
using PvZ2.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class PvZ2FoundationBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/PvZ2 Foundation/Build Prototype")]
    public static void BuildPrototype()
    {
        EnsureSortingLayer("Background", 180000001);
        EnsureSortingLayer("Board", 180000002);
        EnsureSortingLayer("Plants", 180000003);
        EnsureSortingLayer("Projectiles", 180000004);


        Sprite squareSprite = GetOrCreateSquareSprite();
        Sprite sunSprite = GetOrCreateSunSprite();
        SunCollectible sunPrefab = CreateSunPrefab(sunSprite, squareSprite);
        PeaProjectile peaPrefab = CreatePeaPrefab(sunSprite);


        PlantBase peashooterPrefab = CreatePlantPrefab(
            "Peashooter", "Assets/Prefabs/Peashooter.prefab", squareSprite,
            new Color(0.12f, 0.62f, 0.22f), new Vector3(0.38f, 0.68f, 1f), "pea", sunPrefab, peaPrefab);
        PlantBase sunflowerPrefab = CreatePlantPrefab(
            "Sunflower", "Assets/Prefabs/Sunflower.prefab", squareSprite,
            new Color(1f, 0.73f, 0.08f), new Vector3(0.74f, 0.74f, 1f), "sun", sunPrefab, null);
        PlantBase wallNutPrefab = CreatePlantPrefab(
            "WallNut", "Assets/Prefabs/WallNut.prefab", squareSprite,
            new Color(0.48f, 0.27f, 0.09f), new Vector3(0.68f, 0.86f, 1f), "nut", sunPrefab, null);

        PlantData peashooterData = CreatePlantData(
            "Assets/Data/Peashooter.asset", "peashooter", "Peashooter",
            peashooterPrefab, 100, 5f, 100, 20, 1.5f);
        PlantData sunflowerData = CreatePlantData(
            "Assets/Data/Sunflower.asset", "sunflower", "Sunflower",
            sunflowerPrefab, 50, 6f, 80, 0, 5f);
        PlantData wallNutData = CreatePlantData(
            "Assets/Data/WallNut.asset", "wall-nut", "Wall-Nut",
            wallNutPrefab, 50, 12f, 500, 0, 0f);

        ZombieController normalZombiePrefab = CreateZombiePrefab(
            "NormalZombie", "Assets/Prefabs/NormalZombie.prefab", squareSprite, sunSprite, false);
        ZombieController coneheadZombiePrefab = CreateZombiePrefab(
            "ConeheadZombie", "Assets/Prefabs/ConeheadZombie.prefab", squareSprite, sunSprite, true);
        ZombieData normalZombieData = CreateZombieData(
            "Assets/Data/NormalZombie.asset", "normal-zombie", "Normal Zombie",
            normalZombiePrefab, 180, 0.48f, 0.72f, 20, 1f);
        ZombieData coneheadZombieData = CreateZombieData(
            "Assets/Data/ConeheadZombie.asset", "conehead-zombie", "Conehead Zombie",
            coneheadZombiePrefab, 420, 0.42f, 0.72f, 20, 1f);


        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera();
        CreateGlobalLight();
        CreateBattlefieldVisuals(squareSprite);
        GridManager grid = CreateGrid(squareSprite);

        var plantContainer = new GameObject("Plants");
        var zombieContainer = new GameObject("Zombies");
        var projectileContainer = new GameObject("Projectiles");
        var sunContainer = new GameObject("Sun Collectibles");
        var systems = new GameObject("Gameplay Systems");

        LaneCombatRegistry combatRegistry = systems.AddComponent<LaneCombatRegistry>();
        combatRegistry.Configure(grid.Rows);

        ResourceManager resources = systems.AddComponent<ResourceManager>();
        resources.ConfigureStartingSun(50, true);

        PlantSelectionController selection = systems.AddComponent<PlantSelectionController>();
        PlantPlacementController placement = systems.AddComponent<PlantPlacementController>();
        placement.Configure(grid, selection, resources, plantContainer.transform);

        BoardInteractionRouter router = systems.AddComponent<BoardInteractionRouter>();
        router.Configure(placement);
        systems.AddComponent<BoardPointerInput>().Configure(router, camera);

        BasicZombieSpawner zombieSpawner = systems.AddComponent<BasicZombieSpawner>();
        zombieSpawner.Configure(
            combatRegistry, grid, zombieContainer.transform, 6.15f, -6.5f,
            normalZombieData, coneheadZombieData);SkySunSpawner skySpawner = systems.AddComponent<SkySunSpawner>();
        skySpawner.Configure(grid, resources, sunPrefab, sunContainer.transform, 8f, 2.6f);

        CreatePlacementFeedback(
            squareSprite, grid, selection, placement, camera, systems.transform);
        selection.SelectPlant(sunflowerData);

        CreateInterface(
            resources, selection, placement, peashooterData, sunflowerData, wallNutData,
            squareSprite, sunSprite);
        CreateEventSystem();

        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PvZ2 Phase 2 scene created successfully.");
    }

    private static Camera CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-0.15f, 0.08f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.25f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.16f, 0.11f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        return camera;
    }

    private static void CreateGlobalLight()
    {
        var lightObject = new GameObject("Global Light 2D");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

private static void CreateBattlefieldVisuals(Sprite sprite)
    {
        var environment = new GameObject("Battlefield Visuals");
        CreateShape("Field Background", environment.transform, sprite,
            new Color(0.08f, 0.17f, 0.09f), new Vector3(-0.15f, 0f, 1f),
            new Vector3(12f, 6.7f, 1f), "Background", -10);
        CreateShape("Home Strip", environment.transform, sprite,
            new Color(0.47f, 0.31f, 0.18f), new Vector3(-4.57f, 0f, 0.2f),
            new Vector3(0.72f, 5.6f, 1f), "Board", 0);
        CreateShape("Zombie Entry", environment.transform, sprite,
            new Color(0.18f, 0.23f, 0.20f), new Vector3(4.29f, 0f, 0.2f),
            new Vector3(0.52f, 5.6f, 1f), "Board", 0);
        for (int row = 0; row < 5; row++)
            CreateShape($"Entry Marker {row}", environment.transform, sprite,
                new Color(0.55f, 0.18f, 0.12f), new Vector3(3.9f, -2.24f + row * 1.12f, 0.1f),
                new Vector3(0.1f, 0.74f, 1f), "Board", 2);
    }

private static GridManager CreateGrid(Sprite sprite)
    {
        var gridObject = new GameObject("GridManager");
        GridManager grid = gridObject.AddComponent<GridManager>();
        grid.BuildGrid(5, 9, new Vector2(-3.75f, -2.24f), new Vector2(.9f, 1.12f), sprite);
        foreach (GridCell cell in gridObject.GetComponentsInChildren<GridCell>())
        {
            SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
            renderer.color = (cell.Row + cell.Column) % 2 == 0
                ? new Color(0.27f, 0.58f, 0.23f)
                : new Color(0.24f, 0.54f, 0.21f);
            renderer.sortingLayerName = "Board";
            renderer.sortingOrder = 1;
        }
        return grid;
    }

    private static void CreatePlacementFeedback(
        Sprite sprite,
        GridManager grid,
        PlantSelectionController selection,
        PlantPlacementController placement,
        Camera camera,
        Transform parent)
    {
        var feedbackObject = new GameObject("Placement Feedback");
        feedbackObject.transform.SetParent(parent, false);
        feedbackObject.transform.localScale = new Vector3(
            grid.CellSize.x * 0.94f,
            grid.CellSize.y * 0.94f,
            1f);
        SpriteRenderer renderer = feedbackObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Board";
        renderer.sortingOrder = 5;
        renderer.enabled = false;
        feedbackObject.AddComponent<PlacementFeedbackView>().Configure(
            grid, selection, placement, camera, renderer);
    }

    private static void CreateInterface(
        ResourceManager resources,
        PlantSelectionController selection,
        PlantPlacementController placement,
        PlantData peashooter,
        PlantData sunflower,
        PlantData wallNut,
        Sprite squareSprite,
        Sprite sunSprite)
    {
        var canvasObject = new GameObject(
            "Gameplay UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = 5;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;

        var panelObject = new GameObject("Plant Bar", typeof(RectTransform), typeof(Image));
        panelObject.layer = 5;
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(438f, 112f);
        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.09f, 0.055f, 0.94f);
        panelImage.raycastTarget = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateSunCounter(panelObject.transform, font, sunSprite, resources);

        CreatePlantCard(panelObject.transform, font, selection, placement, resources, sunflower,
            sunSprite, sunSprite, new Color(0.78f, 0.54f, 0.05f), 104f);
        CreatePlantCard(panelObject.transform, font, selection, placement, resources, peashooter,
            squareSprite, sunSprite, new Color(0.12f, 0.50f, 0.19f), 212f);
        CreatePlantCard(panelObject.transform, font, selection, placement, resources, wallNut,
            squareSprite, sunSprite, new Color(0.42f, 0.23f, 0.08f), 320f);
    }

    private static void CreateSunCounter(
        Transform parent,
        Font font,
        Sprite sunSprite,
        ResourceManager resources)
    {
        var counter = new GameObject("Sun Counter", typeof(RectTransform), typeof(Image));
        counter.layer = 5;
        counter.transform.SetParent(parent, false);
        RectTransform counterRect = counter.GetComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(0f, 0.5f);
        counterRect.anchorMax = new Vector2(0f, 0.5f);
        counterRect.pivot = new Vector2(0f, 0.5f);
        counterRect.anchoredPosition = new Vector2(8f, 0f);
        counterRect.sizeDelta = new Vector2(90f, 94f);
        Image counterBackground = counter.GetComponent<Image>();
        counterBackground.color = new Color(0.13f, 0.17f, 0.09f, 1f);
        counterBackground.raycastTarget = false;

        Image icon = CreateImage(
            "Sun Icon", counter.transform, sunSprite, new Color(1f, 0.84f, 0.12f));
        SetRect(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -9f), new Vector2(42f, 42f));

        Text value = CreateText(
            "Value", counter.transform, font, resources.CurrentSun.ToString(), 26,
            new Color(1f, 0.92f, 0.45f));
        SetRect(value.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(-8f, 36f));
        counter.AddComponent<SunCounterView>().Configure(resources, value);
    }

    private static void CreatePlantCard(
        Transform parent,
        Font font,
        PlantSelectionController selection,
        PlantPlacementController placement,
        ResourceManager resources,
        PlantData data,
        Sprite iconSprite,
        Sprite sunSprite,
        Color color,
        float x)
    {
        var card = new GameObject(
            $"{data.displayName} Card",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(CanvasGroup),
            typeof(Outline));
        card.layer = 5;
        card.transform.SetParent(parent, false);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(102f, 94f);

        Image background = card.GetComponent<Image>();
        background.color = color;
        Button button = card.GetComponent<Button>();
        button.targetGraphic = background;

        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.93f, 0.35f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);
        outline.enabled = false;

        Image icon = CreateImage("Plant Icon", card.transform, iconSprite, Color.white);
        SetRect(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(38f, 38f));

        Text nameLabel = CreateText(
            "Name", card.transform, font, data.displayName, 13, Color.white);
        SetRect(nameLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(-6f, 22f));

        Image costIcon = CreateImage(
            "Cost Icon", card.transform, sunSprite, new Color(1f, 0.88f, 0.18f));
        SetRect(costIcon.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 0f), new Vector2(12f, 7f), new Vector2(18f, 18f));

        Text costLabel = CreateText(
            "Cost", card.transform, font, data.sunCost.ToString(), 15, Color.white);
        SetRect(costLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 0f), new Vector2(34f, 4f), new Vector2(58f, 24f));
        costLabel.alignment = TextAnchor.MiddleLeft;

        Image cooldown = CreateImage(
            "Cooldown Overlay", card.transform, GetOrCreateSquareSprite(),
            new Color(0.05f, 0.08f, 0.06f, 0.68f));
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Vertical;
        cooldown.fillOrigin = (int)Image.OriginVertical.Top;
        cooldown.fillAmount = 0f;
        cooldown.raycastTarget = false;
        cooldown.enabled = false;
        SetStretch(cooldown.rectTransform, Vector2.zero, Vector2.zero);

        card.AddComponent<PlantSelectionButton>().Configure(
            data, selection, placement, resources, nameLabel, costLabel, background,
            icon, cooldown, outline, color);
    }

    private static void CreateEventSystem()
    {
        var eventSystemObject = new GameObject(
            "EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystemObject.layer = 5;
        eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static Text CreateText(
        string name,
        Transform parent,
        Font font,
        string value,
        int fontSize,
        Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(
        string name,
        Transform parent,
        Sprite sprite,
        Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetStretch(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = minOffset;
        rect.offsetMax = maxOffset;
    }

    private static GameObject CreateShape(
        string name,
        Transform parent,
        Sprite sprite,
        Color color,
        Vector3 position,
        Vector3 scale,
        string sortingLayer,
        int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = scale;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    private static SunCollectible CreateSunPrefab(Sprite sunSprite, Sprite squareSprite)
    {
        const string path = "Assets/Prefabs/SunCollectible.prefab";
        var root = new GameObject("SunCollectible");
        root.transform.localScale = Vector3.one * 0.72f;

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sunSprite;
        renderer.color = new Color(1f, 0.84f, 0.12f);
        renderer.sortingLayerName = "Plants";
        renderer.sortingOrder = 30;

        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.radius = 0.48f;
        collider.isTrigger = true;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            float radians = angle * Mathf.Deg2Rad;
            var ray = new GameObject($"Ray {i}");
            ray.transform.SetParent(root.transform, false);
            ray.transform.localPosition = new Vector3(
                Mathf.Cos(radians) * 0.66f,
                Mathf.Sin(radians) * 0.66f,
                0f);
            ray.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            ray.transform.localScale = new Vector3(0.28f, 0.09f, 1f);
            SpriteRenderer rayRenderer = ray.AddComponent<SpriteRenderer>();
            rayRenderer.sprite = squareSprite;
            rayRenderer.color = new Color(1f, 0.72f, 0.06f);
            rayRenderer.sortingLayerName = "Plants";
            rayRenderer.sortingOrder = 29;
        }

        root.AddComponent<SunCollectible>();
        GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefabObject.GetComponent<SunCollectible>();
    }

    private static PlantBase CreatePlantPrefab(
        string name,
        string path,
        Sprite sprite,
        Color primary,
        Vector3 bodyScale,
        string decoration,
        SunCollectible sunPrefab,
        PeaProjectile peaPrefab)
    {
        var root = new GameObject(name);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = primary;
        renderer.sortingLayerName = "Plants";
        renderer.sortingOrder = 10;
        root.transform.localScale = bodyScale;
        root.AddComponent<PlantBase>();

        Color detailColor = decoration == "sun"
            ? new Color(0.48f, 0.25f, 0.08f)
            : decoration == "nut"
                ? new Color(0.72f, 0.48f, 0.2f)
                : new Color(0.16f, 0.72f, 0.25f);
        var detail = new GameObject(decoration == "sun" ? "Flower Center" : "Plant Detail");
        detail.transform.SetParent(root.transform, false);
        detail.transform.localPosition = decoration == "pea"
            ? new Vector3(0.42f, 0.16f, 0f)
            : decoration == "nut"
                ? new Vector3(-0.18f, 0.12f, 0f)
                : Vector3.zero;
        detail.transform.localScale = decoration == "pea"
            ? new Vector3(0.52f, 0.5f, 1f)
            : decoration == "nut"
                ? new Vector3(0.18f, 0.5f, 1f)
                : new Vector3(0.42f, 0.42f, 1f);
        SpriteRenderer detailRenderer = detail.AddComponent<SpriteRenderer>();
        detailRenderer.sprite = sprite;
        detailRenderer.color = detailColor;
        detailRenderer.sortingLayerName = "Plants";
        detailRenderer.sortingOrder = 11;

        if (decoration == "sun")
        {
            root.AddComponent<SunflowerProducer>().Configure(
                sunPrefab, null, null, 4f, 9f);
        }

        if (decoration == "pea")
        {
            root.AddComponent<PeashooterController>().Configure(
                peaPrefab, null, null, 5f, 6.6f);
        }


        GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefabObject.GetComponent<PlantBase>();
    }

    private static PlantData CreatePlantData(
        string path,
        string id,
        string displayName,
        PlantBase prefab,
        int cost,
        float cooldown,
        int health,
        int damage,
        float interval)
    {
        PlantData data = AssetDatabase.LoadAssetAtPath<PlantData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<PlantData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.id = id;
        data.displayName = displayName;
        data.prefab = prefab;
        data.sunCost = cost;
        data.cooldown = cooldown;
        data.maxHealth = health;
        data.attackDamage = damage;
        data.attackInterval = interval;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static Sprite GetOrCreateSquareSprite()
    {
        const string path = "Assets/Art/WhiteSquare.asset";
        Sprite existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (existing != null)
        {
            return existing;
        }

        var pixels = new Color32[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 255, 255, 255);
        }

        return CreateGeneratedSprite(path, "WhiteSquareTexture", "WhiteSquare", pixels);
    }

    private static Sprite GetOrCreateSunSprite()
    {
        const string path = "Assets/Art/SunCircle.asset";
        Sprite existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (existing != null)
        {
            return existing;
        }

        const int size = 32;
        var pixels = new Color32[size * size];
        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = 14.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                byte alpha = distance <= radius ? (byte)255 : (byte)0;
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        return CreateGeneratedSprite(path, "SunCircleTexture", "SunCircle", pixels);
    }

    private static Sprite CreateGeneratedSprite(
        string path,
        string textureName,
        string spriteName,
        Color32[] pixels)
    {
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
        {
            name = textureName,
            filterMode = FilterMode.Bilinear
        };
        texture.SetPixels32(pixels);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, path);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 32f, 32f),
            new Vector2(0.5f, 0.5f),
            32f);
        sprite.name = spriteName;
        AssetDatabase.AddObjectToAsset(sprite, path);
        EditorUtility.SetDirty(texture);
        AssetDatabase.SaveAssets();
        return sprite;
    }

    private static void EnsureSortingLayer(string layerName, long uniqueId)
    {
        Object tagManagerAsset = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset");
        var tagManager = new SerializedObject(tagManagerAsset);
        SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
        for (int i = 0; i < sortingLayers.arraySize; i++)
        {
            SerializedProperty element = sortingLayers.GetArrayElementAtIndex(i);
            SerializedProperty nameProperty = element.FindPropertyRelative("name");
            if (nameProperty != null && nameProperty.stringValue == layerName)
            {
                return;
            }
        }

        int index = sortingLayers.arraySize;
        sortingLayers.InsertArrayElementAtIndex(index);
        SerializedProperty newLayer = sortingLayers.GetArrayElementAtIndex(index);
        SerializedProperty newName = newLayer.FindPropertyRelative("name");
        SerializedProperty newId = newLayer.FindPropertyRelative("uniqueID");
        SerializedProperty locked = newLayer.FindPropertyRelative("locked");
        if (newName != null) newName.stringValue = layerName;
        if (newId != null) newId.longValue = uniqueId;
        if (locked != null) locked.boolValue = false;
        tagManager.ApplyModifiedProperties();
    }


private static ZombieData CreateZombieData(
        string path,
        string id,
        string displayName,
        ZombieController prefab,
        int health,
        float movementSpeed,
        float attackRange,
        int attackDamage,
        float attackInterval)
    {
        ZombieData data = AssetDatabase.LoadAssetAtPath<ZombieData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ZombieData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.id = id;
        data.displayName = displayName;
        data.prefab = prefab;
        data.maxHealth = Mathf.Max(1, health);
        data.movementSpeed = Mathf.Max(0f, movementSpeed);
        data.attackRange = Mathf.Max(0f, attackRange);
        data.attackDamage = Mathf.Max(0, attackDamage);
        data.attackInterval = Mathf.Max(0.01f, attackInterval);
        EditorUtility.SetDirty(data);
        return data;
    }


private static ZombieController CreateZombiePrefab(
        string name,
        string path,
        Sprite squareSprite,
        Sprite roundSprite,
        bool conehead)
    {
        var root = new GameObject(name);
        root.transform.localScale = new Vector3(0.62f, 0.92f, 1f);
        SpriteRenderer body = root.AddComponent<SpriteRenderer>();
        body.sprite = squareSprite;
        body.color = conehead
            ? new Color(0.36f, 0.43f, 0.31f)
            : new Color(0.43f, 0.50f, 0.39f);
        body.sortingLayerName = "Plants";
        body.sortingOrder = 12;

        var head = new GameObject("Head");
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        head.transform.localScale = new Vector3(0.86f, 0.62f, 1f);
        SpriteRenderer headRenderer = head.AddComponent<SpriteRenderer>();
        headRenderer.sprite = roundSprite;
        headRenderer.color = new Color(0.52f, 0.59f, 0.43f);
        headRenderer.sortingLayerName = "Plants";
        headRenderer.sortingOrder = 13;

        if (conehead)
        {
            var cone = new GameObject("Cone");
            cone.transform.SetParent(root.transform, false);
            cone.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            cone.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            cone.transform.localScale = new Vector3(0.42f, 0.62f, 1f);
            SpriteRenderer coneRenderer = cone.AddComponent<SpriteRenderer>();
            coneRenderer.sprite = squareSprite;
            coneRenderer.color = new Color(0.95f, 0.43f, 0.06f);
            coneRenderer.sortingLayerName = "Plants";
            coneRenderer.sortingOrder = 14;
        }

        root.AddComponent<BoxCollider2D>().isTrigger = true;
        root.AddComponent<ZombieController>();
        GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefabObject.GetComponent<ZombieController>();
    }


private static PeaProjectile CreatePeaPrefab(Sprite sprite)
    {
        const string path = "Assets/Prefabs/PeaProjectile.prefab";
        var root = new GameObject("PeaProjectile");
        root.transform.localScale = Vector3.one * 0.24f;
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.25f, 0.92f, 0.18f);
        renderer.sortingLayerName = "Projectiles";
        renderer.sortingOrder = 20;
        root.AddComponent<CircleCollider2D>().isTrigger = true;
        root.AddComponent<PeaProjectile>();
        GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefabObject.GetComponent<PeaProjectile>();
    }
}
