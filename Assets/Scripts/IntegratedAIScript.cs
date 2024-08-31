using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntegratedAIScript : MonoBehaviour
{
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
        unit.currentAP = unit.maxAP; // 턴 시작 시 AP를 회복

        // 매 턴마다 가장 가까운 적을 탐색하고 경로를 설정
        Unit closestEnemy = FindClosestEnemy();

        if (closestEnemy != null)
        {
            StartCoroutine(MoveAndAttack(closestEnemy));
        }
        else
        {
            StartCoroutine(EndTurnWithDelay(1f)); // 목표가 없으면 턴 종료
        }
    }

    private Unit FindClosestEnemy()
    {
        Unit closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        string targetTag = unit.CompareTag("Ally") ? "Enemy" : "Ally"; // 자신의 태그에 따라 적군을 찾음

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
        bool hasMoved = false;  // 이동 여부를 추적하기 위한 플래그

        // 이미 공격 가능한 위치에 있는지 확인
        if (!IsInAttackRange(target) && enableMoveToAttack)
        {
            // 공격 가능한 위치가 아니라면 이동
            yield return StartCoroutine(MoveAlongAStarPath(target));
            hasMoved = true;
        }

        // 이동이 불가능했거나, 이동 후 사정거리 내에 적이 있는 경우 공격
        if ((hasMoved || IsInAttackRange(target)) && enableAttack && unit.currentAP >= attackAPCost)
        {
            yield return StartCoroutine(Attack(target));
        }

        // 공격 후 AP가 남아있으면 새로운 타겟을 찾아 계속 공격
        while (unit.currentAP >= attackAPCost)
        {
            Unit newTarget = FindClosestEnemy();
            if (newTarget != null && IsInAttackRange(newTarget))
            {
                yield return StartCoroutine(Attack(newTarget));
            }
            else
            {
                break;
            }
        }

        // 이동 및 공격 후 턴 종료
        StartCoroutine(EndTurnWithDelay(1f));
    }

    private bool IsInAttackRange(Unit target)
    {
        if (unit.weapon.durability <= 0)
        {
            Debug.LogWarning($"{name} has no weapon equipped. Cannot check attack range.");
            return false; // 무기가 없으면 공격할 수 없음
        }

        return Vector3.Distance(transform.position, target.transform.position) <= unit.weapon.range;
    }

    private IEnumerator MoveAlongAStarPath(Unit target)
    {
        if (!enableChase) yield break; // 추적 기능이 비활성화된 경우 이동하지 않음

        HashSet<Vector3> walkableTiles = GetWalkableTiles(target);

        // 유닛의 현재 위치를 타일 그리드에 맞춰 스냅
        Vector3 startPos = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y / TileYScale) * TileYScale, 0);
        Vector3 targetPos = new Vector3(Mathf.Round(target.transform.position.x), Mathf.Round(target.transform.position.y / TileYScale) * TileYScale, 0);

        if (!walkableTiles.Contains(startPos))
        {
            Debug.LogError($"유닛의 시작 위치가 유효하지 않습니다. 위치: {startPos}");
            yield break;
        }

        // 목표 유닛을 포위할 수 있는 위치를 찾음 (목표 유닛 주변의 4방향 타일)
        List<Vector3> possibleTargetPositions = GetSurroundingTiles(targetPos, walkableTiles);

        if (possibleTargetPositions.Count == 0)
        {
            Debug.LogError("포위할 수 있는 위치가 없습니다.");
            yield break;
        }

        // 가능한 포위 위치 중에서 가장 가까운 위치로 경로를 계산
        List<Vector3> path = null;
        float shortestDistance = Mathf.Infinity;
        foreach (Vector3 pos in possibleTargetPositions)
        {
            List<Vector3> tempPath = AStarPathfinding.FindPath(startPos, pos, walkableTiles);
            if (tempPath.Count > 0 && tempPath.Count < shortestDistance)
            {
                path = tempPath;
                shortestDistance = tempPath.Count;
            }
        }

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("포위할 수 있는 경로를 찾을 수 없습니다.");
            yield break;
        }

        Debug.Log("포위할 수 있는 경로를 찾았습니다. 이동을 시작합니다.");

        foreach (Vector3 step in path)
        {
            if (unit.currentAP >= moveAPCost)
            {
                Vector3 newPosition = new Vector3(Mathf.Round(step.x), Mathf.Round(step.y / TileYScale) * TileYScale, 0);

                Collider2D unitCollider = Physics2D.OverlapPoint(newPosition, unitLayerMask);
                if (unitCollider != null && unitCollider.gameObject != this.gameObject)
                {
                    Debug.LogWarning("유닛이 있어 이동할 수 없습니다.");
                    yield break;
                }

                transform.position = newPosition;
                unit.currentAP -= moveAPCost;

                Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z);

                yield return new WaitForSeconds(0.2f); // 0.2초 대기
            }
            else
            {
                break;
            }
        }
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

    private IEnumerator Attack(Unit target)
    {
        // 유닛이 "ZombieCrow" 태그가 달린 무기를 가지고 있는지 확인
        if (unit.weapon.durability > 0 && unit.weapon.tags.Contains("ZombieCrow"))
        {
            // ZombieClaw 공격 옵션 실행
            ZombieClaw zombieClaw = new ZombieClaw();

            // AP와 스태미나가 충분한지 확인
            if (unit.currentAP >= 3 && unit.currentStamina >= 30)
            {
                zombieClaw.Execute(unit, target, FindObjectOfType<AttackManager>());
            }
            else
            {
                Debug.Log("AP 또는 스태미나가 부족하여 ZombieClaw 공격을 중단합니다.");
                yield break; // 충분하지 않다면 루프 종료
            }
        }
        else
        {
            // 기존 공격 로직
            while (unit.currentAP >= attackAPCost && unit.currentStamina >= 15) // 여기서 스태미나도 함께 확인
            {
                unit.currentAP -= attackAPCost;
                unit.currentStamina -= 15; // 스태미나 소모

                Debug.Log($"{name}이(가) {target.name}을(를) 공격했습니다!");

                // 공격 처리
                AttackManager attackManager = FindObjectOfType<AttackManager>();
                if (attackManager != null)
                {
                    attackManager.ProcessAttack(unit, target, 3, 20, 75);
                }

                yield return new WaitForSeconds(1f); // 1초 대기 후 다음 공격 시도

                if (target.currentHealth <= 0)
                {
                    Debug.Log($"{target.name}이(가) 파괴되었습니다.");
                    break;
                }
            }

            // AP나 스태미나가 부족한 경우 무한 루프 방지
            if (unit.currentAP < attackAPCost || unit.currentStamina < 15)
            {
                Debug.Log("AP 또는 스태미나가 부족하여 공격을 중단합니다.");
                yield break;
            }
        }
    }

    private HashSet<Vector3> GetWalkableTiles(Unit target)
    {
        HashSet<Vector3> walkableTiles = new HashSet<Vector3>();

        // 맵 전체를 스캔하여 타일 정보를 수집합니다.
        foreach (Collider2D tile in Physics2D.OverlapBoxAll(transform.position, new Vector2(100, 100 * TileYScale), 0, tileLayerMask))
        {
            if (tile != null)
            {
                Vector3 tilePosition = new Vector3(Mathf.Round(tile.transform.position.x), Mathf.Round(tile.transform.position.y / TileYScale) * TileYScale, 0);
                walkableTiles.Add(tilePosition);
            }
        }

        // 현재 유닛을 제외한 모든 유닛을 장애물로 간주합니다.
        Collider2D[] unitColliders = Physics2D.OverlapBoxAll(transform.position, new Vector2(100, 100 * TileYScale), 0, unitLayerMask);
        foreach (Collider2D unitCollider in unitColliders)
        {
            if (unitCollider != null && unitCollider.gameObject != this.gameObject && unitCollider.gameObject != target.gameObject)
            {
                Vector3 unitPosition = new Vector3(Mathf.Round(unitCollider.transform.position.x), Mathf.Round(unitCollider.transform.position.y / TileYScale) * TileYScale, 0);
                walkableTiles.Remove(unitPosition); // 유닛의 위치를 장애물로 처리
            }
        }

        return walkableTiles;
    }

    private IEnumerator EndTurnWithDelay(float delay)
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
            turnManager.EndTurn(); // 턴 매니저에 턴 종료를 알림
        }
    }
}