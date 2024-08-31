using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileMapGenerator : MonoBehaviour
{
    public GameObject tilePrefab;  // 타일 프리팹
    public Transform parent;
    public int mapWidth = 32;      // 타일맵 너비
    public int mapHeight = 32;     // 타일맵 높이
    public float tileSpacing = 1.0f; // 타일 간 간격

    void Start()
    {
        GenerateTileMap();
    }

    void GenerateTileMap()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // 타일 생성
                GameObject tile = Instantiate(tilePrefab, new Vector3(x * tileSpacing, y * tileSpacing * 0.5625f, 0), Quaternion.identity);

                // 부모 오브젝트 설정
                tile.transform.parent = parent;

                // 타일 이름 설정 (선택 사항)
                tile.name = $"Tile_{x}_{y}";
            }
        }
    }
}