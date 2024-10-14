using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilePlacer : MonoBehaviour
{
    public Tilemap tilemap; // 현재 활성화된 타일맵 (레이어)
    public TileData currentTileData; // 현재 선택된 타일 데이터
    public Camera mainCamera;

    public Tilemap previewTilemap; // 미리보기 타일맵 참조

    // 현재 선택된 툴
    public enum ToolType { Place, Remove }
    public ToolType currentTool;

    // 드래그 변수
    private bool isDragging = false;
    private Vector3Int dragStart;
    private Vector3Int dragEnd;

    // 미리보기 타일을 위한 변수
    private GameObject previewTile;
    private SpriteRenderer previewRenderer;

    void Start()
    {
        if (previewTilemap == null)
        {
            Debug.LogError("Preview Tilemap is not assigned.");
        }
    }

    void Update()
    {
        if (!IsPointerOverUI())
        {
            HandleInput();
            UpdatePreviewTile();
        }
        else
            ClearRectanglePreview();
    }

    // 입력 처리 메서드
    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 드래그 시작
            isDragging = true;
            dragStart = GetGridPosition();
            dragEnd = dragStart; // 초기화
        }

        if (isDragging)
        {
            if (Input.GetMouseButton(0))
            {
                // 드래그 중일 때 dragEnd 업데이트
                dragEnd = GetGridPosition();
                UpdateRectanglePreview();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                // 드래그 종료 및 채우기
                isDragging = false;
                FillRectangle(dragStart, dragEnd);
                ClearRectanglePreview();
            }
        }
    }

    void UpdatePreviewTile()
    {
        if (isDragging)
        {
            // 드래그 중일 때는 개별 미리보기를 표시하지 않음
            return;
        }

        if (currentTileData != null && tilemap != null)
        {
            Vector3Int gridPosition = GetGridPosition();
            if (gridPosition != null)
            {
                previewTilemap.ClearAllTiles();
                previewTilemap.SetTile(gridPosition, CreatePreviewTile());
            }
        }
        else
        {
            previewTilemap.ClearAllTiles();
        }
    }

    // 사각형 영역의 미리보기 업데이트
    void UpdateRectanglePreview()
    {
        if (previewTilemap == null)
            return;

        previewTilemap.ClearAllTiles();

        Vector3Int min = new Vector3Int(
            Mathf.Min(dragStart.x, dragEnd.x),
            Mathf.Min(dragStart.y, dragEnd.y),
            0 // z는 Sorting Order로 관리
        );

        Vector3Int max = new Vector3Int(
            Mathf.Max(dragStart.x, dragEnd.x),
            Mathf.Max(dragStart.y, dragEnd.y),
            0
        );

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                previewTilemap.SetTile(pos, CreatePreviewTile());
            }
        }
    }

    // 사각형 미리보기 타일 클리어
    public void ClearRectanglePreview()
    {
        if (previewTilemap != null)
        {
            previewTilemap.ClearAllTiles();
        }
    }

    // 사각형 영역 채우기 메서드
    void FillRectangle(Vector3Int start, Vector3Int end)
    {
        if (tilemap == null || currentTileData == null)
            return;

        Vector3Int min = new Vector3Int(
            Mathf.Min(start.x, end.x),
            Mathf.Min(start.y, end.y),
            0
        );

        Vector3Int max = new Vector3Int(
            Mathf.Max(start.x, end.x),
            Mathf.Max(start.y, end.y),
            0
        );

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (currentTool == ToolType.Place)
                {
                    PlaceTileAtPosition(pos);
                }
                else if (currentTool == ToolType.Remove)
                {
                    RemoveTileAtPosition(pos);
                }
            }
        }
    }

    // 특정 위치에 타일 배치
    void PlaceTileAtPosition(Vector3Int pos)
    {
        if (LayerManager.Instance.IsLayerLocked(tilemap.name))
            return;

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = currentTileData.tileSprite;
        tile.color = Color.white; // 원하는 색상 설정
        tilemap.SetTile(pos, tile);

        int layerHeight = LayerManager.Instance.GetLayerHeight(tilemap.name);

        TileInfo tileInfo = new TileInfo(currentTileData, layerHeight);
        TileDataManager.Instance.SetTileInfo(pos, tileInfo, tilemap.name);
    }

    // 특정 위치의 타일 제거
    void RemoveTileAtPosition(Vector3Int pos)
    {
        if (LayerManager.Instance.IsLayerLocked(tilemap.name))
            return;

        tilemap.SetTile(pos, null);
        TileDataManager.Instance.RemoveTileInfo(pos, tilemap.name);
    }

    // 미리보기 타일 생성
    Tile CreatePreviewTile()
    {
        Tile previewTile = ScriptableObject.CreateInstance<Tile>();
        previewTile.sprite = currentTileData.tileSprite;
        // 미리보기 타일의 색상을 반투명하게 설정
        previewTile.color = new Color(1f, 1f, 1f, 0.3f); // 더 낮은 알파값
        return previewTile;
    }

    // 마우스 위치를 그리드 위치로 변환
    Vector3Int GetGridPosition()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPosition = tilemap.WorldToCell(mouseWorldPos);
        gridPosition.z = 0;
        return gridPosition;
    }

    // UI 터치 방지를 위한 메서드
    bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
}