using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
[ExecuteInEditMode]
public class GridCell : MonoBehaviour
{
    public GridType gridType;
    public Vector2Int pos;
    public Dictionary<GridDirection, GridCell> neighbors;
    private void Start()
    {
        neighbors = new Dictionary<GridDirection, GridCell>();
        var directions = new Dictionary<GridDirection, Vector2Int>
        {
            { GridDirection.Left, Vector2Int.left },
            { GridDirection.Right, Vector2Int.right },
            { GridDirection.Top, Vector2Int.up },
            { GridDirection.Down, Vector2Int.down }
        };
        foreach (var direction in directions)
        {
            if (GridManager.Instance.gridCellDic.TryGetValue(pos + direction.Value * 2, out GridCell gridPos))
            {
                neighbors.Add(direction.Key, gridPos);
            }
        }
    }
    public void SnapToGrid(float PADDING)
    {
        int x = Mathf.RoundToInt(transform.position.x / PADDING);
        int y = Mathf.RoundToInt(transform.position.y / PADDING);
        pos = new Vector2Int(x, y);
        transform.position = new Vector3(x * PADDING, y * PADDING, 1f);
    }
}
public enum GridType
{
    InGrid,
    OutOfGrid
}
public enum GridDirection
{
    Left,
    Right,
    Top,
    Down
}