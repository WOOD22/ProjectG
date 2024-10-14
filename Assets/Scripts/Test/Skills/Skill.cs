using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill")]
public class Skill : ScriptableObject
{
    public string skillName;
    public string description;
    public int apCost;
    public int spCost;
    public int cooldown;
    public TargetType targetType;
    public int range;

    public virtual void UseSkill(Unit user, Unit target)
    {
        // 기본 스킬 사용 로직
        Debug.Log($"{user.name} used {skillName} on {target.name}");
    }
}

public enum TargetType
{
    Self,
    Ally,
    Enemy,
    AllAllies,
    AllEnemies,
    All
}