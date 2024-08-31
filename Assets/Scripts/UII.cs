using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UII : MonoBehaviour
{
    public TextMeshProUGUI apText;       // AP를 표시하는 텍스트
    public TextMeshProUGUI staminaText;  // 스태미나를 표시하는 텍스트
    public TextMeshProUGUI magazineText;  // 장탄수를 표시하는 텍스트

    public Button pistolQuickShotButton; // 권총_빠른사격 버튼
    public Button pistolAimedShotButton; // 권총_조준사격 버튼

    private Unit currentTurnUnit;        // 현재 턴의 유닛

    void Start()
    {
        gameObject.SetActive(false);     // 처음에는 비활성화
    }

    // 현재 턴의 유닛을 설정하는 메서드
    public void SetCurrentTurnUnit(Unit unit)
    {
        currentTurnUnit = unit;
        UpdateUI();
        gameObject.SetActive(true);      // 통제되는 유닛일 때 UI 활성화

        // 무기 태그에 따라 버튼 활성화
        UpdateWeaponButtons();
    }

    // UI를 비활성화하는 메서드 (턴 종료 시 호출)
    public void DeactivateUI()
    {
        gameObject.SetActive(false);
    }

    // UI 업데이트 메서드
    private void UpdateUI()
    {
        if (currentTurnUnit != null)
        {
            apText.text = $"AP: {currentTurnUnit.currentAP}/{currentTurnUnit.maxAP}";
            staminaText.text = $"Stamina: {currentTurnUnit.currentStamina}/{currentTurnUnit.maxStamina}";
            magazineText.text = $"Magazine: {currentTurnUnit.weapon.magazine_capacity}/{currentTurnUnit.weaponData.magazine_capacity}";
        }
    }

    // 무기 태그에 따라 버튼 활성화/비활성화
    private void UpdateWeaponButtons()
    {
        if (currentTurnUnit != null && currentTurnUnit.weapon.durability > 0)
        {
            // 무기의 태그에 Pistol이 포함되어 있는지 확인
            bool hasPistolTag = currentTurnUnit.weapon.tags.Contains("Pistol");

            // 버튼 활성화 여부 설정
            pistolQuickShotButton.gameObject.SetActive(hasPistolTag);
            pistolAimedShotButton.gameObject.SetActive(hasPistolTag);
        }
        else
        {
            // 무기가 없거나 Pistol 태그가 없을 경우 버튼 비활성화
            pistolQuickShotButton.gameObject.SetActive(false);
            pistolAimedShotButton.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 매 프레임 UI를 갱신하여 최신 상태를 반영
        if (currentTurnUnit != null)
        {
            UpdateUI();
        }
    }
}