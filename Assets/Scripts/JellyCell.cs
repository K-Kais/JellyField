using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellyCell : MonoBehaviour
{
    private Jellyfier jellyfier;
    private Dictionary<JellyCellType, (JellyCellType, JellyCellType, MeshType, MeshType)> adjacentCells;
    private Dictionary<JellyCellType, Vector2Int> positions;
    public JellyCellType type;
    public JellyColor color;
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

        positions = new Dictionary<JellyCellType, Vector2Int>
        {
            { JellyCellType.TopLeft, new Vector2Int(0, 0) },
            { JellyCellType.TopRight, new Vector2Int(1, 0) },
            { JellyCellType.DownLeft, new Vector2Int(0, 1) },
            { JellyCellType.DownRight, new Vector2Int(1, 1) }
        };
    }
    public MeshType GetMeshCell() => HandleMeshCell(type);
    private MeshType HandleMeshCell(JellyCellType type)
    {
        var (firstNeighbor, secondNeighbor, firstMeshType, secondMeshType) = adjacentCells[type];
        if (color == jellyfier.jellyCellDic[firstNeighbor].color)
        {
            return firstMeshType;
        }
        else if (color == jellyfier.jellyCellDic[secondNeighbor].color)
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
        return color switch
        {
            JellyColor.Red => Color.red,
            JellyColor.Blue => Color.blue,
            JellyColor.Yellow => Color.yellow,
            JellyColor.Green => Color.green,
            _ => Color.white
        };
    }
    public bool IsDiagonal(JellyCellType targetCell)
    {
        return positions[type].x != positions[targetCell].x && positions[type].y != positions[targetCell].y;
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