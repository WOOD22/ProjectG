using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitMovement : MonoBehaviour
{
    public Tilemap floorTilemap; // 바닥 타일맵
    public Tilemap wallTilemap; // 벽 타일맵
    public Vector3Int unitPosition; // 유닛의 현재 위치

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int clickCell = floorTilemap.WorldToCell(worldPos);

            // 클릭한 위치가 바닥 타일이고 벽이 아닌지 확인
            if (floorTilemap.HasTile(clickCell) && !IsObstacle(clickCell))
            {
                List<Vector3Int> path = FindPath(unitPosition, clickCell);

                if (path != null)
                {
                    StartCoroutine(MoveAlongPath(path));
                }
            }
        }
    }

    List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        Queue<Vector3Int> openSet = new Queue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

        openSet.Enqueue(start);

        while (openSet.Count > 0)
        {
            Vector3Int current = openSet.Dequeue();

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            closedSet.Add(current);

            foreach (Vector3Int neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor) || IsObstacle(neighbor))
                    continue;

                if (!openSet.Contains(neighbor))
                {
                    openSet.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }
        return null; // 경로를 찾지 못한 경우
    }

    List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        List<Vector3Int> totalPath = new List<Vector3Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }
        return totalPath;
    }

    IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell)
    {
        yield return cell + new Vector3Int(1, 0, 0); // 오른쪽
        yield return cell + new Vector3Int(-1, 0, 0); // 왼쪽
        yield return cell + new Vector3Int(0, 1, 0); // 위
        yield return cell + new Vector3Int(0, -1, 0); // 아래
    }

    bool IsObstacle(Vector3Int cell)
    {
        // 벽 타일맵에 타일이 있으면 장애물로 간주
        return wallTilemap.HasTile(cell);
    }

    System.Collections.IEnumerator MoveAlongPath(List<Vector3Int> path)
    {
        foreach (Vector3Int cell in path)
        {
            Vector3 targetPos = floorTilemap.CellToWorld(cell);
            while (transform.position != targetPos)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * 5);
                yield return null;
            }
            unitPosition = cell;
        }
    }
}