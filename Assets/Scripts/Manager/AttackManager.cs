using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AttackManager : MonoBehaviour
{
    public LayerMask tileLayerMask;
    public LayerMask unitLayerMask;

    private HashSet<Vector3> attackableTiles = new HashSet<Vector3>();
    private bool isAttackModeActive = false;

    private Unit currentTurnUnit;
    private IAttackOption selectedAttackOption;

    private MovementManager movementManager;
    private HitChanceDisplay hitChanceDisplay; // 명중 확률 UI

    private const float TileYScale = 0.5625f; // 타일 Y축 스케일 비율
    private const float HitChanceYOffset = 20f; // 명중 확률 UI의 Y축 오프셋 값

    private void Awake()
    {
        movementManager = FindObjectOfType<MovementManager>(); // MovementManager를 찾아서 참조
        hitChanceDisplay = FindObjectOfType<HitChanceDisplay>(); // HitChanceDisplay를 찾아서 참조
    }

    public void SetCurrentTurnUnit(Unit unit)
    {
        currentTurnUnit = unit;
    }

    public void OnAttackButtonClick(IAttackOption attackOption)
    {
        movementManager.ClearPathHighlight();
        if (!isAttackModeActive && currentTurnUnit != null && currentTurnUnit.weapon.durability > 0)
        {
            isAttackModeActive = true;
            selectedAttackOption = attackOption;
            UpdateAttackableTiles();
        }
    }

    public void Reload()
    {
        currentTurnUnit.weapon.magazine_capacity = currentTurnUnit.weaponData.magazine_capacity;
        Debug.Log("재장전 완료!");
    }

    public void OnPistolQuickShotButtonClicked()
    {
        IAttackOption quickShot = new QuickShot();
        OnAttackButtonClick(quickShot);
    }

    public void OnPistolAimedShotButtonClicked()
    {
        IAttackOption aimedShot = new AimedShot();
        OnAttackButtonClick(aimedShot);
    }

    public void ProcessAttack(Unit attacker, Unit target, int apCost, int staminaCost, float hitChance)
    {
        if (attacker.currentAP < apCost || attacker.currentStamina < staminaCost)
        {
            Debug.Log("AP 또는 스태미나가 부족하여 공격할 수 없습니다.");
            CancelAttack();
            return;
        }

        attacker.currentAP -= apCost;
        attacker.currentStamina -= staminaCost;
        Debug.Log($"공격으로 인해 {apCost} AP와 {staminaCost} 스태미나를 소모했습니다. 남은 AP: {attacker.currentAP}, 남은 스태미나: {attacker.currentStamina}");

        if (Random.value * 100f <= hitChance)
        {
            Debug.Log("공격이 명중했습니다!");
            ExecuteWeaponAttack(attacker, target);
        }
        else
        {
            Debug.Log("공격이 빗나갔습니다.");
        }

        // AP가 부족하면 공격 취소
        if (attacker.currentAP < apCost)
        {
            CancelAttack();
        }
    }

    private void ExecuteWeaponAttack(Unit attacker, Unit target)
    {
        if (attacker.weapon.durability > 0)
        {
            bool hitHead = Random.value < target.headHitChance;
            int damageDealt = Random.Range(attacker.weapon.min_damage, attacker.weapon.max_damage + 1);
            int directHealthDamage = Mathf.FloorToInt(damageDealt * (attacker.weapon.armorPenetration / 100f));
            int remainingDamage = damageDealt - directHealthDamage;

            if (hitHead)
            {
                int armorDamage = Mathf.FloorToInt(remainingDamage * (attacker.weapon.armorShredding / 100f));
                target.currentHealth -= directHealthDamage;
                target.headArmor -= armorDamage;

                if (armorDamage > target.headArmor)
                {
                    int overflowDamage = Mathf.FloorToInt((armorDamage - target.headArmor) * (100f / attacker.weapon.armorShredding));
                    target.currentHealth -= overflowDamage;
                }
            }
            else
            {
                int armorDamage = Mathf.FloorToInt(remainingDamage * (attacker.weapon.armorShredding / 100f));
                target.currentHealth -= directHealthDamage;
                target.bodyArmor -= armorDamage;

                if (armorDamage > target.bodyArmor)
                {
                    int overflowDamage = Mathf.FloorToInt((armorDamage - target.bodyArmor) * (100f / attacker.weapon.armorShredding));
                    target.currentHealth -= overflowDamage;
                }
            }

            attacker.weapon.durability -= 1;
            if (attacker.weapon.durability <= 0)
            {
                Debug.Log("무기가 파괴되었습니다!");
            }

            if (target.currentHealth <= 0)
            {
                target.Die();
            }
        }
    }

    private void UpdateAttackableTiles()
    {
        HighlightAttackableTiles(false);
        attackableTiles = FindAttackableTiles(currentTurnUnit.transform.position, currentTurnUnit.weapon.range);
        HighlightAttackableTiles(true);
    }

    private HashSet<Vector3> FindAttackableTiles(Vector3 startPos, float range)
    {
        HashSet<Vector3> attackable = new HashSet<Vector3>();
        Queue<(Vector3 position, float distance)> queue = new Queue<(Vector3, float)>();
        queue.Enqueue((startPos, 0f));
        attackable.Add(startPos);

        Vector3[] directions = {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1 * TileYScale, 0),
            new Vector3(0, -1 * TileYScale, 0),
            new Vector3(1, 1 * TileYScale, 0),   // 대각선 방향
            new Vector3(1, -1 * TileYScale, 0),
            new Vector3(-1, 1 * TileYScale, 0),
            new Vector3(-1, -1 * TileYScale, 0)
        };

        float[] directionCosts = {
            1f,    // 좌우
            1f,    // 상하
            1f,    // 좌우
            1f,    // 상하
            1.4f,  // 대각선 상우
            1.4f,  // 대각선 하우
            1.4f,  // 대각선 상좌
            1.4f   // 대각선 하좌
        };

        while (queue.Count > 0)
        {
            var (currentPosition, currentDistance) = queue.Dequeue();

            for (int i = 0; i < directions.Length; i++)
            {
                Vector3 neighbor = currentPosition + directions[i];
                float newDistance = currentDistance + directionCosts[i];

                if (newDistance <= range && !attackable.Contains(neighbor))
                {
                    Collider2D hit = Physics2D.OverlapPoint(neighbor, tileLayerMask);
                    if (hit != null)
                    {
                        attackable.Add(neighbor);
                        queue.Enqueue((neighbor, newDistance));
                    }
                }
            }
        }

        return attackable;
    }

    private void HighlightAttackableTiles(bool highlight)
    {
        foreach (Vector3 tilePos in attackableTiles)
        {
            Collider2D tileCollider = Physics2D.OverlapPoint(tilePos, tileLayerMask);
            if (tileCollider != null)
            {
                Transform tile = tileCollider.transform;
                if (tile.childCount > 1)
                {
                    tile.GetChild(1).gameObject.SetActive(highlight);
                }
            }
        }
    }

    private void Update()
    {
        // UI 클릭 여부 확인
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return; // UI가 클릭된 경우 타일 클릭 무시
        }

        Unit targetUnit = null; // targetUnit 변수를 상위 범위에서 선언

        if (isAttackModeActive)
        {
            Vector3 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPosition.z = 0;

            // 타일의 중앙 위치로 스냅 (Y축 조정 포함)
            targetPosition = new Vector3(Mathf.Round(targetPosition.x), Mathf.Round(targetPosition.y / TileYScale) * TileYScale, 0);

            Collider2D unitCollider = Physics2D.OverlapPoint(targetPosition, unitLayerMask);
            if (unitCollider != null)
            {
                targetUnit = unitCollider.GetComponent<Unit>();
                if (targetUnit != null && selectedAttackOption != null)
                {
                    // 명중 확률 계산
                    float distance = Vector3.Distance(currentTurnUnit.transform.position, targetUnit.transform.position);
                    float hitChance = Mathf.Clamp(selectedAttackOption.CalculateHitChance(currentTurnUnit, targetUnit, distance), 0f, 100f);

                    // 명중 확률 UI 표시 (마우스 위치 기준, 약간 위로 이동)
                    Vector3 mousePosition = Input.mousePosition;
                    mousePosition.y += HitChanceYOffset;
                    hitChanceDisplay.ShowHitChance(hitChance, mousePosition);
                }
            }
            else
            {
                hitChanceDisplay.HideHitChance(); // 적이 없는 타일로 마우스를 이동하면 숨김
            }

            // 타일 클릭 처리
            if (Input.GetMouseButtonDown(0))
            {
                if (unitCollider != null && targetUnit != null && selectedAttackOption != null)
                {
                    selectedAttackOption.Execute(currentTurnUnit, targetUnit, this);
                }
                else
                {
                    CancelAttack(); // 적이 없는 타일을 클릭하면 공격 취소
                }
            }

            // 오른쪽 마우스 버튼을 누르면 공격 취소
            if (Input.GetMouseButtonDown(1))
            {
                CancelAttack();
            }
        }
    }

    private void CancelAttack()
    {
        isAttackModeActive = false;
        HighlightAttackableTiles(false);
        hitChanceDisplay.HideHitChance(); // 공격 취소 후 명중 확률 UI 숨김
    }

    public void OnTurnEnd()
    {
        CancelAttack();
    }
}