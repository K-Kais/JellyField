using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JellyMeshFilter", menuName = "JellyMeshFilter")]
public class JellyMeshFilter : SerializedScriptableObject
{
    [DictionaryDrawerSettings(KeyLabel = "Mesh Type", ValueLabel = "Mesh Filter")]
    public Dictionary<MeshType, MeshFilter> meshDictionary = new Dictionary<MeshType, MeshFilter>();
}