using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newTileSet", menuName = "ScriptableObjects/TileSet", order = 1)]
public class TileSet : ScriptableObject
{
    [SerializeField]
    public List<TileAttributes> tiles;

    private void Awake()
    {
        //if (tiles is null)
        //{
        //    tiles = new List<TileAttributes>();
        //    tiles.Add(new TileAttributes()); //white space tile must have id 0
        //}
        //else if (tiles.Count > 0 && tiles[0].mesh is not null)
        //{
        //    tiles.Insert(0, new TileAttributes()); //white space tile must have id 0
        //}
    }

    public Mesh GetMesh(int id)
    {
        return tiles[id].mesh;
    }

    public Material GetMaterial(int id)
    {
        return tiles[id].material;
    }

}
