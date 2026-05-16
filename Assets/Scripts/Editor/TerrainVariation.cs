using UnityEngine;
using UnityEditor;

/// <summary>
/// Applique trois Terrain Layers utilisant la même texture martienne
/// à des tailles de tuile différentes (200 / 40 / 15), mélangés par
/// Perlin Noise multi-échelle. Résultat : sol 100% martien/orangé,
/// sans répétition visible quelle que soit la distance.
/// Run via : Tools → Racebird → Apply Terrain Variation
/// Supprimer ce fichier après exécution.
/// </summary>
public static class TerrainVariation
{
    private const string DiffusePath = "Assets/CyberRace/Source/Terrain/Textures/T_RockyPath2.png";
    private const string NormalPath  = "Assets/CyberRace/Source/Terrain/Textures/T_RockyPath2_normal.png";

    private const string Layer0Asset = "Assets/CyberRace/Source/Terrain/Layers/T_Rocky_200.terrainlayer";
    private const string Layer1Asset = "Assets/CyberRace/Source/Terrain/Layers/T_Rocky_40.terrainlayer";
    private const string Layer2Asset = "Assets/CyberRace/Source/Terrain/Layers/T_Rocky_15.terrainlayer";

    [MenuItem("Tools/Racebird/Apply Terrain Variation")]
    public static void ApplyVariation()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains.Length == 0)
        {
            Debug.LogError("[TerrainVariation] Aucun Terrain actif trouvé dans la scène.");
            return;
        }

        Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(DiffusePath);
        Texture2D normal  = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        if (diffuse == null)
        {
            Debug.LogError("[TerrainVariation] Texture introuvable : " + DiffusePath);
            return;
        }

        TerrainLayer layer0 = EnsureLayer(Layer0Asset, diffuse, normal, 200f);
        TerrainLayer layer1 = EnsureLayer(Layer1Asset, diffuse, normal,  40f);
        TerrainLayer layer2 = EnsureLayer(Layer2Asset, diffuse, normal,  15f);
        AssetDatabase.SaveAssets();

        float ox = Random.Range(0f, 9999f);
        float oz = Random.Range(0f, 9999f);

        foreach (Terrain terrain in terrains)
        {
            TerrainData td = terrain.terrainData;
            td.terrainLayers = new TerrainLayer[] { layer0, layer1, layer2 };

            int res = td.alphamapResolution;
            float[,,] maps = new float[res, res, 3];

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float nx = x / (float)res;
                    float ny = y / (float)res;

                    float w0 = 0.5f;
                    float w1 = Mathf.PerlinNoise(ox + nx / 0.008f, oz + ny / 0.008f) * 0.35f;
                    float w2 = Mathf.PerlinNoise(ox * 3f + nx / 0.04f, oz * 3f + ny / 0.04f) * 0.15f;

                    float sum = w0 + w1 + w2;
                    maps[y, x, 0] = w0 / sum;
                    maps[y, x, 1] = w1 / sum;
                    maps[y, x, 2] = w2 / sum;
                }
            }

            td.SetAlphamaps(0, 0, maps);
            td.RefreshPrototypes();
            terrain.Flush();
            EditorUtility.SetDirty(td);
            Debug.Log($"[TerrainVariation] '{terrain.name}' — 3 layers (200/40/15) appliqués.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[TerrainVariation] Terminé. Tu peux supprimer Assets/Scripts/Editor/TerrainVariation.cs.");
    }

    private static TerrainLayer EnsureLayer(string assetPath, Texture2D diffuse, Texture2D normal, float tileSize)
    {
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(assetPath);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, assetPath);
        }

        layer.diffuseTexture   = diffuse;
        layer.normalMapTexture = normal;
        layer.tileSize         = new Vector2(tileSize, tileSize);
        layer.tileOffset       = Vector2.zero;
        layer.normalScale      = 1f;
        layer.metallic         = 0f;
        layer.smoothness       = 0.05f;

        EditorUtility.SetDirty(layer);
        return layer;
    }
}
