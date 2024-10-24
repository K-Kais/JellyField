using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellyCell : MonoBehaviour
{
    public JellyCellType jellyCellType;
    public JellyColor jellyColor;
    private Jellyfier jellyfier;
    private void Start()
    {
        jellyfier = GetComponentInParent<Jellyfier>();
    }

    public void CheckGrid(Vector2Int pos)
    {
        if(jellyCellType == JellyCellType.TopLeft)
        {
            jellyfier.jellyCellDic.TryGetValue(JellyCellType.TopRight, out JellyCell topRight);
            jellyfier.jellyCellDic.TryGetValue(JellyCellType.DownLeft, out JellyCell downLeft);
            jellyfier.jellyCellDic.TryGetValue(JellyCellType.DownRight, out JellyCell downRight);
        }
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