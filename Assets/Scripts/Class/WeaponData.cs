using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Weapon
{
    public string weaponName;              // 무기 이름
    public int value;                      // 무기의 가치
    public int max_damage;                 // 무기의 최대 데미지
    public int min_damage;                 // 무기의 최소 데미지
    public int accuracy;                   // 무기의 명중
    public float range;                    // 무기의 사정거리
    public int durability;                 // 무기의 내구도
    public int armorPenetration;           // 방어구 관통력
    public int armorShredding;             // 방어구 파괴력
    public int fatigueCost;                // 소모 피로도
    public int magazine_capacity;          // 장탄수
    public Sprite weaponIcon;              // 무기 아이콘 (UI용)
    public List<string> tags;              // 무기 태그 목록
}

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapon Data", order = 51)]
public class WeaponData : ScriptableObject
{
    public string weaponName;              // 무기 이름
    public int value;                      // 무기의 가치
    public int max_damage;                 // 무기의 최대 데미지
    public int min_damage;                 // 무기의 최소 데미지
    public int accuracy;                   // 무기의 명중
    public float range;                    // 무기의 사정거리
    public int durability;                 // 무기의 내구도
    public int armorPenetration;           // 방어구 관통력
    public int armorShredding;             // 방어구 파괴력
    public int fatigueCost;                // 소모 피로도
    public int magazine_capacity;          // 장탄수
    public Sprite weaponIcon;              // 무기 아이콘 (UI용)
    public List<string> tags;              // 무기 태그 목록

    // WeaponData 구조체를 반환하는 메서드
    public Weapon ToWeapon()
    {
        return new Weapon
        {
            weaponName = this.weaponName,
            value = this.value,
            max_damage = this.max_damage,
            min_damage = this.min_damage,
            accuracy = this.accuracy,
            range = this.range,
            durability = this.durability,
            armorPenetration = this.armorPenetration,
            armorShredding = this.armorShredding,
            fatigueCost = this.fatigueCost,
            magazine_capacity = this.magazine_capacity,
            weaponIcon = this.weaponIcon,
            tags = new List<string>(this.tags) // 리스트 복사
        };
    }
}


