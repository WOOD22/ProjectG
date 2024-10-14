using UnityEngine;

[CreateAssetMenu(fileName = "Heal", menuName = "Skills/Heal")]
public class HealSkill : Skill
{
    public int healAmount;

    public override void UseSkill(Unit user, Unit target)
    {
        target.Heal(healAmount);
        Debug.Log($"{user.name} healed {target.name} for {healAmount} HP");
    }
}