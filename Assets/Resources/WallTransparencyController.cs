using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WallTransparencyController : MonoBehaviour
{
    public Tilemap[] wallTilemaps;  // 각 레이어의 벽 타일맵 배열
    public float transparencyRadius = 1f;
    public float wallTransparency = 0.1f;

    private Dictionary<Vector3Int, Tile>[] originalWallTiles;  // 각 레이어별 원본 타일 저장
    private Camera mainCamera;
    public int currentLayer = 0;  // 현재 활성화된 레이어

    private void Start()
    {
        mainCamera = Camera.main;
        originalWallTiles = new Dictionary<Vector3Int, Tile>[wallTilemaps.Length];
        for (int i = 0; i < wallTilemaps.Length; i++)
        {
            originalWallTiles[i] = new Dictionary<Vector3Int, Tile>();
        }
    }

    private void Update()
    {
        UpdateWallTransparency();
    }

    public void SetCurrentLayer(int layer)
    {
        if (layer >= 0 && layer < wallTilemaps.Length)
        {
            currentLayer = layer;
        }
    }

    void UpdateWallTransparency()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector3Int centerCell = wallTilemaps[currentLayer].WorldToCell(mouseWorldPos);

        // 이전에 변경된 모든 타일을 원래 상태로 복원
        for (int layer = 0; layer <= currentLayer; layer++)
        {
            foreach (var kvp in originalWallTiles[layer])
            {
                wallTilemaps[layer].SetTile(kvp.Key, kvp.Value);
            }
            originalWallTiles[layer].Clear();
        }

        // 현재 레이어와 그 아래 레이어의 벽 타일 투명도 조정
        for (int layer = 0; layer <= currentLayer; layer++)
        {
            AdjustLayerTransparency(layer, centerCell, mouseWorldPos);
        }
    }

    void AdjustLayerTransparency(int layer, Vector3Int centerCell, Vector3 mouseWorldPos)
    {
        for (int x = -Mathf.CeilToInt(transparencyRadius); x <= Mathf.CeilToInt(transparencyRadius); x++)
        {
            for (int y = -Mathf.CeilToInt(transparencyRadius); y <= Mathf.CeilToInt(transparencyRadius); y++)
            {
                Vector3Int checkCell = centerCell + new Vector3Int(x, y, 0);
                if (Vector3.Distance(wallTilemaps[layer].GetCellCenterWorld(checkCell), mouseWorldPos) <= transparencyRadius)
                {
                    TileBase tile = wallTilemaps[layer].GetTile(checkCell);
                    if (tile != null)
                    {
                        Tile originalTile = tile as Tile;
                        if (originalTile != null)
                        {
                            originalWallTiles[layer][checkCell] = originalTile;

                            Tile transparentTile = ScriptableObject.CreateInstance<Tile>();
                            transparentTile.sprite = originalTile.sprite;
                            Color tileColor = originalTile.color;
                            tileColor.a = wallTransparency;
                            transparentTile.color = tileColor;

                            wallTilemaps[layer].SetTile(checkCell, transparentTile);
                        }
                    }
                }
            }
        }
    }
}
