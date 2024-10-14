public class TileInfo
{
    public TileData tileData;
    public int height; // 고도 값

    public TileInfo(TileData data, int height)
    {
        tileData = data;
        this.height = height;
    }
}
