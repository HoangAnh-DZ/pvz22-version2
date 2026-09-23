using System.IO;
using System.Linq;
using PvZ2.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase6AssetIntegrationBuilder
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string Imported = "Assets/Art/External/Imported";
    const string MaleFolder = Imported + "/Zombies/pzUH_Zombie/Male";
    const string SunflowerHead = Imported + "/Plants/Sunflower/Openclipart/sunflower-head.png";
    const string SunflowerBase = Imported + "/Plants/Sunflower/Kenney/foliage-base.png";
    const string WallNut = Imported + "/Plants/WallNut/Openclipart/cartoon-acorn.png";
    const string Lawn = Imported + "/Environment/Lawn/FlashyFeather/seamless-grass.png";
    const string Backdrop = Imported + "/Environment/Background/Kenney/background-grass.png";
    const string House = Imported + "/Environment/Background/Kenney/home-house.png";
    const string HomeFence = Imported + "/Environment/Background/Kenney/home-fence.png";
    const string HomeBush = Imported + "/Environment/Background/Kenney/home-bush.png";
    const string EntryFence = Imported + "/Environment/Background/Kenney/entry-fence.png";
    const string DeadTree = Imported + "/Environment/Background/Kenney/entry-dead-tree.png";
    const string Pea = Imported + "/Projectiles/Openclipart/pea-projectile.png";
    const string Sun = Imported + "/Sun/Openclipart/cartoon-sun.png";

    [MenuItem("Tools/PvZ2 Foundation/Build Phase 6 Licensed Assets")]
    public static void BuildPhase6()
    {
        Phase5PresentationBuilder.BuildPhase5();
        ApplyLicensedAssets();
    }

    [MenuItem("Tools/PvZ2 Foundation/Apply Phase 6 Licensed Assets")]
    public static void ApplyLicensedAssets()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureExternalTextures();
        ConfigureZombieTextures();

        Sprite[] idle = LoadFrames("Idle");
        Sprite[] walk = LoadFrames("Walk");
        Sprite[] attack = LoadFrames("Attack");
        Sprite[] dead = LoadFrames("Dead");
        if (idle.Length != 15 || walk.Length != 10 || attack.Length != 8 || dead.Length != 12)
            throw new System.InvalidOperationException(
                $"Unexpected licensed zombie frame counts: idle={idle.Length}, walk={walk.Length}, attack={attack.Length}, dead={dead.Length}");

        Sprite square = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Generated/ShapeSquare.asset")
            .OfType<Sprite>().First();
        ApplyZombiePrefab("Assets/Prefabs/NormalZombie.prefab", false, square, idle, walk, attack, dead);
        ApplyZombiePrefab("Assets/Prefabs/ConeheadZombie.prefab", true, square, idle, walk, attack, dead);
        ApplyPeashooterPresentation();
        ApplySunflowerPrefab();
        ApplyWallNutPrefab();
        ApplyProjectilePrefab();
        ApplySunCollectiblePrefab();
        ApplySceneEnvironment();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PvZ2 Phase 6 external licensed assets integrated successfully.");
    }

    static void ConfigureExternalTextures()
    {
        ConfigureTexture(SunflowerHead, 800f, new Vector2(.5f, .5f));
        ConfigureTexture(SunflowerBase, 160f, new Vector2(.5f, 0f));
        ConfigureTexture(WallNut, 800f, new Vector2(.5f, 0f));
        ConfigureTexture(Lawn, 128f, new Vector2(.5f, .5f), true);
        ConfigureTexture(Backdrop, 128f, new Vector2(.5f, .5f));
        ConfigureTexture(House, 128f, new Vector2(.5f, 0f));
        ConfigureTexture(HomeFence, 128f, new Vector2(.5f, 0f));
        ConfigureTexture(HomeBush, 128f, new Vector2(.5f, 0f));
        ConfigureTexture(EntryFence, 128f, new Vector2(.5f, 0f));
        ConfigureTexture(DeadTree, 128f, new Vector2(.5f, 0f));
        ConfigureTexture(Pea, 650f, new Vector2(.5f, .5f));
        ConfigureTexture(Sun, 600f, new Vector2(.5f, .5f));
    }

    static void ConfigureTexture(string path, float pixelsPerUnit, Vector2 pivot, bool repeat = false)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Missing external asset", path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.compressionQuality = 90;
        TextureImporterSettings settings = new();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    static void ApplyPeashooterPresentation()
    {
        const string path = "Assets/Prefabs/Peashooter.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        EnsurePlantAnimator(root.transform.Find("Visual"), PlantPresentationKind.Peashooter);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void ApplySunflowerPrefab()
    {
        const string path = "Assets/Prefabs/Sunflower.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform visual = PreparePlantVisual(root, PlantPresentationKind.Sunflower);
        ExternalPart("Licensed Stem and Leaves", visual, LoadSprite(SunflowerBase),
            new Vector2(0f, .04f), new Vector2(.76f, .82f), 13);
        ExternalPart("Licensed Sunflower Head", visual, LoadSprite(SunflowerHead),
            new Vector2(0f, .58f), new Vector2(.72f, .72f), 17);
        ConfigureDepth(root, visual);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void ApplyWallNutPrefab()
    {
        const string path = "Assets/Prefabs/WallNut.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform visual = PreparePlantVisual(root, PlantPresentationKind.WallNut);
        ExternalPart("Licensed Cartoon Acorn", visual, LoadSprite(WallNut),
            new Vector2(0f, .02f), new Vector2(.78f, 1.00f), 16);
        ConfigureDepth(root, visual);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static Transform PreparePlantVisual(GameObject root, PlantPresentationKind kind)
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
        EnsurePlantAnimator(visual, kind);
        return visual;
    }

    static void EnsurePlantAnimator(Transform visual, PlantPresentationKind kind)
    {
        if (visual == null) throw new System.InvalidOperationException("Plant Visual is required.");
        PlantPresentationAnimator animator = visual.GetComponent<PlantPresentationAnimator>();
        if (animator == null) animator = visual.gameObject.AddComponent<PlantPresentationAnimator>();
        animator.Configure(kind);
    }

    static void ConfigureDepth(GameObject root, Transform visual)
    {
        LaneDepthVisual depth = root.GetComponent<LaneDepthVisual>();
        if (depth != null) depth.Configure(visual);
    }

    static void ApplyProjectilePrefab()
    {
        const string path = "Assets/Prefabs/PeaProjectile.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(Pea);
        renderer.color = Color.white;
        renderer.sortingLayerName = "Plants";
        renderer.sortingOrder = 30;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void ApplySunCollectiblePrefab()
    {
        const string path = "Assets/Prefabs/SunCollectible.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        while (root.transform.childCount > 0)
            Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
        SpriteRenderer oldRenderer = root.GetComponent<SpriteRenderer>();
        if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
        ExternalPart("Licensed Cartoon Sun", root.transform, LoadSprite(Sun),
            Vector2.zero, new Vector2(.58f, .58f), 40);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void ApplySceneEnvironment()
    {
        GridManager grid = Object.FindFirstObjectByType<GridManager>();
        if (grid == null) throw new System.InvalidOperationException("GridManager is required.");
        float width = grid.Columns * grid.CellSize.x;
        float height = grid.Rows * grid.CellSize.y;
        float left = grid.GetLaneLeftBoundary(0);
        float right = grid.GetLaneRightBoundary(0);

        GameObject lawn = GameObject.Find("Lawn Base");
        if (lawn == null) lawn = new GameObject("Lawn Base");
        lawn.transform.position = new Vector3(-.15f, 0f, .25f);
        lawn.transform.localScale = Vector3.one;
        SpriteRenderer lawnRenderer = lawn.GetComponent<SpriteRenderer>();
        if (lawnRenderer == null) lawnRenderer = lawn.AddComponent<SpriteRenderer>();
        lawnRenderer.sprite = LoadSprite(Lawn);
        lawnRenderer.color = Color.white;
        lawnRenderer.sortingLayerName = "Board";
        lawnRenderer.sortingOrder = 0;
        lawnRenderer.drawMode = SpriteDrawMode.Tiled;
        lawnRenderer.tileMode = SpriteTileMode.Continuous;
        lawnRenderer.size = new Vector2(width + .08f, height + .08f);

        foreach (GridCell cell in grid.GetComponentsInChildren<GridCell>())
        {
            SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = new Color(1f, 1f, 1f, .10f);
        }

        GameObject old = GameObject.Find("Battlefield Visuals");
        if (old != null) Object.DestroyImmediate(old);
        GameObject environment = new("Battlefield Visuals");
        ScenePart("Licensed Garden Backdrop", environment.transform, LoadSprite(Backdrop),
            new Vector2(-.15f, 0f), new Vector2(12f, 6.85f), "Background", -20);
        ScenePart("Licensed Home House", environment.transform, LoadSprite(House),
            new Vector2(left - .86f, -height * .50f + .10f), new Vector2(1.55f, 2.05f), "Board", -1, true);
        ScenePart("Licensed Home Bush", environment.transform, LoadSprite(HomeBush),
            new Vector2(left - .55f, -height * .50f + .04f), new Vector2(.92f, .54f), "Board", 3, true);
        ScenePart("Licensed Zombie Dead Tree", environment.transform, LoadSprite(DeadTree),
            new Vector2(right + .78f, -height * .50f + .04f), new Vector2(1.15f, 2.15f), "Board", 1, true);
        for (int row = 0; row < grid.Rows; row++)
        {
            float y = grid.GetCellCenter(row, 0).y - grid.CellSize.y * .45f;
            ScenePart("Licensed Home Fence " + row, environment.transform, LoadSprite(HomeFence),
                new Vector2(left - .20f, y), new Vector2(.48f, .42f), "Board", 2, true);
            ScenePart("Licensed Entry Fence " + row, environment.transform, LoadSprite(EntryFence),
                new Vector2(right + .28f, y), new Vector2(.62f, .46f), "Board", 2, true);
        }
    }

    static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new FileNotFoundException("Sprite import failed", path);
        return sprite;
    }

    static GameObject ExternalPart(string name, Transform parent, Sprite sprite, Vector2 position,
        Vector2 targetSize, int order)
    {
        GameObject part = new(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = new Vector3(position.x, position.y, 0f);
        Fit(part.transform, sprite, targetSize);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingLayerName = "Plants";
        renderer.sortingOrder = order;
        return part;
    }

    static GameObject ScenePart(string name, Transform parent, Sprite sprite, Vector2 position,
        Vector2 targetSize, string layer, int order, bool bottomAligned = false)
    {
        GameObject part = new(name);
        part.transform.SetParent(parent, false);
        float y = bottomAligned ? position.y + targetSize.y * .5f : position.y;
        part.transform.position = new Vector3(position.x, y, .2f);
        Fit(part.transform, sprite, targetSize);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;
        return part;
    }

    static void Fit(Transform target, Sprite sprite, Vector2 targetSize)
    {
        Vector2 source = sprite.bounds.size;
        target.localScale = new Vector3(targetSize.x / source.x, targetSize.y / source.y, 1f);
    }

    static void ConfigureZombieTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { MaleFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 360f;
            importer.spritePivot = new Vector2(.5f, 0f);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 90;
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(.5f, 0f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    static Sprite[] LoadFrames(string state)
    {
        return AssetDatabase.FindAssets("t:Texture2D", new[] { MaleFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => Path.GetFileNameWithoutExtension(path).StartsWith(state + " ("))
            .OrderBy(FrameNumber)
            .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
            .Where(sprite => sprite != null)
            .ToArray();
    }

    static int FrameNumber(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        int open = name.LastIndexOf('(');
        int close = name.LastIndexOf(')');
        if (open < 0 || close <= open) return int.MaxValue;
        return int.TryParse(name.Substring(open + 1, close - open - 1), out int value) ? value : int.MaxValue;
    }

    static void ApplyZombiePrefab(string path, bool conehead, Sprite square, Sprite[] idle,
        Sprite[] walk, Sprite[] attack, Sprite[] dead)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform visual = root.transform.Find("Visual");
        if (visual == null)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
        }
        while (visual.childCount > 0) Object.DestroyImmediate(visual.GetChild(0).gameObject);
        visual.localPosition = new Vector3(0f, .015f, 0f);
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;
        GameObject body = new("Licensed Zombie Body");
        body.transform.SetParent(visual, false);
        SpriteRenderer renderer = body.AddComponent<SpriteRenderer>();
        renderer.sprite = walk[0];
        renderer.flipX = true;
        renderer.sortingLayerName = "Plants";
        renderer.sortingOrder = 16;
        ZombieSpriteAnimator animator = visual.GetComponent<ZombieSpriteAnimator>();
        if (animator == null) animator = visual.gameObject.AddComponent<ZombieSpriteAnimator>();
        animator.Configure(root.GetComponent<ZombieController>(), renderer, idle, walk, attack, dead, 10f);
        if (conehead)
        {
            Color orange = new(.96f, .39f, .06f);
            Part("Cone Brim", visual, square, orange, new Vector2(0f, 1.34f), new Vector2(.56f, .10f), 23);
            Part("Cone Lower", visual, square, orange, new Vector2(0f, 1.47f), new Vector2(.39f, .24f), 22);
            Part("Cone Upper", visual, square, new Color(1f, .51f, .09f), new Vector2(0f, 1.64f), new Vector2(.21f, .20f), 22);
        }
        ConfigureDepth(root, visual);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void Part(string name, Transform parent, Sprite sprite, Color color, Vector2 pos, Vector2 scale, int order)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = "Plants";
        renderer.sortingOrder = order;
    }
}
