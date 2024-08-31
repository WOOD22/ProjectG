using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinding
{
    private const float TileYScale = 0.5625f; // 타일 Y축 스케일 비율

    public static List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos, HashSet<Vector3> walkableTiles)
    {
        if (!walkableTiles.Contains(startPos) || !walkableTiles.Contains(targetPos))
        {
            Debug.LogError("시작점 또는 목표점이 유효하지 않습니다.");
            return new List<Vector3>();
        }

        List<Vector3> openSet = new List<Vector3>();
        HashSet<Vector3> closedSet = new HashSet<Vector3>();

        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        Dictionary<Vector3, float> gCost = new Dictionary<Vector3, float>();
        Dictionary<Vector3, float> fCost = new Dictionary<Vector3, float>();

        openSet.Add(startPos);
        gCost[startPos] = 0;
        fCost[startPos] = Vector3.Distance(startPos, targetPos);

        while (openSet.Count > 0)
        {
            Vector3 current = GetLowestFCostNode(openSet, fCost);

            if (current == targetPos)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector3 neighbor in GetNeighbors(current, walkableTiles))
            {
                if (closedSet.Contains(neighbor)) continue;

                float tentativeGCost = gCost[current] + Vector3.Distance(current, neighbor);
                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGCost >= gCost[neighbor])
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gCost[neighbor] = tentativeGCost;
                fCost[neighbor] = gCost[neighbor] + Vector3.Distance(neighbor, targetPos);
            }
        }

        Debug.LogWarning("경로를 찾을 수 없습니다.");
        return new List<Vector3>(); // 경로를 찾지 못했을 때 빈 리스트 반환
    }

    private static Vector3 GetLowestFCostNode(List<Vector3> openSet, Dictionary<Vector3, float> fCost)
    {
        Vector3 lowest = openSet[0];
        float lowestCost = fCost[lowest];

        foreach (Vector3 node in openSet)
        {
            if (fCost[node] < lowestCost)
            {
                lowest = node;
                lowestCost = fCost[lowest];
            }
        }

        return lowest;
    }

    private static List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> cameFrom, Vector3 current)
    {
        List<Vector3> path = new List<Vector3>();
        while (cameFrom.ContainsKey(current))
        {
            path.Insert(0, current);
            current = cameFrom[current];
        }
        return path;
    }

    private static List<Vector3> GetNeighbors(Vector3 node, HashSet<Vector3> walkableTiles)
    {
        List<Vector3> neighbors = new List<Vector3>();

        Vector3[] directions = {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1 * TileYScale, 0),
            new Vector3(0, -1 * TileYScale, 0),
            new Vector3(1, 1 * TileYScale, 0),
            new Vector3(1, -1 * TileYScale, 0),
            new Vector3(-1, 1 * TileYScale, 0),
            new Vector3(-1, -1 * TileYScale, 0),
        };

        foreach (Vector3 dir in directions)
        {
            Vector3 neighbor = node + dir;
            if (walkableTiles.Contains(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }
}