using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellyCell : MonoBehaviour
{
    public JellyCellType jellyCellType;
    public JellyColor jellyColor;
    private Jellyfier jellyfier;
    private void Awake()
    {
        jellyfier = GetComponentInParent<Jellyfier>();
    }

    public MeshType GetMeshCell()
    {
        switch (jellyCellType)
        {
            case JellyCellType.TopLeft: return HandleTopLeft();
            case JellyCellType.TopRight: return HandleTopRight();
            case JellyCellType.DownLeft: return HandleDownLeft();
            case JellyCellType.DownRight: return HandleDownRight();
            default: throw new System.ArgumentOutOfRangeException();
        }
    }
    private MeshType HandleTopLeft()
    {
        if (jellyColor == jellyfier.jellyCellDic[JellyCellType.TopRight].jellyColor)
        {
            return MeshType.TopLeftRight;
        }
        else if (jellyColor == jellyfier.jellyCellDic[JellyCellType.DownLeft].jellyColor)
        {
            return MeshType.LeftTopDown;
        }
        else return MeshType.TopLeft;
    }

    private MeshType HandleTopRight()
    {
        if (jellyColor == jellyfier.jellyCellDic[JellyCellType.TopLeft].jellyColor)
        {
            return MeshType.TopLeftRight;
        }
        else if (jellyColor == jellyfier.jellyCellDic[JellyCellType.DownRight].jellyColor)
        {
            return MeshType.RightTopDown;
        }
        else return MeshType.TopRight;
    }

    private MeshType HandleDownLeft()
    {
        if (jellyColor == jellyfier.jellyCellDic[JellyCellType.TopLeft].jellyColor)
        {
            return MeshType.LeftTopDown;
        }
        else if (jellyColor == jellyfier.jellyCellDic[JellyCellType.DownRight].jellyColor)
        {
            return MeshType.DownLeftRight;
        }
        else return MeshType.DownLeft;
    }

    private MeshType HandleDownRight()
    {
        if (jellyColor == jellyfier.jellyCellDic[JellyCellType.TopRight].jellyColor)
        {
            return MeshType.RightTopDown;
        }
        else if (jellyColor == jellyfier.jellyCellDic[JellyCellType.DownLeft].jellyColor)
        {
            return MeshType.DownLeftRight;
        }
        else return MeshType.DownRight;
    }
    public Color GetColor()
    {
        switch (jellyColor)
        {
            case JellyColor.Red:
                return Color.red;
            case JellyColor.Blue:
                return Color.blue;
            case JellyColor.Yellow:
                return Color.yellow;
            case JellyColor.Green:
                return Color.green;
            default:
                return Color.white;
        }
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