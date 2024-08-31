using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public GameObject allyUnitPrefab;
    public GameObject enemyUnitPrefab;
    public int allyCount = 5;
    public int enemyCount = 5;
    public int mapWidth = 32;
    public int mapHeight = 32;

    public TurnManager turnManager; // TurnManager를 연결

    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

    void Awake()
    {
        SpawnUnits(allyUnitPrefab, allyCount, "Ally");
        SpawnUnits(enemyUnitPrefab, enemyCount, "Enemy");
    }

    void SpawnUnits(GameObject unitPrefab, int count, string tag)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2Int spawnPosition;
            do
            {
                spawnPosition = new Vector2Int(Random.Range(0, mapWidth), Random.Range(0, mapHeight));
            } while (occupiedPositions.Contains(spawnPosition));

            occupiedPositions.Add(spawnPosition);

            Vector3 worldPosition = new Vector3(spawnPosition.x, spawnPosition.y * 0.5625f, 0);
            GameObject unit = Instantiate(unitPrefab, worldPosition, Quaternion.identity);
            unit.tag = tag;

            AdjustUnitZPosition(unit.transform); // 유닛의 Z축 위치 조정

            Unit unitScript = unit.GetComponent<Unit>();
            if (unitScript != null)
            {
                // 유닛 스탯 설정 코드 (이전과 동일)
                unitScript.headArmor = Random.Range(100, 301);
                unitScript.bodyArmor = Random.Range(100, 501);
                unitScript.addStamina = Random.Range(100, 121);
                unitScript.addStrength = Random.Range(1, 6);
                unitScript.addDexterity = Random.Range(1, 6);

                unitScript.maxHealth = Random.Range(100, 151);
                unitScript.basicStamina = Random.Range(50, 101);
                unitScript.maxMorale = Random.Range(50, 101);

                unitScript.strength = Random.Range(1, 7);
                unitScript.dexterity = Random.Range(1, 7);
                unitScript.intelligence = Random.Range(1, 7);
                unitScript.charisma = Random.Range(1, 7);
                unitScript.luck = Random.Range(1, 7);

                // 유닛을 TurnManager에 추가
                turnManager.units.Add(unitScript);
            }

            unit.name = $"{tag}_Unit_{i + 1}";
        }
    }

    // 유닛의 Z축 위치를 Y축 값에 따라 조정하는 메서드
    void AdjustUnitZPosition(Transform unitTransform)
    {
        float yPos = unitTransform.position.y;
        float zPos = Mathf.Lerp(-1f, -0.1f, yPos / 10f); // Y축 값에 따라 Z축 값을 보간
        unitTransform.position = new Vector3(unitTransform.position.x, yPos, zPos);
    }
}