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

        Sprite squareSprite = GetOrCreateSquareSprite();
        PlantBase peashooterPrefab = CreatePlantPrefab(
            "Peashooter", "Assets/Prefabs/Peashooter.prefab", squareSprite,
            new Color(0.12f, 0.62f, 0.22f), new Vector3(0.38f, 0.68f, 1f), "pea");
        PlantBase sunflowerPrefab = CreatePlantPrefab(
            "Sunflower", "Assets/Prefabs/Sunflower.prefab", squareSprite,
            new Color(1f, 0.73f, 0.08f), new Vector3(0.74f, 0.74f, 1f), "sun");
        PlantBase wallNutPrefab = CreatePlantPrefab(
            "WallNut", "Assets/Prefabs/WallNut.prefab", squareSprite,
            new Color(0.48f, 0.27f, 0.09f), new Vector3(0.68f, 0.86f, 1f), "nut");

        PlantData peashooterData = CreatePlantData(
            "Assets/Data/Peashooter.asset", "peashooter", "Peashooter",
            peashooterPrefab, 100, 5f, 100, 20, 1.5f);
        PlantData sunflowerData = CreatePlantData(
            "Assets/Data/Sunflower.asset", "sunflower", "Sunflower",
            sunflowerPrefab, 50, 6f, 80, 0, 5f);
        PlantData wallNutData = CreatePlantData(
            "Assets/Data/WallNut.asset", "wall-nut", "Wall-Nut",
            wallNutPrefab, 50, 12f, 500, 0, 0f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera();
        CreateGlobalLight();
        CreateBattlefieldVisuals(squareSprite);
        GridManager grid = CreateGrid(squareSprite);

        var plantContainer = new GameObject("Plants");
        var systems = new GameObject("Gameplay Systems");
        ResourceManager resources = systems.AddComponent<ResourceManager>();
        resources.ConfigureStartingSun(250, true);
        PlantSelectionController selection = systems.AddComponent<PlantSelectionController>();
        PlantPlacementController placement = systems.AddComponent<PlantPlacementController>();
        placement.Configure(grid, selection, resources, plantContainer.transform);
        systems.AddComponent<BoardPointerInput>().Configure(placement, camera);
        selection.SelectPlant(peashooterData);

        CreateInterface(resources, selection, peashooterData, sunflowerData, wallNutData);
        CreateEventSystem();

        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PvZ2 Foundation prototype created successfully.");
    }

    private static Camera CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-0.7f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.15f;
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
            new Color(0.14f, 0.28f, 0.16f), new Vector3(-0.7f, 0f, 1f),
            new Vector3(15.5f, 8f, 1f), "Background", -10);
        CreateShape("Home Strip", environment.transform, sprite,
            new Color(0.47f, 0.31f, 0.18f), new Vector3(-7.05f, 0f, 0.2f),
            new Vector3(1.35f, 5.75f, 1f), "Board", 0);
        CreateShape("Zombie Entry Strip", environment.transform, sprite,
            new Color(0.18f, 0.23f, 0.20f), new Vector3(5.6f, 0f, 0.2f),
            new Vector3(1.45f, 5.75f, 1f), "Board", 0);
        CreateShape("Home Door", environment.transform, sprite,
            new Color(0.23f, 0.12f, 0.06f), new Vector3(-7.05f, 0.15f, 0.1f),
            new Vector3(0.72f, 1.65f, 1f), "Board", 2);

        for (int row = 0; row < 5; row++)
        {
            CreateShape($"Entry Marker {row}", environment.transform, sprite,
                new Color(0.36f, 0.45f, 0.38f), new Vector3(5.6f, -2.1f + row * 1.05f, 0.1f),
                new Vector3(0.45f, 0.12f, 1f), "Board", 2);
        }
    }

    private static GridManager CreateGrid(Sprite sprite)
    {
        var gridObject = new GameObject("GridManager");
        GridManager grid = gridObject.AddComponent<GridManager>();
        grid.BuildGrid(5, 9, new Vector2(-5.75f, -2.1f), new Vector2(1.25f, 1.05f), sprite);

        foreach (GridCell cell in gridObject.GetComponentsInChildren<GridCell>())
        {
            SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Board";
            renderer.sortingOrder = 1;
            renderer.color = ((cell.Row + cell.Column) & 1) == 0
                ? new Color(0.27f, 0.58f, 0.23f)
                : new Color(0.24f, 0.54f, 0.21f);
        }

        return grid;
    }

    private static void CreateInterface(
        ResourceManager resources,
        PlantSelectionController selection,
        PlantData peashooter,
        PlantData sunflower,
        PlantData wallNut)
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
        panelRect.anchoredPosition = new Vector2(18f, -18f);
        panelRect.sizeDelta = new Vector2(880f, 104f);
        panelObject.GetComponent<Image>().color = new Color(0.06f, 0.1f, 0.07f, 0.9f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text sunText = CreateText("Sun Counter", panelObject.transform, font, "SUN  250", 32,
            new Color(1f, 0.86f, 0.22f));
        RectTransform sunRect = sunText.rectTransform;
        sunRect.anchorMin = new Vector2(0f, 0f);
        sunRect.anchorMax = new Vector2(0f, 1f);
        sunRect.pivot = new Vector2(0f, 0.5f);
        sunRect.anchoredPosition = new Vector2(18f, 0f);
        sunRect.sizeDelta = new Vector2(170f, -18f);
        sunText.gameObject.AddComponent<SunCounterView>().Configure(resources, sunText);

        CreatePlantCard(panelObject.transform, font, selection, peashooter,
            new Color(0.12f, 0.5f, 0.19f), 190f);
        CreatePlantCard(panelObject.transform, font, selection, sunflower,
            new Color(0.78f, 0.54f, 0.05f), 410f);
        CreatePlantCard(panelObject.transform, font, selection, wallNut,
            new Color(0.42f, 0.23f, 0.08f), 630f);

        Text hintText = CreateText("Hint", canvasObject.transform, font,
            "SELECT A PLANT  •  CLICK A LAWN CELL", 24, new Color(0.9f, 0.96f, 0.9f, 0.95f));
        RectTransform hintRect = hintText.rectTransform;
        hintRect.anchorMin = new Vector2(1f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.anchoredPosition = new Vector2(-22f, -28f);
        hintRect.sizeDelta = new Vector2(620f, 52f);
        hintText.alignment = TextAnchor.MiddleRight;
    }

    private static void CreatePlantCard(
        Transform parent,
        Font font,
        PlantSelectionController selection,
        PlantData data,
        Color color,
        float x)
    {
        var card = new GameObject(
            $"{data.displayName} Card", typeof(RectTransform), typeof(Image), typeof(Button));
        card.layer = 5;
        card.transform.SetParent(parent, false);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(205f, 78f);
        Image image = card.GetComponent<Image>();
        image.color = color;
        card.GetComponent<Button>().targetGraphic = image;

        Text label = CreateText("Label", card.transform, font, data.displayName, 24, Color.white);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);
        card.AddComponent<PlantSelectionButton>().Configure(data, selection, label, image, color);
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

    private static PlantBase CreatePlantPrefab(
        string name,
        string path,
        Sprite sprite,
        Color primary,
        Vector3 bodyScale,
        string decoration)
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
        const string spritePath = "Assets/Art/WhiteSquare.asset";
        Sprite existing = AssetDatabase.LoadAllAssetsAtPath(spritePath).OfType<Sprite>().FirstOrDefault();
        if (existing != null)
        {
            return existing;
        }

        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "WhiteSquareTexture" };
        var pixels = new Color32[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 255, 255, 255);
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, spritePath);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
        sprite.name = "WhiteSquare";
        AssetDatabase.AddObjectToAsset(sprite, spritePath);
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
}
