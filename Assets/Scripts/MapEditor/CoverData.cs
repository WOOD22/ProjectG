using UnityEngine;

[CreateAssetMenu(fileName = "New Cover Data", menuName = "Cover Data", order = 1)]
public class CoverData : ScriptableObject
{
    public enum CoverType { Full, Partial } // 완전/부분 엄폐물 타입
    public CoverType coverType;

    public bool isDestructible; // 파괴 가능 여부

    public Sprite coverSprite; // 엄폐물 스프라이트

    // 파괴 시 변경될 스프라이트 (파괴된 상태)
    public Sprite destroyedSprite;
}
