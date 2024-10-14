using UnityEngine;

[System.Serializable]
public class WeaponEffect
{
    public enum EffectType
    {
        Accuracy
    }

    public EffectType type;
    public int level;

    public int GetAccuracyBonus()
    {
        if (type == EffectType.Accuracy)
        {
            return level;
        }
        return 0;
    }
}