using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MovementManager : MonoBehaviour
{
    public LayerMask tileLayerMask; // 타일이 있는 레이어
    public LayerMask unitLayerMask; // 유닛이 있는 레이어
    public int moveAPCost = 2; // 이동 시 소모되는 AP

    private List<Vector3> currentPath = new List<Vector3>(); // 현재 선택한 경로
    private bool isTurnActive = false; // 현재 턴 여부

    private Unit currentTurnUnit; // 현재 턴의 유닛
    private Vector3 highlightedTilePosition = Vector3.zero; // 하이라이트된 타일의 위치

    private AttackManager attackManager; // 공격 매니저 참조

    private const float TileYScale = 0.5625f; // 타일 Y축 스케일 비율

    public void AdjustUnitZPosition(Transform unitTransform)
    {
        float yPos = unitTransform.position.y;
        float zPos = Mathf.Lerp(-1f, -0.1f, yPos / 10f); // Y축 값에 따라 Z축 값을 보간합니다.
        unitTransform.position = new Vector3(unitTransform.position.x, yPos, zPos);
    }

    private void Awake()
    {
        attackManager = FindObjectOfType<AttackManager>(); // AttackManager를 찾아서 참조
    }

    // 현재 턴의 유닛을 설정하는 메서드
    public void SetCurrentTurnUnit(Unit unit)
    {
        currentTurnUnit = unit;
        AdjustUnitZPosition(currentTurnUnit.transform); // 턴이 시작될 때 위치 조정
        StartTurn(); // 턴이 시작되면 자동으로 활성화
    }

    // 유닛의 턴 시작 시 호출
    public void StartTurn()
    {
        isTurnActive = true;
        currentTurnUnit.RecoverAP(); // 매 턴 시작 시 AP 회복
    }

    // 유닛의 턴 종료 시 호출
    public void EndTurn()
    {
        isTurnActive = false;
        ClearPathHighlight(); // 턴 종료 시 경로 하이라이트 제거
        highlightedTilePosition = Vector3.zero; // 하이라이트된 타일 위치 초기화

        // 공격 매니저의 공격 모드도 취소
        AttackManager attackManager = FindObjectOfType<AttackManager>();
        if (attackManager != null)
        {
            attackManager.OnTurnEnd(); // 턴 종료 시 공격 취소
        }
    }

    // 경로 하이라이트를 초기화하는 메서드
    public void ClearPathHighlight()
    {
        foreach (Vector3 tilePos in currentPath)
        {
            HighlightTile(tilePos, false, false); // 기존 경로의 하이라이트 제거
        }
        currentPath.Clear(); // 경로 리스트 초기화
    }

    private void HighlightTile(Vector3 position, bool highlight, bool overAP)
    {
        Collider2D tileCollider = Physics2D.OverlapPoint(position, tileLayerMask);
        if (tileCollider != null)
        {
            Transform tile = tileCollider.transform;
            if (tile.childCount > 0)
            {
                if (highlight)
                {
                    if (overAP && tile.childCount > 2) // AP 초과 타일 하이라이트
                    {
                        tile.GetChild(2).gameObject.SetActive(true);
                    }
                    else
                    {
                        tile.GetChild(0).gameObject.SetActive(true);
                    }
                }
                else
                {
                    for (int i = 0; i < tile.childCount; i++)
                    {
                        tile.GetChild(i).gameObject.SetActive(false); // 모든 자식 비활성화
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (isTurnActive)
        {
            // UI가 클릭된 경우를 감지하여 타일 클릭을 무시하도록 설정
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 타일 클릭 시 경로 하이라이트
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                targetPosition.z = 0;

                // 타일의 중앙 위치로 스냅
                targetPosition = new Vector3(Mathf.Round(targetPosition.x), Mathf.Round(targetPosition.y / TileYScale) * TileYScale, 0);

                HashSet<Vector3> walkableTiles = GetWalkableTiles();

                // 시작점과 목표점이 유효한지 확인
                Vector3 startPos = new Vector3(Mathf.Round(currentTurnUnit.transform.position.x), Mathf.Round(currentTurnUnit.transform.position.y / TileYScale) * TileYScale, 0);
                if (!walkableTiles.Contains(startPos))
                {
                    Debug.LogError($"유닛의 시작 위치가 유효하지 않습니다. 위치: {startPos}");
                    return;
                }

                if (!walkableTiles.Contains(targetPosition))
                {
                    Debug.LogError($"목표 위치가 유효하지 않습니다. 위치: {targetPosition}");
                    return;
                }

                // 경로 설정
                if (targetPosition == highlightedTilePosition)
                {
                    // 같은 타일을 다시 클릭하면 이동 시작
                    StartCoroutine(MoveAlongPath(currentPath));  // 경로에 따라 이동 시작
                }
                else
                {
                    // 기존 경로 하이라이트 해제
                    ClearPathHighlight();

                    // 새로운 경로 설정 및 하이라이트
                    highlightedTilePosition = targetPosition;
                    currentPath = AStarPathfinding.FindPath(startPos, targetPosition, walkableTiles);
                    int apSpent = 0;
                    foreach (Vector3 step in currentPath)
                    {
                        apSpent += moveAPCost;
                        bool overAP = apSpent > currentTurnUnit.currentAP;
                        HighlightTile(step, true, overAP); // 경로 하이라이트
                    }
                }
            }

            // 오른쪽 마우스 버튼을 누르면 취소
            if (Input.GetMouseButtonDown(1))
            {
                // 하이라이트 해제
                ClearPathHighlight();
                highlightedTilePosition = Vector3.zero;
            }
        }
    }

    private HashSet<Vector3> GetWalkableTiles()
    {
        HashSet<Vector3> walkableTiles = new HashSet<Vector3>();

        // 맵 전체를 스캔하여 타일 정보를 수집합니다.
        foreach (Collider2D tile in Physics2D.OverlapBoxAll(currentTurnUnit.transform.position, new Vector2(100, 100 / TileYScale), 0, tileLayerMask))
        {
            if (tile != null)
            {
                Vector3 tilePosition = new Vector3(Mathf.Round(tile.transform.position.x), Mathf.Round(tile.transform.position.y / TileYScale) * TileYScale, 0);
                walkableTiles.Add(tilePosition);
            }
        }

        // 현재 유닛의 위치를 강제로 이동 가능한 타일로 포함시킴
        Vector3 currentPosition = new Vector3(Mathf.Round(currentTurnUnit.transform.position.x), Mathf.Round(currentTurnUnit.transform.position.y / TileYScale) * TileYScale, 0);
        walkableTiles.Add(currentPosition);  // 강제로 현재 유닛의 위치를 포함시킴

        // 다른 유닛들을 장애물로 간주
        Collider2D[] unitColliders = Physics2D.OverlapBoxAll(currentTurnUnit.transform.position, new Vector2(100, 100 / TileYScale), 0, unitLayerMask);
        foreach (Collider2D unitCollider in unitColliders)
        {
            if (unitCollider != null && unitCollider.gameObject != currentTurnUnit.gameObject) // 자기 자신은 제외
            {
                Vector3 unitPosition = new Vector3(Mathf.Round(unitCollider.transform.position.x), Mathf.Round(unitCollider.transform.position.y / TileYScale) * TileYScale, 0);
                walkableTiles.Remove(unitPosition); // 유닛의 위치를 장애물로 처리
            }
        }

        return walkableTiles;
    }

    // 선택한 경로를 따라 이동
    private IEnumerator MoveAlongPath(List<Vector3> path)
    {
        foreach (Vector3 step in path)
        {
            if (currentTurnUnit.SpendAP(moveAPCost) && currentTurnUnit.SpendStamina(10)) // 이동 시 2 AP와 10 스태미나 소모
            {
                currentTurnUnit.transform.position = step;

                // Y축 값에 따라 Z축 값 조정
                AdjustUnitZPosition(currentTurnUnit.transform);

                // 이동한 타일의 하이라이트 해제
                HighlightTile(step, false, false);

                // 카메라를 유닛 위치로 이동
                Camera.main.transform.position = new Vector3(step.x, step.y, Camera.main.transform.position.z);

                yield return new WaitForSeconds(0.1f); // 0.1초마다 1칸 이동
            }
            else
            {
                break;
            }
        }

        // 이동이 끝난 후 남은 모든 하이라이트를 해제
        ClearPathHighlight();

        if (currentTurnUnit.currentAP >= moveAPCost) // 남은 AP가 있으면 이동 가능한 타일 재계산
        {
            // 경로 재계산 또는 다른 로직 추가 가능
        }
        else
        {
            EndTurn(); // 이동 후 턴 종료
        }
    }

    // 공격 가능 범위 하이라이트를 시작할 때 경로 하이라이트 해제
    public void OnAttackRangeHighlight()
    {
        ClearPathHighlight();
    }
}