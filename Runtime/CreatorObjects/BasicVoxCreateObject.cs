using UnityEngine;
using System.Collections.Generic;
using Miventech.NativeVoxReader.Data;
using Miventech.NativeVoxReader.Abstract;

namespace Miventech.NativeVoxReader.CreatorObjects
{
    /// <summary>
    /// Basic implementation of VoxMeshBuilderAbstract.
    /// </summary>
    public class BasicVoxCreateObject : VoxCreateObjectAbstract
    {
        public override void BuildObject(VoxModel model, Color32[] palette)
        {
            GameObject ChildObject = new GameObject("VoxModel");
            ChildObject.transform.SetParent(this.transform);
            ChildObject.transform.localPosition = (Vector3)model.position;
            ChildObject.transform.localRotation = Quaternion.identity;
            ChildObject.transform.localScale = Vector3.one;

            MeshFilter meshFilter = ChildObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = ChildObject.AddComponent<MeshRenderer>();
            
            // Local color palette asset
            Texture2D paletteTexture = GeneratePaletteTexture(palette);
            
            // Use a standard or unlit shader that supports textures.
            Material mat = new Material(Shader.Find("Standard")); 
            mat.mainTexture = paletteTexture;
            // Point filter for voxel/pixel art look
            mat.mainTexture.filterMode = FilterMode.Point; 
            
            meshRenderer.material = mat;

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            GenerateGreedyMesh(model, palette, vertices, triangles, uvs);

            // Re-center mesh local position
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] -= new Vector3(model.size.x * 0.5f, model.size.z * 0.5f, model.size.y * 0.5f);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            meshFilter.mesh = mesh;
        }

