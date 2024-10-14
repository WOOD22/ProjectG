using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolBarUI : MonoBehaviour
{
    public TilePlacer tilePlacer;

    public enum ToolType { Place, Remove }
    public ToolType currentTool;

    public void SetPlaceTool()
    {
        currentTool = ToolType.Place;
    }

    public void SetRemoveTool()
    {
        currentTool = ToolType.Remove;
    }

    void Update()
    {
        // TilePlacer에서 currentTool을 활용하도록 수정
        //tilePlacer.currentTool = currentTool;
    }
}
