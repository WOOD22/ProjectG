using UnityEngine;

[CreateAssetMenu(fileName = "Fireball", menuName = "Skills/Fireball")]
public class FireballSkill : Skill
{
    public int damage;

    public override void UseSkill(Unit user, Unit target)
    {
        target.TakeDamage(damage, DamageType.Fire);
        Debug.Log($"{user.name} hit {target.name} with a fireball for {damage} damage");
    }
}