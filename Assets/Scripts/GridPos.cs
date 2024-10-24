using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
[ExecuteInEditMode]
public class GridPos : MonoBehaviour
{
    public Vector2Int pos;
    public int[] data;

    public void SnapToGrid(float PADDING)
    {
        int x = Mathf.RoundToInt(transform.position.x / PADDING);
        int y = Mathf.RoundToInt(transform.position.y / PADDING);
        pos = new Vector2Int(x, y);
        transform.position = new Vector3(x * PADDING, y * PADDING, 1);
    }
}