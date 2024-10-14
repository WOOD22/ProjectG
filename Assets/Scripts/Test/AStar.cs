using UnityEngine;
using System.Collections.Generic;

public class AStar : MonoBehaviour
{
    private static Grid grid;

    void Start()
    {
        grid = FindObjectOfType<Grid>();
    }

    public static List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, HashSet<Vector3Int> obstacles, System.Func<Vector3Int, bool> isWallFunc)
    {
        List<Vector3Int> openSet = new List<Vector3Int>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, int> gScore = new Dictionary<Vector3Int, int>();
        Dictionary<Vector3Int, int> fScore = new Dictionary<Vector3Int, int>();

        openSet.Add(start);
        gScore[start] = 0;
        fScore[start] = HeuristicCostEstimate(start, goal);

        while (openSet.Count > 0)
        {
            Vector3Int current = GetLowestFScore(openSet, fScore);
            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector3Int neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor) || obstacles.Contains(neighbor) || isWallFunc(neighbor))
                    continue;

                int tentativeGScore = gScore[current] + 1;

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
                else if (tentativeGScore >= gScore[neighbor])
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, goal);
            }
        }

        return null;
    }

    private static List<Vector3Int> GetNeighbors(Vector3Int cell)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>
        {
            cell + Vector3Int.up,
            cell + Vector3Int.right,
            cell + Vector3Int.down,
            cell + Vector3Int.left
        };

        return neighbors;
    }

    private static int HeuristicCostEstimate(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static Vector3Int GetLowestFScore(List<Vector3Int> openSet, Dictionary<Vector3Int, int> fScore)
    {
        Vector3Int lowest = openSet[0];
        foreach (Vector3Int item in openSet)
        {
            if (fScore[item] < fScore[lowest])
                lowest = item;
        }
        return lowest;
    }

    private static List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        List<Vector3Int> path = new List<Vector3Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}