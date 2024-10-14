using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 게임 내 무기를 정의하는 ScriptableObject 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon")]
public class Weapon : ScriptableObject
{
    #region Fields

    [Header("Basic Info")]
    public string weaponName;
    public WeaponType weaponType;
    public DamageType damageType;

    [Header("Stats")]
    public int attackPower;
    public int range;

    [Header("Requirements")]
    public int requiredPHY;
    public int requiredTEC;
    public int requiredINT;
    public int requiredWIL;
    public int requiredCHA;
    public int requiredLUK;
    public List<string> requiredPerks = new List<string>();

    [Header("Effects")]
    public List<WeaponEffect> effects = new List<WeaponEffect>();

    #endregion

    #region Methods

    /// <summary>
    /// 유닛이 이 무기를 장착할 수 있는지 확인합니다.
    /// </summary>
    /// <param name="unit">확인할 유닛</param>
    /// <returns>장착 가능 여부</returns>
    public bool CanEquip(Unit unit)
    {
        if (!MeetsStatRequirements(unit))
        {
            return false;
        }

        if (!MeetsPerkRequirements(unit))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 유닛이 무기의 스탯 요구사항을 충족하는지 확인합니다.
    /// </summary>
    private bool MeetsStatRequirements(Unit unit)
    {
        return unit.PHY >= requiredPHY &&
               unit.TEC >= requiredTEC &&
               unit.INT >= requiredINT &&
               unit.WIL >= requiredWIL &&
               unit.CHA >= requiredCHA &&
               unit.LUK >= requiredLUK;
    }

    /// <summary>
    /// 유닛이 무기의 퍼크 요구사항을 충족하는지 확인합니다.
    /// </summary>
    private bool MeetsPerkRequirements(Unit unit)
    {
        // TODO: 퍼크 체크 로직 구현
        // 현재는 모든 퍼크 요구사항을 충족한다고 가정
        return true;
    }

    /// <summary>
    /// 무기의 정확도 보너스를 계산합니다.
    /// </summary>
    /// <returns>정확도 보너스</returns>
    public int GetAccuracyBonus()
    {
        int bonus = 0;
        foreach (WeaponEffect effect in effects)
        {
            if (effect.type == WeaponEffect.EffectType.Accuracy)
            {
                bonus += effect.GetAccuracyBonus();
            }
        }
        return bonus;
    }

    /// <summary>
    /// 대상에게 데미지를 입힙니다.
    /// </summary>
    /// <param name="attacker">공격자</param>
    /// <param name="target">대상</param>
    public void DealDamage(Unit attacker, Unit target)
    {
        int damage = CalculateAttackPower(attacker);
        target.TakeDamage(damage, damageType);
    }

    /// <summary>
    /// 공격력을 계산합니다.
    /// </summary>
    /// <param name="attacker">공격자</param>
    /// <returns>계산된 공격력</returns>
    private int CalculateAttackPower(Unit attacker)
    {
        int basePower = attackPower + attacker.PHY;

        // TODO: 무기 효과로 인한 추가 공격력 계산 로직 구현
        // 현재 WeaponEffect에는 공격력 보너스 관련 메서드가 없으므로 주석 처리합니다.
        /*
        foreach (WeaponEffect effect in effects)
        {
            basePower += effect.GetAttackPowerBonus(attacker);
        }
        */

        return basePower;
    }

    #endregion
}