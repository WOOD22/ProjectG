using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public Color highlightColor = Color.yellow; // 하이라이트 색상
    private Color originalColor; // 원래 색상


    // 유닛 속성
    public int headArmor;  // 머리 내구도
    public int bodyArmor;  // 몸통 내구도
    public int basicStamina; // 기본 스태미나
    public int addStamina; // 보조 스태미나
    public int addStrength; // 보조 근력
    public int addDexterity; // 보조 기교

    public int maxHealth; // 최대 생명력
    public int currentHealth; // 현재 생명력
    public int maxStamina; // 최대 스태미나
    public int currentStamina; // 현재 스태미나
    public int maxMorale; // 최대 사기
    public int currentMorale; // 현재 사기

    public int strength; // 근력
    public int dexterity; // 기교
    public int intelligence; // 지능
    public int charisma; // 매력
    public int luck; // 행운

    public int initiative; // 우선권
    public int maxAP; // 최대 액션 포인트
    public int currentAP; // 현재 액션 포인트

    public float headHitChance = 0.25f; // 머리 피격 확률
    public float bodyHitChance = 0.75f; // 몸통 피격 확률

    public Weapon weapon; // 무기 데이터를 저장하는 구조체

    public WeaponData weaponData; // ScriptableObject 참조

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color; // 원래 색상 저장
        }

        if (weaponData != null)
        {
            // WeaponData에서 구조체 생성
            weapon = weaponData.ToWeapon();
        }

        // 최대 스태미나 계산: 기본 스태미나 + 보조 스태미나
        maxStamina = basicStamina + addStamina;
        currentStamina = maxStamina;

        // AP 계산: 기교 + 보조 기교
        maxAP = 5 + dexterity + addDexterity;
        currentAP = maxAP;

        // 상태 초기화
        currentHealth = maxHealth;
        currentMorale = maxMorale;
    }

    // 턴 우선권 계산용
    public void CalculateInitiative()
    {
        initiative = Random.Range(0, 6) + dexterity + addDexterity + intelligence;
    }

    // AP 소모 처리
    public bool SpendAP(int amount)
    {
        if (currentAP >= amount)
        {
            currentAP -= amount;
            return true;
        }
        else
        {
            Debug.LogWarning("AP가 부족합니다.");
            return false;
        }
    }

    // 스태미나 소모 처리
    public bool SpendStamina(int amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            return true;
        }
        else
        {
            Debug.LogWarning("스태미나가 부족합니다.");
            return false;
        }
    }

    // 매 턴 AP 회복
    public void RecoverAP()
    {
        currentAP = maxAP;
    }

    // 사망 처리
    public void Die()
    {
        Debug.Log($"{name}이(가) 사망했습니다.");
        gameObject.SetActive(false); // 유닛 비활성화
    }

    // 하이라이트 활성화
    public void Highlight()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlightColor;
        }
    }

    // 하이라이트 비활성화
    public void RemoveHighlight()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}