using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads blocks.json and spawns prefabs once the first stroke provides a world anchor.
/// </summary>
public class BlockSpawner : MonoBehaviour
{
    [Serializable]
    public class BlockData
    {
        public int id;
        public string type;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public float depth;
        public int area;
    }

    [Serializable]
    public class BlockList
    {
        public BlockData[] blocks;
    }

    [Serializable]
    public class PrefabEntry
    {
        public string typeName;
        public GameObject prefab;
    }

    [Header("JSON Source")]
    public string jsonFileName = "blocks.json";

    [Header("Prefab Mapping")]
    public List<PrefabEntry> prefabMap = new List<PrefabEntry>();
    public GameObject defaultPrefab;

    [Header("Layout")]
    [Tooltip("Used for right/up/forward so JSON x,y spread in front of the user instead of at world origin.")]
    public Camera arCamera;

    // JSON positions are roughly normalised (~-1..1); fixed spread in metres (no inspector knobs).
    const float XyMetersPerNormUnit = 1.5f;
    const float ForwardFromZ = 0.2f;
    const float ZNeutral = 0.55f;

    private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
    private Dictionary<string, GameObject> _prefabLookup;

    private Vector3 _spawnOrigin;
    private bool _hasSpawnOrigin;

    /// <summary>All spawned instances are parented here; tweak local pos/rot/scale from UI to adjust the whole group.</summary>
    public Transform SpawnedBlocksRoot { get; private set; }

    void Awake()
    {
        var rootGo = new GameObject("SpawnedBlocksRoot");
        rootGo.transform.SetParent(transform, false);
        SpawnedBlocksRoot = rootGo.transform;
    }

    void Start()
    {
        BuildPrefabLookup();
    }

    /// <summary>Wire this to ARDrawManager → onFirstStrokeWorldOrigin (Vector3).</summary>
    public void OnFirstStrokeWorldOrigin(Vector3 worldOrigin)
    {
        _spawnOrigin = worldOrigin;
        _hasSpawnOrigin = true;
        SpawnFromJsonAtOrigin(worldOrigin);
    }

    public void RespawnBlocks()
    {
        if (!_hasSpawnOrigin)
        {
            Debug.LogWarning("[BlockSpawner] No first stroke yet — cannot respawn.");
            return;
        }
        SpawnFromJsonAtOrigin(_spawnOrigin);
    }

    public void SpawnAll()
    {
        RespawnBlocks();
    }

    public void ClearSpawned()
    {
        foreach (GameObject obj in _spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        _spawnedObjects.Clear();
    }

    private void BuildPrefabLookup()
    {
        _prefabLookup = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        foreach (PrefabEntry entry in prefabMap)
        {
            if (!string.IsNullOrEmpty(entry.typeName) && entry.prefab != null)
                _prefabLookup[entry.typeName] = entry.prefab;
        }
    }

    private void SpawnFromJsonAtOrigin(Vector3 origin)
    {
        ClearSpawned();

        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"[BlockSpawner] JSON not found: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        BlockList data = JsonUtility.FromJson<BlockList>(json);
        if (data?.blocks == null || data.blocks.Length == 0)
        {
            Debug.LogWarning("[BlockSpawner] No blocks in JSON.");
            return;
        }

        Transform cam = arCamera != null ? arCamera.transform : transform;
        Vector3 right = cam.right;
        Vector3 up = cam.up;
        Vector3 forward = cam.forward;

        foreach (BlockData block in data.blocks)
            SpawnBlock(block, origin, right, up, forward);
    }

    private void SpawnBlock(BlockData block, Vector3 origin, Vector3 right, Vector3 up, Vector3 forward)
    {
        GameObject prefab = ResolvePrefab(block.type);
        bool isDefault = prefab == null;

        GameObject obj = isDefault ? GameObject.CreatePrimitive(PrimitiveType.Cube) : Instantiate(prefab);
        obj.name = $"Block_{block.id}_{block.type}";

        Vector3 p = ArrayToVector3(block.position);
        obj.transform.position = origin
            + right * (p.x * XyMetersPerNormUnit)
            + up * (p.y * XyMetersPerNormUnit)
            + forward * ((p.z - ZNeutral) * ForwardFromZ);

        // Flip vertical vs device build: 180° around camera right so blocks aren’t upside down.
        Quaternion jsonRot = Quaternion.Euler(ArrayToVector3(block.rotation));
        obj.transform.rotation = Quaternion.AngleAxis(180f, right) * jsonRot;

        Vector3 scl = ArrayToVector3(block.scale);
        obj.transform.localScale = scl;

        obj.transform.SetParent(SpawnedBlocksRoot, true);

        if (isDefault)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                float t = Mathf.Clamp01(block.depth);
                rend.material.color = Color.Lerp(Color.red, Color.blue, t);
            }
        }

        _spawnedObjects.Add(obj);
    }

    private GameObject ResolvePrefab(string typeName)
    {
        if (_prefabLookup != null && _prefabLookup.TryGetValue(typeName, out GameObject prefab))
            return prefab;
        return defaultPrefab;
    }

    private static Vector3 ArrayToVector3(float[] arr)
    {
        if (arr == null || arr.Length < 3)
            return Vector3.zero;
        return new Vector3(arr[0], arr[1], arr[2]);
    }
}
