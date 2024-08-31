using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackOption
{
    void Execute(Unit attacker, Unit target, AttackManager attackManager);
    float CalculateHitChance(Unit attacker, Unit target, float distance); // 명중률 계산 메서드 추가
}

public class QuickShot : IAttackOption
{
    public void Execute(Unit attacker, Unit target, AttackManager attackManager)
    {
        if (attacker.weapon.magazine_capacity > 0)
        {
            float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
            float hitChance = CalculateHitChance(attacker, target, distance);
            int apCost = 2;
            int staminaCost = 15;

            attackManager.ProcessAttack(attacker, target, apCost, staminaCost, hitChance);

            attacker.weapon.magazine_capacity--;
            Debug.Log($"Aimed Shot fired. Remaining ammo: {attacker.weapon.magazine_capacity}/{attacker.weaponData.magazine_capacity}");
        }
        else
        {
            Debug.Log("탄약이 부족하여 공격할 수 없습니다.");
        }
    }

    public float CalculateHitChance(Unit attacker, Unit target, float distance)
    {
        float accuracy = attacker.weapon.accuracy;
        float bonus = attacker.dexterity - target.dexterity;
        return (accuracy - 15 + (bonus * 5)) - ((distance - 1) * 16);
    }
}

public class AimedShot : IAttackOption
{
    public void Execute(Unit attacker, Unit target, AttackManager attackManager)
    {
        if (attacker.weapon.magazine_capacity > 0)
        {
            float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
            float hitChance = CalculateHitChance(attacker, target, distance);
            int apCost = 3;
            int staminaCost = 20;

            attackManager.ProcessAttack(attacker, target, apCost, staminaCost, hitChance);

            attacker.weapon.magazine_capacity--;
            Debug.Log($"Aimed Shot fired. Remaining ammo: {attacker.weapon.magazine_capacity}/{attacker.weaponData.magazine_capacity}");
        }
        else
        {
            Debug.Log("탄약이 부족하여 공격할 수 없습니다.");
        }
    }

    public float CalculateHitChance(Unit attacker, Unit target, float distance)
    {
        float accuracy = attacker.weapon.accuracy;
        float bonus = attacker.dexterity - target.dexterity;
        return (accuracy + (bonus * 5)) - ((distance - 1) * 8);
    }
}

public class ZombieClaw : IAttackOption
{
    public void Execute(Unit attacker, Unit target, AttackManager attackManager)
    {
        float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
        float accuracy = attacker.weapon.accuracy;
        float hitChance = accuracy + 10; // 좀비의 공격은 근접 공격이므로 명중률이 가깝게 설정됨
        int apCost = 3;
        int staminaCost = 30;

        attackManager.ProcessAttack(attacker, target, apCost, staminaCost, hitChance);
    }

    public float CalculateHitChance(Unit attacker, Unit target, float distance)
    {
        float accuracy = attacker.weapon.accuracy;
        float bonus = attacker.dexterity - target.dexterity;
        return (accuracy + (bonus * 5));
    }
}