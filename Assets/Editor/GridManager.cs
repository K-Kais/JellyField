using UnityEngine;
using System.Linq;
using UnityEditor;
[CustomEditor(typeof(GridPos))]
public class GridManager : Editor
{
    public float PADDING = 1.1f;
    private GridPos[] gridPos;
    [ContextMenu("Nap to Grid All")]
    private void OnEnable()
    {
        gridPos = FindObjectsOfType<GridPos>();
    }
    private void OnSceneGUI()
    {
        if (gridPos == null)
            return;
        gridPos.ToList().ForEach(x => x.SnapToGrid(PADDING));
    }
}