using UnityEngine;

[CreateAssetMenu(fileName = "New Trigger Item", menuName = "SRPG/Items/Trigger")]
public class TriggerItem : Item
{
    public string triggerType;

    public override void Use(Unit user)
    {
        // 트리거 아이템은 사용 시 효과가 없지만, 오브젝트와의 상호작용에 사용됩니다.
        Debug.Log($"{user.name} activated {itemName} of type {triggerType}");
    }
}
