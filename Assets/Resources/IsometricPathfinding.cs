using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class IsometricPathfinding : MonoBehaviour
{
    public WallTransparencyController wallTransparencyController;

    public Tilemap[] groundTilemaps; // 여러 레이어의 지면 타일맵
    public Tilemap[] wallTilemaps;   // 여러 레이어의 벽 타일맵
    public Tilemap[] objectTilemaps; // 여러 레이어의 오브젝트(계단 포함) 타일맵
    public GameObject unit;
    public GameObject[] layerObjects; // 각 레이어의 게임 오브젝트

    private Vector3Int[] directions = new Vector3Int[]
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0)
    };

    private bool isMoving = false;
    private Vector3Int currentGoal;
    private Color originalTileColor;
    public int currentLayer = 0;

    void Start()
    {
        // 초기 레이어 설정
        SetLayerVisibility(currentLayer);
    }

    void Update()
    {
        if (!isMoving && Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    void HandleMouseClick()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector3Int clickedCell = groundTilemaps[currentLayer].WorldToCell(mouseWorldPos);

        if (IsValidDestination(clickedCell))
        {
            Vector3Int startCell = groundTilemaps[currentLayer].WorldToCell(unit.transform.position);
            List<Vector3Int> path = FindPath(startCell, clickedCell);

            if (path != null && path.Count > 0)
            {
                currentGoal = clickedCell;
                HighlightTile(clickedCell, Color.green);
                StartCoroutine(MoveAlongPath(path));
            }
        }
    }

    bool IsValidDestination(Vector3Int cell)
    {
        return groundTilemaps[currentLayer].cellBounds.Contains(cell) &&
               groundTilemaps[currentLayer].HasTile(cell) &&
               !wallTilemaps[currentLayer].HasTile(cell);
    }

    void HighlightTile(Vector3Int tilePosition, Color color)
    {
        TileBase tile = groundTilemaps[currentLayer].GetTile(tilePosition);
        if (tile != null)
        {
            originalTileColor = groundTilemaps[currentLayer].GetColor(tilePosition);
            groundTilemaps[currentLayer].SetTileFlags(tilePosition, TileFlags.None);
            groundTilemaps[currentLayer].SetColor(tilePosition, color);
        }
    }

    void ResetTileColor(Vector3Int tilePosition)
    {
        groundTilemaps[currentLayer].SetColor(tilePosition, originalTileColor);
        groundTilemaps[currentLayer].SetTileFlags(tilePosition, TileFlags.None);
    }

    Vector3Int WorldToCell(Vector3 worldPosition)
    {
        Vector3 cellSize = groundTilemaps[currentLayer].cellSize;
        Matrix4x4 isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));
        Vector3 isoPosition = isoMatrix.MultiplyPoint3x4(worldPosition);
        int x = Mathf.FloorToInt(isoPosition.x / cellSize.x);
        int y = Mathf.FloorToInt(isoPosition.y / cellSize.y);
        return new Vector3Int(x, y, 0);
    }

    List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        var openSet = new List<Vector3Int> { start };
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, float> { { start, 0 } };
        var fScore = new Dictionary<Vector3Int, float> { { start, HeuristicCostEstimate(start, goal) } };

        while (openSet.Count > 0)
        {
            var current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (fScore[openSet[i]] < fScore[current])
                    current = openSet[i];
            }

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            foreach (var direction in directions)
            {
                var neighbor = current + direction;

                if (!IsValidMove(current, neighbor))
                    continue;

                var tentativeGScore = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    bool IsValidMove(Vector3Int from, Vector3Int to)
    {
        if (!groundTilemaps[currentLayer].HasTile(to) || wallTilemaps[currentLayer].HasTile(to))
            return false;

        // 현재 레이어의 오브젝트 타일맵에서 계단 타일 확인
        TileBase objectTile = objectTilemaps[currentLayer].GetTile(to);
        if (objectTile is StairTile stairTile)
        {
            return (to - from) == stairTile.GetValidEntryDirection();
        }

        return true;
    }

    float HeuristicCostEstimate(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        var path = new List<Vector3Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    IEnumerator MoveAlongPath(List<Vector3Int> path)
    {
        isMoving = true;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3Int cell = path[i];
            Vector3 worldPosition = groundTilemaps[currentLayer].GetCellCenterWorld(cell);

            while (Vector3.Distance(unit.transform.position, worldPosition) > 0.1f)
            {
                unit.transform.position = Vector3.MoveTowards(unit.transform.position, worldPosition, 5f * Time.deltaTime);
                yield return null;
            }

            TileBase objectTile = objectTilemaps[currentLayer].GetTile(cell);
            if (objectTile is StairTile stairTile)
            {
                int previousLayer = currentLayer;
                int newLayer = stairTile.targetLayer;

                yield return StartCoroutine(TransitionLayerEffect(previousLayer, newLayer));

                currentLayer = newLayer;
                UpdateUnitRenderingOrder();

                // 계단 출구 위치 계산
                Vector3Int exitPosition = stairTile.GetExitPosition(cell);

                // 유닛을 출구 위치로 즉시 이동
                unit.transform.position = groundTilemaps[currentLayer].GetCellCenterWorld(exitPosition);

                // 경로에 출구 위치 추가 (만약 다음 위치가 있다면 그 앞에 삽입)
                if (i + 1 < path.Count)
                {
                    path.Insert(i + 1, exitPosition);
                }
            }
        }

        ResetTileColor(currentGoal);
        isMoving = false;
    }

    IEnumerator TransitionLayerEffect(int fromLayer, int toLayer)
    {
        if (toLayer > fromLayer)
        {
            // 상위 레이어로 이동할 때만 페이드 효과 적용
            yield return StartCoroutine(FadeLayer(toLayer, 0f, 1f));
        }

        // 레이어 가시성 변경
        SetLayerVisibility(toLayer);
    }

    void SetLayerVisibility(int visibleLayer)
    {
        for (int i = 0; i < layerObjects.Length; i++)
        {
            bool isVisible = i <= visibleLayer;
            layerObjects[i].SetActive(isVisible);

            // 각 타일맵의 TilemapCollider2D 컴포넌트 활성화/비활성화
            SetTilemapColliderState(groundTilemaps[i], isVisible);
            SetTilemapColliderState(wallTilemaps[i], isVisible);
            SetTilemapColliderState(objectTilemaps[i], isVisible);
        }
    }

    void SetTilemapColliderState(Tilemap tilemap, bool isActive)
    {
        TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            collider.enabled = isActive;
        }
    }

    IEnumerator FadeLayer(int layer, float startAlpha, float endAlpha)
    {
        float fadeDuration = 0.5f;
        float elapsedTime = 0f;

        Renderer[] renderers = layerObjects[layer].GetComponentsInChildren<Renderer>();

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);

            foreach (Renderer renderer in renderers)
            {
                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }

            yield return null;
        }
    }

    void UpdateUnitRenderingOrder()
    {
        // 유닛의 Sprite Renderer 컴포넌트를 가져옵니다
        SpriteRenderer unitRenderer = unit.GetComponent<SpriteRenderer>();
        if (unitRenderer != null)
        {
            // 현재 레이어에 따라 정렬 순서를 설정합니다
            unitRenderer.sortingOrder = (currentLayer + 1) * 2 - 1;
        }
    }
}
