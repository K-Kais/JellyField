using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

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
                cell.jellyColor = colors[UnityEngine.Random.Range(0, colors.Count)];
            }

            var distinctColors = cells.Select(cell => cell.jellyColor).Distinct().ToList();
            if (distinctColors.Count == 1) isConditionMet = true;
            else if (distinctColors.Count == 3) isConditionMet = true;
            else if (distinctColors.Count == 4) isConditionMet = true;
            else if (distinctColors.Count == 2)
            {
                int color1Count = cells.Count(cell => cell.jellyColor == distinctColors[0]);
                int color2Count = cells.Count(cell => cell.jellyColor == distinctColors[1]);
                if ((color1Count == 2 && color2Count == 2))
                {
                    if (cells[0].jellyColor != cells[3].jellyColor && cells[1].jellyColor != cells[2].jellyColor)
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
        bool allSameColor = jellyCellDic.Skip(1).All(cell => cell.Value.jellyColor == firstCell.jellyColor);
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
    public void Combine1()
    {
        SetGridCell();
        var jellyToDestroy = new List<GameObject>();
        foreach (var keyValuePair in gridCell.neighbors)
        {
            var neighbor = keyValuePair.Value;
            if (neighbor.transform.childCount == 0) continue;

            var neighborDirection = keyValuePair.Key;
            var jellyfierNeighbor = neighbor.transform.GetComponentInChildren<Jellyfier>();
            if (jellyfierNeighbor == null) return;

            if (jellyType == JellyType.Base)
            {
                var baseColor = jellyCellDic.First().Value.jellyColor;
                if (jellyfierNeighbor.jellyType == JellyType.Base)
                {
                    var neighborColor = jellyfierNeighbor.jellyCellDic.First().Value.jellyColor;
                    if (baseColor != neighborColor) continue;
                    else
                    {
                        jellyToDestroy.Add(jellyfierNeighbor.gameObject);
                        if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                    }
                }
                else if (jellyfierNeighbor.jellyType == JellyType.TwoCells)
                {
                    if (jellyfierNeighbor.meshTypes.Contains(MeshType.LeftTopDown))
                    {
                        if (neighborDirection == GridDirection.Left)
                        {
                            if (jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopRight, out JellyCell TopRight)
                                && TopRight.jellyColor != baseColor)
                            {
                                continue;
                            }
                            else
                            {
                                TopRight.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.TopLeft].jellyColor;
                                jellyfierNeighbor.jellyCellDic[JellyCellType.DownRight].jellyColor = TopRight.jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                        else if (neighborDirection == GridDirection.Right)
                        {
                            if (jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopLeft, out JellyCell TopLeft)
                                && TopLeft.jellyColor != baseColor)
                            {
                                continue;
                            }
                            else
                            {
                                TopLeft.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.TopRight].jellyColor;
                                jellyfierNeighbor.jellyCellDic[JellyCellType.DownLeft].jellyColor = TopLeft.jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                        else if (neighborDirection == GridDirection.Top)
                        {
                            jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownLeft, out JellyCell DownLeft);
                            jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownRight, out JellyCell DownRight);
                            if (DownLeft.jellyColor != baseColor
                                && DownRight.jellyColor != baseColor)
                            {
                                continue;
                            }
                            else
                            {
                                if (DownLeft.jellyColor == baseColor)
                                {
                                    DownLeft.jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.jellyCellDic[JellyCellType.TopLeft].jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else if (DownRight.jellyColor == baseColor)
                                {
                                    DownRight.jellyColor = DownLeft.jellyColor;
                                    jellyfierNeighbor.jellyCellDic[JellyCellType.TopRight].jellyColor = DownLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                        }
                        else if (neighborDirection == GridDirection.Down)
                        {
                            jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopLeft, out JellyCell TopLeft);
                            jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopRight, out JellyCell TopRight);
                            if (TopLeft.jellyColor != baseColor
                                && TopRight.jellyColor != baseColor)
                            {
                                continue;
                            }
                            else
                            {
                                if (TopLeft.jellyColor == baseColor)
                                {
                                    TopLeft.jellyColor = TopRight.jellyColor;
                                    jellyfierNeighbor.jellyCellDic[JellyCellType.DownLeft].jellyColor = TopLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else if (TopRight.jellyColor == baseColor)
                                {
                                    TopRight.jellyColor = TopLeft.jellyColor;
                                    jellyfierNeighbor.jellyCellDic[JellyCellType.DownRight].jellyColor = TopRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                        }
                    }
                    else if (jellyfierNeighbor.meshTypes.Contains(MeshType.TopLeftRight))
                    {
                        if (neighborDirection == GridDirection.Left)
                        {
                            jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopRight, out JellyCell TopRight);
                            jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownRight, out JellyCell DownRight);
                            if (TopRight.jellyColor != baseColor
                                && DownRight.jellyColor != baseColor)
                            {
                                continue;
                            }
                            else
                            {
                                if (TopRight.jellyColor == baseColor)
                                {
                                    TopRight.jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.jellyCellDic[JellyCellType.TopLeft].jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else if (DownRight.jellyColor == baseColor)
                                {
                                    DownRight.jellyColor = TopRight.jellyColor;
                                    jellyfierNeighbor.jellyCellDic[JellyCellType.DownLeft].jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                        }
                        else if (neighborDirection == GridDirection.Right)
                        {
                            jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopLeft, out JellyCell TopLeft);
                            jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownLeft, out JellyCell DownLeft);
                            if (TopLeft.jellyColor != baseColor
                                && DownLeft.jellyColor != baseColor)
                            {
                                continue;
                            }
                            else
                            {
                                if (TopLeft.jellyColor == baseColor)
                                {
                                    TopLeft.jellyColor = DownLeft.jellyColor;
                                    jellyfierNeighbor.jellyCellDic[JellyCellType.TopRight].jellyColor = TopLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else if (DownLeft.jellyColor == baseColor)
                                {
                                    DownLeft.jellyColor = TopLeft.jellyColor;
                                    jellyfierNeighbor.jellyCellDic[JellyCellType.DownRight].jellyColor = DownLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                        }
                        else if (neighborDirection == GridDirection.Top)
                        {
                            if (jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownLeft, out JellyCell DownLeft)
                                && DownLeft.jellyColor != baseColor)
                            {
                                continue;
                            }
                            else
                            {
                                DownLeft.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.TopLeft].jellyColor;
                                jellyfierNeighbor.jellyCellDic[JellyCellType.DownRight].jellyColor = DownLeft.jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                        else if (neighborDirection == GridDirection.Down)
                        {
                            if (jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopLeft, out JellyCell TopLeft)
                                && TopLeft.jellyColor != baseColor)
                            {
                                continue;
                            }
                            else
                            {
                                TopLeft.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.DownLeft].jellyColor;
                                jellyfierNeighbor.jellyCellDic[JellyCellType.TopRight].jellyColor = TopLeft.jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                    }
                }
                else if (jellyfierNeighbor.jellyType == JellyType.ThreeCells)
                {
                    if (neighborDirection == GridDirection.Left)
                    {
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopRight, out JellyCell TopRight);
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownRight, out JellyCell DownRight);
                        if (TopRight.jellyColor == DownRight.jellyColor)
                        {
                            if (TopRight.jellyColor != baseColor) continue;
                            else
                            {
                                TopRight.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.TopLeft].jellyColor;
                                DownRight.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.DownLeft].jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                        else if (TopRight.jellyColor != baseColor
                            && DownRight.jellyColor != baseColor)
                        {
                            continue;
                        }
                        else
                        {
                            var TopLeft = jellyfierNeighbor.jellyCellDic[JellyCellType.TopLeft];
                            var DownLeft = jellyfierNeighbor.jellyCellDic[JellyCellType.DownLeft];
                            if (TopLeft.jellyColor == DownLeft.jellyColor)
                            {
                                if (TopRight.jellyColor == baseColor)
                                {
                                    TopRight.jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else if (DownRight.jellyColor == baseColor)
                                {
                                    DownRight.jellyColor = TopRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                            else if (TopRight.jellyColor == baseColor)
                            {
                                if (TopLeft.jellyColor == TopRight.jellyColor)
                                {
                                    TopRight.jellyColor = DownRight.jellyColor;
                                    TopLeft.jellyColor = DownLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else
                                {
                                    TopRight.jellyColor = TopLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                            else if (DownRight.jellyColor == baseColor)
                            {
                                if (DownLeft.jellyColor == DownRight.jellyColor)
                                {
                                    DownRight.jellyColor = TopRight.jellyColor;
                                    DownLeft.jellyColor = TopLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else
                                {
                                    DownRight.jellyColor = DownLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                        }
                    }
                    else if (neighborDirection == GridDirection.Right)
                    {
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopLeft, out JellyCell TopLeft);
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownLeft, out JellyCell DownLeft);
                        if (TopLeft.jellyColor == DownLeft.jellyColor)
                        {
                            if (TopLeft.jellyColor != baseColor) continue;
                            else
                            {
                                TopLeft.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.TopRight].jellyColor;
                                DownLeft.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.DownRight].jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                        else if (TopLeft.jellyColor != baseColor
                            && DownLeft.jellyColor != baseColor)
                        {
                            continue;
                        }
                        else
                        {
                            var TopRight = jellyfierNeighbor.jellyCellDic[JellyCellType.TopRight];
                            var DownRight = jellyfierNeighbor.jellyCellDic[JellyCellType.DownRight];
                            if (TopRight.jellyColor == DownRight.jellyColor)
                            {
                                if (TopLeft.jellyColor == baseColor)
                                {
                                    TopLeft.jellyColor = DownLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else if (DownLeft.jellyColor == baseColor)
                                {
                                    DownLeft.jellyColor = TopLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                            else if (TopLeft.jellyColor == baseColor)
                            {
                                if (TopRight.jellyColor == TopLeft.jellyColor)
                                {
                                    TopLeft.jellyColor = DownLeft.jellyColor;
                                    TopRight.jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else
                                {
                                    TopLeft.jellyColor = TopRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                            else if (DownLeft.jellyColor == baseColor)
                            {
                                if (DownRight.jellyColor == DownLeft.jellyColor)
                                {
                                    DownLeft.jellyColor = TopLeft.jellyColor;
                                    DownRight.jellyColor = TopRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else
                                {
                                    DownLeft.jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                        }
                    }
                    else if (neighborDirection == GridDirection.Top)
                    {
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownRight, out JellyCell DownRight);
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownLeft, out JellyCell DownLeft);
                        if (DownRight.jellyColor == DownLeft.jellyColor)
                        {
                            if (DownRight.jellyColor != baseColor) continue;
                            else
                            {
                                DownRight.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.TopRight].jellyColor;
                                DownLeft.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.TopLeft].jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                        else if (DownRight.jellyColor != baseColor
                            && DownLeft.jellyColor != baseColor)
                        {
                            continue;
                        }
                        else
                        {
                            var TopRight = jellyfierNeighbor.jellyCellDic[JellyCellType.TopRight];
                            var TopLeft = jellyfierNeighbor.jellyCellDic[JellyCellType.TopLeft];
                            if (TopRight.jellyColor == TopLeft.jellyColor)
                            {
                                if (DownRight.jellyColor == baseColor)
                                {
                                    DownRight.jellyColor = DownLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else if (DownLeft.jellyColor == baseColor)
                                {
                                    DownLeft.jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                            else if (DownLeft.jellyColor == baseColor)
                            {
                                if (DownLeft.jellyColor == TopLeft.jellyColor)
                                {
                                    TopLeft.jellyColor = TopRight.jellyColor;
                                    DownLeft.jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else
                                {
                                    DownLeft.jellyColor = TopLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                            else if (DownRight.jellyColor == baseColor)
                            {
                                if (DownRight.jellyColor == TopRight.jellyColor)
                                {
                                    TopRight.jellyColor = TopLeft.jellyColor;
                                    DownRight.jellyColor = DownLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else
                                {
                                    DownRight.jellyColor = TopRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                        }
                    }
                    else if (neighborDirection == GridDirection.Down)
                    {
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopRight, out JellyCell TopRight);
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopLeft, out JellyCell TopLeft);
                        if (TopRight.jellyColor == TopLeft.jellyColor)
                        {
                            if (TopRight.jellyColor != baseColor) continue;
                            else
                            {
                                TopRight.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.DownRight].jellyColor;
                                TopLeft.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.DownLeft].jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                        else if (TopRight.jellyColor != baseColor
                            && TopLeft.jellyColor != baseColor)
                        {
                            continue;
                        }
                        else
                        {
                            var DownRight = jellyfierNeighbor.jellyCellDic[JellyCellType.DownRight];
                            var DownLeft = jellyfierNeighbor.jellyCellDic[JellyCellType.DownLeft];
                            if (DownRight.jellyColor == DownLeft.jellyColor)
                            {
                                if (TopRight.jellyColor == baseColor)
                                {
                                    TopRight.jellyColor = TopLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else if (TopLeft.jellyColor == baseColor)
                                {
                                    TopLeft.jellyColor = TopRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                            else if (TopLeft.jellyColor == baseColor)
                            {
                                if (TopLeft.jellyColor == DownLeft.jellyColor)
                                {
                                    DownLeft.jellyColor = DownRight.jellyColor;
                                    TopLeft.jellyColor = TopRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else
                                {
                                    TopLeft.jellyColor = DownLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                            else if (TopRight.jellyColor == baseColor)
                            {
                                if (TopRight.jellyColor == DownRight.jellyColor)
                                {
                                    DownRight.jellyColor = DownLeft.jellyColor;
                                    TopRight.jellyColor = TopLeft.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                                else
                                {
                                    TopRight.jellyColor = DownRight.jellyColor;
                                    jellyfierNeighbor.SetMeshFilter();
                                    if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                                }
                            }
                        }
                    }
                }
                else if (jellyfierNeighbor.jellyType == JellyType.FourCells)
                {
                    if (neighborDirection == GridDirection.Left)
                    {
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopRight, out JellyCell TopRight);
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownRight, out JellyCell DownRight);
                        if (TopRight.jellyColor != baseColor
                            && DownRight.jellyColor != baseColor)
                        {
                            continue;
                        }
                        else
                        {
                            if (TopRight.jellyColor == baseColor)
                            {
                                TopRight.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.TopLeft].jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                            else if (DownRight.jellyColor == baseColor)
                            {
                                DownRight.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.DownLeft].jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                    }
                    else if (neighborDirection == GridDirection.Right)
                    {
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopLeft, out JellyCell TopLeft);
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownLeft, out JellyCell DownLeft);
                        if (TopLeft.jellyColor != baseColor
                            && DownLeft.jellyColor != baseColor)
                        {
                            continue;
                        }
                        else
                        {
                            if (TopLeft.jellyColor == baseColor)
                            {
                                TopLeft.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.TopRight].jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                            else if (DownLeft.jellyColor == baseColor)
                            {
                                DownLeft.jellyColor = jellyfierNeighbor.jellyCellDic[JellyCellType.DownRight].jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                    }
                    else if (neighborDirection == GridDirection.Top)
                    {
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownLeft, out JellyCell DownLeft);
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.DownRight, out JellyCell DownRight);
                        if (DownLeft.jellyColor != baseColor
                            && DownRight.jellyColor != baseColor)
                        {
                            continue;
                        }
                        else
                        {
                            if (DownLeft.jellyColor == baseColor)
                            {
                                DownLeft.jellyColor = DownRight.jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                            else if (DownRight.jellyColor == baseColor)
                            {
                                DownRight.jellyColor = DownLeft.jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                    }
                    else if (neighborDirection == GridDirection.Down)
                    {
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopLeft, out JellyCell TopLeft);
                        jellyfierNeighbor.jellyCellDic.TryGetValue(JellyCellType.TopRight, out JellyCell TopRight);
                        if (TopLeft.jellyColor != baseColor
                            && TopRight.jellyColor != baseColor)
                        {
                            continue;
                        }
                        else
                        {
                            if (TopLeft.jellyColor == baseColor)
                            {
                                TopLeft.jellyColor = TopRight.jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                            else if (TopRight.jellyColor == baseColor)
                            {
                                TopRight.jellyColor = TopLeft.jellyColor;
                                jellyfierNeighbor.SetMeshFilter();
                                if (!jellyToDestroy.Contains(gameObject)) jellyToDestroy.Add(gameObject);
                            }
                        }
                    }
                }
            }
            else if (jellyType == JellyType.TwoCells)
            {
                if (jellyfierNeighbor.jellyType == JellyType.Base)
                {
                    var baseColor = jellyfierNeighbor.jellyCellDic.First().Value.jellyColor;
                    if (meshTypes.Contains(MeshType.LeftTopDown))
                    {
                        if (neighborDirection == GridDirection.Left)
                        {
                            if (jellyCellDic[JellyCellType.TopLeft].jellyColor != baseColor) continue;
                            else
                            {
                                jellyToDestroy.Add(jellyfierNeighbor.gameObject);
                                jellyCellDic[JellyCellType.TopLeft].jellyColor = jellyCellDic[JellyCellType.TopRight].jellyColor;
                                jellyCellDic[JellyCellType.DownLeft].jellyColor = jellyCellDic[JellyCellType.DownRight].jellyColor;
                                SetMeshFilter();
                            }
                        }
                        else if (neighborDirection == GridDirection.Right)
                        {
                            if (jellyCellDic[JellyCellType.TopRight].jellyColor != baseColor) continue;
                            else
                            {
                                jellyToDestroy.Add(jellyfierNeighbor.gameObject);
                                jellyCellDic[JellyCellType.TopRight].jellyColor = jellyCellDic[JellyCellType.TopLeft].jellyColor;
                                jellyCellDic[JellyCellType.DownRight].jellyColor = jellyCellDic[JellyCellType.DownLeft].jellyColor;
                                SetMeshFilter();
                            }
                        }
                        else if (neighborDirection == GridDirection.Top || neighborDirection == GridDirection.Down)
                        {
                            if (jellyCellDic[JellyCellType.TopRight].jellyColor != baseColor
                                && jellyCellDic[JellyCellType.TopLeft].jellyColor != baseColor) continue;

                            else if (jellyCellDic[JellyCellType.TopLeft].jellyColor == baseColor)
                            {
                                jellyCellDic[JellyCellType.TopLeft].jellyColor = jellyCellDic[JellyCellType.TopRight].jellyColor;
                                jellyCellDic[JellyCellType.DownLeft].jellyColor = jellyCellDic[JellyCellType.DownRight].jellyColor;
                            }
                            else if (jellyCellDic[JellyCellType.TopRight].jellyColor == baseColor)
                            {
                                jellyCellDic[JellyCellType.TopRight].jellyColor = jellyCellDic[JellyCellType.TopLeft].jellyColor;
                                jellyCellDic[JellyCellType.DownRight].jellyColor = jellyCellDic[JellyCellType.DownLeft].jellyColor;
                            }
                            SetMeshFilter();
                            jellyToDestroy.Add(jellyfierNeighbor.gameObject);
                        }
                    }
                    else if (meshTypes.Contains(MeshType.TopLeftRight))
                    {
                        if (neighborDirection == GridDirection.Top)
                        {
                            if (jellyCellDic[JellyCellType.TopLeft].jellyColor != baseColor) continue;
                            else
                            {
                                jellyToDestroy.Add(jellyfierNeighbor.gameObject);
                                jellyCellDic[JellyCellType.TopLeft].jellyColor = jellyCellDic[JellyCellType.DownLeft].jellyColor;
                                jellyCellDic[JellyCellType.TopRight].jellyColor = jellyCellDic[JellyCellType.DownRight].jellyColor;
                                SetMeshFilter();
                            }
                        }
                        else if (neighborDirection == GridDirection.Down)
                        {
                            if (jellyCellDic[JellyCellType.DownLeft].jellyColor != baseColor) continue;
                            else
                            {
                                jellyToDestroy.Add(jellyfierNeighbor.gameObject);
                                jellyCellDic[JellyCellType.DownLeft].jellyColor = jellyCellDic[JellyCellType.TopLeft].jellyColor;
                                jellyCellDic[JellyCellType.DownRight].jellyColor = jellyCellDic[JellyCellType.TopRight].jellyColor;
                                SetMeshFilter();
                            }
                        }
                        else if (neighborDirection == GridDirection.Left || neighborDirection == GridDirection.Right)
                        {
                            if (jellyCellDic[JellyCellType.TopLeft].jellyColor != baseColor
                                && jellyCellDic[JellyCellType.DownLeft].jellyColor != baseColor) continue;

                            else if (jellyCellDic[JellyCellType.TopLeft].jellyColor == baseColor)
                            {
                                jellyCellDic[JellyCellType.TopLeft].jellyColor = jellyCellDic[JellyCellType.DownLeft].jellyColor;
                                jellyCellDic[JellyCellType.TopRight].jellyColor = jellyCellDic[JellyCellType.DownRight].jellyColor;
                            }
                            else if (jellyCellDic[JellyCellType.DownLeft].jellyColor == baseColor)
                            {
                                jellyCellDic[JellyCellType.DownLeft].jellyColor = jellyCellDic[JellyCellType.TopLeft].jellyColor;
                                jellyCellDic[JellyCellType.DownRight].jellyColor = jellyCellDic[JellyCellType.TopRight].jellyColor;
                            }
                            SetMeshFilter();
                            jellyToDestroy.Add(jellyfierNeighbor.gameObject);
                        }
                    }
                }
                else if (jellyfierNeighbor.jellyType == JellyType.TwoCells)
                {
                    if (meshTypes.Contains(MeshType.LeftTopDown) && jellyfierNeighbor.meshTypes.Contains(MeshType.LeftTopDown))
                    {

                    }
                }
            }
        }
        foreach (var jelly in jellyToDestroy) if (jelly != gameObject) Destroy(jelly);
        if (jellyToDestroy.Contains(gameObject)) Destroy(gameObject);
    }
    public void Combine()
    {
        SetGridCell();
        var jellyToCombine = new Dictionary<GridCell, JellyCell>();
        var jellyGrid = new List<JellyCell>();

        foreach (var keyValuePair in gridCell.neighbors)
        {
            var neighbor = keyValuePair.Value;
            if (neighbor.transform.childCount == 0) continue;

            var direction = keyValuePair.Key;
            var jellyfierNeighbor = neighbor.transform.GetComponentInChildren<Jellyfier>();
            if (direction == GridDirection.Left)
            {
                jellyGrid = new List<JellyCell>();
                jellyGrid.AddRange(jellyfierNeighbor.jellyCellDic.Values);
                jellyGrid.AddRange(jellyCellDic.Values);
                JellyCombineGrid jellyCombineGrid = new JellyCombineGrid();
                jellyCombineGrid.InitializeLeftRight(jellyGrid);

                var leftResults = jellyCombineGrid.GetResults();
                if (leftResults != null)
                {
                    foreach (var jelly in leftResults)
                    {
                        Debug.Log(jelly.jellyColor);
                    }
                }
                continue;
            }
            else if (direction == GridDirection.Right)
            {
                jellyGrid = new List<JellyCell>();
                jellyGrid.AddRange(jellyCellDic.Values);
                jellyGrid.AddRange(jellyfierNeighbor.jellyCellDic.Values);
                JellyCombineGrid jellyCombineGrid = new JellyCombineGrid();
                jellyCombineGrid.InitializeLeftRight(jellyGrid);

                var leftResults = jellyCombineGrid.GetResults();
                if (leftResults != null)
                {
                    foreach (var jelly in leftResults)
                    {
                        Debug.Log(jelly.jellyColor);
                    }
                }
                continue;
            }
            else if (direction == GridDirection.Top)
            {
                jellyGrid = new List<JellyCell>();
                jellyGrid.AddRange(jellyfierNeighbor.jellyCellDic.Values);
                jellyGrid.AddRange(jellyCellDic.Values);
                JellyCombineGrid jellyCombineGrid = new JellyCombineGrid();
                jellyCombineGrid.InitializeTopDown(jellyGrid);

                var leftResults = jellyCombineGrid.GetResults();
                if (leftResults != null)
                {
                    foreach (var jelly in leftResults)
                    {
                        Debug.Log(jelly.jellyColor);
                    }
                }
                continue;
            }
            else if (direction == GridDirection.Down)
            {
                jellyGrid = new List<JellyCell>();
                jellyGrid.AddRange(jellyCellDic.Values);
                jellyGrid.AddRange(jellyfierNeighbor.jellyCellDic.Values);
                JellyCombineGrid jellyCombineGrid = new JellyCombineGrid();
                jellyCombineGrid.InitializeTopDown(jellyGrid);

                var leftResults = jellyCombineGrid.GetResults();
                if (leftResults != null)
                {
                    foreach (var jelly in leftResults)
                    {
                        Debug.Log(jelly.jellyColor);
                    }
                }
                continue;
            }
        }




        //foreach (var keyValuePair in gridCell.neighbors)
        //{
        //    var neighbor = keyValuePair.Value;
        //    if (neighbor.transform.childCount == 0) continue;

        //    var jellyfierNeighbor = neighbor.transform.GetComponentInChildren<Jellyfier>();
        //    foreach (var jelly in jellyfierNeighbor.jellyCellDic)
        //    {
        //        jellyToCombine.Add(neighbor, jelly.Value);
        //    }
        //}

        //var distinctColors = jellyCellDic.Values.Select(cell => cell.jellyColor).Distinct().ToList();
        //var jellyColorDirections = new Dictionary<GridDirection, JellyCell>();
        //foreach (var jelly in jellyToCombine)
        //{
        //    distinctColors.Select(color =>
        //    {
        //        if (jelly.Value.jellyColor == color)
        //        {
        //            var direction = jelly.Key.neighbors.First().Key;
        //            jellyColorDirections.Add(direction, jelly.Value);
        //        }
        //        return color;
        //    });
        //}

        //var jellyNexToColors = new List<JellyCell>();
        //foreach (var jelly in jellyColorDirections)
        //{
        //    if (jellyType == JellyType.Base)
        //    {
        //        if (jelly.Key == GridDirection.Left)
        //        {
        //            jellyColorDirections.TryGetValue(GridDirection.Left, out JellyCell leftJelly);
        //        }
        //    }
        //}

        //var jellyToDestroys = new List<GameObject>();
        //foreach (var jelly in jellyNexToColors)
        //{

        //}
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

public class JellyCombineGrid
{
    private JellyCell[,] grid = new JellyCell[2, 4];
    private List<JellyCell> results;

    public void InitializeLeftRight(List<JellyCell> jellyCells)
    {
        grid[0, 0] = jellyCells[0]; grid[0, 2] = jellyCells[4];
        grid[0, 1] = jellyCells[1]; grid[0, 3] = jellyCells[5];
        grid[1, 0] = jellyCells[2]; grid[1, 2] = jellyCells[6];
        grid[1, 1] = jellyCells[3]; grid[1, 3] = jellyCells[7];
    }
    public void InitializeTopDown(List<JellyCell> jellyCells)
    {
        grid[0, 0] = jellyCells[1]; grid[0, 2] = jellyCells[5];
        grid[0, 1] = jellyCells[3]; grid[0, 3] = jellyCells[7];
        grid[1, 0] = jellyCells[0]; grid[1, 2] = jellyCells[4];
        grid[1, 1] = jellyCells[2]; grid[1, 3] = jellyCells[6];
    }
    public List<JellyCell> GetResults()
    {
        if (!NextTo()) return null;

        results.AddRange(grid);
        if (IsUniformColor(results)) return results;

        var checks = new List<Func<bool>> { Square, LTopLeft, LTopRight, LDownLeft, LDownRight, HorizontalTop, HorizontalDown, NextTo };
        foreach (var check in checks) if (check()) return results;
        return null;
    }
    private bool IsUniformColor(List<JellyCell> cells) => cells.Select(cell => cell.jellyColor).Distinct().Count() == 1;
    private bool Square()
    {
        results = new List<JellyCell>
        {
           grid[0, 0], grid[0, 1], grid[0, 2], grid[0, 3],
           grid[1, 0], grid[1, 1]
        };
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
           grid[0, 0], grid[0, 1], 
           grid[1, 0], grid[1, 1], grid[1, 2], grid[1, 3],
        };
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
           grid[0, 0],grid[0, 1], grid[0, 2], grid[0, 3],
                                  grid[1, 2], grid[1, 3]
        };
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
                                   grid[0, 2], grid[0, 3],
            grid[1, 0],grid[1, 1], grid[1, 2], grid[1, 3]
        };
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
           grid[0, 0], grid[0, 1], grid[0, 2],
           grid[1, 0], grid[1, 1], grid[1, 2]
        };
        if (IsUniformColor(results)) return true;

        var temp = results[5];
        results.RemoveAt(5);
        if (IsUniformColor(results)) return true;

        results.Add(temp);
        results.RemoveAt(2);
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
          grid[0, 1], grid[0, 2], grid[0, 3],
          grid[1, 1], grid[1, 2], grid[1, 3]
        };
        if (IsUniformColor(results)) return true;

        temp = results[3];
        results.RemoveAt(3);
        if (IsUniformColor(results)) return true;

        results.Add(temp);
        results.RemoveAt(0);
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
            grid[0, 1], grid[0, 2],
            grid[1, 1], grid[1, 2]
        };
        if (IsUniformColor(results)) return true;
        return false;
    }

    private bool NextTo()
    {
        results = new List<JellyCell>
        {
            grid[0, 1], grid[0, 2],
        };
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
            grid[1, 1], grid[1, 2]
        };
        if (IsUniformColor(results)) return true;

        results.Clear();
        return false;
    }

    private bool HorizontalTop()
    {
        results = new List<JellyCell>
        {
            grid[0, 0], grid[0, 1], grid[0, 2], grid[0, 3]
        };
        if (IsUniformColor(results)) return true;

        var temp = results[3];
        results.RemoveAt(3);
        if (IsUniformColor(results)) return true;

        results.Add(temp);
        results.RemoveAt(0);
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool HorizontalDown()
    {
        results = new List<JellyCell>
        {
            grid[1, 0], grid[1, 1], grid[1, 2], grid[1, 3]
        };
        if (IsUniformColor(results)) return true;

        var temp = results[3];
        results.RemoveAt(3);
        if (IsUniformColor(results)) return true;

        results.Add(temp);
        results.RemoveAt(0);
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool LTopLeft()
    {
        results = new List<JellyCell>
        {
            grid[0, 1], grid[0, 2], grid[0, 3],
            grid[1, 1],
        };
        if (IsUniformColor(results)) return true;

        results.RemoveAt(2);
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool LTopRight()
    {
        results = new List<JellyCell>
        {
            grid[0, 0],grid[0, 1], grid[0, 2],
                                   grid[1, 2],
        };
        if (IsUniformColor(results)) return true;

        results.RemoveAt(0);
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool LDownLeft()
    {
        results = new List<JellyCell>
        {
            grid[0, 1],
            grid[1, 1], grid[1, 2], grid[1, 3]
        };
        if (IsUniformColor(results)) return true;

        results.RemoveAt(3);
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool LDownRight()
    {
        results = new List<JellyCell>
        {
                                   grid[0, 2],
            grid[1, 0],grid[1, 1], grid[1, 2],
        };
        if (IsUniformColor(results)) return true;

        results.RemoveAt(1);
        if (IsUniformColor(results)) return true;
        return false;
    }
}
