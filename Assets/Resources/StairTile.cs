using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Stair Tile", menuName = "Tiles/Stair Tile")]
public class StairTile : Tile
{
    public enum StairDirection
    {
        North,
        South,
        East,
        West
    }

    public StairDirection entryDirection;
    public int targetLayer;

    public Vector3Int GetValidEntryDirection()
    {
        switch (entryDirection)
        {
            case StairDirection.North:
                return new Vector3Int(0, -1, 0);
            case StairDirection.South:
                return new Vector3Int(0, 1, 0);
            case StairDirection.East:
                return new Vector3Int(-1, 0, 0);
            case StairDirection.West:
                return new Vector3Int(1, 0, 0);
            default:
                return Vector3Int.zero;
        }
    }

    public Vector3Int GetExitPosition(Vector3Int entryPosition)
    {
        switch (entryDirection)
        {
            case StairDirection.North:
                return entryPosition + new Vector3Int(-1, -2, 0);
            case StairDirection.South:
                return entryPosition + new Vector3Int(1, 2, 0);
            case StairDirection.East:
                return entryPosition + new Vector3Int(-2, -1, 0);
            case StairDirection.West:
                return entryPosition + new Vector3Int(2, 1, 0);
            default:
                return entryPosition;
        }
    }
}