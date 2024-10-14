using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntegratedAIScript : MonoBehaviour
{/*
    public bool enableChase = true; // 추적 가능 여부 (on/off)
    public bool enableMoveToAttack = true; // 공격 가능한 위치로 이동 여부 (on/off)
    public bool enableAttack = true; // 공격 여부 (on/off)

    public int moveAPCost = 2; // 이동 시 소모되는 AP
    public int attackAPCost = 3; // 공격 시 소모되는 AP
    public float detectionRange = 10f; // 적을 감지할 수 있는 범위

    private Unit unit;

    public LayerMask tileLayerMask;  // 타일이 있는 레이어
    public LayerMask unitLayerMask;  // 유닛이 있는 레이어

    private const float TileYScale = 0.5625f; // 타일 Y축 스케일 비율

    void Start()
    {
        unit = GetComponent<Unit>();
        unit.currentAP = unit.maxAP; // 턴 시작 시 AP를 최대치로 설정
    }

    public void StartTurn()
    {
        ResetAP();

        Unit closestEnemy = FindClosestEnemy();
        if (closestEnemy != null)
        {
            StartCoroutine(MoveAndAttack(closestEnemy));
        }
        else
        {
            EndTurnWithDelay(1f); // 목표가 없으면 턴 종료
        }
    }

    private void ResetAP()
    {
        unit.currentAP = unit.maxAP; // 턴 시작 시 AP를 회복
    }

    private Unit FindClosestEnemy()
    {
        Unit closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        string targetTag = unit.CompareTag("Ally") ? "Enemy" : "Ally";
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRange);

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(targetTag))
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = collider.GetComponent<Unit>();
                }
            }
        }

        return closestEnemy;
    }

    private IEnumerator MoveAndAttack(Unit target)
    {
        if (!IsInAttackRange(target) && enableMoveToAttack)
        {
            yield return StartCoroutine(MoveAlongAStarPath(target));
        }

        if (IsInAttackRange(target))
        {
            yield return StartCoroutine(Attack(target));
        }

        EndTurnWithDelay(1f);
    }

    private bool IsInAttackRange(Unit target)
    {
        return Vector3.Distance(transform.position, target.transform.position) <= unit.weapon.range;
    }

    private IEnumerator MoveAlongAStarPath(Unit target)
    {
        if (!enableChase) yield break;

        HashSet<Vector3> walkableTiles = GetWalkableTiles(target);
        Vector3 startPos = SnapToGrid(transform.position);
        Vector3 targetPos = SnapToGrid(target.transform.position);

        if (!walkableTiles.Contains(startPos))
        {
            Debug.LogError($"유닛의 시작 위치가 유효하지 않습니다. 위치: {startPos}");
            yield break;
        }

        List<Vector3> possibleTargetPositions = GetSurroundingTiles(targetPos, walkableTiles);
        if (possibleTargetPositions.Count == 0)
        {
            Debug.LogError("포위할 수 있는 위치가 없습니다.");
            yield break;
        }

        List<Vector3> path = FindShortestPath(startPos, possibleTargetPositions, walkableTiles);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("포위할 수 있는 경로를 찾을 수 없습니다.");
            yield break;
        }

        yield return MoveAlongPath(path);
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(Mathf.Round(position.x), Mathf.Round(position.y / TileYScale) * TileYScale, 0);
    }

    private List<Vector3> GetSurroundingTiles(Vector3 targetPos, HashSet<Vector3> walkableTiles)
    {
        List<Vector3> surroundingTiles = new List<Vector3>();

        Vector3[] directions = {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1 * TileYScale, 0),
            new Vector3(0, -1 * TileYScale, 0)
        };

        foreach (Vector3 dir in directions)
        {
            Vector3 neighbor = targetPos + dir;
            if (walkableTiles.Contains(neighbor))
            {
                surroundingTiles.Add(neighbor);
            }
        }

        return surroundingTiles;
    }

    private List<Vector3> FindShortestPath(Vector3 startPos, List<Vector3> possibleTargetPositions, HashSet<Vector3> walkableTiles)
    {
        List<Vector3> bestPath = null;
        float shortestDistance = Mathf.Infinity;

        foreach (Vector3 pos in possibleTargetPositions)
        {
            List<Vector3> tempPath = AStarPathfinding.FindPath(startPos, pos, walkableTiles);
            if (tempPath.Count > 0 && tempPath.Count < shortestDistance)
            {
                bestPath = tempPath;
                shortestDistance = tempPath.Count;
            }
        }

        return bestPath;
    }

    private IEnumerator MoveAlongPath(List<Vector3> path)
    {
        foreach (Vector3 step in path)
        {
            if (unit.currentAP >= moveAPCost)
            {
                Vector3 newPosition = SnapToGrid(step);
                Collider2D unitCollider = Physics2D.OverlapPoint(newPosition, unitLayerMask);

                if (unitCollider != null && unitCollider.gameObject != this.gameObject)
                {
                    Debug.LogWarning("유닛이 있어 이동할 수 없습니다.");
                    yield break;
                }

                transform.position = newPosition;
                unit.currentAP -= moveAPCost;

                Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z);
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                break;
            }
        }
    }

    private IEnumerator Attack(Unit target)
    {
        if (unit.weaponData != null && unit.weapon.tags.Contains("ZombieCrow"))
        {
            ZombieClaw zombieClaw = new ZombieClaw();
            zombieClaw.Execute(unit, target, FindObjectOfType<AttackManager>());
        }
        else
        {
            while (unit.currentAP >= attackAPCost)
            {
                unit.currentAP -= attackAPCost;

                Debug.Log($"{name}이(가) {target.name}을(를) 공격했습니다!");
                AttackManager attackManager = FindObjectOfType<AttackManager>();
                if (attackManager != null)
                {
                    attackManager.ProcessAttack(unit, target, 3, 20, 75);
                }

                yield return new WaitForSeconds(1f);

                if (target.currentHealth <= 0)
                {
                    Debug.Log($"{target.name}이(가) 파괴되었습니다.");
                    break;
                }
            }
        }
    }

    private HashSet<Vector3> GetWalkableTiles(Unit target)
    {
        HashSet<Vector3> walkableTiles = new HashSet<Vector3>();

        foreach (Collider2D tile in Physics2D.OverlapBoxAll(transform.position, new Vector2(100, 100 * TileYScale), 0, tileLayerMask))
        {
            if (tile != null)
            {
                Vector3 tilePosition = SnapToGrid(tile.transform.position);
                walkableTiles.Add(tilePosition);
            }
        }

        Collider2D[] unitColliders = Physics2D.OverlapBoxAll(transform.position, new Vector2(100, 100 * TileYScale), 0, unitLayerMask);
        foreach (Collider2D unitCollider in unitColliders)
        {
            if (unitCollider != null && unitCollider.gameObject != this.gameObject && unitCollider.gameObject != target.gameObject)
            {
                Vector3 unitPosition = SnapToGrid(unitCollider.transform.position);
                walkableTiles.Remove(unitPosition);
            }
        }

        return walkableTiles;
    }

    private void EndTurnWithDelay(float delay)
    {
        StartCoroutine(EndTurnAfterDelay(delay));
    }

    private IEnumerator EndTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndTurn();
    }

    private void EndTurn()
    {
        Debug.Log($"{name}의 턴이 종료되었습니다.");
        TurnManager turnManager = FindObjectOfType<TurnManager>();
        if (turnManager != null)
        {
            turnManager.EndTurn();
        }
    }*/
}