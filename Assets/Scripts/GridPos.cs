using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
[ExecuteInEditMode]
public class GridPos : MonoBehaviour
{
    public GridType gridType;
    public Vector2Int pos;
    public List<GridPos> gridNeighbors;
    private void Start()
    {
        gridNeighbors = new List<GridPos>();
        var directions = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        for (int i = 0; i < directions.Length; i++)
        {
            if (GridManager.Instance.gridDic.TryGetValue(pos + directions[i] * 2, out GridPos gridPos))
            {
                gridNeighbors.Add(gridPos);
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