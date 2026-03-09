using System.Collections.Generic;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MaykerStudio.Tools
{
    public class Tightmesh_NavMesh : MonoBehaviour
    {
        [Range(1, 10)]
        public int BlockSize = 4;
        
        [Range(1, 5)]
        public int MaxDistanceFromPlatform = 2;

        [Range(1, 5)]
        public int MaxDistanceFromLedges = 2;

        [EditorButton(nameof(ScanTiles))]
        [SerializeField]
        private Tilemap tileMap;

        private NavMeshSurface navMesh;

        void ScanTiles()
        {
            if (!navMesh)
                navMesh = GetComponent<NavMeshSurface>();

            navMesh.BuildNavMesh();

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            NavMeshModifier mod = quad.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = 1;

            List<GameObject> quads = new List<GameObject>();

            for (int n = tileMap.cellBounds.xMax; n > tileMap.cellBounds.xMin; n--)
            {
                for (int p = tileMap.cellBounds.yMax; p > tileMap.cellBounds.yMin; p--)
                {
                    Vector3Int localPlace = (new Vector3Int(n, p, (int)tileMap.transform.position.y));

                    if (tileMap.HasTile(localPlace) && !tileMap.HasTile(localPlace + Vector3Int.down))
                    {
                        localPlace += Vector3Int.down;

                        int i = BlockSize;

                        while (!tileMap.HasTile(localPlace + Vector3Int.right) &&
                            !tileMap.HasTile(localPlace + Vector3Int.left) &&
                            !tileMap.HasTile(localPlace + Vector3Int.down * MaxDistanceFromPlatform) &&
                            !CheckIfLedge(tileMap, localPlace) &&
                            i > 0)
                        {
                            Vector3 center = tileMap.GetCellCenterWorld(localPlace);

                            quads.Add(Instantiate(quad, center, Quaternion.identity));

                            localPlace += Vector3Int.down;

                            i--;
                        }
                    }
                }
            }

            DestroyImmediate(quad);

            navMesh.BuildNavMesh();

            foreach (var q in quads)
            {
                DestroyImmediate(q);
            }

            quads.Clear();
        }

        private bool CheckIfLedge(Tilemap tileMap, Vector3Int pos)
        {
            pos += Vector3Int.down * MaxDistanceFromPlatform;

            if (!tileMap.HasTile(pos))
            {
                if (MaxDistanceFromLedges > 1)
                {
                    for (int i = 1; i <= MaxDistanceFromLedges; i++)
                    {
                        if (tileMap.HasTile(pos + Vector3Int.right * i) && !tileMap.HasTile(pos + Vector3Int.right * i + Vector3Int.down))
                            return true;
                        else if (tileMap.HasTile(pos + Vector3Int.left * i) && !tileMap.HasTile(pos + Vector3Int.left * i + Vector3Int.down))
                            return true;
                    }
                }
                else if (tileMap.HasTile(pos + Vector3Int.right))
                {
                    //TOOD: Maybe some processing to connect platforms based on edges
                    return true;
                }
                else if (tileMap.HasTile(pos + Vector3Int.left))
                {
                    //TOOD: Maybe some processing to connect platforms based on edges
                    return true;
                }
            }

            return false;
        }
    }

}
/// <summary>
/// Use this to make the an optimized mesh for platform 2D that will prevent most of the Wrong paths from happening.
/// </summary>
