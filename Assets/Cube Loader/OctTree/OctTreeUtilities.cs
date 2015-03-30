namespace CubeServerTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using CubeServer;
    using UnityEngine;

    public class OctTreeUtilities
    {

        private class MyPair 
        {
            public MyPair(OctTree<CubeBounds> oc, int it2)
            {
                Item1 = oc;
                Item2 = it2;
            }

            public OctTree<CubeBounds> Item1 { get; set; }
            public int Item2 { get; set; }
        }

        public static void Dump(OctTree<CubeBounds> octTree)
        {
            Queue<MyPair> enumeration = new Queue<MyPair>();
            enumeration.Enqueue(new MyPair(octTree, 0));

            while (enumeration.Count > 0)
            {
                MyPair next = enumeration.Dequeue();
                OctTree<CubeBounds> nextOctTree = next.Item1;
                int indent = next.Item2;

                Trace.IndentLevel = indent;
                Trace.WriteLine(nextOctTree.ToString());
                UnityEngine.Debug.Log(nextOctTree.ToString());

                foreach (CubeBounds obj in nextOctTree.Objects)
                {
                    Trace.WriteLine(" " + obj.ToString());
                    UnityEngine.Debug.Log(" " + obj.ToString());
                }

                if (nextOctTree.HasChildren)
                {
                    byte active = nextOctTree.OctantMask;
                    for (int bit = 0; bit < 8; bit++)
                    {
                        if (((active >> bit) & 0x01) == 0x01)
                        {
                            OctTree<CubeBounds> childNode = nextOctTree.Octant[bit];
                            if (childNode != null)
                            {
                                enumeration.Enqueue(new MyPair(childNode, indent + 1));
                            }
                        }
                    }
                }
            }
        }
    }
}