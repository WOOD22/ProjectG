using UnityEngine;

public static class IsoUtil
{
    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환합니다.
    /// </summary>
    /// <param name="gridPosition">그리드 좌표 (Vector2Int)</param>
    /// <param name="tileWidth">타일의 너비 (float)</param>
    /// <param name="tileHeight">타일의 높이 (float)</param>
    /// <returns>월드 좌표 (Vector3)</returns>
    public static Vector3 GridToWorld(Vector2Int gridPosition, float tileWidth, float tileHeight)
    {
        float x = (gridPosition.x - gridPosition.y) * (tileWidth * 0.5f);
        float y = (gridPosition.x + gridPosition.y) * (tileHeight * 0.5f);
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환합니다.
    /// </summary>
    /// <param name="worldPosition">월드 좌표 (Vector3)</param>
    /// <param name="tileWidth">타일의 너비 (float)</param>
    /// <param name="tileHeight">타일의 높이 (float)</param>
    /// <returns>그리드 좌표 (Vector2Int)</returns>
    public static Vector2Int WorldToGrid(Vector3 worldPosition, float tileWidth, float tileHeight)
    {
        float gridX = (worldPosition.x / (tileWidth * 0.5f) + worldPosition.y / (tileHeight * 0.5f)) * 0.5f;
        float gridY = (worldPosition.y / (tileHeight * 0.5f) - worldPosition.x / (tileWidth * 0.5f)) * 0.5f;
        return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
    }
}
