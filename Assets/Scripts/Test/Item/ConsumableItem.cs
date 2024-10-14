using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable Item", menuName = "SRPG/Items/Consumable")]
public class ConsumableItem : Item
{
    public int hpRestore;
    public int spRestore;
    public int damage;
    public DamageType damageType;

    public override void Use(Unit user)
    {
        if (hpRestore > 0)
            user.Heal(hpRestore);
        if (spRestore > 0)
            user.RestoreSP(spRestore);
        if (damage > 0)
        {
            // 가장 가까운 적을 찾아 데미지를 줍니다.
            Unit nearestEnemy = SRPGManager.Instance.GetNearestEnemy(user);
            if (nearestEnemy != null)
                nearestEnemy.TakeDamage(damage, damageType);
        }
        Debug.Log($"{user.name} used {itemName}");
    }
}