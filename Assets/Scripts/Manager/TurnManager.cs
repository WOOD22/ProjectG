using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public List<Unit> units = new List<Unit>(); // 모든 유닛의 리스트
    private int currentTurnIndex = 0;
    public MovementManager movementManager; // MovementManager 참조
    public AttackManager attackManager; // AttackManager 참조
    public CameraController cameraController; // CameraController 참조
    public UII unitStatusUI; // UnitStatusUI 참조

    private Unit previousTurnUnit; // 이전 턴의 유닛

    void Start()
    {
        StartNewTurn();
    }

    // 턴 시작 시 우선권 계산 및 정렬
    public void StartNewTurn()
    {
        foreach (var unit in units)
        {
            unit.CalculateInitiative();
        }

        // 우선권에 따라 유닛 정렬 (내림차순)
        units.Sort((u1, u2) => u2.initiative.CompareTo(u1.initiative));

        currentTurnIndex = 0;
        ProcessTurn();
    }

    // 현재 턴의 유닛 처리
    public void ProcessTurn()
    {
        units.RemoveAll(unit => !unit.gameObject.activeSelf); // 비활성화된 유닛 제거

        // 모든 아군이 사망했는지 확인
        bool alliesRemain = units.Exists(unit => unit.CompareTag("Ally"));
        if (!alliesRemain)
        {
            Debug.Log("모든 아군이 사망했습니다. 게임 종료 또는 다른 행동을 취합니다.");
            // 여기에 게임 종료 로직을 추가할 수 있습니다.
            return;
        }

        if (currentTurnIndex < units.Count)
        {
            Unit currentUnit = units[currentTurnIndex];

            // 이전 유닛의 하이라이트 제거
            if (previousTurnUnit != null)
            {
                previousTurnUnit.RemoveHighlight();
            }

            // 현재 유닛 하이라이트 활성화
            currentUnit.Highlight();
            previousTurnUnit = currentUnit;

            // MovementManager와 AttackManager에 현재 턴의 유닛 설정
            movementManager.SetCurrentTurnUnit(currentUnit);
            attackManager.SetCurrentTurnUnit(currentUnit);

            // 카메라 컨트롤러에 현재 턴의 유닛 설정
            cameraController.SetTargetUnit(currentUnit.transform);

            // 유닛의 레이어에 따라 제어
            if (currentUnit.gameObject.layer == LayerMask.NameToLayer("UncontrolUnit"))
            {
                // 조종 불가능한 유닛은 임시로 스킵
                Debug.Log($"{currentUnit.name}의 턴을 스킵합니다.");
                EndTurn(); // 턴 종료 함수 호출하여 다음 유닛으로 넘어감

                // 기존 AI 동작은 주석 처리
                /*
                IntegratedAIScript aiScript = currentUnit.GetComponent<IntegratedAIScript>();
                if (aiScript != null)
                {
                    unitStatusUI.DeactivateUI(); // UI 비활성화
                    aiScript.StartTurn();
                    return; // AI 유닛이 행동을 마치면 반환하여 플레이어가 조작하지 못하게 함
                }
                */
            }
            else if (currentUnit.gameObject.layer == LayerMask.NameToLayer("ControlUnit"))
            {
                // 플레이어가 조종할 수 있는 유닛의 턴 시작
                Debug.Log($"{currentUnit.name}의 턴입니다.");
                unitStatusUI.SetCurrentTurnUnit(currentUnit); // UI 활성화 및 업데이트
                movementManager.StartTurn(); // MovementManager에서 턴 시작 처리
            }
        }
        else
        {
            // 모든 유닛이 턴을 마치면 새로운 턴 시작
            StartNewTurn();
        }
    }

    // 턴 종료 버튼에서 호출
    public void EndTurn()
    {
        // 현재 유닛의 턴 종료
        if (currentTurnIndex < units.Count)
        {
            currentTurnIndex++;
            movementManager.EndTurn();
            ProcessTurn(); // 다음 유닛의 턴을 진행
        }
    }
}