using System.Linq;
using PvZ2.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Phase4SceneBuilder
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string LevelPath = "Assets/Data/FrontYardLevel.asset";

    [MenuItem("Tools/PvZ2 Foundation/Build Phase 4")]
    public static void BuildPhase4()
    {
        PvZ2FoundationBuilder.BuildPrototype();
        EnhanceOpenScene();
    }

    [MenuItem("Tools/PvZ2 Foundation/Enhance Current Scene Phase 4")]
    public static void EnhanceOpenScene()
    {
        Sprite square = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/WhiteSquare.asset").OfType<Sprite>().First();
        Sprite sun = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/SunCircle.asset").OfType<Sprite>().First();
        GridManager grid = Object.FindFirstObjectByType<GridManager>();
        GameObject systems = GameObject.Find("Gameplay Systems");
        BasicZombieSpawner spawner = systems.GetComponent<BasicZombieSpawner>();
        PlantPlacementController placement = systems.GetComponent<PlantPlacementController>();

        BattlefieldProjection projection = grid.GetComponent<BattlefieldProjection>();
        if (projection == null) projection = grid.gameObject.AddComponent<BattlefieldProjection>();
        projection.Configure(new Vector2(-.55f, -.03f), new Vector2(1.24f, 1.03f), .9f, 1.08f);
        grid.BuildProjectedGrid(5, 9, projection, square);
        StyleGrid(grid);

        Camera camera = Camera.main;
        camera.transform.position = new Vector3(-.55f, .12f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 4.02f;
        camera.backgroundColor = new Color(.055f, .105f, .065f);
        BuildEnvironment(grid, square);

        GroundPrefab("Assets/Prefabs/Peashooter.prefab", square, false);
        GroundPrefab("Assets/Prefabs/Sunflower.prefab", square, false);
        GroundPrefab("Assets/Prefabs/WallNut.prefab", square, false);
        GroundPrefab("Assets/Prefabs/NormalZombie.prefab", square, true);
        GroundPrefab("Assets/Prefabs/ConeheadZombie.prefab", square, true);

        LevelData level = BuildLevel();
        WaveManager waves = systems.GetComponent<WaveManager>();
        if (waves == null) waves = systems.AddComponent<WaveManager>();
        waves.Configure(level, spawner, placement);
        BuildHud(waves, square, sun);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PvZ2 Phase 4 scene and level created successfully.");
    }

    static LevelData BuildLevel()
    {
        LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(LevelPath);
        if (level == null)
        {
            level = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(level, LevelPath);
        }
        ZombieData normal = AssetDatabase.LoadAssetAtPath<ZombieData>("Assets/Data/NormalZombie.asset");
        ZombieData cone = AssetDatabase.LoadAssetAtPath<ZombieData>("Assets/Data/ConeheadZombie.asset");
        level.id = "front-yard-01";
        level.displayName = "Front Yard";
        level.preparationDelay = 2.5f;
        level.waveTransitionDelay = 2f;
        level.waves.Clear();
        level.waves.Add(Wave("Wave 1", Entry(normal, 3, .5f, 2.2f)));
        level.waves.Add(Wave("Wave 2", Entry(normal, 4, .3f, 1.8f), Entry(cone, 1, 3f, 1f)));
        level.waves.Add(Wave("Final Wave", Entry(normal, 5, .2f, 1.35f), Entry(cone, 2, 1.8f, 2.4f)));
        EditorUtility.SetDirty(level);
        return level;
    }

    static WaveData Wave(string name, params ZombieSpawnEntry[] entries)
    {
        WaveData wave = new() { displayName = name };
        wave.spawns.AddRange(entries);
        return wave;
    }

    static ZombieSpawnEntry Entry(ZombieData data, int amount, float delay, float interval)
    {
        return new ZombieSpawnEntry { zombie = data, amount = amount, startDelay = delay,
            spawnInterval = interval, laneMode = LaneMode.RandomValidLane };
    }

    static void StyleGrid(GridManager grid)
    {
        foreach (GridCell cell in grid.GetComponentsInChildren<GridCell>())
        {
            SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
            renderer.color = (cell.Row + cell.Column) % 2 == 0
                ? new Color(.24f, .53f, .19f) : new Color(.285f, .59f, .215f);
            renderer.sortingLayerName = "Board";
            renderer.sortingOrder = 1 + cell.Row;
        }
    }

    static void BuildEnvironment(GridManager grid, Sprite square)
    {
        GameObject old = GameObject.Find("Battlefield Visuals");
        if (old != null) Object.DestroyImmediate(old);
        GameObject root = new("Battlefield Visuals");
        Shape("Deep Green Backdrop", root.transform, square, new Color(.08f, .17f, .09f),
            new Vector3(-.55f, 0f, 1f), new Vector3(16f, 8.2f, 1f), "Background", -20);
        Shape("Home Patio", root.transform, square, new Color(.49f, .33f, .18f),
            new Vector3(-6.72f, -.05f, .2f), new Vector3(1.05f, 5.72f, 1f), "Board", 0);
        Shape("Zombie Entry", root.transform, square, new Color(.19f, .245f, .19f),
            new Vector3(5.7f, -.05f, .2f), new Vector3(.9f, 5.72f, 1f), "Board", 0);
        Shape("Stone Path", root.transform, square, new Color(.34f, .36f, .27f),
            new Vector3(-6.72f, -.05f, .1f), new Vector3(.24f, 5.35f, 1f), "Board", 2);
        for (int row = 0; row < grid.Rows; row++)
        {
            float y = grid.GetCellCenter(row, 0).y;
            Shape($"Home Marker {row}", root.transform, square, new Color(.72f, .58f, .3f),
                new Vector3(grid.GetLaneLeftBoundary(row, .06f), y, 0f), new Vector3(.18f, .72f, 1f), "Board", 6 + row);
            Shape($"Entry Marker {row}", root.transform, square, new Color(.55f, .18f, .12f),
                new Vector3(grid.GetLaneRightBoundary(row, .06f), y, 0f), new Vector3(.18f, .72f, 1f), "Board", 6 + row);
        }
    }

    static void GroundPrefab(string path, Sprite square, bool zombie)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Vector3 oldScale = root.transform.localScale;
        Transform[] oldChildren = Enumerable.Range(0, root.transform.childCount).Select(i => root.transform.GetChild(i)).ToArray();
        GameObject visualGo = new("Visual");
        visualGo.transform.SetParent(root.transform, false);
        visualGo.transform.localPosition = new Vector3(0f, zombie ? .38f : .3f, 0f);
        visualGo.transform.localScale = oldScale;
        root.transform.localScale = Vector3.one;
        foreach (Transform child in oldChildren) child.SetParent(visualGo.transform, false);

        SpriteRenderer original = root.GetComponent<SpriteRenderer>();
        if (original != null)
        {
            GameObject body = new("Body");
            body.transform.SetParent(visualGo.transform, false);
            SpriteRenderer copy = body.AddComponent<SpriteRenderer>();
            copy.sprite = original.sprite; copy.color = original.color;
            copy.sortingLayerID = original.sortingLayerID; copy.sortingOrder = original.sortingOrder;
            Object.DestroyImmediate(original);
        }

        GameObject shadow = new("Ground Shadow");
        shadow.transform.SetParent(root.transform, false);
        shadow.transform.localPosition = new Vector3(0f, .02f, .08f);
        shadow.transform.localScale = zombie ? new Vector3(.72f, .18f, 1f) : new Vector3(.62f, .15f, 1f);
        SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = square; shadowRenderer.color = new Color(.02f, .04f, .02f, .38f);
        shadowRenderer.sortingLayerName = "Plants"; shadowRenderer.sortingOrder = 6;

        LaneDepthVisual depth = root.GetComponent<LaneDepthVisual>();
        if (depth == null) depth = root.AddComponent<LaneDepthVisual>();
        depth.Configure(visualGo.transform);
        BoxCollider2D box = root.GetComponent<BoxCollider2D>();
        if (box != null) { box.size = zombie ? new Vector2(.62f, 1.18f) : new Vector2(.7f, .8f); box.offset = new Vector2(0f, .38f); }

        PeashooterController shooter = root.GetComponent<PeashooterController>();
        if (shooter != null)
        {
            SerializedObject so = new(shooter);
            so.FindProperty("spawnOffset").vector3Value = new Vector3(.58f, .52f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void BuildHud(WaveManager waves, Sprite square, Sprite sun)
    {
        GameObject old = GameObject.Find("Phase4 HUD");
        if (old != null) Object.DestroyImmediate(old);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject root = UiObject("Phase4 HUD", canvas.transform, new Color(.08f, .12f, .07f, .92f));
        RectTransform rect = root.GetComponent<RectTransform>();
        Anchor(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(190f, 76f));
        Text wave = Label("Wave", root.transform, font, "GET READY", 22, new Color(1f, .88f, .35f));
        Anchor(wave.rectTransform, new Vector2(0f, .5f), Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(-10f, 0f));
        Text enemies = Label("Enemies", root.transform, font, "ZOMBIES 0", 15, Color.white);
        Anchor(enemies.rectTransform, Vector2.zero, new Vector2(1f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(-10f, 0f));

        GameObject banner = UiObject("Final Wave Banner", canvas.transform, new Color(.58f, .08f, .035f, .96f));
        Anchor(banner.GetComponent<RectTransform>(), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -118f), new Vector2(440f, 72f));
        Text bannerText = Label("Text", banner.transform, font, "FINAL WAVE!", 34, new Color(1f, .9f, .32f));
        Stretch(bannerText.rectTransform);

        Button victoryRestart;
        GameObject victory = Terminal("Victory Panel", canvas.transform, font, "VICTORY!", new Color(.14f, .5f, .18f, .97f), out victoryRestart);
        Button defeatRestart;
        GameObject defeat = Terminal("Defeat Panel", canvas.transform, font, "THE ZOMBIES ATE YOUR BRAINS!", new Color(.48f, .08f, .055f, .97f), out defeatRestart);
        root.AddComponent<WaveHudView>().Configure(waves, wave, enemies, banner, victory, defeat, victoryRestart, defeatRestart);
        banner.SetActive(false); victory.SetActive(false); defeat.SetActive(false);
    }

    static GameObject Terminal(string name, Transform parent, Font font, string message, Color color, out Button restart)
    {
        GameObject panel = UiObject(name, parent, color);
        Anchor(panel.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(560f, 230f));
        Text title = Label("Title", panel.transform, font, message, message == "VICTORY!" ? 48 : 30, Color.white);
        Anchor(title.rectTransform, new Vector2(0f, .46f), Vector2.one, new Vector2(.5f, .5f), new Vector2(0f, 24f), new Vector2(-20f, -20f));
        GameObject buttonGo = UiObject("Restart", panel.transform, new Color(.9f, .7f, .17f));
        Anchor(buttonGo.GetComponent<RectTransform>(), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 28f), new Vector2(190f, 58f));
        restart = buttonGo.AddComponent<Button>();
        Text buttonText = Label("Text", buttonGo.transform, font, "RESTART", 24, new Color(.12f, .12f, .06f));
        Stretch(buttonText.rectTransform);
        return panel;
    }

    static GameObject UiObject(string name, Transform parent, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5; go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color; return go;
    }

    static Text Label(string name, Transform parent, Font font, string value, int size, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = 5; go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = size;
        text.color = color; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; return text;
    }

    static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    { r.anchorMin = min; r.anchorMax = max; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size; }
    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; }
    static GameObject Shape(string name, Transform parent, Sprite sprite, Color color, Vector3 pos, Vector3 scale, string layer, int order)
    {
        GameObject go = new(name); go.transform.SetParent(parent, true); go.transform.position = pos; go.transform.localScale = scale;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
        renderer.sortingLayerName = layer; renderer.sortingOrder = order; return go;
    }

    [MenuItem("Tools/PvZ2 Foundation/Debug Spawn Normal")]
    static void DebugNormal() { if (Application.isPlaying) Object.FindFirstObjectByType<BasicZombieSpawner>()?.SpawnNormal(2); }
    [MenuItem("Tools/PvZ2 Foundation/Debug Spawn Conehead")]
    static void DebugCone() { if (Application.isPlaying) Object.FindFirstObjectByType<BasicZombieSpawner>()?.SpawnConehead(2); }
}