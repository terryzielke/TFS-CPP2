using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor tool: Tools > Fill Terrain With Grass_1
// Adds the KayKit Grass_1 models as terrain detail meshes and paints them over the whole terrain.
// Tools > Set Terrain Base Color (Match Grass): gives the terrain a solid green ground layer.
public static class TerrainGrassFill
{
    private const string ModelsFolder = "Assets/_Art/KayKit_ForestNature/Models";
    private const string MaterialPath = "Assets/_Art/KayKit_ForestNature/Materials/Grass_Instanced.mat";
    private static readonly string[] GrassModels =
    {
        "Grass_1_A_Color1", "Grass_1_B_Color1", "Grass_1_C_Color1", "Grass_1_D_Color1"
    };

    // Per-layer density in coverage mode. 4 layers stack, so keep each one fairly low.
    private const float LayerDensity = 0.25f;

    [MenuItem("Tools/Fill Terrain With Grass_1")]
    private static void Fill()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Grass Fill", "No active Terrain found in the open scene.", "OK");
            return;
        }

        Material grassMat = GetOrCreateInstancedMaterial();
        List<GameObject> grass = new List<GameObject>();
        foreach (string name in GrassModels)
        {
            string path = $"{ModelsFolder}/{name}.fbx";
            RemapMaterials(path, grassMat);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null) grass.Add(model);
        }

        if (grass.Count == 0)
        {
            Debug.LogError($"No Grass_1 models found in {ModelsFolder}");
            return;
        }

        TerrainData data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Fill Terrain With Grass");

        // Reuse existing prototypes for these models so running this twice doesn't duplicate layers
        List<DetailPrototype> prototypes = data.detailPrototypes.ToList();
        List<int> layers = new List<int>();
        foreach (GameObject model in grass)
        {
            int index = prototypes.FindIndex(p => p.prototype == model);
            if (index < 0)
            {
                prototypes.Add(new DetailPrototype());
                index = prototypes.Count - 1;
            }

            prototypes[index] = new DetailPrototype
            {
                prototype = model,
                usePrototypeMesh = true,
                useInstancing = true,
                renderMode = DetailRenderMode.VertexLit,
                minWidth = 0.8f,
                maxWidth = 1.3f,
                minHeight = 0.8f,
                maxHeight = 1.3f,
                density = LayerDensity,
                alignToGround = 0f,
                useDensityScaling = true
            };
            layers.Add(index);
        }

        data.detailPrototypes = prototypes.ToArray();
        data.SetDetailScatterMode(DetailScatterMode.CoverageMode);

        // Full coverage (255) over every detail cell
        int[,] fullMap = new int[data.detailHeight, data.detailWidth];
        for (int y = 0; y < data.detailHeight; y++)
            for (int x = 0; x < data.detailWidth; x++)
                fullMap[y, x] = 255;

        foreach (int layer in layers)
        {
            data.SetDetailLayer(0, 0, layer, fullMap);
        }

        EditorUtility.SetDirty(data);
        Debug.Log($"Filled terrain '{terrain.name}' with {layers.Count} Grass_1 detail layers.");
    }

    private const string GroundFolder = "Assets/_Art/KayKit_ForestNature/Terrain";

    [MenuItem("Tools/Set Terrain Base Color (Match Grass)")]
    private static void SetBaseColor()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Terrain Color", "No active Terrain found in the open scene.", "OK");
            return;
        }

        Color color = SampleGrassColor();
        System.IO.Directory.CreateDirectory(GroundFolder);

        // Solid color texture for the layer
        string texPath = $"{GroundFolder}/GrassGround.png";
        Texture2D tex = new Texture2D(4, 4);
        tex.SetPixels(Enumerable.Repeat(color, 16).ToArray());
        System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(texPath);

        string layerPath = $"{GroundFolder}/GrassGround.terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, layerPath);
        }
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        layer.tileSize = new Vector2(10f, 10f);
        layer.smoothness = 0f;
        layer.metallic = 0f;
        EditorUtility.SetDirty(layer);

        // The first terrain layer is painted over the whole terrain by default
        TerrainData data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Set Terrain Base Color");
        List<TerrainLayer> layers = data.terrainLayers.Where(l => l != layer).ToList();
        layers.Insert(0, layer);
        data.terrainLayers = layers.ToArray();

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log($"Terrain base color set to #{ColorUtility.ToHtmlStringRGB(color)}");
    }

    // Averages the atlas texels under the Grass_1 mesh UVs so the ground matches the grass
    private static Color SampleGrassColor()
    {
        Color fallback = new Color(0.42f, 0.62f, 0.25f);

        // Load straight from file so the texture doesn't need Read/Write enabled
        Texture2D atlas = new Texture2D(2, 2);
        if (!atlas.LoadImage(System.IO.File.ReadAllBytes($"{ModelsFolder}/forest_texture.png"))) return fallback;

        MeshFilter mf = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/{GrassModels[0]}.fbx")?.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return fallback;

        Color sum = Color.black;
        Vector2[] uvs = mf.sharedMesh.uv;
        foreach (Vector2 uv in uvs) sum += atlas.GetPixelBilinear(uv.x, uv.y);
        Object.DestroyImmediate(atlas);

        if (uvs.Length == 0) return fallback;
        Color avg = sum / uvs.Length;
        avg.a = 1f;
        return avg;
    }

    // Terrain instanced details need a material with GPU instancing enabled,
    // and the FBX's embedded materials can't be edited, so make our own.
    private static Material GetOrCreateInstancedMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MaterialPath));

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{ModelsFolder}/forest_texture.png"));
        mat.SetFloat("_Smoothness", 0f);
        mat.enableInstancing = true;

        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }

    // Points every embedded material in the FBX at our instanced material
    private static void RemapMaterials(string path, Material mat)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;

        bool changed = false;
        foreach (Material embedded in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
        {
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), mat);
            changed = true;
        }

        if (changed) importer.SaveAndReimport();
    }
}
