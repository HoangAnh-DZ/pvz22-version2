using System.Linq;
using PvZ2.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Phase5PresentationBuilder
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string GeneratedPath = "Assets/Art/Generated";

    [MenuItem("Tools/PvZ2 Foundation/Build Phase 5 Presentation")]
    public static void BuildPhase5()
    {
        PvZ2FoundationBuilder.BuildPrototype();
        Phase4SceneBuilder.EnhanceOpenScene();
        ApplyPresentation();
    }

    [MenuItem("Tools/PvZ2 Foundation/Apply Phase 5 Presentation")]
    public static void ApplyPresentation()
    {
        EnsureGeneratedArt();
        Sprite square = LoadSprite(GeneratedPath + "/ShapeSquare.asset");
        Sprite circle = LoadSprite(GeneratedPath + "/ShapeCircle.asset");
        GridManager grid = Object.FindFirstObjectByType<GridManager>();
        if (grid == null) throw new System.InvalidOperationException("GridManager is required.");

        StyleLawn(grid, square);
        BuildEnvironment(grid, square, circle);
        BuildPlant("Assets/Prefabs/Peashooter.prefab", "Peashooter", square, circle);
        BuildPlant("Assets/Prefabs/Sunflower.prefab", "Sunflower", square, circle);
        BuildPlant("Assets/Prefabs/WallNut.prefab", "WallNut", square, circle);
        BuildZombie("Assets/Prefabs/NormalZombie.prefab", false, square, circle);
        BuildZombie("Assets/Prefabs/ConeheadZombie.prefab", true, square, circle);
        StyleInterface();

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(-.15f, .08f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4.25f;
            camera.backgroundColor = new Color(.055f, .11f, .065f);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PvZ2 Phase 5 presentation applied successfully.");
    }

    static void EnsureGeneratedArt()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedPath))
            AssetDatabase.CreateFolder("Assets/Art", "Generated");
        CopyIfMissing("Assets/Art/WhiteSquare.asset", GeneratedPath + "/ShapeSquare.asset");
        CopyIfMissing("Assets/Art/SunCircle.asset", GeneratedPath + "/ShapeCircle.asset");
        AssetDatabase.ImportAsset(GeneratedPath + "/ShapeSquare.asset");
        AssetDatabase.ImportAsset(GeneratedPath + "/ShapeCircle.asset");
    }

    static void CopyIfMissing(string source, string target)
    {
        if (AssetDatabase.LoadMainAssetAtPath(target) == null)
            AssetDatabase.CopyAsset(source, target);
    }

    static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
    }

    static void StyleLawn(GridManager grid, Sprite square)
    {
        GameObject baseOld = GameObject.Find("Lawn Base");
        if (baseOld != null) Object.DestroyImmediate(baseOld);
        float width = grid.Columns * grid.CellSize.x;
        float height = grid.Rows * grid.CellSize.y;
        GameObject lawnBase = Shape("Lawn Base", grid.transform.parent, square,
            new Color(.255f, .505f, .19f), new Vector3(-.15f, 0f, .25f),
            new Vector3(width + .08f, height + .08f, 1f), "Board", 0);
        lawnBase.transform.SetSiblingIndex(0);

        foreach (GridCell cell in grid.GetComponentsInChildren<GridCell>())
        {
            SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
            float rowShade = cell.Row % 2 == 0 ? .008f : -.008f;
            float columnShade = (cell.Column % 3 - 1) * .004f;
            renderer.color = new Color(.31f + rowShade + columnShade, .595f + rowShade, .22f + columnShade);
            renderer.sortingLayerName = "Board";
            renderer.sortingOrder = 1;
            cell.transform.localScale = new Vector3(grid.CellSize.x * .997f, grid.CellSize.y * .997f, 1f);
        }
    }

    static void BuildEnvironment(GridManager grid, Sprite square, Sprite circle)
    {
        GameObject old = GameObject.Find("Battlefield Visuals");
        if (old != null) Object.DestroyImmediate(old);
        GameObject root = new("Battlefield Visuals");
        float height = grid.Rows * grid.CellSize.y;
        float left = grid.GetLaneLeftBoundary(0);
        float right = grid.GetLaneRightBoundary(0);

        Shape("Garden Backdrop", root.transform, square, new Color(.075f, .155f, .085f),
            new Vector3(-.15f, 0f, 1f), new Vector3(12f, 6.85f, 1f), "Background", -20);
        Shape("House Wall", root.transform, square, new Color(.62f, .44f, .28f),
            new Vector3(left - .78f, .2f, .4f), new Vector3(.95f, height + .65f, 1f), "Board", -1);
        Shape("House Shade", root.transform, square, new Color(.25f, .20f, .13f),
            new Vector3(left - .30f, .2f, .35f), new Vector3(.12f, height + .45f, 1f), "Board", 0);
        Shape("Home Patio", root.transform, square, new Color(.52f, .46f, .34f),
            new Vector3(left - .18f, 0f, .25f), new Vector3(.34f, height, 1f), "Board", 0);
        Shape("Entry Soil", root.transform, square, new Color(.31f, .29f, .19f),
            new Vector3(right + .34f, 0f, .25f), new Vector3(.66f, height, 1f), "Board", 0);

        for (int row = 0; row < grid.Rows; row++)
        {
            float y = grid.GetCellCenter(row, 0).y;
            Shape("Patio Stone " + row, root.transform, square, new Color(.68f, .62f, .46f),
                new Vector3(left - .17f, y, .1f), new Vector3(.26f, .78f, 1f), "Board", 2);
            Shape("Entry Shadow " + row, root.transform, circle, new Color(.16f, .18f, .10f, .75f),
                new Vector3(right + .28f, y, .08f), new Vector3(.46f, .16f, 1f), "Board", 2);
        }

        for (int i = 0; i < 4; i++)
        {
            float y = -2.45f + i * 1.65f;
            Shape("Shrub " + i, root.transform, circle, new Color(.12f, .33f + i * .012f, .11f),
                new Vector3(right + .72f, y, .05f), new Vector3(.45f, .38f, 1f), "Board", 3);
            Shape("Rock " + i, root.transform, circle, new Color(.43f, .44f, .34f),
                new Vector3(right + .52f, y - .31f, .04f), new Vector3(.22f, .13f, 1f), "Board", 4);
        }
    }

    static void BuildPlant(string path, string kind, Sprite square, Sprite circle)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform visual = PrepareVisual(root, circle, false);
        string layer = "Plants";

        if (kind == "Peashooter")
        {
            Part("Stem", visual, square, new Color(.20f, .58f, .16f), new Vector2(0f, .34f), new Vector2(.13f, .52f), layer, 12);
            Part("Leaf L", visual, circle, new Color(.25f, .68f, .18f), new Vector2(-.19f, .22f), new Vector2(.38f, .18f), layer, 13, 24f);
            Part("Leaf R", visual, circle, new Color(.22f, .62f, .16f), new Vector2(.18f, .27f), new Vector2(.34f, .16f), layer, 13, -22f);
            Part("Head", visual, circle, new Color(.34f, .75f, .22f), new Vector2(0f, .72f), new Vector2(.54f, .54f), layer, 15);
            Part("Snout", visual, circle, new Color(.30f, .69f, .20f), new Vector2(.28f, .73f), new Vector2(.34f, .27f), layer, 16);
            Part("Mouth", visual, circle, new Color(.08f, .24f, .07f), new Vector2(.39f, .73f), new Vector2(.12f, .12f), layer, 17);
            Eye(visual, circle, new Vector2(.08f, .84f), layer, 18);
        }
        else if (kind == "Sunflower")
        {
            Part("Stem", visual, square, new Color(.24f, .58f, .13f), new Vector2(0f, .34f), new Vector2(.12f, .54f), layer, 12);
            Part("Leaf L", visual, circle, new Color(.29f, .65f, .16f), new Vector2(-.19f, .25f), new Vector2(.38f, .18f), layer, 13, 24f);
            Part("Leaf R", visual, circle, new Color(.25f, .60f, .13f), new Vector2(.19f, .31f), new Vector2(.35f, .17f), layer, 13, -24f);
            Vector2 center = new(0f, .72f);
            for (int i = 0; i < 10; i++)
            {
                float angle = i * 36f;
                Vector2 p = center + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * .25f;
                Part("Petal " + i, visual, circle, new Color(1f, .72f, .08f), p, new Vector2(.28f, .17f), layer, 14, angle);
            }
            Part("Face", visual, circle, new Color(.48f, .27f, .07f), center, new Vector2(.48f, .48f), layer, 16);
            Eye(visual, circle, center + new Vector2(-.09f, .06f), layer, 18);
            Eye(visual, circle, center + new Vector2(.09f, .06f), layer, 18);
            Part("Smile", visual, square, new Color(.12f, .07f, .025f), center + new Vector2(0f, -.09f), new Vector2(.16f, .035f), layer, 18);
        }
        else
        {
            Part("Nut Body", visual, circle, new Color(.60f, .34f, .12f), new Vector2(0f, .49f), new Vector2(.66f, .98f), layer, 14);
            Part("Nut Highlight", visual, circle, new Color(.74f, .46f, .19f, .7f), new Vector2(-.13f, .56f), new Vector2(.18f, .58f), layer, 15);
            Eye(visual, circle, new Vector2(-.12f, .63f), layer, 18);
            Eye(visual, circle, new Vector2(.12f, .63f), layer, 18);
            Part("Mouth", visual, square, new Color(.20f, .10f, .035f), new Vector2(0f, .42f), new Vector2(.18f, .035f), layer, 18);
        }

        LaneDepthVisual depth = root.GetComponent<LaneDepthVisual>();
        if (depth != null) depth.Configure(visual);
        PeashooterController shooter = root.GetComponent<PeashooterController>();
        if (shooter != null)
        {
            SerializedObject so = new(shooter);
            so.FindProperty("spawnOffset").vector3Value = new Vector3(.48f, .73f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void BuildZombie(string path, bool cone, Sprite square, Sprite circle)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform visual = PrepareVisual(root, circle, true);
        string layer = "Plants";
        Color cloth = cone ? new Color(.34f, .23f, .18f) : new Color(.30f, .25f, .20f);
        Color skin = new(.48f, .62f, .42f);

        Part("Back Leg", visual, square, new Color(.20f, .18f, .15f), new Vector2(-.12f, .25f), new Vector2(.14f, .45f), layer, 11, -5f);
        Part("Front Leg", visual, square, new Color(.25f, .22f, .18f), new Vector2(.13f, .25f), new Vector2(.14f, .46f), layer, 12, 6f);
        Part("Back Shoe", visual, circle, new Color(.10f, .085f, .07f), new Vector2(-.16f, .055f), new Vector2(.30f, .12f), layer, 13);
        Part("Front Shoe", visual, circle, new Color(.11f, .09f, .075f), new Vector2(.18f, .055f), new Vector2(.31f, .12f), layer, 14);
        Part("Torso", visual, square, cloth, new Vector2(0f, .64f), new Vector2(.46f, .58f), layer, 15);
        Part("Jacket", visual, square, new Color(.20f, .31f, .24f), new Vector2(-.12f, .65f), new Vector2(.22f, .55f), layer, 16, -4f);
        Part("Back Arm", visual, square, skin, new Vector2(-.29f, .65f), new Vector2(.13f, .48f), layer, 14, -28f);
        Part("Front Arm", visual, square, skin, new Vector2(.34f, .69f), new Vector2(.13f, .50f), layer, 17, 66f);
        Part("Head", visual, circle, skin, new Vector2(.02f, 1.08f), new Vector2(.48f, .55f), layer, 18);
        Eye(visual, circle, new Vector2(-.08f, 1.15f), layer, 20);
        Eye(visual, circle, new Vector2(.09f, 1.15f), layer, 20);
        Part("Mouth", visual, square, new Color(.12f, .12f, .08f), new Vector2(.04f, .98f), new Vector2(.22f, .045f), layer, 20);

        if (cone)
        {
            Color orange = new(.96f, .39f, .06f);
            Part("Cone Brim", visual, square, orange, new Vector2(.02f, 1.34f), new Vector2(.64f, .11f), layer, 23);
            Part("Cone Lower", visual, square, orange, new Vector2(.02f, 1.48f), new Vector2(.45f, .25f), layer, 22);
            Part("Cone Upper", visual, square, new Color(1f, .49f, .08f), new Vector2(.02f, 1.65f), new Vector2(.25f, .22f), layer, 22);
        }

        LaneDepthVisual depth = root.GetComponent<LaneDepthVisual>();
        if (depth != null) depth.Configure(visual);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static Transform PrepareVisual(GameObject root, Sprite circle, bool zombie)
    {
        Transform visual = root.transform.Find("Visual");
        if (visual == null)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
        }
        while (visual.childCount > 0) Object.DestroyImmediate(visual.GetChild(0).gameObject);
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;

        Transform oldShadow = root.transform.Find("Ground Shadow");
        if (oldShadow != null) Object.DestroyImmediate(oldShadow.gameObject);
        Part("Ground Shadow", root.transform, circle, new Color(.015f, .03f, .015f, .38f),
            new Vector2(0f, .03f), zombie ? new Vector2(.66f, .18f) : new Vector2(.58f, .16f),
            zombie ? "Zombies" : "Plants", 2);
        return visual;
    }

    static void Eye(Transform parent, Sprite circle, Vector2 pos, string layer, int order)
    {
        Part("Eye White", parent, circle, new Color(.95f, .95f, .80f), pos, new Vector2(.115f, .13f), layer, order);
        Part("Pupil", parent, circle, new Color(.045f, .055f, .035f), pos + new Vector2(.018f, -.006f), new Vector2(.055f, .065f), layer, order + 1);
    }

    static GameObject Part(string name, Transform parent, Sprite sprite, Color color, Vector2 pos,
        Vector2 scale, string layer, int order, float rotation = 0f)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;
        return go;
    }

    static GameObject Shape(string name, Transform parent, Sprite sprite, Color color, Vector3 pos,
        Vector3 scale, string layer, int order)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.localScale = scale;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;
        return go;
    }

    static void StyleInterface()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        RectTransform safe = canvas.transform.Find("Safe Area") as RectTransform;
        if (safe == null)
        {
            GameObject go = new("Safe Area", typeof(RectTransform), typeof(SafeAreaFitter));
            go.layer = 5;
            safe = go.GetComponent<RectTransform>();
            safe.SetParent(canvas.transform, false);
            Stretch(safe);
            Transform[] children = Enumerable.Range(0, canvas.transform.childCount)
                .Select(i => canvas.transform.GetChild(i)).Where(t => t != safe).ToArray();
            foreach (Transform child in children) child.SetParent(safe, false);
        }

        Transform bar = safe.Find("Plant Bar");
        if (bar != null)
        {
            RectTransform r = (RectTransform)bar;
            Anchor(r, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(408f, 104f));
            Image bg = bar.GetComponent<Image>();
            bg.color = new Color(.24f, .16f, .09f, .96f);

            StyleCard(bar.Find("Sun Counter"), new Vector2(7f, 0f), new Vector2(82f, 88f), new Color(.34f, .25f, .11f));
            StyleCard(bar.Find("Sunflower Card"), new Vector2(95f, 0f), new Vector2(96f, 88f), new Color(.72f, .45f, .08f));
            StyleCard(bar.Find("Peashooter Card"), new Vector2(196f, 0f), new Vector2(96f, 88f), new Color(.18f, .47f, .17f));
            StyleCard(bar.Find("Wall-nut Card"), new Vector2(297f, 0f), new Vector2(96f, 88f), new Color(.48f, .28f, .11f));
            StyleCard(bar.Find("WallNut Card"), new Vector2(297f, 0f), new Vector2(96f, 88f), new Color(.48f, .28f, .11f));
        }

        Transform hud = safe.Find("Phase4 HUD");
        if (hud != null)
        {
            Anchor((RectTransform)hud, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 14f), new Vector2(184f, 70f));
            hud.GetComponent<Image>().color = new Color(.20f, .14f, .08f, .92f);
        }

        Transform banner = safe.Find("Final Wave Banner");
        if (banner != null)
        {
            Anchor((RectTransform)banner, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -108f), new Vector2(390f, 62f));
            banner.GetComponent<Image>().color = new Color(.57f, .10f, .035f, .96f);
        }

        StyleTerminal(safe.Find("Victory Panel"), new Color(.16f, .42f, .18f, .98f));
        StyleTerminal(safe.Find("Defeat Panel"), new Color(.46f, .10f, .065f, .98f));
    }

    static void StyleCard(Transform card, Vector2 pos, Vector2 size, Color color)
    {
        if (card == null) return;
        RectTransform r = (RectTransform)card;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        Image image = card.GetComponent<Image>();
        if (image != null) image.color = color;
        PlantSelectionButton selection = card.GetComponent<PlantSelectionButton>();
        if (selection != null)
        {
            SerializedObject so = new(selection);
            so.FindProperty("baseColor").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Transform name = card.Find("Name");
        if (name != null) name.GetComponent<Text>().fontSize = 11;
        Transform icon = card.Find("Plant Icon");
        if (icon != null)
        {
            Image iconImage = icon.GetComponent<Image>();
            if (card.name.Contains("Peashooter")) iconImage.color = new Color(.48f, .90f, .30f);
            else if (card.name.Contains("Wall")) iconImage.color = new Color(.72f, .43f, .17f);
            else iconImage.color = new Color(1f, .78f, .10f);
        }
    }

    static void StyleTerminal(Transform overlay, Color cardColor)
    {
        if (overlay == null) return;
        Image overlayImage = overlay.GetComponent<Image>();
        if (overlayImage != null) overlayImage.color = new Color(.02f, .025f, .015f, .68f);
        Transform card = overlay.Find("Card");
        if (card != null)
        {
            Image image = card.GetComponent<Image>();
            if (image != null) image.color = cardColor;
        }
    }

    static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        r.anchorMin = min; r.anchorMax = max; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }
}