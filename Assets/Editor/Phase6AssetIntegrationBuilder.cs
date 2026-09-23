using System.IO;
using System.Linq;
using PvZ2.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase6AssetIntegrationBuilder
{
    const string MaleFolder = "Assets/Art/External/Imported/Zombies/pzUH_Zombie/Male";

    [MenuItem("Tools/PvZ2 Foundation/Build Phase 6 Licensed Assets")]
    public static void BuildPhase6()
    {
        Phase5PresentationBuilder.BuildPhase5();
        ApplyLicensedAssets();
    }

    [MenuItem("Tools/PvZ2 Foundation/Apply Phase 6 Licensed Assets")]
    public static void ApplyLicensedAssets()
    {
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
        AssetDatabase.SaveAssets();
        Debug.Log("PvZ2 Phase 6 licensed zombie assets integrated successfully.");
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
        return int.TryParse(name.Substring(open + 1, close - open - 1), out int value)
            ? value
            : int.MaxValue;
    }

    static void ApplyZombiePrefab(
        string path,
        bool conehead,
        Sprite square,
        Sprite[] idle,
        Sprite[] walk,
        Sprite[] attack,
        Sprite[] dead)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform visual = root.transform.Find("Visual");
        if (visual == null)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
        }
        while (visual.childCount > 0)
            Object.DestroyImmediate(visual.GetChild(0).gameObject);

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

        LaneDepthVisual depth = root.GetComponent<LaneDepthVisual>();
        if (depth != null) depth.Configure(visual);
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