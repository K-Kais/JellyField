using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class GridManager : SerializedMonoBehaviour
{
    public static GridManager Instance;
    private GridCell[] gridPos;
    [DictionaryDrawerSettings(KeyLabel = "Pos", ValueLabel = "Grid")]
    public Dictionary<Vector2Int, GridCell> gridCellDic = new Dictionary<Vector2Int, GridCell>();
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        gridPos = FindObjectsOfType<GridCell>();
        foreach (var grid in gridPos)
        {
            if (grid.gridType == GridType.InGrid)
            {
                gridCellDic.TryAdd(grid.pos, grid);
            }
        }
    }
    public void SnapToGrid(Transform transform)
    {
        {
            Transform nearestGridPos = null;
            float nearestDistance = Mathf.Infinity;
            foreach (var grid in gridPos)
            {
                float distance = Vector2.Distance(transform.position, grid.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestGridPos = grid.transform;
                }
            }
            if (nearestGridPos != null)
            {
                transform.parent = nearestGridPos;
                transform.position = nearestGridPos.position;
                transform.position -= Vector3.forward;
            }
        }
    }
}

