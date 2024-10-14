using UnityEngine;

[CreateAssetMenu(fileName = "New Tile Data", menuName = "Tile Data", order = 1)]
public class TileData : ScriptableObject
{
    public enum TileType { Ground, Wall } // 필요한 타일 타입 정의

    public TileType tileType;
    public Sprite tileSprite;
    public bool isWalkable;
    public int movementCost;
    // 고도 값은 레이어에서 관리하므로 여기서는 제거합니다.
}