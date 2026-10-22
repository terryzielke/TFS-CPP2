using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor tool: Tools > Forest Scatter
// Randomly places KayKit nature models on the active Terrain in the open scene.
public class ForestScatterWindow : EditorWindow
{
    private const string ModelsFolder = "Assets/_Art/KayKit_ForestNature/Models";
    private const string RootName = "Environment_Forest";

    private int treeCount = 40;
    private int bushCount = 40;
    private int rockCount = 30;
    private int grassCount = 120;

    private float edgePadding = 5f;
    private Vector2 scaleRange = new Vector2(0.8f, 1.3f);
    private float clearRadius = 8f; // keeps the area around the scene origin / player spawn open
    private Vector3 clearCenter = Vector3.zero;
    private bool addColliders = true;
    private int seed = 12345;

    [MenuItem("Tools/Forest Scatter")]
    private static void Open()
    {
        GetWindow<ForestScatterWindow>("Forest Scatter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Counts", EditorStyles.boldLabel);
        treeCount = EditorGUILayout.IntField("Trees", treeCount);
        bushCount = EditorGUILayout.IntField("Bushes", bushCount);
        rockCount = EditorGUILayout.IntField("Rocks", rockCount);
        grassCount = EditorGUILayout.IntField("Grass", grassCount);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
        edgePadding = EditorGUILayout.FloatField("Edge Padding", edgePadding);
        scaleRange = EditorGUILayout.Vector2Field("Scale Min/Max", scaleRange);
        clearCenter = EditorGUILayout.Vector3Field("Clear Area Center", clearCenter);
        clearRadius = EditorGUILayout.FloatField("Clear Area Radius", clearRadius);
        addColliders = EditorGUILayout.Toggle("Colliders (trees/rocks)", addColliders);
        seed = EditorGUILayout.IntField("Random Seed", seed);

        EditorGUILayout.Space();
        if (GUILayout.Button("Scatter", GUILayout.Height(30))) Scatter();
        if (GUILayout.Button("Clear Scattered Objects")) ClearExisting();
    }

    private void Scatter()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Forest Scatter", "No active Terrain found in the open scene.", "OK");
            return;
        }

        ClearExisting();
        Random.InitState(seed);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Forest Scatter");

        ScatterCategory(terrain, root.transform, "Trees", LoadModels("Tree_"), treeCount, addColliders);
        ScatterCategory(terrain, root.transform, "Bushes", LoadModels("Bush_"), bushCount, false);
        ScatterCategory(terrain, root.transform, "Rocks", LoadModels("Rock_"), rockCount, addColliders);
        ScatterCategory(terrain, root.transform, "Grass", LoadModels("Grass_", "_Color1"), grassCount, false);
    }

    // Finds models whose file name starts with prefix (and optionally contains a filter)
    private List<GameObject> LoadModels(string prefix, string mustContain = null)
    {
        return AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => System.IO.Path.GetFileName(p).StartsWith(prefix))
            .Where(p => mustContain == null || p.Contains(mustContain))
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(go => go != null)
            .ToList();
    }

    private void ScatterCategory(Terrain terrain, Transform root, string name, List<GameObject> models, int count, bool collider)
    {
        if (models.Count == 0 || count <= 0) return;

        Transform parent = new GameObject(name).transform;
        parent.SetParent(root, false);

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        int placed = 0;
        int attempts = 0;
        while (placed < count && attempts < count * 10)
        {
            attempts++;

            float x = Random.Range(origin.x + edgePadding, origin.x + size.x - edgePadding);
            float z = Random.Range(origin.z + edgePadding, origin.z + size.z - edgePadding);
            Vector3 pos = new Vector3(x, 0f, z);

            Vector3 flatCenter = new Vector3(clearCenter.x, 0f, clearCenter.z);
            if (Vector3.Distance(pos, flatCenter) < clearRadius) continue;

            pos.y = terrain.SampleHeight(pos) + origin.y;

            GameObject prefab = models[Random.Range(0, models.Count)];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            instance.transform.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
            instance.isStatic = true;

            if (collider)
            {
                foreach (MeshFilter mf in instance.GetComponentsInChildren<MeshFilter>())
                {
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
            }

            placed++;
        }
    }

    private void ClearExisting()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
    }
}