        private Texture2D GeneratePaletteTexture(Color32[] palette)
        {
            Texture2D tex = new Texture2D(256, 1, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            
            for (int i = 0; i < 256; i++)
            {
                if (i < palette.Length)
                    tex.SetPixel(i, 0, palette[i]);
                else
                    tex.SetPixel(i, 0, Color.black);
            }
            tex.Apply();
            return tex;
        }

        private void GenerateGreedyMesh(VoxModel model, Color32[] palette, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            // Init volume buffer
            Vector3Int size = model.size;
            // int for colorIndex (1-255). 0 = empty.
            int[,,] volume = new int[size.x, size.y, size.z];
            
            foreach (var v in model.voxels)
            {
                // Index safety check
                if(v.x < size.x && v.y < size.y && v.z < size.z)
                    volume[v.x, v.y, v.z] = v.colorIndex;
            }

            // 2. Iterate over 3 dimensions (axes)
            // d=0 -> X (Width plane YZ)
            // d=1 -> Y (Depth plane XZ)
            // d=2 -> Z (Height plane XY)
            for (int d = 0; d < 3; d++)
            {
                int u = (d + 1) % 3; // U-axis of the slicing plane
                int v = (d + 2) % 3; // V-axis of the slicing plane

                int[] x = new int[3]; // 3D position cursor
                int[] q = new int[3]; // Swipe direction cursor on axis d
                q[d] = 1;

                // Two directions per axis: -1 (back or negative face) and +1 (front or positive face)
                for (int faceDir = -1; faceDir <= 1; faceDir += 2)
                {
                    // 2D mask for the current slice. Stores the color index.
                    int[] mask = new int[size[u] * size[v]];

                    // Sweep through the volume in direction d
                    for (x[d] = 0; x[d] < size[d]; x[d]++)
                    {
                        int n = 0;
                        // Fill mask for this slice x[d]
                        for (x[v] = 0; x[v] < size[v]; x[v]++)
                        {
                            for (x[u] = 0; x[u] < size[u]; x[u]++)
                            {
                                // Get current color
                                int cCurrent = volume[x[0], x[1], x[2]];
                                
                                // Get neighbor color in the face direction
                                int cNeighbor = 0;
                                int nx = x[0] + (d == 0 ? faceDir : 0);
                                int ny = x[1] + (d == 1 ? faceDir : 0);
                                int nz = x[2] + (d == 2 ? faceDir : 0);

                                if (nx >= 0 && nx < size.x && 
                                    ny >= 0 && ny < size.y && 
                                    nz >= 0 && nz < size.z)
                                {
                                    cNeighbor = volume[nx, ny, nz];
                                }

                                // Face visibility check
                                bool visible = (cCurrent != 0 && cNeighbor == 0);
                                mask[n++] = visible ? cCurrent : 0;
                            }
                        }

                        // Greedy Meshing algorithm on the mask[]
                        n = 0;
                        for (int j = 0; j < size[v]; j++)
                        {
                            for (int i = 0; i < size[u]; i++)
                            {
                                int c = mask[n];
                                if (c != 0)
                                {
                                    // Found start of a quad. Calculate width.
                                    int width = 1;
                                    while (i + width < size[u] && mask[n + width] == c)
                                    {
                                        width++;
                                    }

                                    // Calculate height.
                                    int height = 1;
                                    bool done = false;
                                    while (j + height < size[v])
                                    {
                                        // Check if the next row has a segment of the same width and color
                                        for (int k = 0; k < width; k++)
                                        {
                                            if (mask[n + k + height * size[u]] != c)
                                            {
                                                done = true;
                                                break;
                                            }
                                        }
                                        if (done) break;
                                        height++;
                                    }

                                    // Add Quad
                                    int[] pos = new int[3];
                                    pos[u] = i; 
                                    pos[v] = j; 
                                    pos[d] = x[d];

                                    // Adjust depth based on face direction
                                    int depthOffset = (faceDir == 1) ? 1 : 0;
                                    pos[d] += depthOffset;

                                    // Calculate UV from palette coord
                                    int colorIndex = c - 1;
                                    if (colorIndex < 0) colorIndex = 0;
                                    if (colorIndex > 255) colorIndex = 255;
                                    
                                    float uCoord = (colorIndex + 0.5f) / 256.0f;
                                    Vector2 uv = new Vector2(uCoord, 0.5f);

                                    AddGreedyQuad(pos, u, v, d, width, height, faceDir, uv, vertices, triangles, uvs);

                                    // Clear mask in the used area
                                    for (int ly = 0; ly < height; ly++)
                                    {
                                        for (int lx = 0; lx < width; lx++)
                                        {
                                            mask[n + lx + ly * size[u]] = 0;
                                        }
                                    }

                                    // Skip processed elements
                                    i += width - 1;
                                    n += width - 1;
                                }
                                n++;
                            }
                        }
                    }
                }
            }
        }

        private void AddGreedyQuad(int[] pos, int axisU, int axisV, int axisD, int width, int height, int faceDir, Vector2 uv, 
                                   List<Vector3> verts, List<int> tris, List<Vector2> uvs)
        {
            // Build the 4 vertices of the quad in VOX coordinates
            // Quad extends in planes U (width) and V (height), fixed in D
            
            // v0: 0,0
            int[] p0 = new int[]{ pos[0], pos[1], pos[2] };
            // v1: w,0
            int[] p1 = new int[]{ pos[0], pos[1], pos[2] };
            p1[axisU] += width;
            // v2: 0,h
            int[] p2 = new int[]{ pos[0], pos[1], pos[2] };
            p2[axisV] += height;
            // v3: w,h
            int[] p3 = new int[]{ pos[0], pos[1], pos[2] };
            p3[axisU] += width;
            p3[axisV] += height;

            // Convert to Unity Coordinates
            // Vox(x,y,z) -> Unity(x,z,y)
            Vector3 v0 = new Vector3(p0[0], p0[2], p0[1]);
            Vector3 v1 = new Vector3(p1[0], p1[2], p1[1]);
            Vector3 v2 = new Vector3(p2[0], p2[2], p2[1]);
            Vector3 v3 = new Vector3(p3[0], p3[2], p3[1]);

            // Add vertices
            int baseIndex = verts.Count;
            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);
            verts.Add(v3);

            // Add UVs (all same for a flat solid color mapping to palette)
            uvs.Add(uv);
            uvs.Add(uv);
            uvs.Add(uv);
            uvs.Add(uv);

            // Winding order depends on the face direction
            if (faceDir == 1)
            {
                // Positive normal
                tris.Add(baseIndex);     // 0
                tris.Add(baseIndex + 2); // 2
                tris.Add(baseIndex + 1); // 1
                
                tris.Add(baseIndex + 1); // 1
                tris.Add(baseIndex + 2); // 2
                tris.Add(baseIndex + 3); // 3
            }
            else
            {
                // Negative normal
                tris.Add(baseIndex);     // 0
                tris.Add(baseIndex + 1); // 1
                tris.Add(baseIndex + 2); // 2

                tris.Add(baseIndex + 1); // 1
                tris.Add(baseIndex + 3); // 3
                tris.Add(baseIndex + 2); // 2
            }
        }
        
        // Remove unused method
        /* private void AddCubeOptimized... */
        
        private void AddFace(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color32 color, List<Vector3> verts, List<int> tris, List<Color32> cols)
        {
            int baseIndex = verts.Count;

            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);
            verts.Add(v3);

            cols.Add(color);
            cols.Add(color);
            cols.Add(color);
            cols.Add(color);

            // First triangle
            tris.Add(baseIndex);
            tris.Add(baseIndex + 1);
            tris.Add(baseIndex + 2);

            // Second triangle
            tris.Add(baseIndex);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex + 3);
        }
        
    }
}

