using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellyCell : MonoBehaviour
{
    private Jellyfier jellyfier;
    private Dictionary<JellyCellType, (JellyCellType, JellyCellType, MeshType, MeshType)> adjacentCells;
    public JellyCellType jellyCellType;
    public JellyColor jellyColor;
    private void Awake()
    {
        jellyfier = GetComponentInParent<Jellyfier>();
        adjacentCells = new Dictionary<JellyCellType, (JellyCellType, JellyCellType, MeshType, MeshType)>
        {
        { JellyCellType.TopLeft, (JellyCellType.TopRight, JellyCellType.DownLeft, MeshType.TopLeftRight, MeshType.LeftTopDown) },
        { JellyCellType.TopRight, (JellyCellType.TopLeft, JellyCellType.DownRight, MeshType.TopLeftRight, MeshType.RightTopDown) },
        { JellyCellType.DownLeft, (JellyCellType.TopLeft, JellyCellType.DownRight, MeshType.LeftTopDown, MeshType.DownLeftRight) },
        { JellyCellType.DownRight, (JellyCellType.TopRight, JellyCellType.DownLeft, MeshType.RightTopDown, MeshType.DownLeftRight) }
        };
    }
    public MeshType GetMeshCell() => HandleMeshCell(jellyCellType);
    private MeshType HandleMeshCell(JellyCellType type)
    {
        var (firstNeighbor, secondNeighbor, firstMeshType, secondMeshType) = adjacentCells[type];
        if (jellyColor == jellyfier.jellyCellDic[firstNeighbor].jellyColor)
        {
            return firstMeshType;
        }
        else if (jellyColor == jellyfier.jellyCellDic[secondNeighbor].jellyColor)
        {
            return secondMeshType;
        }
        else
        {
            return type switch
            {
                JellyCellType.TopLeft => MeshType.TopLeft,
                JellyCellType.TopRight => MeshType.TopRight,
                JellyCellType.DownLeft => MeshType.DownLeft,
                JellyCellType.DownRight => MeshType.DownRight,
                _ => throw new System.ArgumentOutOfRangeException()
            };
        }
    }
    public Color GetColor()
    {
        return jellyColor switch
        {
            JellyColor.Red => Color.red,
            JellyColor.Blue => Color.blue,
            JellyColor.Yellow => Color.yellow,
            JellyColor.Green => Color.green,
            _ => Color.white
        };
    }
}
public enum JellyColor
{
    Red,
    Blue,
    Yellow,
    Green,
}
public enum JellyCellType
{
    TopLeft,
    TopRight,
    DownLeft,
    DownRight
}