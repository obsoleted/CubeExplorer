using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System;
using Assets.Cube_Loader.Extensions;
using CubeServer;
using CubeServerTest;
using Microsoft.Xna.Framework;
using Vector3 = UnityEngine.Vector3;

public class CubeQuery
{
    public int MinimumViewport { get; private set; }
    public int MaximumViewport { get; private set; }
    public string CubeTemplate { get; private set; }
    public string MtlTemplate { get; private set; }
    public string JpgTemplate { get; private set; }
    public string MetadataTemplate { get; private set; }
    public int TextureSubdivide { get; private set; }
    public string TexturePath { get; private set; }

    public Dictionary<int, VLevelQuery> VLevels { get; set; }

    private readonly string indexUrl;

    private readonly MonoBehaviour behavior;

    private readonly OctTree<CubeBounds> octTree;

    public CubeQuery(string sceneIndexUrl, MonoBehaviour behaviour)
    {
        indexUrl = sceneIndexUrl;
        this.behavior = behaviour;
        octTree = new OctTree<CubeBounds>();
    }

    public IEnumerator Load()
    {
        Debug.Log("CubeQuery started against: " + indexUrl);
        WWW loader = WWWExtensions.CreateWWW(path: indexUrl);
        yield return loader;

        var index = JSON.Parse(loader.GetDecompressedText());

        MinimumViewport = index["MinimumViewport"].AsInt;
        MaximumViewport = index["MaximumViewport"].AsInt;
        CubeTemplate = index["CubeTemplate"].Value;
        MtlTemplate = index["MtlTemplate"].Value;
        JpgTemplate = index["JpgTemplate"].Value;
        MetadataTemplate = index["MetadataTemplate"].Value;
        TextureSubdivide = index["TextureSubdivide"].AsInt;
        TexturePath = index["TexturePath"].Value;

        int prevX = 0;
        int prevZ = 0;

        // Populate Viewports
        VLevels = new Dictionary<int, VLevelQuery>();
        for (int i = MinimumViewport; i <= MaximumViewport; i++)
        {
            string path = MetadataTemplate.Replace("{v}", i.ToString());
            var vlevel = new VLevelQuery(i, path, prevX, prevZ, octTree);
            
            yield return behavior.StartCoroutine(vlevel.Load());
            VLevels.Add(i, vlevel);
            prevX = vlevel.CubeMap.GetLength(0);
            prevZ = vlevel.CubeMap.GetLength(2);
        }

        octTree.UpdateTree();
        Debug.Log(octTree);
    }

}

// Describes a viewport level metadata set. 
// Yes, I've madeup 'viewport level' or 'vlevel' as the term for the 
// level of accuracy you might need in a given viewport.
public class VLevelQuery
{
    public int ViewportLevel { get; private set; }
    public bool[, ,] CubeMap { get; private set; }

    private readonly string metadataUrl;

    public Vector3 MinExtent { get; private set; }
    public Vector3 MaxExtent { get; private set; }
    public Vector3 Size { get; private set; }

    private int prevX, prevZ;
    private readonly OctTree<CubeBounds> octTree;
    private readonly OctTree<CubeBounds> globalOctTree; 

    public VLevelQuery(int viewportLevel, string viewportMetadataUrl, int prevX, int prevZ, OctTree<CubeBounds> globalOctTree )
    {
        ViewportLevel = viewportLevel;
        metadataUrl = viewportMetadataUrl;
        this.prevX = prevX;
        this.prevZ = prevZ;
        this.octTree = new OctTree<CubeBounds>();
        this.globalOctTree = globalOctTree;
    }

    public IEnumerator Load()
    {
        WWW loader = WWWExtensions.CreateWWW(path: metadataUrl);
        yield return loader;

        // POPULATE THE BOOL ARRAY...
        var metadata = JSON.Parse(loader.GetDecompressedText());
        if (metadata != null)
        {
            int xMax = metadata["GridSize"]["X"].AsInt;
            int yMax = metadata["GridSize"]["Y"].AsInt;
            int zMax = metadata["GridSize"]["Z"].AsInt;

            int xyMult = 1;
            int zMult = 1;

            CubeMap = new bool[xMax, yMax, zMax];

            if (prevX != 0)
            {
                xyMult = prevX/xMax;
            }
            if (prevZ != 0)
            {
                zMult = prevZ/zMax;
            }

            var cubeExists = metadata["CubeExists"];
            for (int x = 0; x < xMax; x++)
            {
                for (int y = 0; y < yMax; y++)
                {
                    for (int z = 0; z < zMax; z++)
                    {
                        if ((CubeMap[x, y, z] = cubeExists[x][y][z].AsBool))
                        {
                            octTree.Add(new CubeBounds()
                            {
                                BoundingBox =
                                    new BoundingBox(new Microsoft.Xna.Framework.Vector3(x, y, z),
                                        new Microsoft.Xna.Framework.Vector3(x + xyMult, y + xyMult, z + zMult))
                            });
                            globalOctTree.Add(new CubeBounds()
                            {
                                BoundingBox =
                                    new BoundingBox(new Microsoft.Xna.Framework.Vector3(x, y, z),
                                        new Microsoft.Xna.Framework.Vector3(x + xyMult, y + xyMult, z + zMult))
                            });
                        }
                    }
                }
            }

            var extents = metadata["Extents"];
            MinExtent = new Vector3(extents["XMin"].AsFloat, extents["YMin"].AsFloat, extents["ZMin"].AsFloat);
            MaxExtent = new Vector3(extents["XMax"].AsFloat, extents["YMax"].AsFloat, extents["ZMax"].AsFloat);
            Size = new Vector3(extents["XSize"].AsFloat, extents["YSize"].AsFloat, extents["ZSize"].AsFloat);

            Debug.Log("Viewport: " + ViewportLevel);
            Debug.LogFormat("Exists: {0} {1} {2}", CubeMap.GetLength(0), CubeMap.GetLength(1), CubeMap.GetLength(2));
            float xBlockSize = Size.x / CubeMap.GetLength(0);
            float yBlockSize = Size.y / CubeMap.GetLength(1);
            float zBlockSize = Size.z / CubeMap.GetLength(2);


            Debug.LogFormat("CubeSize: {0} {1} {2}", xBlockSize, yBlockSize, zBlockSize);
            Debug.LogFormat("Build OctTree {0} {1}", xyMult, zMult);
            octTree.UpdateTree();
            Debug.LogFormat("Dump OctTree {0} {1}", xyMult, zMult);
            // OctTreeUtilities.Dump(octTree);
            Debug.LogFormat("Done Dump OctTree {0} {1}", xyMult, zMult);

        }
    }
}