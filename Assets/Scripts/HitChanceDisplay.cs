using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HitChanceDisplay : MonoBehaviour
{
    public TMP_Text hitChanceText; // 명중 확률을 표시할 텍스트 UI

    public void ShowHitChance(float hitChance, Vector3 mousePosition)
    {
        hitChance = Mathf.Clamp(hitChance, 0f, 100f); // 0에서 100 사이로 제한
        hitChanceText.text = $"Hit Chance: {Mathf.FloorToInt(hitChance)}%"; // 소수점 없는 정수로 명중률 표시

        // 마우스 상단에 UI 위치 설정
        Vector3 screenPosition = mousePosition + new Vector3(0, 100, 0);
        hitChanceText.transform.position = screenPosition;

        hitChanceText.gameObject.SetActive(true); // UI 활성화
    }

    public void HideHitChance()
    {
        hitChanceText.gameObject.SetActive(false); // UI 비활성화
    }
}