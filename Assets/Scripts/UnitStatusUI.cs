using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitStatusUI : MonoBehaviour
{
    public Slider headArmorSlider;   // 머리 내구도 슬라이더
    public Slider bodyArmorSlider;   // 몸통 내구도 슬라이더
    public Slider healthSlider;      // 생명력 슬라이더

    private Unit unit;
    private int maxHeadArmor;   // 최대 머리 내구도
    private int maxBodyArmor;   // 최대 몸통 내구도

    private void Start()
    {
        unit = GetComponent<Unit>();
        maxHeadArmor = unit.headArmor;   // 시작 시 최대 머리 내구도 설정
        maxBodyArmor = unit.bodyArmor;   // 시작 시 최대 몸통 내구도 설정

        // 슬라이더의 최대값 설정
        headArmorSlider.maxValue = maxHeadArmor;
        bodyArmorSlider.maxValue = maxBodyArmor;
        healthSlider.maxValue = unit.maxHealth;

        // 슬라이더의 초기값 설정
        headArmorSlider.value = unit.headArmor;
        bodyArmorSlider.value = unit.bodyArmor;
        healthSlider.value = unit.currentHealth;
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        // 머리 내구도 슬라이더 업데이트
        headArmorSlider.value = unit.headArmor;

        // 몸통 내구도 슬라이더 업데이트
        bodyArmorSlider.value = unit.bodyArmor;

        // 생명력 슬라이더 업데이트
        healthSlider.value = unit.currentHealth;
    }
}