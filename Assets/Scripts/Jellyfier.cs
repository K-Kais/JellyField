using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Jellyfier : SerializedMonoBehaviour
{
    //A value that describes how fast our jelly object will be bouncing
    [SerializeField] private float bounceSpeed;
    [SerializeField] private float fallForce;

    //We need this value to eventually stop bouncing back and forth.
    [SerializeField] private float stiffness;

    //We need our Meshfilter to get a hold of the mesh;
    public JellyState jellyState;
    public JellyType jellyType;
    public List<MeshType> meshTypes;
    [SerializeField] private JellyMeshFilter jellyMeshFilter;
    [SerializeField] private GridCell gridCell;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;


    [DictionaryDrawerSettings(KeyLabel = "Cell Type", ValueLabel = "Jelly Cell")]
    public Dictionary<JellyCellType, JellyCell> jellyCellDic;


    //We need to keep track of our vertices. 
    //This means not only the current stat of them but also there original position and so forth;
    Vector3[] initialVertices;
    Vector3[] currentVertices;

    Vector3[] vertexVelocities;
    private void Awake()
    {
        AssignRandomColors();
    }

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        SetMeshFilter();
        SetGridCell();
        //fallForce = Random.Range(25, 80);
        RecalculateMesh();
    }

    private void Update()
    {
        UpdateVertices();
    }
    private void AssignRandomColors()
    {
        List<JellyCell> cells = jellyCellDic.Values.ToList();
        List<JellyColor> colors = new List<JellyColor>(System.Enum.GetValues(typeof(JellyColor)).Cast<JellyColor>());

        bool isConditionMet = false;
        while (!isConditionMet)
        {
            foreach (var cell in cells)
            {
                cell.color = colors[Random.Range(0, colors.Count)];
            }

            var distinctColors = cells.Select(cell => cell.color).Distinct().ToList();
            if (distinctColors.Count == 1) isConditionMet = true;
            else if (distinctColors.Count == 3) isConditionMet = true;
            else if (distinctColors.Count == 4) isConditionMet = true;
            else if (distinctColors.Count == 2)
            {
                int color1Count = cells.Count(cell => cell.color == distinctColors[0]);
                int color2Count = cells.Count(cell => cell.color == distinctColors[1]);
                if ((color1Count == 2 && color2Count == 2))
                {
                    if (cells[0].color != cells[3].color && cells[1].color != cells[2].color)
                    {
                        isConditionMet = true;
                    }
                }
                else if (color1Count == 4 || color2Count == 4) isConditionMet = true;
            }
        }
    }
    private void SetGridCell() => gridCell = transform.GetComponentInParent<GridCell>();
    private void SetMeshFilter()
    {
        var firstCell = jellyCellDic[JellyCellType.TopLeft];
        bool allSameColor = jellyCellDic.Skip(1).All(cell => cell.Value.color == firstCell.color);
        var newMesh = new Mesh();

        if (allSameColor)
        {
            newMesh = jellyMeshFilter.meshDictionary[MeshType.Base].mesh;
            meshFilter.mesh = newMesh;
            mesh = newMesh;
            RecalculateMesh();

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", firstCell.GetColor());
            meshRenderer.SetPropertyBlock(propertyBlock);
            jellyType = JellyType.Base;
            return;
        }
        var meshFilters = new List<MeshFilter>();
        var colors = new List<Color>();
        meshTypes = new List<MeshType>();
        foreach (var cell in jellyCellDic)
        {
            var meshType = cell.Value.GetMeshCell();
            if (meshTypes.Contains(meshType)) continue;

            meshFilters.Add(jellyMeshFilter.meshDictionary[meshType]);
            colors.Add(cell.Value.GetColor());
            meshTypes.Add(meshType);
        }
        CombineInstance[] combine = new CombineInstance[meshFilters.Count];
        for (int i = 0; i < meshFilters.Count; i++)
        {
            combine[i].mesh = meshFilters[i].mesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }
        newMesh.CombineMeshes(combine, false);
        meshFilter.mesh = newMesh;
        mesh = newMesh;
        RecalculateMesh();

        meshRenderer.materials = colors.Select(color =>
        {
            Material mat = new Material(meshRenderer.material);
            mat.color = color;
            return mat;
        }).ToArray();
        jellyType = (JellyType)meshFilters.Count;
    }

    private void RecalculateMesh()
    {
        //Getting our vertices (initial and their current state(which is initial since we havent done anything yet, duh))
        initialVertices = mesh.vertices;

        //Obviously we are never changing the actual count of vertices so these two Arrays will always have the same length
        currentVertices = new Vector3[initialVertices.Length];
        vertexVelocities = new Vector3[initialVertices.Length];
        for (int i = 0; i < initialVertices.Length; i++)
        {
            currentVertices[i] = initialVertices[i];
        }
        mesh.vertices = currentVertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
    }

    private void UpdateVertices()
    {
        //We are looping through every vertice  update them depending on their velocity.
        for (int i = 0; i < currentVertices.Length; i++)
        {

            //Before we add the current velocity to the vertice we need to make sure that
            //we consider the fact that our object is a jelly and should be able to bounce back 
            //to do so we first calculate the displacement value. 
            //Since we saved the initial form of the mesh we can use this to revert back to the inital 
            //position over time
            Vector3 currentDisplacement = currentVertices[i] - initialVertices[i];
            vertexVelocities[i] -= currentDisplacement * bounceSpeed * Time.deltaTime;

            //In order for us to be able to stop bouncing at one point we need to reduce
            //the velocity over time. 
            vertexVelocities[i] *= 1f - stiffness * Time.deltaTime;
            currentVertices[i] += vertexVelocities[i] * Time.deltaTime;

        }

        //We then need to set our mesh.vertices to the current vertices 
        //in order to be able to see a change.
        mesh.vertices = currentVertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

    }

    public void OnCollisionEnter(Collision other)
    {
        ContactPoint[] collisonPoints = other.contacts;
        for (int i = 0; i < collisonPoints.Length; i++)
        {
            Vector3 inputPoint = collisonPoints[i].point + (collisonPoints[i].point * .1f);
            ApplyPressureToPoint(inputPoint, fallForce);
        }
    }

    public void ApplyPressureToPoint(Vector3 _point, float _pressure)
    {
        //We need to loop through every single vertice and apply the pressure to it.
        for (int i = 0; i < currentVertices.Length; i++)
        {
            ApplyPressureToVertex(i, _point, _pressure);
        }
    }

    public void ApplyPressureToVertex(int _index, Vector3 _position, float _pressure)
    {
        //In order to know how much pressure we need to apply to each vertice we need to 
        //calculate the distance between our vertice and the point where our extended finger (mouse thingey) 
        //touched our mesh
        Vector3 distanceVerticePoint = currentVertices[_index] - transform.InverseTransformPoint(_position);

        //now begins fun physiquee part.... we need to make use of the Inverse Square Law
        //we do this by dividing the pressure by the distance squared into an adapted pressure
        //TODO: CHANGE
        float adaptedPressure = _pressure / (1f + distanceVerticePoint.sqrMagnitude);

        //What is left to do now is to use this pressure to calculate a velocity for vertices.
        //TODO - First claculate acceleration with mass acceleration = force / mass and then get the
        //velocity by calculating acceleration * Time.deltaTime;
        float velocity = adaptedPressure * Time.deltaTime;
        //Our velocity now still needs a direction, we can calculate this using the
        //normalized distance vertex point from earlier
        vertexVelocities[_index] += distanceVerticePoint.normalized * velocity;
    }
    public void Combine()
    {
        SetGridCell();
        var jellyToCombine = new List<JellyCell>();
        var jellyGrid = new List<JellyCell>();
        JellyCombineGrid jellyCombineGrid = new JellyCombineGrid();

        foreach (var keyValuePair in gridCell.neighbors)
        {
            var neighbor = keyValuePair.Value;
            if (neighbor.transform.childCount == 0) continue;

            var direction = keyValuePair.Key;
            var jellyfierNeighbor = neighbor.transform.GetComponentInChildren<Jellyfier>();
            if (direction == GridDirection.Left)
            {
                JellyDestroyAll(jellyfierNeighbor);
                jellyGrid = new List<JellyCell>();
                jellyGrid.AddRange(jellyfierNeighbor.jellyCellDic.Values);
                jellyGrid.AddRange(jellyCellDic.Values);
                jellyCombineGrid.InitializeLeftRight(jellyGrid);
                var results = jellyCombineGrid.GetResults();
                if (results != null)
                {
                    jellyToCombine.AddRange(results);
                    JellyToDestroys(results);
                    jellyfierNeighbor.SetMeshFilter();
                    SetMeshFilter();
                }
            }
            else if (direction == GridDirection.Right)
            {
                JellyDestroyAll(jellyfierNeighbor);
                jellyGrid = new List<JellyCell>();
                jellyGrid.AddRange(jellyCellDic.Values);
                jellyGrid.AddRange(jellyfierNeighbor.jellyCellDic.Values);
                jellyCombineGrid.InitializeLeftRight(jellyGrid);
                var results = jellyCombineGrid.GetResults();
                if (results != null)
                {
                    jellyToCombine.AddRange(results);
                    JellyToDestroys(results);
                    jellyfierNeighbor.SetMeshFilter();
                    SetMeshFilter();
                }
            }
            else if (direction == GridDirection.Top)
            {
                JellyDestroyAll(jellyfierNeighbor);
                jellyGrid = new List<JellyCell>();
                jellyGrid.AddRange(jellyfierNeighbor.jellyCellDic.Values);
                jellyGrid.AddRange(jellyCellDic.Values);
                jellyCombineGrid.InitializeTopDown(jellyGrid);
                var results = jellyCombineGrid.GetResults();
                if (results != null)
                {
                    jellyToCombine.AddRange(results);
                    JellyToDestroys(results);
                    jellyfierNeighbor.SetMeshFilter();
                    SetMeshFilter();
                }
            }
            else if (direction == GridDirection.Down)
            {
                JellyDestroyAll(jellyfierNeighbor);
                jellyGrid = new List<JellyCell>();
                jellyGrid.AddRange(jellyCellDic.Values);
                jellyGrid.AddRange(jellyfierNeighbor.jellyCellDic.Values);

                jellyCombineGrid.InitializeTopDown(jellyGrid);
                var results = jellyCombineGrid.GetResults();
                if (results != null)
                {
                    jellyToCombine.AddRange(results);
                    JellyToDestroys(results);
                    jellyfierNeighbor.SetMeshFilter();
                    SetMeshFilter();
                }
            }
        }
    }
    private void JellyDestroyAll(Jellyfier neighbor)
    {
        var jellyfiers = new List<JellyCell>();
        jellyfiers.AddRange(neighbor.jellyCellDic.Values);
        jellyfiers.AddRange(jellyCellDic.Values);

        if (jellyfiers.Select(cell => cell.color).Distinct().Count() == 1)
        {
            Destroy(neighbor.gameObject);
            Destroy(gameObject);
        }
    }
    private void JellyToDestroys(List<JellyCell> jellyCells)
    {
        Dictionary<GameObject, List<JellyCell>> jellyCellDic = new Dictionary<GameObject, List<JellyCell>>();
        foreach (var jellyCell in jellyCells)
        {
            var parent = jellyCell.transform.parent.gameObject;
            if (!jellyCellDic.ContainsKey(parent))
            {
                jellyCellDic.Add(parent, new List<JellyCell> { jellyCell });
            }
            else jellyCellDic[parent].Add(jellyCell);
        }

        var jellyToDestroys = new List<GameObject>();
        foreach (var keyValuePair in jellyCellDic)
        {
            var gameObject = keyValuePair.Key;
            var jellyCellList = keyValuePair.Value;
            if (jellyCellList.Count == 4) { jellyToDestroys.Add(gameObject); continue; }

            var jellyCellFirst = jellyCellList[0];
            var jellyfier = gameObject.GetComponent<Jellyfier>();
            var cells = new List<JellyCell>();
            if (jellyCellList.Count == 1)
            {
                foreach (var cell in jellyfier.jellyCellDic)
                {
                    if (cell.Key != jellyCellFirst.type) cells.Add(cell.Value);
                }
                foreach (var cell in cells)
                {
                    int count = cells.Count(jelly => jelly.color == cell.color);
                    if (count == 1 && !jellyCellFirst.IsDiagonal(cell.type))
                    { jellyCellFirst.color = cell.color; break; }
                }
            }
            else if (jellyCellList.Count == 2)
            {
                var jellyCellSecond = jellyCellList[1];
                foreach (var cell in jellyfier.jellyCellDic)
                {
                    if (cell.Key != jellyCellFirst.type && cell.Key != jellyCellSecond.type)
                    {
                        cells.Add(cell.Value);
                    }
                }
                foreach (var cell in cells)
                {
                    int count = cells.Count(jelly => jelly.color == cell.color);
                    if (count == 1)
                    {
                        if (!jellyCellFirst.IsDiagonal(cell.type)) jellyCellFirst.color = cell.color;
                        else if (!jellyCellSecond.IsDiagonal(cell.type)) jellyCellSecond.color = cell.color;
                        continue;
                    }
                    else if (count == 2)
                    {
                        jellyCellFirst.color = cell.color;
                        jellyCellSecond.color = cell.color;
                        break;
                    }
                }
            }
        }
        foreach (var jelly in jellyToDestroys) Destroy(jelly);
    }
    public void SnapToGrid()
    {
        if (GridManager.Instance != null)
            GridManager.Instance.SnapToGrid(transform);
    }
}
public enum JellyState
{
    InGrid,
    OutOfGrid,
    InHand,
}
public enum JellyType
{
    Base,
    TwoCells = 2,
    ThreeCells = 3,
    FourCells = 4,
}
public enum MeshType
{
    Base,
    TopLeft,
    TopRight,
    TopLeftRight,
    DownLeft,
    DownRight,
    DownLeftRight,
    LeftTopDown,
    RightTopDown,
}