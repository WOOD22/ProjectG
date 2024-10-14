using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileDataManager : MonoBehaviour
{
    public static TileDataManager Instance;

    private Dictionary<string, Dictionary<Vector3Int, TileInfo>> layerTileDataMap = new Dictionary<string, Dictionary<Vector3Int, TileInfo>>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SetTileInfo(Vector3Int position, TileInfo info, string layerName)
    {
        if (!layerTileDataMap.ContainsKey(layerName))
            layerTileDataMap[layerName] = new Dictionary<Vector3Int, TileInfo>();

        layerTileDataMap[layerName][position] = info;
    }

    public void RemoveTileInfo(Vector3Int position, string layerName)
    {
        if (layerTileDataMap.ContainsKey(layerName))
        {
            if (layerTileDataMap[layerName].ContainsKey(position))
                layerTileDataMap[layerName].Remove(position);
        }
    }

    public TileInfo GetTileInfo(Vector3Int position, string layerName)
    {
        if (layerTileDataMap.ContainsKey(layerName))
        {
            if (layerTileDataMap[layerName].ContainsKey(position))
                return layerTileDataMap[layerName][position];
        }
        return null;
    }
}
