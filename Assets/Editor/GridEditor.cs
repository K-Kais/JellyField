using UnityEngine;
using System.Linq;
using UnityEditor;
[CustomEditor(typeof(GridCell))]
public class GridEditor : Editor
{
    public float PADDING = 1.1f;
    private GridCell[] gridPos;
    private Jellyfier[] jellyfiers;
    private void OnEnable()
    {
        gridPos = FindObjectsOfType<GridCell>();
        jellyfiers = FindObjectsOfType<Jellyfier>();
    }
    private void OnSceneGUI()
    {
        if (gridPos == null)
            return;
        gridPos.ToList().ForEach(x => x.SnapToGrid(PADDING));
        jellyfiers.ToList().ForEach(x => x.SnapToGrid());
    }
}

[CustomEditor(typeof(Jellyfier))]
public class GridJellyfierEditor : Editor
{
    private Jellyfier[] jellyfiers;
    private void OnEnable()
    {
        jellyfiers = FindObjectsOfType<Jellyfier>();
    }
    private void OnSceneGUI()
    {
        if (jellyfiers == null)
            return;
        jellyfiers.ToList().ForEach(x => x.SnapToGrid());
    }
}